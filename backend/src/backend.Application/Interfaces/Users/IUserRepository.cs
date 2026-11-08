using backend.Domain.Users;

namespace backend.Application.Interfaces.Users;

public interface IUserRepository
{
    Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);
    Task<bool> TryReplacePasswordHashAsync(
        int userId,
        string expectedPasswordHash,
        string newPasswordHash,
        CancellationToken cancellationToken);
}