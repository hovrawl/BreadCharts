using Microsoft.AspNetCore.Identity;

namespace BreadCharts.Web.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string RefreshToken { get; set; } = "";
    
    public string AccessToken { get; set; } = "";
    
    public string ThirdPartyId { get; set; }
}