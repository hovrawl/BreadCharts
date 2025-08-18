BreadCharts – Development Guidelines

Last verified: 2025-08-18 09:29 local time

Scope: This document captures project-specific build, configuration, testing, and development notes for BreadCharts (Blazor Server, .NET 9). It assumes an advanced .NET developer.

Project layout
- BreadCharts.Core (net9.0): Core types/services and Spotify integration primitives.
- BreadCharts.Web (net9.0): Blazor Server front-end + Identity + EF Core (SQLite) + BitzArt.Blazor.Auth + FluentUI.
- Data store: SQLite file at BreadCharts.Web/Data/app.db (copied to output on build).

Build and configuration
- Requirements
  - .NET SDK 9.x
  - Local dev uses SQLite; no external DB required.
  - Optional: Docker (compose.yaml present) if you plan to containerize; default project is Linux container-oriented (DockerDefaultTargetOS=Linux), but standard dotnet run is fine for local.

- Configuration sources (Program.cs)
  - appsettings.json and appsettings.{Environment}.json are loaded. Add env-specific values there.
  - Connection string name: DefaultConnection (required). For local SQLite, this should point to Data/app.db. Example:
    ConnectionStrings:DefaultConnection: Data Source=Data/app.db
  - UserSecrets are enabled for BreadCharts.Web (UserSecretsId=aspnet-BreadCharts.Web-39ce1806-8383-4890-a840-1d814cb49dc0). Use secrets for environment-specific sensitive config when needed.

- EF Core / Identity
  - DbContext: BreadCharts.Web.Data.ApplicationDbContext
  - Identity user: BreadCharts.Web.Data.ApplicationUser with additional tokens (AccessToken, RefreshToken, ThirdPartyId).
  - During Development environment, UseMigrationsEndPoint is enabled; ensure migrations are applied/created as needed.
  - Typical EF commands (run from BreadCharts.Web directory):
    - dotnet ef migrations add <Name>
    - dotnet ef database update
  - The file Data/app.db is marked CopyToOutputDirectory=PreserveNewest, so it will be copied on build.

- Authentication (BitzArt.Blazor.Auth)
  - The app wires BitzArt.Blazor.Auth via AddBlazorAuth<ChartAuthenticationService>() and MapAuthEndpoints().
  - ChartAuthenticationService currently returns a synthetic JwtPair (access/refresh tokens with sane expirations). Replace BuildJwtPair with real token issuance when integrating a real identity provider.
  - Identity cookies are configured with Application/External schemes, Identity UI endpoints are mapped via MapAdditionalIdentityEndpoints().

- UI stack
  - Blazor Server with FluentUI components (Microsoft.FluentUI.AspNetCore.Components).
  - Razor components registered via AddRazorComponents().AddInteractiveServerComponents() and mapped with AddInteractiveServerRenderMode().

- Building and running
  - From repository root:
    - Restore: dotnet restore BreadCharts.sln
    - Build: dotnet build -c Debug BreadCharts.sln
    - Run (web): dotnet run --project .\BreadCharts.Web\BreadCharts.Web.csproj
  - Default HTTPS redirection is enabled; development exception page and migrations endpoint are active in Development.

Testing
- Framework: The repo does not ship with tests by default. You can add a standard .NET test project (MSTest, xUnit, NUnit). Below is a minimal, verified MSTest example against BreadCharts.Core.

- Verified example (MSTest, net9.0)
  1) Create a test project and reference BreadCharts.Core:
     - Create BreadCharts.Tests/BreadCharts.Tests.csproj with:
       <Project Sdk="Microsoft.NET.Sdk">
         <PropertyGroup>
           <TargetFramework>net9.0</TargetFramework>
           <IsPackable>false</IsPackable>
           <Nullable>enable</Nullable>
         </PropertyGroup>
         <ItemGroup>
           <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
           <PackageReference Include="MSTest.TestAdapter" Version="3.6.0" />
           <PackageReference Include="MSTest.TestFramework" Version="3.6.0" />
           <PackageReference Include="FluentAssertions" Version="6.12.0" />
         </ItemGroup>
         <ItemGroup>
           <ProjectReference Include="..\BreadCharts.Core\BreadCharts.Core.csproj" />
         </ItemGroup>
       </Project>

  2) Add a test class (BreadCharts.Tests/ChartOptionTests.cs):
       using System;
       using BreadCharts.Core.Models;
       using FluentAssertions;
       using Microsoft.VisualStudio.TestTools.UnitTesting;

       namespace BreadCharts.Tests;

       [TestClass]
       public class ChartOptionTests
       {
           [TestMethod]
           public void ToString_Should_Return_Name()
           {
               var opt = new ChartOption { Id = "1", Name = "Top Tracks", Type = ChartOptionType.Track };
               opt.ToString().Should().Be("Top Tracks");
           }

           [TestMethod]
           public void Enum_Should_Have_Expected_Values()
           {
               var values = Enum.GetNames(typeof(ChartOptionType));
               values.Should().BeEquivalentTo(new[] { "Track", "Album", "Artist", "Playlist" });
           }
       }

  3) Add the test project to the solution (so standard runners discover it):
       dotnet sln .\BreadCharts.sln add .\BreadCharts.Tests\BreadCharts.Tests.csproj

  4) Run tests (from solution root):
       dotnet test BreadCharts.sln

  This example was executed and passed (2/2 tests) in the current environment with .NET 9 (see Test Results in the session log).

- Notes and tips
  - If you prefer xUnit or NUnit, use the corresponding packages; ensure Microsoft.NET.Test.Sdk is referenced and that the test project targets net9.0.
  - For solution-wide test discovery in IDEs/CI, keep the test project in the solution and name it *.Tests.
  - To keep Core tests fast/deterministic, favor pure logic in BreadCharts.Core; integration tests for BreadCharts.Web should use WebApplicationFactory or custom host only when needed.

Additional development notes
- Code style
  - Nullable is enabled across projects; favor explicit null handling.
  - Implicit usings are enabled; add explicit using when referencing System.* types used in tests or shared libraries to avoid ambiguous discovery issues.
  - Keep Core free of Web dependencies; Web may reference Core.

- EF Core and migrations
  - When changing ApplicationUser or DbContext, add a migration from the BreadCharts.Web directory and update the local SQLite DB.
  - The app copies Data/app.db on build; delete the output DB if you need to reset state.

- Auth flow (current state)
  - ChartAuthenticationService returns a stub JwtPair with expirations; replace with real JWT issuance when integrating with external providers (e.g., Spotify auth callback -> store tokens on ApplicationUser -> issue app JWT pair).
  - Identity endpoints are mapped; confirm cookie/auth scheme alignment if customizing.

- FluentUI
  - Components added via AddFluentUIComponents; ensure client-side static assets are available. Update Microsoft.FluentUI.AspNetCore.Components if upgrading .NET minor versions.

- Configuration hygiene
  - Use appsettings.{Environment}.json and/or user secrets for any tokens or client secrets.
  - Keep DefaultConnection present; Program.cs throws if missing.

Clean-up policy for transient artifacts
- For demonstration, a temporary MSTest project was created and executed successfully. Per repository policy for this task, remove any temporary artifacts after verification to keep the repo clean. Retain only this .junie/guidelines.md file.
