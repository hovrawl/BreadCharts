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

public class AuthResponse
{
    public string AppToken { get; set; } = null!;
    public string? SpotifyAccessToken { get; set; }
    public string? SpotifyRefreshToken { get; set; }
    public string? ExpiresIn { get; set; }
    public UserSummary User { get; set; } = null!;
}

public class UserSummary
{
    public string Id { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Email { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ServerInfoResponse))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(List<SubmittedSong>))]
[JsonSerializable(typeof(SubmitRequest))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails))]
internal partial class AppJsonContext : JsonSerializerContext
{
}
