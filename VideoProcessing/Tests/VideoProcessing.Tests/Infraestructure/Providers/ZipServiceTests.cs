using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class ZipServiceTests : IDisposable
{
    private readonly ILogger<ZipService> _logger;
    private readonly List<string> _tempFiles;
    private readonly List<string> _tempDirectories;

    public ZipServiceTests()
    {
        _logger = Substitute.For<ILogger<ZipService>>();
        _tempFiles = new List<string>();
        _tempDirectories = new List<string>();
    }

    [Fact]
    public async Task CreateZipAsync_WithValidFiles_ShouldCreateZipSuccessfully()
    {
        // Arrange
        var testFiles = CreateTestFiles(3);
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var result = await zipService.CreateZipAsync(testFiles);

        // Assert
        result.Should().NotBeNullOrEmpty();
        File.Exists(result).Should().BeTrue();
        result.Should().EndWith(".zip");
        
        _tempFiles.Add(result);
    }

    [Fact]
    public async Task CreateZipAsync_WithSingleFile_ShouldCreateZipSuccessfully()
    {
        // Arrange
        var testFiles = CreateTestFiles(1);
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var result = await zipService.CreateZipAsync(testFiles);

        // Assert
        result.Should().NotBeNullOrEmpty();
        File.Exists(result).Should().BeTrue();
        
        _tempFiles.Add(result);
    }

    [Fact]
    public async Task CreateZipAsync_WithMultipleFiles_ShouldIncludeAllFiles()
    {
        // Arrange
        var testFiles = CreateTestFiles(5);
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var result = await zipService.CreateZipAsync(testFiles);

        // Assert
        result.Should().NotBeNullOrEmpty();
        File.Exists(result).Should().BeTrue();
        
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result);
        zipArchive.Entries.Count.Should().Be(5);
        
        _tempFiles.Add(result);
    }

    [Fact]
    public async Task CreateZipAsync_WithNullFilesList_ShouldThrowArgumentNullException()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var act = async () => await zipService.CreateZipAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*files*");
    }

    [Fact]
    public async Task CreateZipAsync_WithEmptyFilesList_ShouldThrowArgumentException()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var act = async () => await zipService.CreateZipAsync(new List<string>());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*No files provided*");
    }

    [Fact]
    public async Task CreateZipAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var testFiles = new List<string> { Path.Combine(Path.GetTempPath(), "non-existent-file.txt") };
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var act = async () => await zipService.CreateZipAsync(testFiles);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateZipAsync_WithMixedExistingAndNonExistingFiles_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var testFiles = CreateTestFiles(2);
        testFiles.Add(Path.Combine(Path.GetTempPath(), "non-existent-file.txt"));
        
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var act = async () => await zipService.CreateZipAsync(testFiles);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task CreateZipAsync_ShouldCreateZipInConfiguredPath()
    {
        // Arrange
        var testFiles = CreateTestFiles(2);
        var customPath = Path.Combine(Path.GetTempPath(), $"custom-zips-{Guid.NewGuid():N}");
        Directory.CreateDirectory(customPath);
        _tempDirectories.Add(customPath);
        
        var configuration = CreateConfiguration(customPath);
        var zipService = new ZipService(_logger, configuration);

        // Act
        var result = await zipService.CreateZipAsync(testFiles);

        // Assert
        result.Should().StartWith(customPath);
        File.Exists(result).Should().BeTrue();
        
        _tempFiles.Add(result);
    }

    [Fact]
    public async Task CreateZipAsync_ShouldGenerateUniqueFileName()
    {
        // Arrange
        var testFiles = CreateTestFiles(1);
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var result1 = await zipService.CreateZipAsync(testFiles);
        var result2 = await zipService.CreateZipAsync(testFiles);

        // Assert
        result1.Should().NotBe(result2);
        Path.GetFileName(result1).Should().NotBe(Path.GetFileName(result2));
        
        _tempFiles.Add(result1);
        _tempFiles.Add(result2);
    }

    [Fact]
    public async Task CreateZipAsync_WithRelativeConfigPath_ShouldCreateZipInCorrectLocation()
    {
        // Arrange
        var testFiles = CreateTestFiles(1);
        var configuration = CreateConfiguration("./relative-zips");
        var zipService = new ZipService(_logger, configuration);

        // Act
        var result = await zipService.CreateZipAsync(testFiles);

        // Assert
        result.Should().NotBeNullOrEmpty();
        File.Exists(result).Should().BeTrue();
        Path.IsPathRooted(result).Should().BeTrue();
        
        _tempFiles.Add(result);
    }

    [Fact]
    public void Constructor_ShouldLogConfiguredPath()
    {
        // Arrange
        var configuration = CreateConfiguration();

        // Act
        _ = new ZipService(_logger, configuration);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Zips will be written to configured path")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CreateZipAsync_ShouldCreateValidZipFile()
    {
        // Arrange
        var testFiles = CreateTestFiles(3, 10000); // Create larger files
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var result = await zipService.CreateZipAsync(testFiles);

        // Assert
        var zipSize = new FileInfo(result).Length;
        
        // The zip should exist and be a reasonable size
        zipSize.Should().BeGreaterThan(0);
        File.Exists(result).Should().BeTrue();
        
        // Verify it's a valid zip by opening it
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result);
        zipArchive.Entries.Count.Should().Be(3);
        
        _tempFiles.Add(result);
    }

    [Fact]
    public async Task CreateZipAsync_ShouldPreserveFileNames()
    {
        // Arrange
        var testFiles = CreateTestFiles(3);
        var expectedFileNames = testFiles.Select(Path.GetFileName).ToList();
        
        var configuration = CreateConfiguration();
        var zipService = new ZipService(_logger, configuration);

        // Act
        var result = await zipService.CreateZipAsync(testFiles);

        // Assert
        using var zipArchive = System.IO.Compression.ZipFile.OpenRead(result);
        var entryNames = zipArchive.Entries.Select(e => e.Name).ToList();
        
        entryNames.Should().BeEquivalentTo(expectedFileNames);
        
        _tempFiles.Add(result);
    }

    private Microsoft.Extensions.Configuration.IConfiguration CreateConfiguration(string? customPath = null)
    {
        var config = Substitute.For<Microsoft.Extensions.Configuration.IConfiguration>();
        var zipPath = customPath ?? Path.Combine(Path.GetTempPath(), $"test-zips-{Guid.NewGuid():N}");
        config["ZipsOutputPath"].Returns(zipPath);
        
        if (string.IsNullOrEmpty(customPath))
        {
            _tempDirectories.Add(zipPath);
        }
        
        return config;
    }

    private List<string> CreateTestFiles(int count, int sizeBytes = 100)
    {
        var files = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"test-frame-{Guid.NewGuid():N}.jpg");
            var content = new byte[sizeBytes];
            Random.Shared.NextBytes(content);
            File.WriteAllBytes(filePath, content);
            
            files.Add(filePath);
            _tempFiles.Add(filePath);
        }
        
        return files;
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

