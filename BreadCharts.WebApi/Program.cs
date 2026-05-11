using System.Security.Claims;
using System.Text;
using BreadCharts.Core.Infrastructure;
using BreadCharts.Core.Models;
using BreadCharts.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Services
builder.Services.AddOpenApi();

// DB and Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

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

var app = builder.Build();

// 2. Middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

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
    // In a real scenario, you might want to redirect back to the app with these in the fragment or a secure way
    // For now, we return JSON. The Avalonia app can capture this if it uses a web view or we can use a redirect with params.
    return Results.Ok(new
    {
        AppToken = jwt,
        SpotifyAccessToken = accessToken,
        SpotifyRefreshToken = refreshToken,
        ExpiresIn = result.Properties?.GetTokenValue("expires_at"),
        User = new { user.Id, user.DisplayName, user.Email }
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
