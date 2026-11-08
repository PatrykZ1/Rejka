using backend.Application.Authentication;
using backend.Application.Features.Accounts;
using backend.Application.Interfaces.Authentication;
using backend.Application.Interfaces.Users;
using backend.Domain.Users;

namespace backend.UnitTests;

public class AccountUseCasesTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-02T12:00:00Z");

    [Fact]
    public async Task RegisterAccount_CreatesActiveUserWithNormalizedEmailAndHash()
    {
        var users = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var useCase = new RegisterAccount(users, hasher, new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(new RegisterAccountCommand(
            " driver ", " Driver@Example.com ", "correct horse"));

        Assert.Equal(RegisterAccountStatus.Created, result.Status);
        Assert.Equal("driver", users.AddedUser?.Username);
        Assert.Equal("DRIVER@EXAMPLE.COM", users.AddedUser?.NormalizedEmail);
        Assert.Equal("hash:correct horse", users.AddedUser?.PasswordHash);
        Assert.Equal(UserState.Active, users.AddedUser?.State);
        Assert.Equal(Now, users.AddedUser?.CreatedAt);
    }

    [Fact]
    public async Task RegisterAccount_RejectsInvalidInputBeforeHashing()
    {
        var users = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var useCase = new RegisterAccount(users, hasher, new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(new RegisterAccountCommand("driver", "invalid", "password123"));

        Assert.Equal(RegisterAccountStatus.InvalidInput, result.Status);
        Assert.Equal(0, hasher.HashCalls);
        Assert.Null(users.AddedUser);
    }

    [Fact]
    public async Task RegisterAccount_UsesRepositoryResultForDuplicateEmail()
    {
        var users = new FakeUserRepository { AddResult = false };
        var useCase = new RegisterAccount(users, new FakePasswordHasher(), new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(new RegisterAccountCommand(
            "driver", "driver@example.com", "password123"));

        Assert.Equal(RegisterAccountStatus.EmailAlreadyRegistered, result.Status);
    }

    [Fact]
    public async Task Login_IssuesTokensForActiveAccount()
    {
        var users = new FakeUserRepository { FoundUser = CreateUser() };
        var accessTokens = new FakeAccessTokenIssuer();
        var sessions = new FakeRefreshSessionStore();
        var useCase = CreateLogin(users, new FakePasswordHasher(), accessTokens, new FakeRefreshTokenGenerator(), sessions);

        var result = await useCase.ExecuteAsync("DRIVER@example.com", "password123");

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal("access-token", result.Tokens?.AccessToken);
        Assert.Equal("refresh-token", result.Tokens?.RefreshToken);
        Assert.Equal("replacement-hash", sessions.CreatedTokenHash);
        Assert.Equal(UserState.Active, accessTokens.IssuedState);
    }

    [Fact]
    public async Task Login_RehashesPasswordAfterSuccessfulVerification()
    {
        var user = CreateUser();
        var users = new FakeUserRepository { FoundUser = user };
        var hasher = new FakePasswordHasher { VerificationStatus = PasswordVerificationStatus.SuccessRehashNeeded };
        var useCase = CreateLogin(users, hasher, new FakeAccessTokenIssuer(), new FakeRefreshTokenGenerator(), new FakeRefreshSessionStore());

        var result = await useCase.ExecuteAsync("driver@example.com", "password123");

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal(1, users.ReplaceHashCalls);
        Assert.Equal("hash:password123", user.PasswordHash);
    }

    [Fact]
    public async Task Login_DoesNotIssueTokensForBlockedAccount()
    {
        var users = new FakeUserRepository { FoundUser = CreateUser(UserState.Blocked) };
        var sessions = new FakeRefreshSessionStore();
        var useCase = CreateLogin(users, new FakePasswordHasher(), new FakeAccessTokenIssuer(), new FakeRefreshTokenGenerator(), sessions);

        var result = await useCase.ExecuteAsync("driver@example.com", "password123");

        Assert.Equal(LoginStatus.AccountUnavailable, result.Status);
        Assert.Null(result.Tokens);
        Assert.Null(sessions.CreatedTokenHash);
    }

    [Fact]
    public async Task RefreshSession_RotatesRefreshTokenAndIssuesAccessToken()
    {
        var sessions = new FakeRefreshSessionStore
        {
            RotationResult = new RefreshSessionRotationResult(
                RefreshSessionRotationStatus.Rotated, 41, UserState.Admin)
        };
        var accessTokens = new FakeAccessTokenIssuer();
        var generator = new FakeRefreshTokenGenerator();
        var useCase = CreateRefreshSession(sessions, accessTokens, generator);

        var result = await useCase.ExecuteAsync("old-refresh-token");

        Assert.Equal(RefreshSessionRotationStatus.Rotated, result.Status);
        Assert.Equal("stored:old-refresh-token", sessions.CurrentTokenHash);
        Assert.Equal("replacement-hash", sessions.ReplacementTokenHash);
        Assert.Equal(41, accessTokens.IssuedUserId);
        Assert.Equal(UserState.Admin, accessTokens.IssuedState);
        Assert.Equal("refresh-token", result.Tokens?.RefreshToken);
    }

    [Fact]
    public async Task RefreshSession_RejectsInvalidTokenWithoutIssuingAccessToken()
    {
        var accessTokens = new FakeAccessTokenIssuer();
        var sessions = new FakeRefreshSessionStore
        {
            RotationResult = new RefreshSessionRotationResult(RefreshSessionRotationStatus.ReuseDetected)
        };
        var useCase = CreateRefreshSession(sessions, accessTokens, new FakeRefreshTokenGenerator());

        var result = await useCase.ExecuteAsync("reused-token");

        Assert.Equal(RefreshSessionRotationStatus.ReuseDetected, result.Status);
        Assert.Null(result.Tokens);
        Assert.Null(accessTokens.IssuedUserId);
    }

    [Fact]
    public async Task Logout_RevokesOnlyThePresentedRefreshToken()
    {
        var sessions = new FakeRefreshSessionStore();
        var useCase = new Logout(new FakeRefreshTokenGenerator(), sessions, new FixedTimeProvider(Now));

        await useCase.ExecuteAsync("logout-token");

        Assert.Equal("stored:logout-token", sessions.RevokedTokenHash);
        Assert.Equal(Now, sessions.RevokedAt);
    }

    private static Login CreateLogin(
        FakeUserRepository users,
        FakePasswordHasher hasher,
        FakeAccessTokenIssuer accessTokens,
        FakeRefreshTokenGenerator refreshTokens,
        FakeRefreshSessionStore sessions) => new(
        users,
        hasher,
        accessTokens,
        refreshTokens,
        sessions,
        new AuthenticationTokenLifetime(TimeSpan.FromMinutes(10), TimeSpan.FromDays(14)),
        new FixedTimeProvider(Now));

    private static RefreshSession CreateRefreshSession(
        FakeRefreshSessionStore sessions,
        FakeAccessTokenIssuer accessTokens,
        FakeRefreshTokenGenerator refreshTokens) => new(
        accessTokens,
        refreshTokens,
        sessions,
        new AuthenticationTokenLifetime(TimeSpan.FromMinutes(10), TimeSpan.FromDays(14)),
        new FixedTimeProvider(Now));

    private static User CreateUser(UserState state = UserState.Active)
    {
        var user = new User("driver", "driver@example.com", "existing-hash", Now);
        if (state != UserState.Active)
        {
            user.ChangeState(state, Now);
        }

        return user;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? FoundUser { get; init; }
        public User? AddedUser { get; private set; }
        public bool AddResult { get; init; } = true;
        public int ReplaceHashCalls { get; private set; }

        public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(FoundUser);

        public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
        {
            AddedUser = user;
            return Task.FromResult(AddResult);
        }

        public Task<bool> TryReplacePasswordHashAsync(
            int userId,
            string expectedPasswordHash,
            string newPasswordHash,
            CancellationToken cancellationToken)
        {
            ReplaceHashCalls++;
            return Task.FromResult(true);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordVerificationStatus VerificationStatus { get; init; } = PasswordVerificationStatus.Success;
        public int HashCalls { get; private set; }

        public string Hash(string password)
        {
            HashCalls++;
            return $"hash:{password}";
        }

        public PasswordVerificationStatus Verify(string passwordHash, string providedPassword) => VerificationStatus;
    }

    private sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
    {
        public int? IssuedUserId { get; private set; }
        public UserState? IssuedState { get; private set; }

        public IssuedAccessToken Issue(int userId, UserState userState, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
        {
            IssuedUserId = userId;
            IssuedState = userState;
            return new IssuedAccessToken("access-token", expiresAt);
        }
    }

    private sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        public GeneratedRefreshToken Generate() => new("refresh-token", "replacement-hash");

        public string Hash(string refreshToken) => $"stored:{refreshToken}";
    }

    private sealed class FakeRefreshSessionStore : IRefreshSessionStore
    {
        public string? CreatedTokenHash { get; private set; }
        public string? CurrentTokenHash { get; private set; }
        public string? ReplacementTokenHash { get; private set; }
        public string? RevokedTokenHash { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }
        public RefreshSessionRotationResult RotationResult { get; init; } = new(RefreshSessionRotationStatus.Invalid);

        public Task<bool> CreateAsync(int userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt, CancellationToken cancellationToken)
        {
            CreatedTokenHash = tokenHash;
            return Task.FromResult(true);
        }

        public Task<RefreshSessionRotationResult> RotateAsync(
            string currentTokenHash,
            string replacementTokenHash,
            DateTimeOffset now,
            DateTimeOffset replacementExpiresAt,
            CancellationToken cancellationToken)
        {
            CurrentTokenHash = currentTokenHash;
            ReplacementTokenHash = replacementTokenHash;
            return Task.FromResult(RotationResult);
        }

        public Task RevokeAsync(string tokenHash, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        {
            RevokedTokenHash = tokenHash;
            RevokedAt = revokedAt;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}