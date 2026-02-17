using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

public class BlobFileStorage : IFileStorage
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobFileStorage> _logger;

    public BlobFileStorage(BlobServiceClient blobServiceClient, ILogger<BlobFileStorage> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath cannot be null or empty", nameof(filePath));

        var containerClient = _blobServiceClient.GetBlobContainerClient("processed-videos");
        await containerClient.CreateIfNotExistsAsync();

        var blobName = Path.GetFileName(filePath) ?? Guid.NewGuid().ToString();
        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = File.OpenRead(filePath);
        await blobClient.UploadAsync(stream, overwrite: true);

        _logger.LogInformation("Uploaded file {FilePath} to blob {BlobName}", filePath, blobName);

        return blobClient.Uri.ToString();
    }
}
