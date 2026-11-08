using backend.Application.Authentication;
using backend.Application.Interfaces.Authentication;
using backend.Application.Interfaces.Users;
using backend.Domain.Users;

namespace backend.Application.Features.Accounts;

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    AccountUnavailable,
    SessionCreationFailed
}

public sealed record LoginResult(LoginStatus Status, AuthenticationTokens? Tokens = null);

public sealed class Login(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    IRefreshTokenGenerator refreshTokenGenerator,
    IRefreshSessionStore refreshSessions,
    AuthenticationTokenLifetime tokenLifetime,
    TimeProvider timeProvider)
{
    public async Task<LoginResult> ExecuteAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!AccountEmail.TryNormalize(email, out var normalizedEmail) || password is null)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var user = await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var verification = passwordHasher.Verify(user.PasswordHash, password);
        if (verification == PasswordVerificationStatus.Failed)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        if (user.State is not (UserState.Active or UserState.Admin))
        {
            return new LoginResult(LoginStatus.AccountUnavailable);
        }

        if (verification == PasswordVerificationStatus.SuccessRehashNeeded)
        {
            var previousHash = user.PasswordHash;
            var replacementHash = passwordHasher.Hash(password);
            var replaced = await users.TryReplacePasswordHashAsync(
                user.Id,
                previousHash,
                replacementHash,
                cancellationToken);
            if (!replaced)
            {
                return new LoginResult(LoginStatus.InvalidCredentials);
            }

            user.ChangePasswordHash(replacementHash);
        }

        var now = timeProvider.GetUtcNow();
        var accessToken = accessTokenIssuer.Issue(
            user.Id,
            user.State,
            now,
            now.Add(tokenLifetime.AccessTokenLifetime));
        var refreshToken = refreshTokenGenerator.Generate();
        var refreshExpiresAt = now.Add(tokenLifetime.RefreshTokenLifetime);
        var stored = await refreshSessions.CreateAsync(
            user.Id,
            refreshToken.Hash,
            refreshExpiresAt,
            now,
            cancellationToken);

        if (!stored)
        {
            return new LoginResult(LoginStatus.SessionCreationFailed);
        }

        return new LoginResult(LoginStatus.Success, new AuthenticationTokens(
            accessToken.Value,
            refreshToken.Value,
            accessToken.ExpiresAt,
            refreshExpiresAt));
    }
}