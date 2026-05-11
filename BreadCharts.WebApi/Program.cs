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
    })
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
        options.CallbackPath = "/auth/callback";
        options.SaveTokens = true;
        
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
        bool exists = File.Exists(dbPath);
        if (!exists)
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
                        ""RefreshToken"" TEXT NULL
                    );
                    CREATE TABLE IF NOT EXISTS ""SubmittedSongs"" (
                        ""Id"" TEXT NOT NULL PRIMARY KEY,
                        ""SpotifyId"" TEXT NOT NULL,
                        ""Title"" TEXT NOT NULL,
                        ""Artist"" TEXT NOT NULL,
                        ""Album"" TEXT NOT NULL,
                        ""ImageUrl"" TEXT NOT NULL,
                        ""SubmittedById"" TEXT NOT NULL,
                        ""SubmittedAt"" TEXT NOT NULL,
                        CONSTRAINT ""FK_SubmittedSongs_AspNetUsers_SubmittedById"" FOREIGN KEY (""SubmittedById"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
                    );
                    CREATE TABLE IF NOT EXISTS ""SongVotes"" (
                        ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        ""SongId"" TEXT NOT NULL,
                        ""UserId"" TEXT NOT NULL,
                        ""VotedAt"" TEXT NOT NULL,
                        CONSTRAINT ""FK_SongVotes_AspNetUsers_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
                        CONSTRAINT ""FK_SongVotes_SubmittedSongs_SongId"" FOREIGN KEY (""SongId"") REFERENCES ""SubmittedSongs"" (""Id"") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_SongVotes_SongId"" ON ""SongVotes"" (""SongId"");
                    CREATE INDEX IF NOT EXISTS ""IX_SongVotes_UserId"" ON ""SongVotes"" (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_SubmittedSongs_SubmittedById"" ON ""SubmittedSongs"" (""SubmittedById"");
                ";
                command.ExecuteNonQuery();
            }
            finally
            {
                db.Database.CloseConnection();
            }
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
app.MapGet("/auth/spotify", (string? redirectUrl) =>
{
    var props = new AuthenticationProperties { RedirectUri = "/auth/finalize" };
    if (!string.IsNullOrEmpty(redirectUrl)) props.Items["redirectUrl"] = redirectUrl;
    return Results.Challenge(props, ["Spotify"]);
});

// Finalize OAuth and issue JWT + Spotify tokens
app.MapGet("/auth/finalize", async (
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    IConfiguration config) =>
{
    var result = await context.AuthenticateAsync("Spotify");
    if (!result.Succeeded) return Results.Unauthorized();

    var principal = result.Principal;
    var spotifyId = principal.FindFirstValue("urn:spotify:id") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = principal.FindFirstValue(ClaimTypes.Email);
    var name = principal.FindFirstValue(ClaimTypes.Name);
    var accessToken = result.Properties?.GetTokenValue("access_token");
    var refreshToken = result.Properties?.GetTokenValue("refresh_token");

    if (string.IsNullOrEmpty(spotifyId)) return Results.BadRequest("Missing Spotify ID");

    var user = await userManager.FindByLoginAsync("Spotify", spotifyId);
    if (user == null)
    {
        user = new ApplicationUser 
        { 
            UserName = email ?? spotifyId, 
            Email = email, 
            DisplayName = name ?? "", 
            ThirdPartyId = spotifyId 
        };
        await userManager.CreateAsync(user);
        await userManager.AddLoginAsync(user, new UserLoginInfo("Spotify", spotifyId, "Spotify"));
    }
    else
    {
        user.DisplayName = name ?? user.DisplayName;
        await userManager.UpdateAsync(user);
    }

    // Generate JWT for app authentication
    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "SUPER_SECRET_KEY_PLEASE_CHANGE_IN_PRODUCTION");
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
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
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

app.Run();

public record SubmitRequest(string TrackId, string TrackName);
