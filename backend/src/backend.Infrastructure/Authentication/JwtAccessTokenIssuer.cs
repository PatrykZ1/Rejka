using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Application.Authentication;
using backend.Application.Interfaces.Authentication;
using backend.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend.Infrastructure.Authentication;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtAccessTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.Issuer) || string.IsNullOrWhiteSpace(_options.Audience))
        {
            throw new InvalidOperationException("JWT issuer and audience must be configured.");
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(_options.SigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("JWT signing key must be a Base64-encoded key.", exception);
        }

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must contain at least 256 bits.");
        }

        _signingKey = new SymmetricSecurityKey(keyBytes);
    }

    public IssuedAccessToken Issue(
        int userId,
        UserState userState,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAt, issuedAt);

        var userIdValue = userId.ToString(CultureInfo.InvariantCulture);
        var stateValue = userState switch
        {
            UserState.Active => "ACTIVE",
            UserState.Admin => "ADMIN",
            _ => throw new ArgumentOutOfRangeException(nameof(userState))
        };
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userIdValue),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("state", stateValue),
            new Claim(ClaimTypes.Role, stateValue == "ADMIN" ? "ADMIN" : "USER")
        };
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}