using backend.Application.Authentication;
using backend.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Authentication;

public sealed class IdentityPasswordHasher(IOptions<PasswordHasherOptions> options) : IPasswordHasher
{
    private readonly PasswordHasher<object> _passwordHasher = new PasswordHasher<object>(options);
    private static readonly object User = new();

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return _passwordHasher.HashPassword(User, password);
    }

    public PasswordVerificationStatus Verify(string passwordHash, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);
        ArgumentNullException.ThrowIfNull(providedPassword);

        PasswordVerificationResult result;
        try
        {
            result = _passwordHasher.VerifyHashedPassword(User, passwordHash, providedPassword);
        }
        catch (FormatException)
        {
            return PasswordVerificationStatus.Failed;
        }

        return result switch
        {
            PasswordVerificationResult.Success => PasswordVerificationStatus.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationStatus.SuccessRehashNeeded,
            _ => PasswordVerificationStatus.Failed
        };
    }
}