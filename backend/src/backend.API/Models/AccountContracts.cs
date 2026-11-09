namespace backend.API.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, string RefreshToken);

public sealed record RegisterRequest(string Username, string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);
