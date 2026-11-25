using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using backend.Application.Interfaces.Analysis;
using Microsoft.Extensions.Logging;

namespace backend.Infrastructure.Analysis;

public sealed class FfmpegVideoFrameExtractor : IVideoFrameExtractor
{
    private static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(1);
    private const int ReadBufferSize = 64 * 1024;
    private readonly ILogger<FfmpegVideoFrameExtractor> _logger;

    public FfmpegVideoFrameExtractor(ILogger<FfmpegVideoFrameExtractor> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<byte[]> ExtractFramesAsync(
        string videoPath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoPath);
        var duration = await ProbeDurationAsync(videoPath, cancellationToken);
        if (duration > MaximumDuration)
        {
            throw new VideoDurationLimitExceededException(duration, MaximumDuration);
        }

        var startInfo = CreateProcessStartInfo("ffmpeg");
        startInfo.ArgumentList.Add("-hide_banner");
        startInfo.ArgumentList.Add("-loglevel");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-nostdin");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(videoPath);
        startInfo.ArgumentList.Add("-map");
        startInfo.ArgumentList.Add("0:v:0");
        startInfo.ArgumentList.Add("-vf");
        startInfo.ArgumentList.Add("fps=1");
        startInfo.ArgumentList.Add("-an");
        startInfo.ArgumentList.Add("-sn");
        startInfo.ArgumentList.Add("-dn");
        startInfo.ArgumentList.Add("-fps_mode");
        startInfo.ArgumentList.Add("passthrough");
        startInfo.ArgumentList.Add("-c:v");
        startInfo.ArgumentList.Add("mjpeg");
        startInfo.ArgumentList.Add("-q:v");
        startInfo.ArgumentList.Add("3");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("image2pipe");
        startInfo.ArgumentList.Add("pipe:1");

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("FFmpeg could not be started.");
        }

        var standardErrorTask = process.StandardError.ReadToEndAsync();
        var completed = false;
        try
        {
            await foreach (var frame in ReadJpegFramesAsync(
                process.StandardOutput.BaseStream,
                cancellationToken))
            {
                yield return frame;
            }

            await process.WaitForExitAsync(cancellationToken);
            var standardError = await standardErrorTask;
            if (process.ExitCode != 0)
            {
                throw new InvalidDataException(
                    $"FFmpeg failed to extract video frames (exit code {process.ExitCode}): {standardError.Trim()}");
            }

            completed = true;
        }
        finally
        {
            if (!completed && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    private async Task<TimeSpan> ProbeDurationAsync(string videoPath, CancellationToken cancellationToken)
    {
        var startInfo = CreateProcessStartInfo("ffprobe");
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-show_entries");
        startInfo.ArgumentList.Add("format=duration");
        startInfo.ArgumentList.Add("-of");
        startInfo.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
        startInfo.ArgumentList.Add(videoPath);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("FFprobe could not be started.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;
            if (process.ExitCode != 0)
            {
                throw new InvalidDataException(
                    $"FFprobe could not read video metadata (exit code {process.ExitCode}): {standardError.Trim()}");
            }

            if (!double.TryParse(
                standardOutput.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var seconds)
                || !double.IsFinite(seconds)
                || seconds <= 0)
            {
                throw new InvalidDataException("FFprobe returned an invalid video duration.");
            }

            var duration = TimeSpan.FromSeconds(seconds);
            _logger.LogDebug("Video duration is {Duration}.", duration);
            return duration;
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            throw;
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(string executable)
    {
        return new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    private static async IAsyncEnumerable<byte[]> ReadJpegFramesAsync(
        Stream output,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
        using var frame = new MemoryStream();
        var insideFrame = false;
        var previousByteWasMarkerPrefix = false;

        try
        {
            while (true)
            {
                var bytesRead = await output.ReadAsync(
                    buffer.AsMemory(0, ReadBufferSize),
                    cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                for (var index = 0; index < bytesRead; index++)
                {
                    var value = buffer[index];
                    if (!insideFrame)
                    {
                        if (previousByteWasMarkerPrefix && value == 0xD8)
                        {
                            frame.WriteByte(0xFF);
                            frame.WriteByte(0xD8);
                            insideFrame = true;
                            previousByteWasMarkerPrefix = false;
                        }
                        else
                        {
                            previousByteWasMarkerPrefix = value == 0xFF;
                        }

                        continue;
                    }

                    frame.WriteByte(value);
                    if (previousByteWasMarkerPrefix && value == 0xD9)
                    {
                        yield return frame.ToArray();
                        frame.SetLength(0);
                        insideFrame = false;
                        previousByteWasMarkerPrefix = false;
                    }
                    else
                    {
                        previousByteWasMarkerPrefix = value == 0xFF;
                    }
                }
            }

            if (insideFrame)
            {
                throw new InvalidDataException("FFmpeg returned an incomplete JPEG frame.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
