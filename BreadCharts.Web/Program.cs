using BreadCharts.Core.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using BreadCharts.Web.Components;
using BreadCharts.Web.Components.Account;
using BreadCharts.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();
builder.Services.AddSingleton<ISpotifyClientService, SpotifyClientService>();
builder.Services.AddScoped<IChartService, ChartService>();
builder.Services.AddScoped<BreadCharts.Web.Services.IVotingService, BreadCharts.Web.Services.VotingService>();
builder.Services.AddOptions<BreadCharts.Web.Services.VotingOptions>()
    .Bind(builder.Configuration.GetSection("Voting"))
    .PostConfigure(opt => { if (opt.MaxVotesPerUser <= 0) opt.MaxVotesPerUser = 10; });
builder.Services.AddHttpContextAccessor();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    });

authBuilder.AddSpotify(options =>
{
    options.ClientId = builder.Configuration["AuthServiceConfig:ClientId"] ?? string.Empty;
    options.ClientSecret = builder.Configuration["AuthServiceConfig:ClientSecret"] ?? string.Empty;
    options.CallbackPath = "/signin-spotify";
    options.SaveTokens = true;

    var scopes = new List<string>
    {
        SpotifyAPI.Web.Scopes.UserReadEmail,
        SpotifyAPI.Web.Scopes.UserReadPrivate,
        SpotifyAPI.Web.Scopes.PlaylistReadPrivate,
        SpotifyAPI.Web.Scopes.PlaylistReadCollaborative,
        SpotifyAPI.Web.Scopes.PlaylistModifyPrivate,
        SpotifyAPI.Web.Scopes.PlaylistModifyPublic,
        SpotifyAPI.Web.Scopes.UserTopRead,
        SpotifyAPI.Web.Scopes.Streaming,
    };
    foreach (var s in scopes) options.Scope.Add(s);
    
    // Harden correlation cookie to reduce SameSite issues
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.CorrelationCookie.HttpOnly = true;
    
    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
    {
        OnCreatingTicket = async context =>
        {
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
            using var response = await context.Backchannel.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();
            using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;

            if (root.TryGetProperty("display_name", out var displayNameEl))
            {
                var displayName = displayNameEl.GetString();
                if (!string.IsNullOrEmpty(displayName))
                {
                    context.Identity!.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, displayName));
                }
            }
            if (root.TryGetProperty("email", out var emailEl))
            {
                var email = emailEl.GetString();
                if (!string.IsNullOrEmpty(email))
                {
                    context.Identity!.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email));
                }
            }
            if (root.TryGetProperty("id", out var idEl))
            {
                var id = idEl.GetString();
                if (!string.IsNullOrEmpty(id))
                {
                    context.Identity!.AddClaim(new System.Security.Claims.Claim("urn:spotify:id", id));
                }
            }

            if (!string.IsNullOrEmpty(context.AccessToken))
            {
                context.Identity!.AddClaim(new System.Security.Claims.Claim("urn:spotify:access_token", context.AccessToken));
            }
            if (!string.IsNullOrEmpty(context.RefreshToken))
            {
                context.Identity!.AddClaim(new System.Security.Claims.Claim("urn:spotify:refresh_token", context.RefreshToken));
            }
        }
    };
});

authBuilder.AddIdentityCookies();

IHostEnvironment env = builder.Environment;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Ensure authentication/authorization are in the pipeline before mapping components
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Lightweight endpoint to initiate Spotify OAuth challenge aligned with our custom flow
app.MapGet("/auth/spotify", (HttpContext http, string? returnUrl) =>
{
    // After OAuth completes at /signin-spotify, the middleware will redirect here
    var redirectAfterOAuth = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = $"/auth/finalize?returnUrl={Uri.EscapeDataString(redirectAfterOAuth)}"
    };
    return Results.Challenge(props, new[] { "Spotify" });
});

// Finalize external login: exchange external cookie for app cookie, then redirect
app.MapGet("/auth/finalize", async (
    HttpContext http,
    [FromServices] UserManager<ApplicationUser> userManager,
    [FromServices] SignInManager<ApplicationUser> signInManager,
    [FromQuery] string? returnUrl) =>
{
    // Read the external authentication result created by the OAuth middleware
    var extAuth = await http.AuthenticateAsync(IdentityConstants.ExternalScheme);
    if (!(extAuth?.Succeeded ?? false) || extAuth.Principal is null)
    {
        return Results.Redirect("/welcome");
    }

    var principal = extAuth.Principal;
    var spotifyId = principal.FindFirst("urn:spotify:id")?.Value;
    var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    var access = principal.FindFirst("urn:spotify:access_token")?.Value;
    var refresh = principal.FindFirst("urn:spotify:refresh_token")?.Value;

    // Upsert user (by external login first, then email)
    ApplicationUser? user = null;
    if (!string.IsNullOrEmpty(spotifyId))
    {
        user = await userManager.FindByLoginAsync("Spotify", spotifyId);
    }
    if (user is null && !string.IsNullOrEmpty(email))
    {
        user = await userManager.FindByEmailAsync(email);
    }
    if (user is null)
    {
        var username = email ?? $"spotify-{spotifyId}";
        user = new ApplicationUser
        {
            UserName = username,
            Email = email ?? string.Empty,
            EmailConfirmed = true,
            ThirdPartyId = spotifyId ?? string.Empty,
        };
        var createRes = await userManager.CreateAsync(user);
        if (!createRes.Succeeded)
        {
            return Results.Redirect("/welcome");
        }
        if (!string.IsNullOrEmpty(spotifyId))
        {
            await userManager.AddLoginAsync(user, new UserLoginInfo("Spotify", spotifyId, "Spotify"));
        }
    }

    // Persist only third-party id; do NOT persist access/refresh tokens (session-only requirement)
    if (!string.IsNullOrEmpty(spotifyId)) user.ThirdPartyId = spotifyId;
    await userManager.UpdateAsync(user);

    // Sign in with a session (non-persistent) application cookie, carrying over access/refresh claims
    var appClaims = new List<System.Security.Claims.Claim>();
    if (!string.IsNullOrEmpty(access)) appClaims.Add(new System.Security.Claims.Claim("urn:spotify:access_token", access));
    if (!string.IsNullOrEmpty(refresh)) appClaims.Add(new System.Security.Claims.Claim("urn:spotify:refresh_token", refresh ?? string.Empty));
    await signInManager.SignInWithClaimsAsync(user, isPersistent: false, appClaims);
    await http.SignOutAsync(IdentityConstants.ExternalScheme);

    // No-op: services will create user-scoped Spotify clients on demand using cached tokens

    // Redirect to final destination (default home)
    var destination = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    return Results.Redirect(destination);
});

// Minimal logout endpoint to replace templated Identity UI endpoints.
app.MapPost("/Account/Logout", async (
    HttpContext context,
    [FromServices] SignInManager<ApplicationUser> signInManager,
    [FromForm] string? returnUrl) =>
{
    await signInManager.SignOutAsync();
    var target = string.IsNullOrWhiteSpace(returnUrl) ? "/welcome" : $"~/{returnUrl}";
    return TypedResults.LocalRedirect(target);
});

app.Run();