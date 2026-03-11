using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

public class BlobVideoDownloader : IVideoDownloader
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobVideoDownloader> _logger;
    private readonly string? _outputBasePath;

    public BlobVideoDownloader(BlobServiceClient blobServiceClient, ILogger<BlobVideoDownloader> logger,
        IConfiguration configuration)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;

        var configuredPath = configuration["VideoDownloadOutputPath"];

        var basePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        Directory.CreateDirectory(basePath);
        _outputBasePath = basePath;
    }

    public async Task<string> DownloadAsync(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
            throw new ArgumentException("blobUrl cannot be null or empty", nameof(blobUrl));

        var uri = new Uri(blobUrl);

        var segments = uri.AbsolutePath.Trim('/').Split('/', 3);

        if (segments.Length < 2 || string.IsNullOrWhiteSpace(segments[0]) || string.IsNullOrWhiteSpace(segments[1]))
            throw new ArgumentException("blobUrl must contain container and blob name (e.g. '/container/blob')",
                nameof(blobUrl));

        string containerName;
        string blobName;

        if (segments[0] == "devstoreaccount1")
        {
            // Azurite
            containerName = segments[1];
            blobName = string.Join('/', segments.Skip(2));
        }
        else
        {
            // Azure real
            containerName = segments[0];
            blobName = string.Join('/', segments.Skip(1));
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var ext = Path.GetExtension(blobName);

        // TODO: Ajustar logica paga considerar o Path como o "a "pasta" antes do id do usuario e upload
        var tempFile = Path.Combine(_outputBasePath, $"{Guid.NewGuid()}{ext}");

        try
        {
            var containerExists = await containerClient.ExistsAsync();
            if (!containerExists)
            {
                _logger.LogError("Container '{ContainerName}' does not exist for blobUrl {BlobUrl}", containerName,
                    blobUrl);
                throw new InvalidOperationException(
                    $"Container '{containerName}' does not exist for blobUrl '{blobUrl}'.");
            }

            var blobExists = await blobClient.ExistsAsync();
            if (!blobExists)
            {
                _logger.LogError("Blob '{BlobName}' not found in container '{ContainerName}' for blobUrl {BlobUrl}",
                    blobName, containerName, blobUrl);
                throw new FileNotFoundException($"Blob '{blobName}' not found in container '{containerName}'.");
            }

            var dir = Path.GetDirectoryName(tempFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            await blobClient.DownloadToAsync(tempFile);
            _logger.LogInformation("Downloaded blob to {TempFile}", tempFile);
            return tempFile;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to download blob {BlobUrl}", blobUrl);
            throw;
        }
    }
}