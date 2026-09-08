using System;

namespace BreadCharts.Core.Models;

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

public class AuthSessionStatusResponse
{
    public string Status { get; set; } = null!; // pending, complete, failed, expired
    public string? AppToken { get; set; }
    public string? SpotifyAccessToken { get; set; }
    public string? SpotifyRefreshToken { get; set; }
    public string? ExpiresIn { get; set; }
    public UserSummary? User { get; set; }
    public string? Error { get; set; }
}

public class CreateSessionResponse
{
    public string SessionId { get; set; } = null!;
    public string AuthUrl { get; set; } = null!;
}
