using backend.Application.Interfaces.Authentication;

namespace backend.Application.Features.Accounts;

public sealed class Logout(IRefreshTokenGenerator refreshTokenGenerator, IRefreshSessionStore refreshSessions, TimeProvider timeProvider)
{
    public Task ExecuteAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Task.CompletedTask;
        }

        return refreshSessions.RevokeAsync(
            refreshTokenGenerator.Hash(refreshToken),
            timeProvider.GetUtcNow(),
            cancellationToken);
    }
}