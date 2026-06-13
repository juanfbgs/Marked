using System.Security.Claims;

namespace Marked.Features.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetCurrentUserId(this ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(claimValue) || !Guid.TryParse(claimValue, out var userId))
            throw new UnauthorizedAccessException("A valid User ID Guid claim is missing from token.");

        return userId;
    }
}