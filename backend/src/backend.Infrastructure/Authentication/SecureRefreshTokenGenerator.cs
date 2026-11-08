using System.Security.Cryptography;
using System.Text;
using backend.Application.Authentication;
using backend.Application.Interfaces.Authentication;

namespace backend.Infrastructure.Authentication;

public sealed class SecureRefreshTokenGenerator : IRefreshTokenGenerator
{
    public GeneratedRefreshToken Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var value = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return new GeneratedRefreshToken(value, Hash(value));
    }

    public string Hash(string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}