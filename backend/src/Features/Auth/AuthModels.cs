namespace Marked.Features.Auth;

public record RegisterRequest(
    string FirstName, 
    string LastName, 
    string Username, 
    string Email, 
    string Password, 
    string ConfirmPassword);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, string RefreshToken);
public record RefreshRequest(string RefreshToken);