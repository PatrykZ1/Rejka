namespace backend.Application.Interfaces.Analysis;

public interface IMediaAnalysisQueue
{
    ValueTask EnqueueAnalysisAsync(int mediaId, CancellationToken cancellationToken = default);
}
