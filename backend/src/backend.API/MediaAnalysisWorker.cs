using backend.Application.Interfaces.Analysis;
using backend.Application.Interfaces.Storage;

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

                await ProcessAsync(job, stoppingToken);
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

    private async Task ProcessAsync(MediaAnalysisWorkItem job, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMediaAnalysisRepository>();

        try
        {
            var storage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
            var recognizer = scope.ServiceProvider.GetRequiredService<IPlateRecognitionService>();
            await using var image = await storage.DownloadBlobAsync(job.BlobName, cancellationToken);
            var plates = await recognizer.RecognizeAsync(image, cancellationToken);
            await repository.CompleteAsync(job.MediaId, plates, cancellationToken);
            _logger.LogInformation(
                "Media analysis completed for {MediaId}; accepted {PlateCount} plate readings.",
                job.MediaId,
                plates.Count);
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

    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            InvalidDataException or FormatException => "INVALID_IMAGE",
            FileNotFoundException => "MODEL_OR_BLOB_MISSING",
            _ => "ANALYSIS_FAILED"
        };
    }
}
