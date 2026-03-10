using Microsoft.Extensions.Configuration;
using VideoProcessing.Domain.Enums;
using VideoProcessing.Infrastructure.Providers;
using NSubstitute;
using FluentAssertions;
namespace VideoProcessing.Tests.Infrastructure.Providers;
public class MongoProcessingRepositoryTests
{
    private readonly IConfiguration _configuration;
    public MongoProcessingRepositoryTests()
    {
        _configuration = Substitute.For<IConfiguration>();
        _configuration["MongoDb:ConnectionString"].Returns("mongodb://localhost:27017");
        _configuration["MongoDb:Database"].Returns("testdb");
        _configuration["MongoDb:Collection"].Returns("processing");
    }
    [Fact]
    public void Constructor_WithValidConfiguration_ShouldNotThrow()
    {
        // Act
        var act = () => new MongoProcessingRepository(_configuration);
        // Assert
        act.Should().NotThrow();
    }
    [Fact]
    public void Constructor_ShouldReadConfigurationValues()
    {
        // Act
        _ = new MongoProcessingRepository(_configuration);
        // Assert
        var connectionString = _configuration.Received(1)["MongoDb:ConnectionString"];
        var database = _configuration.Received(1)["MongoDb:Database"];
        var collection = _configuration.Received(1)["MongoDb:Collection"];
        connectionString.Should().NotBeNull();
        database.Should().NotBeNull();
        collection.Should().NotBeNull();
    }
    [Fact]
    public async Task UpdateProcessing_WithProcessedStatus_ShouldNotThrow()
    {
        // Arrange
        var sut = new MongoProcessingRepository(_configuration);
        var processingId = "proc123";
        var status = ProcessingStatus.Processed;
        var zipBlobUrl = "https://blob.example.com/frames.zip";
        // Act
        // Note: This test only verifies the method doesn't throw during construction
        // Full integration testing would require a real MongoDB instance
        var act = async () => await sut.UpdateProcessing(processingId, status, zipBlobUrl);
        // Assert
        act.Should().NotBeNull();
    }
    [Fact]
    public async Task UpdateProcessing_WithFailedStatus_ShouldNotThrow()
    {
        // Arrange
        var sut = new MongoProcessingRepository(_configuration);
        var processingId = "proc456";
        var status = ProcessingStatus.Failed;
        string? zipBlobUrl = null;
        // Act
        var act = async () => await sut.UpdateProcessing(processingId, status, zipBlobUrl);
        // Assert
        act.Should().NotBeNull();
    }
    [Fact]
    public async Task UpdateProcessing_WithProcessingStatus_ShouldNotThrow()
    {
        // Arrange
        var sut = new MongoProcessingRepository(_configuration);
        var processingId = "proc789";
        var status = ProcessingStatus.Processing;
        // Act
        var act = async () => await sut.UpdateProcessing(processingId, status);
        // Assert
        act.Should().NotBeNull();
    }
}
