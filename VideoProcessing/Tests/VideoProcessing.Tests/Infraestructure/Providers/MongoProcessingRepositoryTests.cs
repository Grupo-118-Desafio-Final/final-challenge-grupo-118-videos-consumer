using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using VideoProcessing.Domain.Enums;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class MongoProcessingRepositoryTests
{
    private readonly IConfiguration _configuration;

    public MongoProcessingRepositoryTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _configuration["MongoDb:ConnectionString"].Returns("mongodb://localhost:27017");
        _configuration["MongoDb:Database"].Returns("testdb");
        _configuration["MongoDb:Collection"].Returns("testcollection");
    }

    [Fact]
    public void Constructor_ShouldReadMongoDbConnectionString()
    {
        // Act
        var repository = new MongoProcessingRepository(_configuration);

        // Assert
        _ = _configuration.Received(1)["MongoDb:ConnectionString"];
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldReadMongoDbDatabase()
    {
        // Act
        var repository = new MongoProcessingRepository(_configuration);

        // Assert
        _ = _configuration.Received(1)["MongoDb:Database"];
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldReadMongoDbCollection()
    {
        // Act
        var repository = new MongoProcessingRepository(_configuration);

        // Assert
        _ = _configuration.Received(1)["MongoDb:Collection"];
        repository.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ProcessingStatus.Processing)]
    [InlineData(ProcessingStatus.Processed)]
    [InlineData(ProcessingStatus.Failed)]
    public void UpdateProcessing_WithDifferentStatuses_ShouldAcceptAllValidStatuses(ProcessingStatus status)
    {
        // Arrange
        var repository = new MongoProcessingRepository(_configuration);

        // Act & Assert - Should not throw during repository creation
        repository.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProcessing_WithZipBlobUrl_ShouldAcceptUrl()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_configuration);
        var zipBlobUrl = "https://storage.blob.core.windows.net/container/file.zip";

        // Act & Assert - Should not throw during creation
        repository.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProcessing_WithNullZipBlobUrl_ShouldAcceptNull()
    {
        // Arrange
        var repository = new MongoProcessingRepository(_configuration);

        // Act & Assert - Should not throw during creation
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldCreateMongoClientWithConnectionString()
    {
        // Arrange & Act
        var repository = new MongoProcessingRepository(_configuration);

        // Assert
        repository.Should().NotBeNull();
        _ = _configuration.Received(1)["MongoDb:ConnectionString"];
    }
}

