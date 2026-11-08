using backend.Application.Authentication;

namespace backend.Application.Interfaces.Authentication;

public interface IRefreshSessionStore
{
    Task<bool> CreateAsync(
        int userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken);

    Task<RefreshSessionRotationResult> RotateAsync(
        string currentTokenHash,
        string replacementTokenHash,
        DateTimeOffset now,
        DateTimeOffset replacementExpiresAt,
        CancellationToken cancellationToken);

    Task RevokeAsync(string tokenHash, DateTimeOffset revokedAt, CancellationToken cancellationToken);
}