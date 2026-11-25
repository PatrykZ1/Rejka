using backend.Application.Interfaces.Analysis;
using backend.Application.Interfaces.Storage;
using backend.Infrastructure.Analysis;

namespace backend.API;

public sealed class MediaAnalysisWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<MediaAnalysisWorker> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<MediaAnalysisWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMediaAnalysisRepository>();
                var job = await repository.ClaimNextAsync(stoppingToken);
                if (job is null)
                {
                    await Task.Delay(IdleDelay, stoppingToken);
                    continue;
                }

                var processing = ProcessAsync(job, stoppingToken);
                var heartbeat = MaintainLeaseAsync(job.MediaId, processing, stoppingToken);
                await Task.WhenAll(processing, heartbeat);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Media analysis worker iteration failed.");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }
    }

    private async Task MaintainLeaseAsync(
        int mediaId,
        Task processing,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!processing.IsCompleted
            && await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (processing.IsCompleted)
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMediaAnalysisRepository>();
                if (!await repository.HeartbeatAsync(mediaId, cancellationToken))
                {
                    _logger.LogWarning(
                        "Media analysis lease for {MediaId} is no longer active.",
                        mediaId);
                    return;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to renew media analysis lease for {MediaId}.", mediaId);
            }
        }
    }

    private async Task ProcessAsync(MediaAnalysisWorkItem job, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMediaAnalysisRepository>();

        try
        {
            var storage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
            var recognizer = scope.ServiceProvider.GetRequiredService<IPlateRecognitionService>();
            if (IsVideo(job.BlobName))
            {
                var extractor = scope.ServiceProvider.GetRequiredService<IVideoFrameExtractor>();
                await ProcessVideoAsync(job, storage, recognizer, repository, extractor, cancellationToken);
            }
            else
            {
                await using var image = await storage.DownloadBlobAsync(job.BlobName, cancellationToken);
                var plates = await recognizer.RecognizeAsync(image, cancellationToken);
                await repository.CompleteAsync(job.MediaId, plates, cancellationToken);
                _logger.LogInformation(
                    "Media analysis completed for {MediaId}; accepted {PlateCount} plate readings.",
                    job.MediaId,
                    plates.Count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Media analysis failed for {MediaId} on attempt {Attempt}.", job.MediaId, job.AttemptCount);
            await repository.FailAsync(job.MediaId, GetErrorCode(exception), cancellationToken);
        }
    }

    private async Task ProcessVideoAsync(
        MediaAnalysisWorkItem job,
        IBlobStorageService storage,
        IPlateRecognitionService recognizer,
        IMediaAnalysisRepository repository,
        IVideoFrameExtractor extractor,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(job.BlobName);
        var temporaryDirectory = Directory.CreateTempSubdirectory("rejka-video-");
        try
        {
            var videoPath = Path.Combine(temporaryDirectory.FullName, $"source{extension}");
            await using (var source = await storage.DownloadBlobAsync(job.BlobName, cancellationToken))
            await using (var destination = new FileStream(
                videoPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            var seenRegistrations = new HashSet<string>(StringComparer.Ordinal);
            var frameCount = 0;
            await foreach (var frameBytes in extractor.ExtractFramesAsync(videoPath, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                frameCount++;
                await using var frame = new MemoryStream(frameBytes, writable: false);
                var readings = await recognizer.RecognizeAsync(frame, cancellationToken);
                var newReadings = new List<PlateRecognition>();
                foreach (var reading in readings)
                {
                    var registration = reading.Registration.Trim().ToUpperInvariant();
                    if (seenRegistrations.Add(registration))
                    {
                        newReadings.Add(new PlateRecognition(
                            registration,
                            reading.DetectionConfidence,
                            reading.RecognitionConfidence));
                    }
                }

                if (newReadings.Count > 0)
                {
                    await repository.SaveRecognizedPlatesAsync(
                        job.MediaId,
                        newReadings,
                        cancellationToken);
                    _logger.LogInformation(
                        "Video analysis found {NewPlateCount} new plate readings for {MediaId} at sampled frame {FrameNumber}.",
                        newReadings.Count,
                        job.MediaId,
                        frameCount);
                }
                else if (frameCount % 60 == 0)
                {
                    _logger.LogInformation(
                        "Video analysis processed {FrameCount} sampled frames for {MediaId}.",
                        frameCount,
                        job.MediaId);
                }
            }

            await repository.CompleteAsync(job.MediaId, Array.Empty<PlateRecognition>(), cancellationToken);
            _logger.LogInformation(
                "Video analysis completed for {MediaId}; processed {FrameCount} sampled frames and found {PlateCount} unique plates.",
                job.MediaId,
                frameCount,
                seenRegistrations.Count);
        }
        finally
        {
            temporaryDirectory.Delete(recursive: true);
        }
    }

    private static bool IsVideo(string blobName)
    {
        var extension = Path.GetExtension(blobName);
        return extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            VideoDurationLimitExceededException => "VIDEO_TOO_LONG",
            InvalidDataException or FormatException => "INVALID_MEDIA",
            FileNotFoundException => "MODEL_OR_BLOB_MISSING",
            System.ComponentModel.Win32Exception => "FFMPEG_NOT_INSTALLED",
            _ => "ANALYSIS_FAILED"
        };
    }
}
