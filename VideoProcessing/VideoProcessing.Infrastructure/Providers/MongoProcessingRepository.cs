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
        var filter = Builders<BsonDocument>.Filter.Eq("Id", processingId);
        var update = Builders<BsonDocument>.Update
            .Set("ZipBlobUrl", zipBlobUrl)
            .Set("Status", status.ToString());

        await _collection.UpdateOneAsync(filter, update);
    }
}
