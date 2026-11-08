using backend.Application.Authentication;
using backend.Application.Interfaces.Authentication;
using backend.Application.Interfaces.Users;
using backend.Domain.Users;

namespace backend.Application.Features.Accounts;

public sealed record RegisterAccountCommand(string Username, string Email, string Password);

public enum RegisterAccountStatus
{
    Created,
    InvalidInput,
    EmailAlreadyRegistered
}

public sealed record RegisterAccountResult(RegisterAccountStatus Status, int? UserId = null);

public sealed class RegisterAccount(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
{
    public async Task<RegisterAccountResult> ExecuteAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var username = command.Username?.Trim();
        var email = command.Email?.Trim();
        var password = command.Password;
        if (string.IsNullOrEmpty(username) || username.Length > 64 || string.IsNullOrEmpty(email) ||
            !AccountEmail.TryNormalize(email, out _) ||
            password is null || password.Length < 8 || password.Length > 1024)
        {
            return new RegisterAccountResult(RegisterAccountStatus.InvalidInput);
        }

        var passwordHash = passwordHasher.Hash(password);
        var user = new User(username, email, passwordHash, timeProvider.GetUtcNow());
        var created = await users.TryAddAsync(user, cancellationToken);

        return created
            ? new RegisterAccountResult(RegisterAccountStatus.Created, user.Id)
            : new RegisterAccountResult(RegisterAccountStatus.EmailAlreadyRegistered);
    }
}