using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using VideoProcessing.Domain.Ports.On;
using VideoProcessing.Domain.Enums;

namespace VideoProcessing.Infrastructure.Providers;

public class MongoProcessingRepository : IProcessingRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoProcessingRepository(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];
        var client = new MongoClient(connectionString);

        var databaseName = configuration["MongoDb:Database"];
        var collectionName = configuration["MongoDb:Collection"];
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<BsonDocument>(collectionName);
    }

    // Novo construtor testável
    public MongoProcessingRepository(IMongoCollection<BsonDocument> collection)
    {
        _collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task UpdateProcessing(string processingId, ProcessingStatus status, string? zipBlobUrl = null)
    {
        var guid = Guid.Parse(processingId);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(guid, GuidRepresentation.Standard));

        var update = Builders<BsonDocument>.Update
            .Set("zipBlobUrl", zipBlobUrl)
            .Set("status", status.ToString());

        await _collection.UpdateOneAsync(filter, update);
    }
}