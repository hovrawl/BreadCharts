var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddProject<Projects.BreadCharts_Web>("breadcharts-web");
web
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("Spotify__ClientId", "7412007f28e045bf90c021c079e92655")
    .WithEnvironment("Spotify__ClientSecret", "29b610aae47944668afc79084a856f0b");

builder.Build().Run();
