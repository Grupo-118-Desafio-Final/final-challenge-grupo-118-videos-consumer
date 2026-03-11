using Azure;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class BlobVideoDownloaderTests : IDisposable
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobVideoDownloader> _logger;
    private readonly List<string> _tempFiles;
    private readonly IConfiguration _configuration;

    public BlobVideoDownloaderTests()
    {
        _blobServiceClient = Substitute.For<BlobServiceClient>();
        _configuration = Substitute.For<IConfiguration>();
        _logger = Substitute.For<ILogger<BlobVideoDownloader>>();
        _tempFiles = new List<string>();
    }

    [Fact]
    public async Task DownloadAsync_WithValidAzureBlobUrl_ShouldDownloadSuccessfully()
    {
        // Arrange
        var blobUrl = "https://myaccount.blob.core.windows.net/mycontainer/myvideo.mp4";
        var containerName = "mycontainer";
        var blobName = "myvideo.mp4";

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(containerClient);
        containerClient.GetBlobClient(blobName).Returns(blobClient);

        containerClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));
        blobClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));

        blobClient.DownloadToAsync(Arg.Any<string>()).Returns(Task.FromResult(Substitute.For<Response>()));

        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var result = await downloader.DownloadAsync(blobUrl);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith(".mp4");
        File.Exists(result).Should().BeFalse(); // File is mocked, won't actually exist

        await blobClient.Received(1).DownloadToAsync(Arg.Any<string>());
        _tempFiles.Add(result);
    }

    [Fact]
    public async Task DownloadAsync_WithAzuriteBlobUrl_ShouldDownloadSuccessfully()
    {
        // Arrange
        var blobUrl = "http://127.0.0.1:10000/devstoreaccount1/videos/test.mp4";
        var containerName = "videos";
        var blobName = "test.mp4";

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(containerClient);
        containerClient.GetBlobClient(blobName).Returns(blobClient);

        containerClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));
        blobClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));

        blobClient.DownloadToAsync(Arg.Any<string>()).Returns(Task.FromResult(Substitute.For<Response>()));

        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var result = await downloader.DownloadAsync(blobUrl);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith(".mp4");

        await blobClient.Received(1).DownloadToAsync(Arg.Any<string>());
        _tempFiles.Add(result);
    }

    [Fact]
    public async Task DownloadAsync_WithNullBlobUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await downloader.DownloadAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*blobUrl*");
    }

    [Fact]
    public async Task DownloadAsync_WithEmptyBlobUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await downloader.DownloadAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*blobUrl*");
    }

    [Fact]
    public async Task DownloadAsync_WithWhitespaceBlobUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await downloader.DownloadAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*blobUrl*");
    }

    [Fact]
    public async Task DownloadAsync_WithInvalidBlobUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var blobUrl = "https://myaccount.blob.core.windows.net/";
        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await downloader.DownloadAsync(blobUrl);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*container and blob name*");
    }

    [Fact]
    public async Task DownloadAsync_WhenContainerDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var blobUrl = "https://myaccount.blob.core.windows.net/mycontainer/myvideo.mp4";
        var containerName = "mycontainer";
        var blobName = "myvideo.mp4";

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(containerClient);
        containerClient.GetBlobClient(blobName).Returns(blobClient);

        containerClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(false, null!)));

        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await downloader.DownloadAsync(blobUrl);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Container*does not exist*");
    }

    [Fact]
    public async Task DownloadAsync_WhenBlobDoesNotExist_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var blobUrl = "https://myaccount.blob.core.windows.net/mycontainer/myvideo.mp4";
        var containerName = "mycontainer";
        var blobName = "myvideo.mp4";

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(containerClient);
        containerClient.GetBlobClient(blobName).Returns(blobClient);

        containerClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));
        blobClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(false, null!)));

        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await downloader.DownloadAsync(blobUrl);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*Blob*not found*");
    }

    [Fact]
    public async Task DownloadAsync_WhenRequestFails_ShouldThrowRequestFailedException()
    {
        // Arrange
        var blobUrl = "https://myaccount.blob.core.windows.net/mycontainer/myvideo.mp4";
        var containerName = "mycontainer";
        var blobName = "myvideo.mp4";

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(containerClient);
        containerClient.GetBlobClient(blobName).Returns(blobClient);

        containerClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));
        blobClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));

        blobClient.DownloadToAsync(Arg.Any<string>())
            .Returns<Task<Response>>(_ => Task.FromException<Response>(new RequestFailedException("Download failed")));

        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await downloader.DownloadAsync(blobUrl);

        // Assert
        await act.Should().ThrowAsync<RequestFailedException>();
    }

    [Fact]
    public async Task DownloadAsync_WithNestedBlobPath_ShouldHandleCorrectly()
    {
        // Arrange
        var blobUrl = "https://myaccount.blob.core.windows.net/mycontainer/user123/processing456/video.mp4";
        var containerName = "mycontainer";
        var blobName = "user123/processing456/video.mp4";

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(containerClient);
        containerClient.GetBlobClient(blobName).Returns(blobClient);

        containerClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));
        blobClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));

        blobClient.DownloadToAsync(Arg.Any<string>()).Returns(Task.FromResult(Substitute.For<Response>()));

        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        var result = await downloader.DownloadAsync(blobUrl);

        // Assert
        result.Should().NotBeNullOrEmpty();
        containerClient.Received(1).GetBlobClient(blobName);
        _tempFiles.Add(result);
    }

    [Fact]
    public async Task DownloadAsync_ShouldLogSuccessfulDownload()
    {
        // Arrange
        var blobUrl = "https://myaccount.blob.core.windows.net/mycontainer/video.mp4";
        var containerName = "mycontainer";
        var blobName = "video.mp4";

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(containerClient);
        containerClient.GetBlobClient(blobName).Returns(blobClient);

        containerClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));
        blobClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(true, null!)));

        blobClient.DownloadToAsync(Arg.Any<string>()).Returns(Task.FromResult(Substitute.For<Response>()));

        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        await downloader.DownloadAsync(blobUrl);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Downloaded blob")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task DownloadAsync_WhenContainerDoesNotExist_ShouldLogError()
    {
        // Arrange
        var blobUrl = "https://myaccount.blob.core.windows.net/mycontainer/video.mp4";
        var containerName = "mycontainer";
        var blobName = "video.mp4";

        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();

        _blobServiceClient.GetBlobContainerClient(containerName).Returns(containerClient);
        containerClient.GetBlobClient(blobName).Returns(blobClient);

        containerClient.ExistsAsync().Returns(Task.FromResult(Response.FromValue(false, null!)));

        var downloader = new BlobVideoDownloader(_blobServiceClient, _logger, _configuration);

        // Act
        try
        {
            await downloader.DownloadAsync(blobUrl);
        }
        catch
        {
            // Expected
        }

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("does not exist")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
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
    }
}