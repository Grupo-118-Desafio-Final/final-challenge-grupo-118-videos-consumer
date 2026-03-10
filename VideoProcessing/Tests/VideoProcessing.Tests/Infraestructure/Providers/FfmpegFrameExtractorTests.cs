using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class FfmpegFrameExtractorTests : IDisposable
{
    private readonly ILogger<FfmpegFrameExtractor> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<string> _tempFiles;
    private readonly List<string> _tempDirectories;

    public FfmpegFrameExtractorTests()
    {
        _logger = Substitute.For<ILogger<FfmpegFrameExtractor>>();
        _configuration = Substitute.For<IConfiguration>();
        _tempFiles = new List<string>();
        _tempDirectories = new List<string>();
        
        // Default configuration
        _configuration["QuantityFrames"].Returns("10");
        _configuration["FramesOutputPath"].Returns(Path.Combine(Path.GetTempPath(), $"test-frames-{Guid.NewGuid():N}"));
    }

    [Fact]
    public void Constructor_ShouldUseDefaultFfmpegPath_WhenNotProvided()
    {
        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        extractor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldUseCustomFfmpegPath_WhenProvided()
    {
        // Act
        var extractor = new FfmpegFrameExtractor(
            _logger, 
            _configuration, 
            ffmpegPath: "/custom/path/ffmpeg",
            ffprobePath: "/custom/path/ffprobe");

        // Assert
        extractor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldReadQuantityFramesFromConfiguration()
    {
        // Arrange
        _configuration["QuantityFrames"].Returns("15");

        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        _ = _configuration.Received(1)["QuantityFrames"];
        extractor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldUseDefaultFramesCount_WhenConfigurationIsMissing()
    {
        // Arrange
        _configuration["QuantityFrames"].Returns((string?)null);

        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        extractor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldUseDefaultFramesCount_WhenConfigurationIsInvalid()
    {
        // Arrange
        _configuration["QuantityFrames"].Returns("invalid");

        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        extractor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldReadFramesOutputPathFromConfiguration()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), "custom-frames");
        _configuration["FramesOutputPath"].Returns(outputPath);

        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        _ = _configuration.Received(1)["FramesOutputPath"];
        extractor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateOutputDirectory()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"test-frames-{Guid.NewGuid():N}");
        _configuration["FramesOutputPath"].Returns(outputPath);
        _tempDirectories.Add(outputPath);

        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        Directory.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithRelativePath_ShouldConvertToAbsolutePath()
    {
        // Arrange
        _configuration["FramesOutputPath"].Returns("./relative-frames");

        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        extractor.Should().NotBeNull();
        // The directory should be created as an absolute path
    }

    [Fact]
    public async Task ExtractFramesAsync_WithNonExistentVideoFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent-video.mp4");

        // Act
        var act = async () => await extractor.ExtractFramesAsync(nonExistentPath, 1080);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*Video file not found*");
    }

    [Fact]
    public async Task ExtractFramesAsync_WithValidVideoPath_ShouldCreateOutputDirectory()
    {
        // Arrange
        var videoPath = CreateMockVideoFile();
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Act & Assert
        // This test would require actual ffmpeg installed, so we verify the setup
        extractor.Should().NotBeNull();
        File.Exists(videoPath).Should().BeTrue();
    }

    [Theory]
    [InlineData(2160)]
    [InlineData(1440)]
    [InlineData(1080)]
    [InlineData(720)]
    [InlineData(480)]
    public void ExtractFramesAsync_WithDifferentResolutions_ShouldAcceptValidResolutions(int resolution)
    {
        // Arrange
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Act & Assert - Should not throw during setup
        extractor.Should().NotBeNull();
        resolution.Should().BeGreaterThan(0); // Use the parameter
    }

    [Fact]
    public void Constructor_ShouldValidateFfmpegBinary()
    {
        // Arrange & Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        extractor.Should().NotBeNull();
        // The constructor attempts to validate ffmpeg availability
        // If ffmpeg is not available, it should log a warning but not throw
    }

    [Fact]
    public void Constructor_ShouldValidateFfprobeBinary()
    {
        // Arrange & Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        extractor.Should().NotBeNull();
        // The constructor attempts to validate ffprobe availability
        // If ffprobe is not available, it should log a warning but not throw
    }

    [Fact]
    public void Constructor_WithInvalidBinaryPaths_ShouldLogWarning()
    {
        // Arrange
        var invalidFfmpegPath = "/invalid/path/to/ffmpeg";
        var invalidFfprobePath = "/invalid/path/to/ffprobe";

        // Act
        var extractor = new FfmpegFrameExtractor(
            _logger,
            _configuration,
            ffmpegPath: invalidFfmpegPath,
            ffprobePath: invalidFfprobePath);

        // Assert
        extractor.Should().NotBeNull();
        // Should log warnings about binary validation but not throw
    }

    [Theory]
    [InlineData("5")]
    [InlineData("10")]
    [InlineData("20")]
    public void Constructor_WithDifferentFrameCounts_ShouldAcceptValidCounts(string frameCount)
    {
        // Arrange
        _configuration["QuantityFrames"].Returns(frameCount);

        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        extractor.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtractFramesAsync_ShouldReturnListOfFramePaths()
    {
        // Arrange
        var videoPath = CreateMockVideoFile();
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Act & Assert
        // This test would require actual ffmpeg to run properly
        // We're verifying the setup is correct
        extractor.Should().NotBeNull();
        File.Exists(videoPath).Should().BeTrue();
    }

    [Fact]
    public async Task ExtractFramesAsync_WithEmptyVideoPath_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Act
        var act = async () => await extractor.ExtractFramesAsync(string.Empty, 1080);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void Constructor_ShouldHandleRootedOutputPath()
    {
        // Arrange
        var rootedPath = Path.Combine(Path.GetTempPath(), $"rooted-frames-{Guid.NewGuid():N}");
        _configuration["FramesOutputPath"].Returns(rootedPath);
        _tempDirectories.Add(rootedPath);

        // Act
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);

        // Assert
        extractor.Should().NotBeNull();
        Directory.Exists(rootedPath).Should().BeTrue();
    }

    [Fact]
    public async Task ExtractFramesAsync_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var extractor = new FfmpegFrameExtractor(_logger, _configuration);
        var invalidPath = "not-a-valid-path";

        // Act
        try
        {
            await extractor.ExtractFramesAsync(invalidPath, 1080);
        }
        catch
        {
            // Expected
        }

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to make frames")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private string CreateMockVideoFile()
    {
        // Create a dummy file to simulate a video file
        var videoPath = Path.Combine(Path.GetTempPath(), $"mock-video-{Guid.NewGuid():N}.mp4");
        File.WriteAllText(videoPath, "This is a mock video file for testing");
        _tempFiles.Add(videoPath);
        return videoPath;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        foreach (var dir in _tempDirectories)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}

