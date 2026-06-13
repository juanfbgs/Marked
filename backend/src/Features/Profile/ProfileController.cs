using Marked.Data;
using Marked.Features.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marked.Features.Profile;

[ApiController]
[Route("api/user")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context) => _context = context;

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetCurrentUserId();

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.FirstName,
                u.LastName,
                u.Username,
                u.Email,
            })
            .FirstOrDefaultAsync();

        if (user is null)
            return NotFound();

        return Ok(new { user });
    }
}