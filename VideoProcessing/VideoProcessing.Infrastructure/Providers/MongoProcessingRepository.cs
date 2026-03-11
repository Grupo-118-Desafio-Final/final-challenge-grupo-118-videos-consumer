using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoProcessing.Domain.Ports.On;
using VideoProcessing.Domain.Enums;

namespace VideoProcessing.Infrastructure.Providers;

public class MongoProcessingRepository : IProcessingRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<MongoProcessingRepository> _logger;

    public MongoProcessingRepository(IConfiguration configuration, ILogger<MongoProcessingRepository> logger)
    {
        _logger = logger;

        var connectionString = configuration["MongoDb:ConnectionString"];
        var client = new MongoClient(connectionString);

        var databaseName = configuration["MongoDb:Database"];
        var collectionName = configuration["MongoDb:Collection"];
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<BsonDocument>(collectionName);
    }

    // Novo construtor testável
    public MongoProcessingRepository(IMongoCollection<BsonDocument> collection, ILogger<MongoProcessingRepository> logger)
    {
        _logger = logger;
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task UpdateProcessing(string processingId, ProcessingStatus status, string? zipBlobUrl = null)
    {
        _logger.LogInformation("Updating processing with ID {ProcessingId} to status {Status}", processingId, status);

        var guid = Guid.Parse(processingId);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(guid, GuidRepresentation.Standard));

        var update = Builders<BsonDocument>.Update
            .Set("zipBlobUrl", zipBlobUrl)
            .Set("status", status.ToString());

        var result = await _collection.UpdateOneAsync(filter, update);

        if (result.MatchedCount == 0)
            _logger.LogWarning("No document found with ID {ProcessingId} to update", processingId);
        else
            _logger.LogInformation("Successfully updated processing with ID {ProcessingId}", processingId);
    }
}