using Microsoft.AspNetCore.Identity;

namespace BreadCharts.Core.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    
    public string ThirdPartyId { get; set; } = "";
}
