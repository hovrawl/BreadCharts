using BreadCharts.Core.Models;

namespace BreadCharts.Core.Services;

public interface IAuthenticationService
{
    Uri LoginChallenge();

    Task<bool> ChallengeCallback(string userId, string code);
    
    
    Task<UserProfile> GetUserProfile();
}