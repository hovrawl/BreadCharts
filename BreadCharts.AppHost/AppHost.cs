var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddProject<Projects.BreadCharts_Web>("breadcharts-web");
web
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

var api = builder.AddProject<Projects.BreadCharts_WebApi>("breadcharts-api");
// var desktop = builder.AddProject<Projects.BreadCharts_Avalonia_Desktop>("breadcharts-avalonia");

builder.Build().Run();
