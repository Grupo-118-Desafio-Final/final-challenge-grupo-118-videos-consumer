using Microsoft.Extensions.Logging;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

public class FileSystem : IFileSystem
{
    private readonly ILogger<FileSystem> _logger;

    public FileSystem(ILogger<FileSystem> logger)
    {
        _logger = logger;
    }


    public bool DeleteFile(string filePath)
    {
        _logger.LogInformation($"Deleting file {filePath}");

        if (string.IsNullOrWhiteSpace(filePath)) return false;
        if (File.Exists(filePath))
        {
            _logger.LogInformation($"Found file {filePath}");
            File.Delete(filePath);
            return true;
        }

        return false;
    }

    public bool DeleteFiles(IEnumerable<string>? filePaths)
    {
        if (filePaths is null) return false;

        var filePathsList = filePaths.ToList();
        if (!filePathsList.Any()) return false;

        var directory = Path.GetDirectoryName(filePathsList.First());
        if (string.IsNullOrWhiteSpace(directory)) return false;

        _logger.LogInformation("Deleting files in directory {Directory}", directory);

        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}