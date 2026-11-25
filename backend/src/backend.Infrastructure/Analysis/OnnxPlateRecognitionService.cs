using System.Text;
using backend.Application.Interfaces.Analysis;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace backend.Infrastructure.Analysis;

public sealed class OnnxPlateRecognitionService : IPlateRecognitionService, IDisposable
{
    private const int DetectorSize = 640;
    private const int OcrWidth = 128;
    private const int OcrHeight = 64;
    private const int OcrSlots = 10;
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_";
    private const int MaximumImageDimension = 10000;
    private const long MaximumImagePixels = 40_000_000;

    private readonly InferenceSession _detector;
    private readonly InferenceSession _ocr;
    private readonly string _detectorInputName;
    private readonly string _detectorOutputName;
    private readonly string _ocrInputName;
    private readonly string _ocrOutputName;
    private readonly PlateRecognitionOptions _options;

    public OnnxPlateRecognitionService(IOptions<PlateRecognitionOptions> options)
    {
        _options = options.Value;
        ValidateOptions(_options);

        _detector = new InferenceSession(ResolveModelPath(_options.DetectorModelPath));
        _ocr = new InferenceSession(ResolveModelPath(_options.OcrModelPath));
        _detectorInputName = GetSingleTensorName(_detector.InputMetadata, "detector input");
        _detectorOutputName = GetSingleTensorName(_detector.OutputMetadata, "detector output");
        _ocrInputName = GetNamedTensor(_ocr.InputMetadata, "input", "OCR input");
        _ocrOutputName = GetNamedTensor(_ocr.OutputMetadata, "plate", "OCR plate output");

        ValidateShape(_detector.InputMetadata[_detectorInputName].Dimensions, [1, 3, DetectorSize, DetectorSize], "detector input");
        ValidateShape(_detector.OutputMetadata[_detectorOutputName].Dimensions, [1, 5, 8400], "detector output");
        ValidateBatchShape(_ocr.InputMetadata[_ocrInputName].Dimensions, [OcrHeight, OcrWidth, 3], "OCR input");
        if (!_ocr.OutputMetadata[_ocrOutputName].Dimensions.Skip(1).SequenceEqual([OcrSlots, Alphabet.Length]))
        {
            throw new InvalidOperationException("The OCR output must contain 10 character slots with 37 classes each.");
        }
        if (!_ocr.OutputMetadata.ContainsKey("region"))
        {
            throw new InvalidOperationException("The OCR model does not expose the expected region output.");
        }
    }

    public async Task<IReadOnlyList<PlateRecognition>> RecognizeAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        using var buffer = new MemoryStream();
        await imageStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        using var image = DecodeOrientedImage(buffer);

        var transform = CreateLetterboxTransform(image.Width, image.Height);
        using var letterboxed = CreateLetterboxedImage(image, transform);
        var detectorInput = CreateDetectorTensor(letterboxed);
        using var detectorResults = _detector.Run([
            NamedOnnxValue.CreateFromTensor(_detectorInputName, detectorInput)
        ]);
        var detectorOutput = detectorResults.Single(result => result.Name == _detectorOutputName).AsTensor<float>();
        var boxes = DecodeDetections(detectorOutput, transform, image.Width, image.Height);

        var results = new List<PlateRecognition>(boxes.Count);
        var accepted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var box in boxes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var crop = CreatePlateCrop(image, box);
            if (crop is null)
            {
                continue;
            }

            var (registration, confidence) = RecognizeCrop(crop);
            if (registration is null
                || confidence < _options.MinimumOcrConfidence
                || !accepted.Add(registration))
            {
                continue;
            }

