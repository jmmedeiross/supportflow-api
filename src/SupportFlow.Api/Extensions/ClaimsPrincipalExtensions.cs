using System.Security.Claims;
using SupportFlow.Api.Models;

namespace SupportFlow.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        string? value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out Guid userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user identifier.");
    }

    public static bool IsSupportTeam(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(UserRole.Agent) || principal.IsInRole(UserRole.Admin);
    }
}
