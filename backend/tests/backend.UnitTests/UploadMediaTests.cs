using System.IO;
using backend.Application.Features.Media;
using backend.Application.Interfaces.Analysis;
using backend.Application.Interfaces.Media;
using backend.Application.Interfaces.Storage;
using backend.Domain.Locations;
using backend.Domain.Media;
using MediaFile = backend.Domain.Media.File;
using Microsoft.Extensions.Logging.Abstractions;

namespace backend.UnitTests;

public sealed class UploadMediaTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-18T12:00:00Z");

    [Fact]
    public async Task UploadMedia_SuccessfullyUploadsImageAndSavesToDatabase()
    {
        // Arrange
        var fakeStorage = new FakeBlobStorageService();
        var fakeRepo = new FakeMediaRepository();
        var fakeQueue = new FakeMediaAnalysisQueue();
        var timeProvider = new TestTimeProvider(Now);

        var useCase = new UploadMedia(fakeStorage, fakeRepo, fakeQueue, timeProvider, NullLogger<UploadMedia>.Instance);

        using var memoryStream = new MemoryStream([0xFF, 0xD8, 0xFF, 0x00]);
        var command = new UploadMediaCommand(
            UserId: 42,
            FileName: "car_photo.jpg",
            ContentType: "image/jpeg",
            FileStream: memoryStream,
            Latitude: 52.2297m,
            Longitude: 21.0122m,
            RecordedAt: Now.AddMinutes(-5),
            Comment: "Zdjęcie z trasy");

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        Assert.Equal(UploadMediaStatus.Success, result.Status);
        Assert.NotNull(result.Media);
        Assert.Equal("car_photo.jpg", result.Media.FileName);
        Assert.Equal(42, result.Media.UserId);
        Assert.Equal(FileState.Active, result.Media.State);
        Assert.Equal(Now, result.Media.UploadedAt);
        Assert.Equal("Zdjęcie z trasy", result.Media.Comment);
        Assert.NotNull(result.Media.Location);
        Assert.Equal(52.2297m, result.Media.Location.Latitude);
        Assert.Equal(21.0122m, result.Media.Location.Longitude);

        Assert.Single(fakeStorage.UploadedBlobs);
        Assert.NotNull(result.SasUrl);
        Assert.Single(fakeRepo.AddedMedia);
        Assert.Equal(1, fakeRepo.AddedMedia[0].Id);
        Assert.Contains(fakeRepo.ActivityRecords, r => r.Action == "UPLOAD" && r.MediaId == 1);
        Assert.Contains(1, fakeQueue.EnqueuedMediaIds);
    }

    [Fact]
    public async Task UploadMedia_RejectsUnsupportedExtensionOrMimeType()
    {
        // Arrange
        var fakeStorage = new FakeBlobStorageService();
        var fakeRepo = new FakeMediaRepository();
        var fakeQueue = new FakeMediaAnalysisQueue();
        var timeProvider = new TestTimeProvider(Now);

        var useCase = new UploadMedia(fakeStorage, fakeRepo, fakeQueue, timeProvider, NullLogger<UploadMedia>.Instance);

        using var memoryStream = new MemoryStream([1, 2, 3]);
        var command = new UploadMediaCommand(
            UserId: 42,
            FileName: "notes.pdf",
            ContentType: "application/pdf",
            FileStream: memoryStream);

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        Assert.Equal(UploadMediaStatus.UnsupportedMediaType, result.Status);
        Assert.Null(result.Media);
        Assert.Empty(fakeStorage.UploadedBlobs);
        Assert.Empty(fakeRepo.AddedMedia);
    }

    [Fact]
    public async Task UploadMedia_RejectsInvalidUserIdOrEmptyFile()
    {
        // Arrange
        var fakeStorage = new FakeBlobStorageService();
        var fakeRepo = new FakeMediaRepository();
        var fakeQueue = new FakeMediaAnalysisQueue();
        var timeProvider = new TestTimeProvider(Now);

        var useCase = new UploadMedia(fakeStorage, fakeRepo, fakeQueue, timeProvider, NullLogger<UploadMedia>.Instance);

        using var emptyStream = new MemoryStream([]);
        var command = new UploadMediaCommand(
            UserId: 0,
            FileName: "test.png",
            ContentType: "image/png",
            FileStream: emptyStream);

        // Act
        var result = await useCase.ExecuteAsync(command);

        // Assert
        Assert.Equal(UploadMediaStatus.InvalidInput, result.Status);
    }

    [Theory]
    [InlineData("upload.exe", "image/png")]
    [InlineData("upload.png", "image/jpeg")]
    public async Task UploadMedia_RejectsMismatchedExtensionAndContentType(string fileName, string contentType)
    {
        var useCase = new UploadMedia(
            new FakeBlobStorageService(),
            new FakeMediaRepository(),
            new FakeMediaAnalysisQueue(),
            new TestTimeProvider(Now),
            NullLogger<UploadMedia>.Instance);

        using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var result = await useCase.ExecuteAsync(new UploadMediaCommand(42, fileName, contentType, stream));

        Assert.Equal(UploadMediaStatus.UnsupportedMediaType, result.Status);
    }

    [Fact]
    public async Task UploadMedia_RejectsImageWithInvalidSignature()
    {
        var storage = new FakeBlobStorageService();
        var useCase = new UploadMedia(
            storage,
            new FakeMediaRepository(),
            new FakeMediaAnalysisQueue(),
            new TestTimeProvider(Now),
            NullLogger<UploadMedia>.Instance);

        using var stream = new MemoryStream([1, 2, 3, 4]);
        var result = await useCase.ExecuteAsync(new UploadMediaCommand(42, "image.jpg", "image/jpeg", stream));

        Assert.Equal(UploadMediaStatus.UnsupportedMediaType, result.Status);
        Assert.Empty(storage.UploadedBlobs);
    }

    [Fact]
    public async Task UploadMedia_RejectsIncompleteOrOutOfRangeCoordinates()
    {
        var storage = new FakeBlobStorageService();
        var useCase = new UploadMedia(
            storage,
            new FakeMediaRepository(),
            new FakeMediaAnalysisQueue(),
            new TestTimeProvider(Now),
            NullLogger<UploadMedia>.Instance);

        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);
        var incompleteResult = await useCase.ExecuteAsync(new UploadMediaCommand(
            42, "image.jpg", "image/jpeg", stream, Latitude: 52.0m));

        using var secondStream = new MemoryStream([0xFF, 0xD8, 0xFF]);
        var outOfRangeResult = await useCase.ExecuteAsync(new UploadMediaCommand(
            42, "image.jpg", "image/jpeg", secondStream, Latitude: 91.0m, Longitude: 0m));

        Assert.Equal(UploadMediaStatus.InvalidInput, incompleteResult.Status);
        Assert.Equal(UploadMediaStatus.InvalidInput, outOfRangeResult.Status);
        Assert.Empty(storage.UploadedBlobs);
    }

    [Fact]
    public async Task UploadMedia_RemovesUploadedBlobWhenDatabaseWriteFails()
    {
        var storage = new FakeBlobStorageService();
        var repository = new FakeMediaRepository { ThrowOnAdd = true };
        var useCase = new UploadMedia(
            storage,
            repository,
            new FakeMediaAnalysisQueue(),
            new TestTimeProvider(Now),
            NullLogger<UploadMedia>.Instance);

        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new UploadMediaCommand(42, "image.jpg", "image/jpeg", stream)));

        Assert.Single(storage.UploadedBlobs);
        Assert.Equal(storage.UploadedBlobs[0].Name, Assert.Single(storage.DeletedBlobs));
    }

    [Fact]
    public async Task UploadMedia_DoesNotExposeStorageExceptionDetails()
    {
        var storage = new FakeBlobStorageService
        {
            UploadException = new InvalidOperationException("internal storage endpoint")
        };
        var useCase = new UploadMedia(
            storage,
            new FakeMediaRepository(),
            new FakeMediaAnalysisQueue(),
            new TestTimeProvider(Now),
            NullLogger<UploadMedia>.Instance);

        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);
        var result = await useCase.ExecuteAsync(new UploadMediaCommand(42, "image.jpg", "image/jpeg", stream));

        Assert.Equal(UploadMediaStatus.StorageFailure, result.Status);
        Assert.DoesNotContain("internal storage endpoint", result.ErrorMessage);
    }

    [Fact]
    public async Task UploadMedia_PropagatesStorageCancellation()
    {
        var storage = new FakeBlobStorageService
        {
            UploadException = new OperationCanceledException()
        };
        var useCase = new UploadMedia(
            storage,
            new FakeMediaRepository(),
            new FakeMediaAnalysisQueue(),
            new TestTimeProvider(Now),
            NullLogger<UploadMedia>.Instance);

        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(new UploadMediaCommand(42, "image.jpg", "image/jpeg", stream)));
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeBlobStorageService : IBlobStorageService
    {
        public List<(string Name, string ContentType)> UploadedBlobs { get; } = [];
        public List<string> DeletedBlobs { get; } = [];
        public Exception? UploadException { get; init; }

        public Task<string> UploadBlobAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            UploadedBlobs.Add((blobName, contentType));
            if (UploadException is not null)
            {
                return Task.FromException<string>(UploadException);
            }

            return Task.FromResult($"https://fakeblob.core.windows.net/rejka-media/{blobName}");
        }

        public string GenerateSasUri(string blobName, TimeSpan validFor)
        {
            return $"https://fakeblob.core.windows.net/rejka-media/{blobName}?sig=fakeSasToken";
        }

        public Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default)
        {
            DeletedBlobs.Add(blobName);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMediaRepository : IMediaRepository
    {
        private int _currentId = 0;
        public bool ThrowOnAdd { get; init; }
        public List<MediaFile> AddedMedia { get; } = [];
        public List<(int MediaId, string Action, DateTimeOffset OccurredAt)> ActivityRecords { get; } = [];

        public Task<int> AddWithActivityRecordAsync(
            MediaFile media,
            string action,
            DateTimeOffset occurredAt,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnAdd)
            {
                return Task.FromException<int>(new InvalidOperationException("Database write failed."));
            }

            _currentId++;
            media.SetId(_currentId);
            AddedMedia.Add(media);
            ActivityRecords.Add((_currentId, action, occurredAt));
            return Task.FromResult(_currentId);
        }

        public Task<MediaFile?> GetByIdAsync(int mediaId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AddedMedia.Find(m => m.Id == mediaId));
        }

    }

    private sealed class FakeMediaAnalysisQueue : IMediaAnalysisQueue
    {
        public List<int> EnqueuedMediaIds { get; } = [];

        public ValueTask EnqueueAnalysisAsync(int mediaId, CancellationToken cancellationToken = default)
        {
            EnqueuedMediaIds.Add(mediaId);
            return ValueTask.CompletedTask;
        }
    }
}
