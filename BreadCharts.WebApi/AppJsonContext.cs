using System.Text.Json.Serialization;
using BreadCharts.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace BreadCharts.WebApi;

public class HealthResponse
{
    public string Status { get; set; } = "ok";
    public string App { get; set; } = "BreadCharts";
}

public class ServerInfoResponse
{
    public string App { get; set; } = "BreadCharts";
    public string MachineName { get; set; } = Environment.MachineName;
    public string Version { get; set; } = "1.0.0";
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}

public class AuthSession
{
    public string Id { get; set; } = null!;
    public string Status { get; set; } = "pending"; // pending, complete, failed, expired
    public AuthResponse? Response { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ServerInfoResponse))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(AuthSession))]
[JsonSerializable(typeof(CreateSessionResponse))]
[JsonSerializable(typeof(AuthSessionStatusResponse))]
[JsonSerializable(typeof(List<SubmittedSong>))]
[JsonSerializable(typeof(SubmitRequest))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
