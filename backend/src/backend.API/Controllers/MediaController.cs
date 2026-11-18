using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend.API.Models;
using backend.Application.Features.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers;

[ApiController]
[Route("media")]
public sealed class MediaController : ControllerBase
{
    private readonly UploadMedia _uploadMedia;

    public MediaController(UploadMedia uploadMedia)
    {
        _uploadMedia = uploadMedia;
    }

    [Authorize]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MediaItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Upload([FromForm] UploadMediaRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { message = "Błędne żądanie: plik jest wymagany." });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Brak autoryzacji (nieprawidłowy token)." });
        }

        await using var stream = request.File.OpenReadStream();

        var command = new UploadMediaCommand(
            UserId: userId.Value,
            FileName: request.File.FileName,
            ContentType: request.File.ContentType,
            FileStream: stream,
            Latitude: request.Latitude,
            Longitude: request.Longitude,
            RecordedAt: request.RecordedAt,
            Comment: request.Comment);

        var result = await _uploadMedia.ExecuteAsync(command, cancellationToken);

        return result.Status switch
        {
            UploadMediaStatus.Success => StatusCode(StatusCodes.Status201Created, MapToResponse(result.Media!, result.SasUrl)),
            UploadMediaStatus.UnsupportedMediaType => StatusCode(StatusCodes.Status415UnsupportedMediaType, new { message = result.ErrorMessage ?? "Nieobsługiwany format pliku." }),
            UploadMediaStatus.InvalidInput => BadRequest(new { message = result.ErrorMessage ?? "Błędne żądanie." }),
            UploadMediaStatus.StorageFailure => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage ?? "Błąd zapisu pliku w magazynie obiektowym." }),
            _ => BadRequest(new { message = "Błędne żądanie." })
        };
    }

    private int? GetCurrentUserId()
    {
        var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(subClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
        {
            return userId;
        }

        return null;
    }

    private static MediaItemResponse MapToResponse(Domain.Media.File media, string? sasUrl)
    {
        return new MediaItemResponse(
            Id: media.Id,
            UserId: media.UserId,
            Url: sasUrl,
            FileName: media.FileName,
            Latitude: media.Location?.Latitude,
            Longitude: media.Location?.Longitude,
            RecordedAt: media.RecordedAt?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            UploadedAt: media.UploadedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            Comment: media.Comment ?? string.Empty,
            Registrations: Array.Empty<string>(),
            Ratings: Array.Empty<RegistrationRatingResponse>());
    }
}
