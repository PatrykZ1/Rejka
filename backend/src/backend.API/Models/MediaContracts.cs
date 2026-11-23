using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace backend.API.Models;

public sealed record RegistrationRatingResponse(
    string Registration,
    int Rating);

public sealed record MediaItemResponse(
    int Id,
    int UserId,
    string? Url,
    string FileName,
    decimal? Latitude,
    decimal? Longitude,
    string? RecordedAt,
    string? UploadedAt,
    string Comment,
    IReadOnlyList<string> Registrations,
    IReadOnlyList<RegistrationRatingResponse> Ratings);

public sealed class UploadMediaRequest
{
    [Required]
    public required IFormFile File { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTimeOffset? RecordedAt { get; set; }

    public string? Comment { get; set; }
}

public sealed class UpdateMediaRequest
{
    public string? FileName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset? RecordedAt { get; set; }
    public string? Comment { get; set; }
}

public sealed record PagedMediaResponse(
    IReadOnlyList<MediaItemResponse> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);
