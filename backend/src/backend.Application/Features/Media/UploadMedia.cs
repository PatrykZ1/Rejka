using backend.Application.Interfaces.Media;
using backend.Application.Interfaces.Storage;
using backend.Domain.Locations;
using backend.Domain.Media;
using System.Buffers.Binary;
using Microsoft.Extensions.Logging;

namespace backend.Application.Features.Media;

public enum UploadMediaStatus
{
    Success,
    InvalidInput,
    UnsupportedMediaType,
    StorageFailure
}

public sealed record UploadMediaCommand(
    int UserId,
    string FileName,
    string ContentType,
    Stream FileStream,
    decimal? Latitude = null,
    decimal? Longitude = null,
    DateTimeOffset? RecordedAt = null,
    string? Comment = null);

public sealed record UploadMediaResult(
    UploadMediaStatus Status,
    Domain.Media.File? Media = null,
    string? SasUrl = null,
    string? ErrorMessage = null);

public sealed class UploadMedia
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IMediaRepository _mediaRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UploadMedia> _logger;

    public UploadMedia(
        IBlobStorageService blobStorageService,
        IMediaRepository mediaRepository,
        TimeProvider timeProvider,
        ILogger<UploadMedia> logger)
    {
        _blobStorageService = blobStorageService;
        _mediaRepository = mediaRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<UploadMediaResult> ExecuteAsync(UploadMediaCommand command, CancellationToken cancellationToken = default)
    {
        if (command.UserId <= 0)
        {
            return new UploadMediaResult(UploadMediaStatus.InvalidInput, ErrorMessage: "Nieprawidłowy identyfikator użytkownika.");
        }

        if (string.IsNullOrWhiteSpace(command.FileName)
            || command.FileStream == null
            || !command.FileStream.CanRead
            || !command.FileStream.CanSeek
            || command.FileStream.Length == 0)
        {
            return new UploadMediaResult(UploadMediaStatus.InvalidInput, ErrorMessage: "Plik jest wymagany i nie może być pusty.");
        }

        if (command.Latitude.HasValue != command.Longitude.HasValue
            || command.Latitude is < -90m or > 90m
            || command.Longitude is < -180m or > 180m)
        {
            return new UploadMediaResult(UploadMediaStatus.InvalidInput, ErrorMessage: "Nieprawidłowe współrzędne lokalizacji.");
        }

        var normalizedContentType = GetNormalizedContentType(command.FileName, command.ContentType);
        if (normalizedContentType is null
            || !await HasValidMediaSignatureAsync(command.FileStream, normalizedContentType, cancellationToken))
        {
            return new UploadMediaResult(UploadMediaStatus.UnsupportedMediaType, ErrorMessage: "Nieobsługiwany format pliku. Dozwolone formaty to JPG, PNG, MP4 oraz MOV.");
        }

        var safeFileName = Path.GetFileName(command.FileName);
        var blobName = $"media/{command.UserId}/{Guid.NewGuid():N}_{safeFileName}";

        string sasUrl;
        try
        {
            sasUrl = _blobStorageService.GenerateSasUri(blobName, TimeSpan.FromHours(24));
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                throw;
            }

            _logger.LogError(ex, "Failed to generate a download URL for uploaded media.");
            return new UploadMediaResult(UploadMediaStatus.StorageFailure, ErrorMessage: "Nie udało się przygotować adresu pliku.");
        }

        try
        {
            await _blobStorageService.UploadBlobAsync(blobName, command.FileStream, normalizedContentType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload media blob {BlobName}.", blobName);
            await DeleteBlobAfterFailureAsync(blobName, ex);
            if (ex is OperationCanceledException)
            {
                throw;
            }

            return new UploadMediaResult(UploadMediaStatus.StorageFailure, ErrorMessage: "Nie udało się zapisać pliku w magazynie obiektowym.");
        }

        var now = _timeProvider.GetUtcNow();
        var location = command.Latitude.HasValue
            ? new Location(command.Latitude.Value, command.Longitude!.Value)
            : null;

        var media = new FileImage(
            fileName: safeFileName,
            userId: command.UserId,
            mediaUrl: blobName,
            uploadedAt: now,
            recordedAt: command.RecordedAt,
            comment: command.Comment ?? string.Empty,
            locationId: null,
            location: location);

        int mediaId;
        try
        {
            var analyzeMedia = normalizedContentType is "image/jpeg"
                or "image/png"
                or "video/mp4"
                or "video/quicktime";
            mediaId = await _mediaRepository.AddWithActivityRecordAsync(
                media,
                "UPLOAD",
                now,
                enqueueMediaAnalysis: analyzeMedia,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save uploaded media {BlobName}; removing the uploaded blob.", blobName);
            await DeleteBlobAfterFailureAsync(blobName, ex);
            throw;
        }

        media.SetId(mediaId);
        if (normalizedContentType is "image/jpeg" or "image/png" or "video/mp4" or "video/quicktime")
        {
            media.SetAnalysisStatus(Domain.Media.MediaAnalysisStatus.Pending);
        }

        return new UploadMediaResult(UploadMediaStatus.Success, Media: media, SasUrl: sasUrl);
    }

    private async Task DeleteBlobAfterFailureAsync(string blobName, Exception operationException)
    {
        try
        {
            await _blobStorageService.DeleteBlobAsync(blobName, CancellationToken.None);
        }
        catch (Exception cleanupException)
        {
            _logger.LogError(cleanupException, "Failed to remove blob {BlobName} after an upload failure.", blobName);
            throw new AggregateException("The operation failed and the uploaded blob could not be removed.", operationException, cleanupException);
        }
    }

    private static string? GetNormalizedContentType(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        if ((extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            && (string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contentType, "image/pjpeg", StringComparison.OrdinalIgnoreCase)))
        {
            return "image/jpeg";
        }

        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            && string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            return "image/png";
        }

        if (extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
            && string.Equals(contentType, "video/mp4", StringComparison.OrdinalIgnoreCase))
        {
            return "video/mp4";
        }

        if (extension.Equals(".mov", StringComparison.OrdinalIgnoreCase)
            && string.Equals(contentType, "video/quicktime", StringComparison.OrdinalIgnoreCase))
        {
            return "video/quicktime";
        }

        return null;
    }

    private static async Task<bool> HasValidMediaSignatureAsync(
        Stream stream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            var header = new byte[16];
            var bytesRead = 0;
            while (bytesRead < header.Length)
            {
                var read = await stream.ReadAsync(header.AsMemory(bytesRead), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            if (contentType == "image/png")
            {
                return bytesRead >= 8
                    && header[0] == 0x89
                    && header[1] == 0x50
                    && header[2] == 0x4E
                    && header[3] == 0x47
                    && header[4] == 0x0D
                    && header[5] == 0x0A
                    && header[6] == 0x1A
                    && header[7] == 0x0A;
            }

            if (contentType == "image/jpeg")
            {
                return bytesRead >= 3
                    && header[0] == 0xFF
                    && header[1] == 0xD8
                    && header[2] == 0xFF;
            }

            if (contentType is "video/mp4" or "video/quicktime")
            {
                if (bytesRead < header.Length
                    || !header.AsSpan(4, 4).SequenceEqual("ftyp"u8))
                {
                    return false;
                }

                var boxSize = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0, 4));
                if (boxSize < header.Length || boxSize > stream.Length)
                {
                    return false;
                }

                var majorBrand = System.Text.Encoding.ASCII.GetString(header, 8, 4);
                return contentType == "video/mp4"
                    ? majorBrand is "isom" or "iso2" or "mp41" or "mp42" or "avc1" or "M4V " or "dash"
                    : majorBrand == "qt  ";
            }

            return false;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}
