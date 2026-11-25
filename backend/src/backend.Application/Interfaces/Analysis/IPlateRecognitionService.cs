namespace backend.Application.Interfaces.Analysis;

public sealed record PlateRecognition(
    string Registration,
    float DetectionConfidence,
    float RecognitionConfidence);

public interface IPlateRecognitionService
{
    Task<IReadOnlyList<PlateRecognition>> RecognizeAsync(
        Stream image,
        CancellationToken cancellationToken = default);
}
