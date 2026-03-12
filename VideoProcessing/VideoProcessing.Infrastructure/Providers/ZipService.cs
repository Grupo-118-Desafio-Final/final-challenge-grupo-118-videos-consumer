using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

public class ZipService : IZipService
{
    private readonly ILogger<ZipService> _logger;
    private readonly string? _outputBasePath;

    public ZipService(ILogger<ZipService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var configuredPath = configuration["ZipsOutputPath"];

        // TODO: Ajustar logica paga considerar o Path como o "a "pasta" antes do id do usuario e upload
        var basePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        Directory.CreateDirectory(basePath);
        _outputBasePath = basePath;
        _logger.LogInformation("Zips will be written to configured path: {Path}", _outputBasePath);
    }

    public async Task<string> CreateZipAsync(List<string> files)
    {
        if (files == null) throw new ArgumentNullException(nameof(files));
        if (files.Count == 0) throw new ArgumentException("No files provided to create zip.", nameof(files));

        foreach (var f in files)
        {
            if (!File.Exists(f)) throw new FileNotFoundException("One of the files to zip was not found.", f);
        }

        var baseDir = _outputBasePath ?? Path.GetTempPath();
        var zipFileName = $"video-frames-{Guid.NewGuid():N}.zip";
        var zipPath = Path.Combine(baseDir, zipFileName);

        _logger.LogInformation("Creating zip file at {ZipPath} with {FileCount} files.", zipPath, files.Count);
        
        using (var zipToOpen = new FileStream(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
        {
            foreach (var filePath in files)
            {
                var entryName = Path.GetFileName(filePath) ?? Guid.NewGuid().ToString();
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await fileStream.CopyToAsync(entryStream);
            }
        }
        
        _logger.LogInformation("Created zip file at {ZipPath} with {FileCount} files.", zipPath, files.Count);        

        return zipPath;
    }
}