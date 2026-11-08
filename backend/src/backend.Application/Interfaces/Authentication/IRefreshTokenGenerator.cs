using backend.Application.Authentication;

namespace backend.Application.Interfaces.Authentication;

public interface IRefreshTokenGenerator
{
    GeneratedRefreshToken Generate();
    string Hash(string refreshToken);
}