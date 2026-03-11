using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class BlobFileStorageTests : IDisposable
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobFileStorage> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _testFilePath;
    private readonly string _containerName = "test-container";

    public BlobFileStorageTests()
    {
        _blobServiceClient = Substitute.For<BlobServiceClient>();
        _logger = Substitute.For<ILogger<BlobFileStorage>>();
        _configuration = Substitute.For<IConfiguration>();
        
        _configuration["AzureBlob:ContainerName"].Returns(_containerName);
        
        // Create a temporary test file
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.txt");
        File.WriteAllText(_testFilePath, "test content");
    }

    [Fact]
    public async Task UploadAsync_WithValidParameters_ShouldUploadSuccessfully()
    {
        // Arrange
        var userId = "user123";
        var processingId = "processing456";
        var blobUrl = "https://test.blob.core.windows.net/container/user123/processing456/test-file.txt";
        
        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();
        
        _blobServiceClient.GetBlobContainerClient(_containerName).Returns(containerClient);
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);
        blobClient.Uri.Returns(new Uri(blobUrl));
        
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        var result = await storage.UploadAsync(_testFilePath, userId, processingId);

        // Assert
        result.Should().Be(blobUrl);
        await containerClient.Received(1).CreateIfNotExistsAsync();
        await blobClient.Received(1).UploadAsync(Arg.Any<Stream>(), overwrite: true);
    }

    [Fact]
    public async Task UploadAsync_WithNullFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await storage.UploadAsync(null!, "user123", "processing456");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*filePath*");
    }

    [Fact]
    public async Task UploadAsync_WithEmptyFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await storage.UploadAsync(string.Empty, "user123", "processing456");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*filePath*");
    }

    [Fact]
    public async Task UploadAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await storage.UploadAsync(_testFilePath, null!, "processing456");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*userId*");
    }

    [Fact]
    public async Task UploadAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await storage.UploadAsync(_testFilePath, string.Empty, "processing456");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*userId*");
    }

    [Fact]
    public async Task UploadAsync_WithNullProcessingId_ShouldThrowArgumentException()
    {
        // Arrange
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await storage.UploadAsync(_testFilePath, "user123", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*processingId*");
    }

    [Fact]
    public async Task UploadAsync_WithEmptyProcessingId_ShouldThrowArgumentException()
    {
        // Arrange
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        var act = async () => await storage.UploadAsync(_testFilePath, "user123", string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*processingId*");
    }

    [Fact]
    public void Constructor_WithMissingContainerName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configWithoutContainer = Substitute.For<IConfiguration>();
        configWithoutContainer["AzureBlob:ContainerName"].Returns((string?)null);
        
        var containerClient = Substitute.For<BlobContainerClient>();
        _blobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(containerClient);
        
        var storage = new BlobFileStorage(_blobServiceClient, _logger, configWithoutContainer);

        // Act
        var act = async () => await storage.UploadAsync(_testFilePath, "user123", "processing456");

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Azure blob container name*");
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateCorrectBlobPath()
    {
        // Arrange
        var userId = "user123";
        var processingId = "processing456";
        var expectedBlobPath = $"/uploads/{userId}/{processingId}/{Path.GetFileName(_testFilePath)}";
        
        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();
        
        _blobServiceClient.GetBlobContainerClient(_containerName).Returns(containerClient);
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);
        blobClient.Uri.Returns(new Uri("https://test.blob.core.windows.net/container/blob"));
        
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        await storage.UploadAsync(_testFilePath, userId, processingId);

        // Assert
        containerClient.Received(1).GetBlobClient(expectedBlobPath);
    }

    [Fact]
    public async Task UploadAsync_ShouldLogSuccessfulUpload()
    {
        // Arrange
        var userId = "user123";
        var processingId = "processing456";
        
        var containerClient = Substitute.For<BlobContainerClient>();
        var blobClient = Substitute.For<BlobClient>();
        
        _blobServiceClient.GetBlobContainerClient(_containerName).Returns(containerClient);
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);
        blobClient.Uri.Returns(new Uri("https://test.blob.core.windows.net/container/blob"));
        
        var storage = new BlobFileStorage(_blobServiceClient, _logger, _configuration);

        // Act
        await storage.UploadAsync(_testFilePath, userId, processingId);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Uploaded file")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}

