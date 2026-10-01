using System.Security.Claims;
using Prometej_core.Auth;

namespace Prometej_api.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserIdOrNull(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue("sub"), out var id) ? id : null;

        public static int GetUserId(this ClaimsPrincipal user) =>
            user.GetUserIdOrNull() ?? throw new InvalidOperationException("The caller has no user id claim.");

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
    }
}
