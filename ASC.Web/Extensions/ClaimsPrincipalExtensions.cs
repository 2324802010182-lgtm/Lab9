using System.Security.Claims;

namespace ASC.Web.Extensions
{
    public class CurrentUserDetails
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public static class ClaimsPrincipalExtensions
    {
        public static CurrentUserDetails GetCurrentUserDetails(this ClaimsPrincipal user)
        {
            return new CurrentUserDetails
            {
                Name = user.Identity?.Name,
                Email = user.FindFirstValue(ClaimTypes.Email)
                        ?? user.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
            };
        }
    }
}