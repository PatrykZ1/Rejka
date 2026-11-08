using backend.Application.Authentication;
using backend.Domain.Users;

namespace backend.Application.Interfaces.Authentication;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(
        int userId,
        UserState userState,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt);
}