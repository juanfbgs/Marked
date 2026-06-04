using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marked.Features.Shared;

[ApiController]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected Guid GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(claimValue) || !Guid.TryParse(claimValue, out var userId))
            throw new UnauthorizedAccessException("A valid User ID Guid claim is missing from token.");
        
        return userId;
    }
}