            results.Add(new PlateRecognition(registration, box.Confidence, confidence));
        }

        return results;
    }

    public void Dispose()
    {
        _detector.Dispose();
        _ocr.Dispose();
    }

    private static void ValidateOptions(PlateRecognitionOptions options)
    {
        if (options.DetectionConfidenceThreshold is < 0 or > 1
            || options.NmsIouThreshold is < 0 or > 1
            || options.MinimumOcrConfidence is < 0 or > 1
            || options.MaximumDetections is < 1 or > 100)
        {
            throw new InvalidOperationException("Plate recognition thresholds or limits are out of range.");
        }
    }

    private static string ResolveModelPath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        if (!System.IO.File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Plate recognition model was not found: {fullPath}", fullPath);
        }

        return fullPath;
    }

    private static string GetSingleTensorName(IReadOnlyDictionary<string, NodeMetadata> metadata, string description)
    {
        if (metadata.Count != 1)
        {
            throw new InvalidOperationException($"Expected one {description}, found {metadata.Count}.");
        }

        return metadata.Keys.Single();
    }

    private static string GetNamedTensor(
        IReadOnlyDictionary<string, NodeMetadata> metadata,
        string name,
        string description)
    {
        if (!metadata.ContainsKey(name))
        {
            throw new InvalidOperationException($"The model does not expose the expected {description} named '{name}'.");
        }

        return name;
    }

    private static void ValidateShape(IReadOnlyList<int> actual, int[] expected, string description)
    {
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(
                $"Unexpected {description} shape [{string.Join(",", actual)}]; expected [{string.Join(",", expected)}].");
        }
    }

    private static void ValidateBatchShape(IReadOnlyList<int> actual, int[] expected, string description)
    {
        if (actual.Count != expected.Length + 1
            || (actual[0] != -1 && actual[0] != 1)
            || !actual.Skip(1).SequenceEqual(expected))
        {
            throw new InvalidOperationException(
                $"Unexpected {description} shape [{string.Join(",", actual)}]; expected [batch,{string.Join(",", expected)}].");
        }
    }

    private static LetterboxTransform CreateLetterboxTransform(int width, int height)
    {
        var scale = Math.Min(DetectorSize / (double)width, DetectorSize / (double)height);
        var resizedWidth = Math.Clamp((int)Math.Round(width * scale), 1, DetectorSize);
        var resizedHeight = Math.Clamp((int)Math.Round(height * scale), 1, DetectorSize);
        var left = (DetectorSize - resizedWidth) / 2;
        var top = (DetectorSize - resizedHeight) / 2;
        return new LetterboxTransform(
            resizedWidth,
            resizedHeight,
            left,
            top,
            resizedWidth / (double)width,
            resizedHeight / (double)height);
    }

    private static SKBitmap DecodeOrientedImage(Stream stream)
    {
        using var codec = SKCodec.Create(stream)
            ?? throw new InvalidDataException("The uploaded image format could not be decoded.");
        var width = codec.Info.Width;
        var height = codec.Info.Height;
        if (width <= 0 || height <= 0
            || width > MaximumImageDimension
            || height > MaximumImageDimension
            || (long)width * height > MaximumImagePixels)
        {
            throw new InvalidDataException("The image dimensions exceed the supported limits.");
        }

        if (codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
        {
            throw new InvalidDataException("Only JPEG and PNG images can be analyzed.");
        }

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var decoded = new SKBitmap(info);
        if (codec.GetPixels(info, decoded.GetPixels()) != SKCodecResult.Success)
        {
            throw new InvalidDataException("The uploaded image could not be fully decoded.");
        }

        return ApplyOrientation(decoded, codec.EncodedOrigin);
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsAxes = origin is SKEncodedOrigin.LeftTop
            or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom
            or SKEncodedOrigin.LeftBottom;
        var width = swapsAxes ? source.Height : source.Width;
        var height = swapsAxes ? source.Width : source.Height;
        var result = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var (targetX, targetY) = origin switch
                {
                    SKEncodedOrigin.TopRight => (source.Width - 1 - x, y),
                    SKEncodedOrigin.BottomRight => (source.Width - 1 - x, source.Height - 1 - y),
                    SKEncodedOrigin.BottomLeft => (x, source.Height - 1 - y),
                    SKEncodedOrigin.LeftTop => (y, x),
                    SKEncodedOrigin.RightTop => (source.Height - 1 - y, x),
                    SKEncodedOrigin.RightBottom => (source.Height - 1 - y, source.Width - 1 - x),
                    SKEncodedOrigin.LeftBottom => (y, source.Width - 1 - x),
                    _ => (x, y)
                };
                result.SetPixel(targetX, targetY, source.GetPixel(x, y));
            }
        }

        return result;
    }

    private static SKBitmap CreateLetterboxedImage(SKBitmap image, LetterboxTransform transform)
    {
        using var resized = image.Resize(
            new SKImageInfo(transform.ResizedWidth, transform.ResizedHeight, SKColorType.Rgba8888, SKAlphaType.Premul),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None))
            ?? throw new InvalidDataException("The image could not be resized for plate detection.");
        var canvasBitmap = new SKBitmap(new SKImageInfo(DetectorSize, DetectorSize, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using var canvas = new SKCanvas(canvasBitmap);
        canvas.Clear(new SKColor(114, 114, 114));
        canvas.DrawBitmap(resized, transform.Left, transform.Top);
        canvas.Flush();
        return canvasBitmap;
    }

    private static DenseTensor<float> CreateDetectorTensor(SKBitmap image)
    {
        var tensor = new DenseTensor<float>([1, 3, DetectorSize, DetectorSize]);
        for (var y = 0; y < DetectorSize; y++)
        {
            for (var x = 0; x < DetectorSize; x++)
            {
                var pixel = image.GetPixel(x, y);
                tensor[0, 0, y, x] = pixel.Red / 255f;
                tensor[0, 1, y, x] = pixel.Green / 255f;
                tensor[0, 2, y, x] = pixel.Blue / 255f;
            }
        }

        return tensor;
    }

    private List<Detection> DecodeDetections(
        Tensor<float> output,
        LetterboxTransform transform,
        int imageWidth,
        int imageHeight)
    {
        var values = output.ToArray();
        var candidates = new List<Detection>();
        const int anchors = 8400;

        for (var index = 0; index < anchors; index++)
        {
            var confidence = values[4 * anchors + index];
            if (!float.IsFinite(confidence) || confidence < _options.DetectionConfidenceThreshold)
            {
                continue;
            }

            var centerX = values[index];
            var centerY = values[anchors + index];
            var width = values[2 * anchors + index];
            var height = values[3 * anchors + index];
            if (!float.IsFinite(centerX) || !float.IsFinite(centerY)
                || !float.IsFinite(width) || !float.IsFinite(height)
                || width <= 0 || height <= 0)
            {
                continue;
            }

            var left = (centerX - width / 2 - transform.Left) / transform.ScaleX;
            var top = (centerY - height / 2 - transform.Top) / transform.ScaleY;
            var right = (centerX + width / 2 - transform.Left) / transform.ScaleX;
            var bottom = (centerY + height / 2 - transform.Top) / transform.ScaleY;
            left = Math.Clamp(left, 0, imageWidth);
            top = Math.Clamp(top, 0, imageHeight);
            right = Math.Clamp(right, 0, imageWidth);
            bottom = Math.Clamp(bottom, 0, imageHeight);
            if (right <= left || bottom <= top)
            {
                continue;
            }

            candidates.Add(new Detection(left, top, right, bottom, confidence));
        }

        candidates.Sort((left, right) => right.Confidence.CompareTo(left.Confidence));
        var kept = new List<Detection>();
        foreach (var candidate in candidates)
        {
            if (kept.All(existing => IntersectionOverUnion(candidate, existing) < _options.NmsIouThreshold))
            {
                kept.Add(candidate);
                if (kept.Count == _options.MaximumDetections)
                {
                    break;
                }
            }
        }

        return kept;
    }

    private static SKBitmap? CreatePlateCrop(SKBitmap image, Detection detection)
    {
        var width = detection.Right - detection.Left;
        var height = detection.Bottom - detection.Top;
        var x0 = Math.Clamp((int)Math.Floor(detection.Left - width * 0.25), 0, image.Width - 1);
        var insetY = height * 1.24 * 0.06;
        var y0 = Math.Clamp((int)Math.Floor(detection.Top - height * 0.12 + insetY), 0, image.Height - 1);
        var x1 = Math.Clamp((int)Math.Ceiling(detection.Right + width * 0.25), x0 + 1, image.Width);
        var y1 = Math.Clamp((int)Math.Ceiling(detection.Bottom + height * 0.12 - insetY), y0 + 1, image.Height);
        if (x1 - x0 < 20 || y1 - y0 < 8)
        {
            return null;
        }

        var crop = new SKBitmap(new SKImageInfo(x1 - x0, y1 - y0, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(crop);
        canvas.DrawBitmap(image, new SKRect(x0, y0, x1, y1), new SKRect(0, 0, crop.Width, crop.Height));
        canvas.Flush();
        return crop;
    }

    private (string? Registration, float Confidence) RecognizeCrop(SKBitmap crop)
    {
        using var resized = crop.Resize(
            new SKImageInfo(OcrWidth, OcrHeight, SKColorType.Rgba8888, SKAlphaType.Premul),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None))
            ?? throw new InvalidDataException("The detected plate crop could not be resized for OCR.");
        var tensor = new DenseTensor<byte>([1, OcrHeight, OcrWidth, 3]);
        for (var y = 0; y < OcrHeight; y++)
        {
            for (var x = 0; x < OcrWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);
                tensor[0, y, x, 0] = pixel.Red;
                tensor[0, y, x, 1] = pixel.Green;
                tensor[0, y, x, 2] = pixel.Blue;
            }
        }

        using var outputs = _ocr.Run([
            NamedOnnxValue.CreateFromTensor(_ocrInputName, tensor)
        ]);
        var scores = outputs.Single(result => result.Name == _ocrOutputName).AsTensor<float>().ToArray();
        if (scores.Length != OcrSlots * Alphabet.Length)
        {
            throw new InvalidOperationException($"Unexpected OCR output value count: {scores.Length}.");
        }

        var text = new StringBuilder(OcrSlots);
        var logConfidence = 0d;
        var characterCount = 0;
        for (var slot = 0; slot < OcrSlots; slot++)
        {
            var start = slot * Alphabet.Length;
            var max = double.NegativeInfinity;
            var best = 0;
            for (var character = 0; character < Alphabet.Length; character++)
            {
                var value = scores[start + character];
                if (!float.IsFinite(value))
                {
                    throw new InvalidOperationException("OCR output contains a non-finite value.");
                }

                if (value > max)
                {
                    max = value;
                    best = character;
                }
            }

            var probabilitySum = 0d;
            var alreadyProbabilities = true;
            for (var character = 0; character < Alphabet.Length; character++)
            {
                var value = scores[start + character];
                alreadyProbabilities &= value >= 0 && value <= 1;
                probabilitySum += value;
            }

            var confidence = alreadyProbabilities && Math.Abs(probabilitySum - 1) < 0.05
                ? (float)max
                : SoftmaxMaximum(scores, start, max);
            var symbol = Alphabet[best];
            if (symbol == '_' && text.Length == 0)
            {
                continue;
            }

            text.Append(symbol);
            if (symbol != '_')
            {
                logConfidence += Math.Log(Math.Max(confidence, 1e-12f));
                characterCount++;
            }
        }

        var registration = text.ToString().TrimEnd('_');
        if (registration.Length is < 4 or > 8
            || registration.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            return (null, 0);
        }

        var overallConfidence = characterCount == 0
            ? 0
            : (float)Math.Exp(logConfidence / characterCount);
        return (registration, overallConfidence);
    }

    private static float SoftmaxMaximum(float[] values, int start, double max)
    {
        var total = 0d;
        for (var character = 0; character < Alphabet.Length; character++)
        {
            total += Math.Exp(values[start + character] - max);
        }

        return (float)(1d / total);
    }

    private static double IntersectionOverUnion(Detection left, Detection right)
    {
        var x0 = Math.Max(left.Left, right.Left);
        var y0 = Math.Max(left.Top, right.Top);
        var x1 = Math.Min(left.Right, right.Right);
        var y1 = Math.Min(left.Bottom, right.Bottom);
        var intersection = Math.Max(0, x1 - x0) * Math.Max(0, y1 - y0);
        var union = (left.Right - left.Left) * (left.Bottom - left.Top)
            + (right.Right - right.Left) * (right.Bottom - right.Top)
            - intersection;
        return union <= 0 ? 0 : intersection / union;
    }

    private sealed record Detection(double Left, double Top, double Right, double Bottom, float Confidence);
    private sealed record LetterboxTransform(int ResizedWidth, int ResizedHeight, int Left, int Top, double ScaleX, double ScaleY);
}
