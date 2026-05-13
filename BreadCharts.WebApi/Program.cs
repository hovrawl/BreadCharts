using System.Security.Claims;
using System.Text;
using BreadCharts.Core.Infrastructure;
using BreadCharts.Core.Models;
using BreadCharts.Core.Services;
using BreadCharts.WebApi;
using BreadCharts.WebApi.CompiledModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseKestrelHttpsConfiguration();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});

// 1. Configure Services
builder.Services.AddOpenApi();

// DB and Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
    options.UseModel(ApplicationDbContextModel.Instance);
});

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Voting Logic
builder.Services.AddScoped<IVotingService, VotingService>();
builder.Services.AddOptions<VotingOptions>()
    .Bind(builder.Configuration.GetSection("Voting"));

// Auth: Spotify + JWT
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "Spotify";
        options.DefaultSignInScheme = "External";
    })
    .AddCookie("External")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "SUPER_SECRET_KEY_PLEASE_CHANGE_IN_PRODUCTION"))
        };
    })
    .AddSpotify(options =>
    {
        options.ClientId = builder.Configuration["Spotify:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Spotify:ClientSecret"] ?? string.Empty;
        
        // Use configured RedirectUri if available, otherwise fallback to default CallbackPath behavior
        // The CallbackPath is where Spotify redirects the USER'S BROWSER back to OUR SERVER.
        // It must match one of the Redirect URIs registered in the Spotify Developer Dashboard.
        var configuredRedirectUri = builder.Configuration["Auth:RedirectUri"];
        if (!string.IsNullOrEmpty(configuredRedirectUri))
        {
            options.CallbackPath = new PathString(new Uri(configuredRedirectUri).AbsolutePath);
        }
        else
        {
            // Default callback path for the API, distinct from the Web app
            options.CallbackPath = "/auth/callback";
        }
        
        options.SaveTokens = true;

        options.Events.OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Failure, "Remote failure during authentication: {Message}", context.Failure?.Message);
            
            if (context.Failure is AuthenticationFailureException authFailure && context.Request.Query.ContainsKey("error"))
            {
                logger.LogError("Spotify error: {Error}, Description: {Description}", 
                    context.Request.Query["error"], 
                    context.Request.Query["error_description"]);
            }

            context.Response.Redirect("/auth/error?message=" + System.Net.WebUtility.UrlEncode(context.Failure?.Message ?? "Unknown error"));
            context.HandleResponse();
            return Task.CompletedTask;
        };
        
        var scopes = new List<string>
        {
            SpotifyAPI.Web.Scopes.UserReadEmail,
            SpotifyAPI.Web.Scopes.UserReadPrivate,
            SpotifyAPI.Web.Scopes.UserTopRead,
            SpotifyAPI.Web.Scopes.Streaming,
            SpotifyAPI.Web.Scopes.PlaylistModifyPublic,
            SpotifyAPI.Web.Scopes.PlaylistModifyPrivate
        };
        foreach (var s in scopes) options.Scope.Add(s);
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 2. Middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
    var dbPath = sqliteConnection.DataSource;

    // For SQLite, EnsureCreated fails in NativeAOT due to design-time model building.
    // We use a manual check and creation for AOT compatibility.
    // Only attempt manual creation if it looks like a local file path.
    if (!string.IsNullOrEmpty(dbPath) && dbPath != ":memory:")
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        
        db.Database.OpenConnection();
        try
        {
            var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""AspNetUsers"" (
                        ""Id"" TEXT NOT NULL PRIMARY KEY,
                        ""UserName"" TEXT NULL,
                        ""NormalizedUserName"" TEXT NULL,
                        ""Email"" TEXT NULL,
                        ""NormalizedEmail"" TEXT NULL,
                        ""EmailConfirmed"" INTEGER NOT NULL,
                        ""PasswordHash"" TEXT NULL,
                        ""SecurityStamp"" TEXT NULL,
                        ""ConcurrencyStamp"" TEXT NULL,
                        ""PhoneNumber"" TEXT NULL,
                        ""PhoneNumberConfirmed"" INTEGER NOT NULL,
                        ""TwoFactorEnabled"" INTEGER NOT NULL,
                        ""LockoutEnd"" TEXT NULL,
                        ""LockoutEnabled"" INTEGER NOT NULL,
                        ""AccessFailedCount"" INTEGER NOT NULL,
                        ""ThirdPartyId"" TEXT NULL,
                        ""AccessToken"" TEXT NULL,
                        ""RefreshToken"" TEXT NULL,
                        ""DisplayName"" TEXT NULL
                    );
                    CREATE TABLE IF NOT EXISTS ""AspNetUserLogins"" (
                        ""LoginProvider"" TEXT NOT NULL,
                        ""ProviderKey"" TEXT NOT NULL,
                        ""ProviderDisplayName"" TEXT NULL,
                        ""UserId"" TEXT NOT NULL,
                        PRIMARY KEY (""LoginProvider"", ""ProviderKey""),
                        CONSTRAINT ""FK_AspNetUserLogins_AspNetUsers_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_AspNetUserLogins_UserId"" ON ""AspNetUserLogins"" (""UserId"");
                    CREATE TABLE IF NOT EXISTS ""SubmittedSongs"" (
                        ""TrackId"" TEXT NOT NULL PRIMARY KEY,
                        ""TrackName"" TEXT NOT NULL,
                        ""SubmittedByUserId"" TEXT NOT NULL,
                        ""SubmittedAtUtc"" TEXT NOT NULL,
                        CONSTRAINT ""FK_SubmittedSongs_AspNetUsers_SubmittedByUserId"" FOREIGN KEY (""SubmittedByUserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
                    );
                    CREATE TABLE IF NOT EXISTS ""SongVotes"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""TrackId"" TEXT NOT NULL,
                        ""UserId"" TEXT NOT NULL,
                        ""VotedAtUtc"" TEXT NOT NULL,
                        CONSTRAINT ""FK_SongVotes_AspNetUsers_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
                        CONSTRAINT ""FK_SongVotes_SubmittedSongs_TrackId"" FOREIGN KEY (""TrackId"") REFERENCES ""SubmittedSongs"" (""TrackId"") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_SongVotes_TrackId"" ON ""SongVotes"" (""TrackId"");
                    CREATE INDEX IF NOT EXISTS ""IX_SongVotes_UserId"" ON ""SongVotes"" (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_SongVotes_TrackId_UserId"" ON ""SongVotes"" (""TrackId"", ""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_SubmittedSongs_SubmittedByUserId"" ON ""SubmittedSongs"" (""SubmittedByUserId"");
                ";
            command.ExecuteNonQuery();

            // Migrations: Add missing columns if they don't exist
            // SQLite doesn't support 'IF NOT EXISTS' for ADD COLUMN in older versions or some providers, 
            // and EF Core's Sqlite provider might not either in raw SQL. 
            // We'll check column existence manually for each potential missing column.

            var columns = new[] { ("AspNetUsers", "DisplayName") };
            foreach (var (table, column) in columns)
            {
                var checkCmd = db.Database.GetDbConnection().CreateCommand();
                checkCmd.CommandText = $"PRAGMA table_info(\"{table}\")";
                bool exists = false;
                using (var reader = checkCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.GetString(1).Equals(column, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                if (!exists)
                {
                    var alterCmd = db.Database.GetDbConnection().CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" TEXT NULL";
                    alterCmd.ExecuteNonQuery();
                }
            }
        }
        finally
        {
            db.Database.CloseConnection();
        }
    }
    else
    {
        // Fallback for in-memory or other cases, though EnsureCreated might still fail in AOT
        db.Database.EnsureCreated();
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

// 3. Auth Endpoints

// Redirect to Spotify
// The 'redirectUrl' here is the CLIENT (Avalonia) URL to return to AFTER the API has processed the tokens.
app.MapGet("/auth/spotify", (string? redirectUrl) =>
{
    var props = new AuthenticationProperties { RedirectUri = "/auth/finalize" };
    if (!string.IsNullOrEmpty(redirectUrl)) props.Items["redirectUrl"] = redirectUrl;
    return Results.Challenge(props, ["Spotify"]);
});

// Finalize OAuth and issue JWT + Spotify tokens
app.MapGet("/auth/finalize", async (
    HttpContext context,
    ApplicationDbContext dbContext,
    IConfiguration config) =>
{
    var result = await context.AuthenticateAsync("External");
    if (!result.Succeeded) return Results.Unauthorized();

    var principal = result.Principal;
    if (principal == null) return Results.Unauthorized();

    var spotifyId = principal.FindFirstValue("urn:spotify:id") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = principal.FindFirstValue(ClaimTypes.Email);
    var name = principal.FindFirstValue(ClaimTypes.Name);
    var accessToken = result.Properties?.GetTokenValue("access_token");
    var refreshToken = result.Properties?.GetTokenValue("refresh_token");

    if (string.IsNullOrEmpty(spotifyId)) return Results.BadRequest("Missing Spotify ID");

    var user = await IdentityQueries.FindUserByLoginQuery(dbContext, "Spotify", spotifyId);
    if (user == null)
    {
        var userName = email ?? spotifyId;
        var normalizedUserName = userName.ToUpperInvariant();
        
        // Manual check for username collision if necessary, or just rely on DB constraints
        if (await IdentityQueries.UserExistsByNormalizedUserNameQuery(dbContext, normalizedUserName))
        {
            // If collision, maybe append something or return error
            // For SSO with Spotify, spotifyId should be unique.
        }

        user = new ApplicationUser 
        { 
            Id = Guid.NewGuid().ToString(),
            UserName = userName, 
            NormalizedUserName = normalizedUserName,
            Email = email, 
            NormalizedEmail = email?.ToUpperInvariant(),
            EmailConfirmed = true,
            DisplayName = name ?? "", 
            ThirdPartyId = spotifyId,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        
        dbContext.Users.Add(user);
        dbContext.UserLogins.Add(new IdentityUserLogin<string>
        {
            LoginProvider = "Spotify",
            ProviderKey = spotifyId,
            ProviderDisplayName = "Spotify",
            UserId = user.Id
        });
        
        await dbContext.SaveChangesAsync();
    }
    else
    {
        // Update user properties directly
        bool changed = false;
        if (user.DisplayName != (name ?? ""))
        {
            user.DisplayName = name ?? "";
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    // Generate JWT for app authentication
    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var jwtKey = config["Jwt:Key"] ?? "SUPER_SECRET_KEY_PLEASE_CHANGE_IN_PRODUCTION";
    var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.DisplayName)
        ]),
        Expires = DateTime.UtcNow.AddDays(7),
        Issuer = config["Jwt:Issuer"],
        Audience = config["Jwt:Audience"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwt = tokenHandler.WriteToken(token);

    // Return all tokens to the app
    var redirectUrl = result.Properties?.Items["redirectUrl"];
    if (!string.IsNullOrEmpty(redirectUrl))
    {
        var builder = new UriBuilder(redirectUrl);
        var query = System.Web.HttpUtility.ParseQueryString(builder.Query);
        query["appToken"] = jwt;
        query["spotifyAccessToken"] = accessToken;
        query["spotifyRefreshToken"] = refreshToken;
        query["expiresIn"] = result.Properties?.GetTokenValue("expires_at");
        builder.Query = query.ToString();
        return Results.Redirect(builder.ToString());
    }

    return Results.Ok(new AuthResponse
    {
        AppToken = jwt,
        SpotifyAccessToken = accessToken,
        SpotifyRefreshToken = refreshToken,
        ExpiresIn = result.Properties?.GetTokenValue("expires_at"),
        User = new UserSummary { Id = user.Id, DisplayName = user.DisplayName, Email = user.Email }
    });
});

// 4. Voting Endpoints
var voting = app.MapGroup("/api/voting").RequireAuthorization();

voting.MapGet("/submissions", async (IVotingService svc, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Results.Ok(await svc.GetSubmissionsAsync(userId!));
});

voting.MapPost("/submit", async (IVotingService svc, ClaimsPrincipal user, [FromBody] SubmitRequest req) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var res = await svc.SubmitAsync(userId!, req.TrackId, req.TrackName);
    return res.ok ? Results.Ok(res.message) : Results.BadRequest(res.message);
});

voting.MapPost("/vote/{trackId}", async (IVotingService svc, ClaimsPrincipal user, string trackId) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var res = await svc.VoteAsync(userId!, trackId);
    return res.ok ? Results.Ok(res.message) : Results.BadRequest(res.message);
});

voting.MapDelete("/vote/{trackId}", async (IVotingService svc, ClaimsPrincipal user, string trackId) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var res = await svc.UnvoteAsync(userId!, trackId);
    return res.ok ? Results.Ok(res.message) : Results.BadRequest(res.message);
});

app.MapGet("/auth/error", (string? message) => Results.Problem(detail: message, title: "Authentication Error"));

app.Run();

public static class IdentityQueries
{
    public static readonly Func<ApplicationDbContext, string, string, Task<ApplicationUser?>> FindUserByLoginQuery =
        EF.CompileAsyncQuery((ApplicationDbContext db, string loginProvider, string providerKey) =>
            db.Users.FirstOrDefault(u => db.UserLogins.Any(l => 
                l.LoginProvider == loginProvider && l.ProviderKey == providerKey && l.UserId == u.Id)));

    public static readonly Func<ApplicationDbContext, string, Task<ApplicationUser?>> FindUserByIdQuery =
        EF.CompileAsyncQuery((ApplicationDbContext db, string id) =>
            db.Users.FirstOrDefault(u => u.Id == id));

    public static readonly Func<ApplicationDbContext, string, Task<bool>> UserExistsByNormalizedUserNameQuery =
        EF.CompileAsyncQuery((ApplicationDbContext db, string normalizedUserName) =>
            db.Users.Any(u => u.NormalizedUserName == normalizedUserName));
}

public record SubmitRequest(string TrackId, string TrackName);
