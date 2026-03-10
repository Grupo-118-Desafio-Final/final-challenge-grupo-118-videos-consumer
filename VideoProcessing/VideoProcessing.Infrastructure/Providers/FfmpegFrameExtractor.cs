using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

[ExcludeFromCodeCoverage]
public class FfmpegFrameExtractor : IFrameExtractor
{
    private readonly ILogger<FfmpegFrameExtractor> _logger;
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;
    private readonly int _framesToExtract;
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(2);
    private readonly string? _outputBasePath;

    public FfmpegFrameExtractor(
        ILogger<FfmpegFrameExtractor> logger,
        IConfiguration configuration,
        string? ffmpegPath = null,
        string? ffprobePath = null)
    {
        _logger = logger;
        _ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? "ffmpeg" : ffmpegPath;
        _ffprobePath = string.IsNullOrWhiteSpace(ffprobePath) ? "ffprobe" : ffprobePath;

        var framesValue = configuration["QuantityFrames"];
        if (!int.TryParse(framesValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _framesToExtract))
        {
            _framesToExtract = 10;
        }

        ValidateBinaryAvailable(_ffmpegPath, "ffmpeg");
        ValidateBinaryAvailable(_ffprobePath, "ffprobe");

        var configuredPath = configuration["FramesOutputPath"];

        var basePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        Directory.CreateDirectory(basePath);
        _outputBasePath = basePath;
    }

    public async Task<List<string>> ExtractFramesAsync(string videoPath, int qualityImage)
    {
        try
        {
            if (!File.Exists(videoPath))
                throw new FileNotFoundException("Video file not found", videoPath);

            var baseDir = _outputBasePath ?? Path.Combine(Path.GetTempPath(), "frames");
            var outputDir = Path.Combine(baseDir, Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(outputDir);

            var framesToExtract = _framesToExtract;

            var extractedFiles = new List<string>(framesToExtract);

            var durationSeconds = await GetVideoDurationSecondsAsync(videoPath);

            if (durationSeconds <= 0)
                throw new InvalidOperationException("Could not determine video duration");

            var interval = durationSeconds / (framesToExtract + 1);

            var q = MapResolutionOrQualityToQscale(qualityImage, out var mappedFromResolution);

            for (int i = 1; i <= framesToExtract; i++)
            {
                var timestampSeconds = interval * i;
                var timestamp = TimeSpan.FromSeconds(timestampSeconds);

                var outputFile = Path.Combine(outputDir, $"frame_{i:D3}.jpg");

                var args =
                    $"-ss {timestampSeconds.ToString(CultureInfo.InvariantCulture)} " +
                    $"-i \"{videoPath}\" " +
                    $"-frames:v 1 " +
                    $"-vf scale={qualityImage}:-1 " +
                    $"-q:v {q} " +
                    $"\"{outputFile}\" -y";


                var result = await RunProcessAsync(_ffmpegPath, args);

                if (result.ExitCode != 0)
                {
                    _logger.LogWarning(
                        "ffmpeg failed for frame {Index}. ExitCode={ExitCode} Error={Error}",
                        i, result.ExitCode, result.StandardError);

                    continue;
                }

                if (!File.Exists(outputFile))
                {
                    _logger.LogWarning(
                        "ffmpeg reported success but frame file not found: {File}",
                        outputFile);

                    continue;
                }

                extractedFiles.Add(outputFile);
            }

            _logger.LogInformation(
                "Frame extraction finished. Generated={Count} Directory={Dir}",
                extractedFiles.Count, outputDir);

            return extractedFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make frames");
            throw;
        }
    }

    private static int MapResolutionOrQualityToQscale(int value, out bool mappedFromResolution)
    {
        var map = new Dictionary<int, int>
        {
            [2160] = 2,
            [1440] = 3,
            [1080] = 4,
            [720] = 8,
            [480] = 12
        };

        if (map.TryGetValue(value, out var qFromMap))
        {
            mappedFromResolution = true;
            return qFromMap;
        }

        mappedFromResolution = false;
        return value;
    }

    private async Task<double> GetVideoDurationSecondsAsync(string videoPath)
    {
        try
        {
            var args =
                "-v error " +
                "-show_entries format=duration " +
                "-of default=noprint_wrappers=1:nokey=1 " +
                $"\"{videoPath}\"";

            var result = await RunProcessAsync(_ffprobePath, args);

            if (result.ExitCode != 0)
            {
                _logger.LogWarning(
                    "ffprobe failed. ExitCode={ExitCode} Error={Error}",
                    result.ExitCode, result.StandardError);
                return 0;
            }

            if (double.TryParse(
                    result.StandardOutput.Trim(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var seconds))
            {
                return seconds;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get video duration");
        }

        return 0;
    }

    private async Task<(int ExitCode, string StandardOutput, string StandardError)> RunProcessAsync(
        string file,
        string args)
    {
        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo(file, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (!proc.Start())
            throw new InvalidOperationException($"Failed to start process {file}");

        var stdOutTask = proc.StandardOutput.ReadToEndAsync();
        var stdErrTask = proc.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(ProcessTimeout);

        await proc.WaitForExitAsync(cts.Token);

        var stdout = await stdOutTask;
        var stderr = await stdErrTask;

        return (proc.ExitCode, stdout, stderr);
    }

    private void ValidateBinaryAvailable(string binary, string name)
    {
        try
        {
            var psi = new ProcessStartInfo(binary, "-version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p == null)
                throw new InvalidOperationException();

            p.WaitForExit(3000);
        }
        catch
        {
            _logger.LogWarning(
                "{Binary} not validated at startup. Make sure it is installed and in PATH.",
                name);
        }
    }
}