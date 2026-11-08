using backend.Application.Authentication;

namespace backend.Application.Interfaces.Authentication;

public interface IPasswordHasher
{
    string Hash(string password);
    PasswordVerificationStatus Verify(string passwordHash, string providedPassword);
}