using backend.Application.Authentication;
using backend.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace backend.UnitTests;

public class IdentityPasswordHasherTests
{
    private static IOptions<PasswordHasherOptions> Options(int iterations = 220_000) =>
        Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = iterations
        });

    [Fact]
    public void Hash_UsesUniqueSaltsAndVerifiesPassword()
    {
        var hasher = new IdentityPasswordHasher(Options());

        var firstHash = hasher.Hash("correct horse battery staple");
        var secondHash = hasher.Hash("correct horse battery staple");

        Assert.NotEqual(firstHash, secondHash);
        Assert.Equal(PasswordVerificationStatus.Success,
            hasher.Verify(firstHash, "correct horse battery staple"));
        Assert.Equal(PasswordVerificationStatus.Failed,
            hasher.Verify(firstHash, "wrong password"));
    }

    [Fact]
    public void Verify_RequestsRehashWhenStoredWorkFactorIsLower()
    {
        var legacyHasher = new PasswordHasher<object>(Options(100_000));
        var legacyHash = legacyHasher.HashPassword(new object(), "correct horse battery staple");
        var hasher = new IdentityPasswordHasher(Options());

        var result = hasher.Verify(legacyHash, "correct horse battery staple");

        Assert.Equal(PasswordVerificationStatus.SuccessRehashNeeded, result);
    }

    [Fact]
    public void Verify_ReturnsFailedForMalformedHash()
    {
        var hasher = new IdentityPasswordHasher(Options());

        var result = hasher.Verify("not a base64 hash", "some password");

        Assert.Equal(PasswordVerificationStatus.Failed, result);
    }

    [Fact]
    public void RefreshTokenGenerator_CreatesUniqueOpaqueTokensAndStoresOnlyDigest()
    {
        var generator = new SecureRefreshTokenGenerator();

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.NotEqual(first.Value, second.Value);
        Assert.NotEqual(first.Value, first.Hash);
        Assert.Equal(64, first.Hash.Length);
        Assert.Equal(first.Hash, generator.Hash(first.Value));
    }

    [Fact]
    public void JwtIssuer_SignsTokenWithExpectedIdentityAndAdminRole()
    {
        var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var issuer = new JwtAccessTokenIssuer(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "backend.tests",
            Audience = "mobile.tests",
            SigningKey = key
        }));
        var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        var issued = issuer.Issue(42, backend.Domain.Users.UserState.Admin, issuedAt, issuedAt.AddMinutes(10));
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "backend.tests",
            ValidateAudience = true,
            ValidAudience = "mobile.tests",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(key))
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(issued.Value, validationParameters, out _);

        Assert.Equal("42", principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("ADMIN", principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);
        Assert.Equal(issuedAt.AddMinutes(10), issued.ExpiresAt);
    }
}