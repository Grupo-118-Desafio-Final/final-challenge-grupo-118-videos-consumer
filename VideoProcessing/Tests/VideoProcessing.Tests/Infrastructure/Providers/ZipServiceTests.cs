using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infrastructure.Providers;

public class ZipServiceTests
{
    private readonly ILogger<ZipService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _testOutputPath;

    public ZipServiceTests()
    {
        _logger = Substitute.For<ILogger<ZipService>>();
        _configuration = Substitute.For<IConfiguration>();
        
        _testOutputPath = Path.Combine(Path.GetTempPath(), "test-zips", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputPath);
        
        _configuration["ZipsOutputPath"].Returns(_testOutputPath);
    }

    [Fact]
    public async Task CreateZipAsync_WithValidFiles_ShouldCreateZipFile()
    {
        // Arrange
        var sut = new ZipService(_logger, _configuration);
        
        var testFiles = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var filePath = Path.Combine(_testOutputPath, $"test-file-{i}.txt");
            await File.WriteAllTextAsync(filePath, $"Content {i}");
            testFiles.Add(filePath);
        }

        // Act
        var zipPath = await sut.CreateZipAsync(testFiles);

        // Assert
        zipPath.Should().NotBeNullOrEmpty();
        File.Exists(zipPath).Should().BeTrue();
        zipPath.Should().EndWith(".zip");
        zipPath.Should().Contain("video-frames-");
        
        // Cleanup
        foreach (var file in testFiles)
        {
            if (File.Exists(file)) File.Delete(file);
        }
        if (File.Exists(zipPath)) File.Delete(zipPath);
    }

    [Fact]
    public async Task CreateZipAsync_WithNullFiles_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sut = new ZipService(_logger, _configuration);

        // Act
        var act = () => sut.CreateZipAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("files");
    }

    [Fact]
    public async Task CreateZipAsync_WithEmptyFilesList_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = new ZipService(_logger, _configuration);
        var emptyList = new List<string>();

        // Act
        var act = () => sut.CreateZipAsync(emptyList);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*No files provided to create zip*")
            .WithParameterName("files");
    }

    [Fact]
    public async Task CreateZipAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var sut = new ZipService(_logger, _configuration);
        var files = new List<string> { "/non/existent/file.txt" };

        // Act
        var act = () => sut.CreateZipAsync(files);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*One of the files to zip was not found*");
    }

    [Fact]
    public async Task CreateZipAsync_WithMixedExistentAndNonExistentFiles_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var sut = new ZipService(_logger, _configuration);
        
        var existingFile = Path.Combine(_testOutputPath, "existing.txt");
        await File.WriteAllTextAsync(existingFile, "Content");
        
        var files = new List<string>
        {
            existingFile,
            "/non/existent/file.txt"
        };

        // Act
        var act = () => sut.CreateZipAsync(files);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
        
        // Cleanup
        if (File.Exists(existingFile)) File.Delete(existingFile);
    }

    [Fact]
    public async Task CreateZipAsync_WithMultipleFiles_ShouldCreateZipWithAllFiles()
    {
        // Arrange
        var sut = new ZipService(_logger, _configuration);
        
        var testFiles = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var filePath = Path.Combine(_testOutputPath, $"file-{i}.txt");
            await File.WriteAllTextAsync(filePath, $"Test content {i}");
            testFiles.Add(filePath);
        }

        // Act
        var zipPath = await sut.CreateZipAsync(testFiles);

        // Assert
        File.Exists(zipPath).Should().BeTrue();
        var fileInfo = new FileInfo(zipPath);
        fileInfo.Length.Should().BeGreaterThan(0);
        
        // Cleanup
        foreach (var file in testFiles)
        {
            if (File.Exists(file)) File.Delete(file);
        }
        if (File.Exists(zipPath)) File.Delete(zipPath);
    }
}

