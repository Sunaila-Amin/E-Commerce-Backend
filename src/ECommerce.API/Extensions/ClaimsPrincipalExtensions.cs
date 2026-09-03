using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return idClaim is not null && int.TryParse(idClaim, out var id)
            ? id
            : throw new UnauthorizedAccessException("User id not found in claims.");
    }

    public static bool IsInRoleName(this ClaimsPrincipal principal, string role) =>
        principal.IsInRole(role);
}
