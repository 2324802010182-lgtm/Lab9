using ASC.Utilities;
using System.Security.Claims;


namespace ASC.Utilities
{
    public static class ClaimsPrincipalExtensions
    {
        public static CurrentUser GetCurrentUser(this ClaimsPrincipal principal)
        {
            if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
            {
                return new CurrentUser
                {
                    IsAuthenticated = false
                };
            }

            return new CurrentUser
            {
                UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                EmailAddress = principal.FindFirst(ClaimTypes.Email)?.Value,
                FullName = principal.Identity.Name,
                UserRole = principal.FindFirst(ClaimTypes.Role)?.Value,
                IsAuthenticated = true
            };
        }
    }
}