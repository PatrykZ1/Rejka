using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend.API.Models;
using backend.Application.Features.Media;
using backend.Application.Interfaces.Media;
using backend.Application.Interfaces.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.API.Controllers;

[ApiController]
[Route("media")]
public sealed class MediaController : ControllerBase
{
    private const long MaxUploadSizeBytes = 1024L * 1024 * 1024;

    private readonly UploadMedia _uploadMedia;
    private readonly IMediaRepository _mediaRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly TimeProvider _timeProvider;

    public MediaController(
        UploadMedia uploadMedia,
        IMediaRepository mediaRepository,
        IBlobStorageService blobStorageService,
        TimeProvider timeProvider)
    {
        _uploadMedia = uploadMedia;
        _mediaRepository = mediaRepository;
        _blobStorageService = blobStorageService;
        _timeProvider = timeProvider;
    }

    [Authorize]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
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

    [Authorize]
    [HttpGet("content")]
    [ProducesResponseType(typeof(PagedMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetContent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Brak autoryzacji (nieprawidłowy token)." });
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest(new { message = "Parametry page muszą być dodatnie; pageSize musi mieścić się w zakresie 1-100." });
        }

        var total = await _mediaRepository.CountByUserAsync(userId.Value, cancellationToken);
        var media = await _mediaRepository.GetByUserAsync(userId.Value, page, pageSize, cancellationToken);
        var registrations = await _mediaRepository.GetRegistrationsByMediaIdsAsync(
            media.Select(item => item.Id).ToArray(),
            cancellationToken);
        var items = media.Select(item => MapToResponse(
            item,
            item.MediaUrl is null ? null : _blobStorageService.GenerateSasUri(item.MediaUrl, TimeSpan.FromHours(24)),
            registrations.GetValueOrDefault(item.Id, Array.Empty<string>())))
            .ToArray();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new PagedMediaResponse(items, page, pageSize, total, totalPages));
    }

    [Authorize]
    [HttpGet("content/{mediaId:int}")]
    [ProducesResponseType(typeof(MediaItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContentItem(int mediaId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Brak autoryzacji (nieprawidłowy token)." });
        }

        var media = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (media == null)
        {
            return NotFound(new { message = "Nie znaleziono medium." });
        }

        if (media.UserId != userId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Brak dostępu do tego medium." });
        }

        var sasUrl = media.MediaUrl is null
            ? null
            : _blobStorageService.GenerateSasUri(media.MediaUrl, TimeSpan.FromHours(24));
        var registrations = await _mediaRepository.GetRegistrationsByMediaIdsAsync([media.Id], cancellationToken);
        return Ok(MapToResponse(
            media,
            sasUrl,
            registrations.GetValueOrDefault(media.Id, Array.Empty<string>())));
    }

    [Authorize]
    [HttpGet("content/{mediaId:int}/download")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadContentItem(int mediaId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Brak autoryzacji (nieprawidłowy token)." });
        }

        var media = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (media == null)
        {
            return NotFound(new { message = "Nie znaleziono medium." });
        }

        if (media.UserId != userId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Brak dostępu do tego medium." });
        }

        if (media.MediaUrl is null)
        {
            return NotFound(new { message = "Plik medium nie jest dostępny." });
        }

        var sasUrl = _blobStorageService.GenerateSasUri(media.MediaUrl, TimeSpan.FromMinutes(15));
        return Redirect(sasUrl);
    }

    [Authorize]
    [HttpPatch("content/{mediaId:int}")]
    [ProducesResponseType(typeof(MediaItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContentItem(
        int mediaId,
        [FromBody] UpdateMediaRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Brak autoryzacji (nieprawidłowy token)." });
        }

        if (request.Latitude.HasValue != request.Longitude.HasValue
            || request.Latitude is < -90m or > 90m
            || request.Longitude is < -180m or > 180m)
        {
            return BadRequest(new { message = "Nieprawidłowe współrzędne lokalizacji." });
        }

        var existing = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (existing == null)
        {
            return NotFound(new { message = "Nie znaleziono medium." });
        }

        if (existing.UserId != userId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Brak dostępu do tego medium." });
        }

        var updated = await _mediaRepository.UpdateMetadataAsync(
            mediaId,
            userId.Value,
            request.FileName,
            request.Comment ?? existing.Comment,
            request.RecordedAt ?? existing.RecordedAt,
            request.Latitude ?? existing.Location?.Latitude,
            request.Longitude ?? existing.Location?.Longitude,
            cancellationToken);

        if (!updated)
        {
            return NotFound(new { message = "Nie znaleziono medium." });
        }

        var media = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        var sasUrl = media?.MediaUrl is null
            ? null
            : _blobStorageService.GenerateSasUri(media.MediaUrl, TimeSpan.FromHours(24));
        var registrations = await _mediaRepository.GetRegistrationsByMediaIdsAsync([media!.Id], cancellationToken);
        return Ok(MapToResponse(
            media,
            sasUrl,
            registrations.GetValueOrDefault(media.Id, Array.Empty<string>())));
    }

    [Authorize]
    [HttpDelete("content/{mediaId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContentItem(int mediaId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "Brak autoryzacji (nieprawidłowy token)." });
        }

        var media = await _mediaRepository.GetByIdAsync(mediaId, cancellationToken);
        if (media == null)
        {
            return NotFound(new { message = "Nie znaleziono medium." });
        }

        if (media.UserId != userId.Value)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Brak dostępu do tego medium." });
        }

        var deleted = await _mediaRepository.DeleteAsync(mediaId, userId.Value, _timeProvider.GetUtcNow(), cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Nie znaleziono medium." });
        }

        if (media.MediaUrl is not null)
        {
            await _blobStorageService.DeleteBlobAsync(media.MediaUrl, cancellationToken);
        }

        return NoContent();
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

    private static MediaItemResponse MapToResponse(
        Domain.Media.File media,
        string? sasUrl,
        IReadOnlyList<string>? registrations = null)
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
            Registrations: registrations ?? Array.Empty<string>(),
            Ratings: Array.Empty<RegistrationRatingResponse>(),
            AnalysisStatus: media.AnalysisStatus.ToString().ToUpperInvariant());
    }
}
