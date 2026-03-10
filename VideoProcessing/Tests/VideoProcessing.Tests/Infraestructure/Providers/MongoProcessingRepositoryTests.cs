using FluentAssertions;
using Microsoft.Extensions.Configuration;
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

    public MongoProcessingRepositoryTests()
    {
        _configuration = Substitute.For<IConfiguration>();
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
        var repository = new MongoProcessingRepository(_configuration);

        // Assert
        _ = _configuration.Received(1)["MongoDb:ConnectionString"];
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConfiguration_ShouldReadMongoDbDatabase()
    {
        // Act
        var repository = new MongoProcessingRepository(_configuration);

        // Assert
        _ = _configuration.Received(1)["MongoDb:Database"];
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConfiguration_ShouldReadMongoDbCollection()
    {
        // Act
        var repository = new MongoProcessingRepository(_configuration);

        // Assert
        _ = _configuration.Received(1)["MongoDb:Collection"];
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConfiguration_ShouldCreateMongoClientWithConnectionString()
    {
        // Arrange & Act
        var repository = new MongoProcessingRepository(_configuration);

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
        var repository = new MongoProcessingRepository(_mockCollection);

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MongoProcessingRepository((IMongoCollection<BsonDocument>)null!);

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
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-123";
        var status = ProcessingStatus.Processing;
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/file.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithProcessedStatus_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-456";
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://storage.blob.core.windows.net/frames/output.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithFailedStatus_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-789";
        var status = ProcessingStatus.Failed;

        // Act
        await repository.UpdateProcessing(processingId, status, null);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ProcessingStatus.Processing)]
    [InlineData(ProcessingStatus.Processed)]
    [InlineData(ProcessingStatus.Failed)]
    public async Task UpdateProcessing_WithAllStatuses_ShouldCallUpdateOneAsync(ProcessingStatus status)
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = $"proc-{status}";
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/file.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Testes do UpdateProcessing - ZipBlobUrl

    [Fact]
    public async Task UpdateProcessing_WithZipBlobUrl_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-with-zip";
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/frames.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithNullZipBlobUrl_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-without-zip";
        var status = ProcessingStatus.Processing;

        // Act
        await repository.UpdateProcessing(processingId, status, null);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithoutZipBlobUrlParameter_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-default";
        var status = ProcessingStatus.Processing;

        // Act - Usando o valor padrão do parâmetro opcional
        await repository.UpdateProcessing(processingId, status);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithComplexZipBlobUrl_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-complex";
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/folder/frames-2024.zip?sv=2021-12-02&ss=bqtf";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Testes do UpdateProcessing - ProcessingId

    [Fact]
    public async Task UpdateProcessing_WithValidProcessingId_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "valid-processing-id-123";
        var status = ProcessingStatus.Processing;

        // Act
        await repository.UpdateProcessing(processingId, status);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithGuidProcessingId_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = Guid.NewGuid().ToString();
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/file.zip";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithLongProcessingId_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = new string('a', 500); // 500 caracteres
        var status = ProcessingStatus.Processing;

        // Act
        await repository.UpdateProcessing(processingId, status);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithSpecialCharactersInProcessingId_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-123-ñ-é-中文";
        var status = ProcessingStatus.Processing;

        // Act
        await repository.UpdateProcessing(processingId, status);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Testes de Integração do UpdateProcessing

    [Fact]
    public async Task UpdateProcessing_MultipleCalls_ShouldCallUpdateOneAsyncMultipleTimes()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);

        // Act
        await repository.UpdateProcessing("proc-1", ProcessingStatus.Processing);
        await repository.UpdateProcessing("proc-2", ProcessingStatus.Processed, "url1");
        await repository.UpdateProcessing("proc-3", ProcessingStatus.Failed);

        // Assert
        await _mockCollection.Received(3).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_CalledTwiceForSameProcessingId_ShouldCallUpdateOneAsyncTwice()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-update-twice";

        // Act
        await repository.UpdateProcessing(processingId, ProcessingStatus.Processing);
        await repository.UpdateProcessing(processingId, ProcessingStatus.Processed, "url");

        // Assert
        await _mockCollection.Received(2).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Testes de Edge Cases

    [Fact]
    public async Task UpdateProcessing_WithEmptyProcessingId_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "";
        var status = ProcessingStatus.Processing;

        // Act
        await repository.UpdateProcessing(processingId, status);

        // Assert - Mesmo com string vazia, deve chamar o método
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_WithEmptyZipBlobUrl_ShouldCallUpdateOneAsync()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var processingId = "proc-empty-url";
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "";

        // Act
        await repository.UpdateProcessing(processingId, status, zipBlobUrl);

        // Assert
        await _mockCollection.Received(1).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessing_ConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_mockCollection);
        var tasks = new List<Task>();

        // Act - Simular chamadas concorrentes
        for (int i = 0; i < 10; i++)
        {
            var processingId = $"proc-{i}";
            tasks.Add(repository.UpdateProcessing(processingId, ProcessingStatus.Processing));
        }

        await Task.WhenAll(tasks);

        // Assert
        await _mockCollection.Received(10).UpdateOneAsync(
            Arg.Any<FilterDefinition<BsonDocument>>(),
            Arg.Any<UpdateDefinition<BsonDocument>>(),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}

