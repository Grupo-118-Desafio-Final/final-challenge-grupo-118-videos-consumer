using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using VideoProcessing.Domain.Enums;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class MongoProcessingRepositoryTests
{
    private readonly IConfiguration _configuration;
    private readonly IMongoCollection<BsonDocument> _mockCollection;
    private readonly ILogger<MongoProcessingRepository> _logger;

    public MongoProcessingRepositoryTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _logger = Substitute.For<ILogger<MongoProcessingRepository>>();

        _configuration["MongoDb:ConnectionString"].Returns("mongodb://localhost:27017");
        _configuration["MongoDb:Database"].Returns("testdb");
        _configuration["MongoDb:Collection"].Returns("testcollection");

        _mockCollection = Substitute.For<IMongoCollection<BsonDocument>>();
    }

    #region Testes do Construtor com IConfiguration

    [Fact]
    public void Constructor_WithConfiguration_ShouldReadMongoDbConnectionString()
    {
        // Act
        var repository = new MongoProcessingRepository(_configuration, _logger);

        // Assert
        _ = _configuration.Received(1)["MongoDb:ConnectionString"];
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConfiguration_ShouldReadMongoDbDatabase()
    {
        // Act
        var repository = new MongoProcessingRepository(_configuration, _logger);

        // Assert
        _ = _configuration.Received(1)["MongoDb:Database"];
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConfiguration_ShouldReadMongoDbCollection()
    {
        // Act
        var repository = new MongoProcessingRepository(_configuration, _logger);

        // Assert
        _ = _configuration.Received(1)["MongoDb:Collection"];
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConfiguration_ShouldCreateMongoClientWithConnectionString()
    {
        // Arrange & Act
        var repository = new MongoProcessingRepository(_configuration, _logger);

        // Assert
        repository.Should().NotBeNull();
        _ = _configuration.Received(1)["MongoDb:ConnectionString"];
    }

    #endregion

    #region Testes do Construtor com IMongoCollection

    [Fact]
    public void Constructor_WithCollection_ShouldInitializeSuccessfully()
    {
        // Act
        var repository = new MongoProcessingRepository(_mockCollection, _logger);

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MongoProcessingRepository((IMongoCollection<BsonDocument>)null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("collection");
    }

    #endregion

    #region Testes do UpdateProcessing - Diferentes Status

    [Fact]
    public async Task UpdateProcessing_WithProcessingStatus_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processing;
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/file.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithProcessedStatus_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://storage.blob.core.windows.net/frames/output.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithFailedStatus_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Failed;

        // Act
        await repository.UpdateProcessing(processingId, status, null);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ProcessingStatus.Processing)]
    [InlineData(ProcessingStatus.Processed)]
    [InlineData(ProcessingStatus.Failed)]
    public async Task UpdateProcessing_WithAllStatuses_ShouldCallUpdateOneAsync(ProcessingStatus status)
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/file.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Testes do UpdateProcessing - ZipBlobUrl

    [Fact]
    public async Task UpdateProcessing_WithZipBlobUrl_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/frames.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithNullZipBlobUrl_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processing;

        // Act
        await repository.UpdateProcessing(processingId, status, null);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithoutZipBlobUrlParameter_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processing;

        // Act - Usando o valor padrão do parâmetro opcional
        await repository.UpdateProcessing(processingId, status);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithComplexZipBlobUrl_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/folder/frames-2024.zip?sv=2021-12-02&ss=bqtf";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Testes do UpdateProcessing - ProcessingId

    [Fact]
    public async Task UpdateProcessing_WithValidProcessingId_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processing;

        // Act
        await repository.UpdateProcessing(processingId, status);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithGuidProcessingId_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/file.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithInvalidGuid_ShouldThrowFormatException()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = "not-a-guid";
        var status = ProcessingStatus.Processing;

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() =>
            repository.UpdateProcessing(processingId, status));
    }

    [Fact]
    public async Task UpdateProcessing_WithEmptyString_ShouldThrowFormatException()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = "";
        var status = ProcessingStatus.Processing;

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() =>
            repository.UpdateProcessing(processingId, status));
    }

    #endregion

    #region Testes de Integração do UpdateProcessing

    [Fact]
    public async Task UpdateProcessing_MultipleCalls_ShouldCallUpdateOneAsyncMultipleTimes()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);

        // Act
        await repository.UpdateProcessing(Guid.NewGuid().ToString(), ProcessingStatus.Processing);
        await repository.UpdateProcessing(Guid.NewGuid().ToString(), ProcessingStatus.Processed, "url1");
        await repository.UpdateProcessing(Guid.NewGuid().ToString(), ProcessingStatus.Failed);

        // Assert
        await _mockCollection.Received(3).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_CalledTwiceForSameProcessingId_ShouldCallUpdateOneAsyncTwice()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();

        // Act
        await repository.UpdateProcessing(processingId, ProcessingStatus.Processing);
        await repository.UpdateProcessing(processingId, ProcessingStatus.Processed, "url");

        // Assert
        await _mockCollection.Received(2).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Testes de Edge Cases

    [Fact]
    public async Task UpdateProcessing_WithEmptyZipBlobUrl_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_ConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection, _logger);
        var tasks = new List<Task>();

        // Act - Simular chamadas concorrentes
        for (int i = 0; i < 10; i++)
        {
            var processingId = Guid.NewGuid().ToString();
            tasks.Add(repository.UpdateProcessing(processingId, ProcessingStatus.Processing));
        }

        await Task.WhenAll(tasks);

        // Assert
        await _mockCollection.Received(10).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            null,
            Arg.Any<CancellationToken>());
    }

    #endregion
}