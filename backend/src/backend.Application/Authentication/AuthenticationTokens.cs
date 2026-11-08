namespace backend.Application.Authentication;

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);

public sealed record GeneratedRefreshToken(string Value, string Hash);

public sealed record AuthenticationTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public enum RefreshSessionRotationStatus
{
    Rotated,
    Invalid,
    Expired,
    Revoked,
    ReuseDetected,
    AccountUnavailable
}

public sealed record RefreshSessionRotationResult(
    RefreshSessionRotationStatus Status,
    int? UserId = null,
    backend.Domain.Users.UserState? UserState = null);