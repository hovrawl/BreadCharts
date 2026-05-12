using System.Text.Json.Serialization;
using BreadCharts.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace BreadCharts.WebApi;

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
