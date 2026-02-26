using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using VideoProcessing.Domain.Ports.On;
using VideoProcessing.Domain.Enums;

namespace VideoProcessing.Infrastructure.Providers;

public class MongoProcessingRepository : IProcessingRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoProcessingRepository(IMongoClient client, IConfiguration configuration)
    {
        var databaseName = configuration["MongoDb:Database"] ?? "videoprocessing";
        var collectionName = configuration["MongoDb:Collection"] ?? "processings";
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<BsonDocument>(collectionName);
    }

    public async Task UpdateProcessing(string processingId, ProcessingStatus status,string? zipBlobUrl = null)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("ProcessingId", processingId);
        var update = Builders<BsonDocument>.Update
            .Set("ZipBlobUrl", zipBlobUrl)
            .Set("Status", status);

        await _collection.UpdateOneAsync(filter, update);
    }
}
