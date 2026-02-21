using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

public class BlobFileStorage : IFileStorage
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobFileStorage> _logger;
    private readonly string _containerName;

    public BlobFileStorage(BlobServiceClient blobServiceClient, ILogger<BlobFileStorage> logger, IConfiguration configuration)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _containerName = configuration?[("AzureBlob:ContainerName")] ?? string.Empty;
    }

    public async Task<string> UploadAsync(string filePath, string userId, string processingId)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath cannot be null or empty", nameof(filePath));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId cannot be null or empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(processingId)) throw new ArgumentException("processingId cannot be null or empty", nameof(processingId));

        if (string.IsNullOrWhiteSpace(_containerName))
        {
            throw new InvalidOperationException("Azure blob container name is not configured. Set 'AzureBlob:ContainerName' in configuration.");
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = Path.GetFileName(filePath) ?? Guid.NewGuid().ToString();
        var blobPath = $"{userId}/{processingId}/{blobName}";
        var blobClient = containerClient.GetBlobClient(blobPath);

        using var stream = File.OpenRead(filePath);
        await blobClient.UploadAsync(stream, overwrite: true);

        _logger.LogInformation("Uploaded file {FilePath} to blob {BlobPath}", filePath, blobPath);

        return blobClient.Uri.ToString();
    }
}
