using backend.Application.Interfaces.Analysis;
using Microsoft.Extensions.Logging;

namespace backend.Infrastructure.Analysis;

public sealed class MediaAnalysisQueue : IMediaAnalysisQueue
{
    private readonly ILogger<MediaAnalysisQueue> _logger;

    public MediaAnalysisQueue(ILogger<MediaAnalysisQueue> logger)
    {
        _logger = logger;
    }

    public ValueTask EnqueueAnalysisAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Zlecono analizę pliku w tle dla MediaId: {MediaId}", mediaId);
        return ValueTask.CompletedTask;
    }
}
