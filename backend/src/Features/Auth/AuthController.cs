
using Marked.Data;
using Marked.Domain;
using Marked.Features.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marked.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AppDbContext _context;

    private readonly IConfiguration _config;

    public AuthController(AuthService authService, AppDbContext context, IConfiguration config)
    {
        _authService = authService;
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
        {
            return BadRequest("Username or Email already exists.");
        }

        var newUser = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = request.Username,
            PasswordHash = _authService.HashPassword(request.Password)
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Registration successful." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        return Ok(await GenerateTokenPair(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken
                                   && u.RefreshTokenExpiresAt > DateTime.UtcNow);

        if (user is null)
            return Unauthorized("Invalid or expired refresh token.");

        return Ok(await GenerateTokenPair(user));
    }

    private async Task<AuthResponse> GenerateTokenPair(User user)
    {
        var accessToken = _authService.GenerateAccessToken(user.Id, user.Username);
        var refreshToken = _authService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("JwtSettings:ExpirationInDays"));
        await _context.SaveChangesAsync();

        return new AuthResponse(accessToken, refreshToken);
    }

}