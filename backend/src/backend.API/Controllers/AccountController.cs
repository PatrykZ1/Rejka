using backend.API.Models;
using backend.Application.Authentication;
using backend.Application.Features.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers;

[ApiController]
[Route("account")]
public sealed class AccountController(
    RegisterAccount registerAccount,
    Login login,
    Logout logout,
    RefreshSession refreshSession) : ControllerBase
{
    private readonly RegisterAccount _registerAccount = registerAccount;
    private readonly Login _login = login;
    private readonly Logout _logout = logout;
    private readonly RefreshSession _refreshSession = refreshSession;

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerAccount.ExecuteAsync(
            new RegisterAccountCommand(request.Username, request.Email, request.Password),
            cancellationToken);

        return result.Status switch
        {
            RegisterAccountStatus.Created => StatusCode(StatusCodes.Status201Created, new { userId = result.UserId, message = "Konto zostało pomyślnie założone" }),
            RegisterAccountStatus.InvalidInput => BadRequest(new { message = "Słabe hasło lub nieprawidłowe dane" }),
            RegisterAccountStatus.EmailAlreadyRegistered => Conflict(new { message = "Adres email już istnieje w bazie" }),
            _ => BadRequest()
        };
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _login.ExecuteAsync(request.Email, request.Password, cancellationToken);

        return result.Status switch
        {
            LoginStatus.Success => Ok(new LoginResponse(result.Tokens!.AccessToken, result.Tokens.RefreshToken)),
            LoginStatus.InvalidCredentials => Unauthorized(new { message = "Błędne dane logowania" }),
            LoginStatus.AccountUnavailable => StatusCode(StatusCodes.Status403Forbidden, new { message = "Użytkownik zablokowany lub usunięty" }),
            LoginStatus.SessionCreationFailed => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Nie udało się utworzyć sesji" }),
            _ => BadRequest(new { message = "Brakujące lub nieprawidłowe dane żądania" })
        };
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _logout.ExecuteAsync(request.RefreshToken, cancellationToken);
        return Ok(new { message = "Wylogowano pomyślnie" });
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _refreshSession.ExecuteAsync(request.RefreshToken, cancellationToken);

        return result.Status switch
        {
            RefreshSessionRotationStatus.Rotated => Ok(new LoginResponse(result.Tokens!.AccessToken, result.Tokens.RefreshToken)),
            RefreshSessionRotationStatus.AccountUnavailable => StatusCode(StatusCodes.Status403Forbidden, new { message = "Użytkownik zablokowany lub usunięty" }),
            RefreshSessionRotationStatus.Expired or
            RefreshSessionRotationStatus.Revoked or
            RefreshSessionRotationStatus.ReuseDetected or
            RefreshSessionRotationStatus.Invalid => Unauthorized(new { message = "Nieprawidłowy, wygasły lub unieważniony refresh token" }),
            _ => BadRequest(new { message = "Brakujące lub nieprawidłowe dane żądania" })
        };
    }
}
