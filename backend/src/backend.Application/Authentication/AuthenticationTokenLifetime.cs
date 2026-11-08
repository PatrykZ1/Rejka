namespace backend.Application.Authentication;

public sealed record AuthenticationTokenLifetime(
    TimeSpan AccessTokenLifetime,
    TimeSpan RefreshTokenLifetime);