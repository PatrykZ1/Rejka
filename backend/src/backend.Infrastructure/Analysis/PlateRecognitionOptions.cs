namespace backend.Infrastructure.Analysis;

public sealed class PlateRecognitionOptions
{
    public string DetectorModelPath { get; set; } = "Models/plate_detector.onnx";
    public string OcrModelPath { get; set; } = "Models/plate_ocr.onnx";
    public float DetectionConfidenceThreshold { get; set; } = 0.20f;
    public float NmsIouThreshold { get; set; } = 0.45f;
    public float MinimumOcrConfidence { get; set; } = 0.85f;
    public int MaximumDetections { get; set; } = 20;
}
