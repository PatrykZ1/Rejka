using backend.Application.Authentication;
using backend.Application.Interfaces.Authentication;

namespace backend.Application.Features.Accounts;

public sealed record RefreshSessionResult(
    RefreshSessionRotationStatus Status,
    AuthenticationTokens? Tokens = null);

public sealed class RefreshSession(
    IAccessTokenIssuer accessTokenIssuer,
    IRefreshTokenGenerator refreshTokenGenerator,
    IRefreshSessionStore refreshSessions,
    AuthenticationTokenLifetime tokenLifetime,
    TimeProvider timeProvider)
{
    public async Task<RefreshSessionResult> ExecuteAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new RefreshSessionResult(RefreshSessionRotationStatus.Invalid);
        }

        var now = timeProvider.GetUtcNow();
        var replacement = refreshTokenGenerator.Generate();
        var refreshExpiresAt = now.Add(tokenLifetime.RefreshTokenLifetime);
        var rotation = await refreshSessions.RotateAsync(
            refreshTokenGenerator.Hash(refreshToken),
            replacement.Hash,
            now,
            refreshExpiresAt,
            cancellationToken);

        if (rotation.Status != RefreshSessionRotationStatus.Rotated ||
            rotation.UserId is null || rotation.UserState is null)
        {
            return new RefreshSessionResult(rotation.Status);
        }

        var accessExpiresAt = now.Add(tokenLifetime.AccessTokenLifetime);
        var accessToken = accessTokenIssuer.Issue(
            rotation.UserId.Value,
            rotation.UserState.Value,
            now,
            accessExpiresAt);

        return new RefreshSessionResult(RefreshSessionRotationStatus.Rotated, new AuthenticationTokens(
            accessToken.Value,
            replacement.Value,
            accessToken.ExpiresAt,
            refreshExpiresAt));
    }
}