namespace backend.Application.Interfaces.Analysis;

public interface IVideoFrameExtractor
{
    IAsyncEnumerable<byte[]> ExtractFramesAsync(
        string videoPath,
        CancellationToken cancellationToken = default);
}
