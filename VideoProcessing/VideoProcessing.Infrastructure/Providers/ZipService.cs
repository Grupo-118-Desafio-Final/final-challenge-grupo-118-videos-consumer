using System.IO.Compression;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

public class ZipService : IZipService
{
    public async Task<string> CreateZipAsync(List<string> files)
    {
        if (files == null) throw new ArgumentNullException(nameof(files));
        if (files.Count == 0) throw new ArgumentException("No files provided to create zip.", nameof(files));

        foreach (var f in files)
        {
            if (!File.Exists(f)) throw new FileNotFoundException("One of the files to zip was not found.", f);
        }

        var tempDir = Path.GetTempPath();
        var zipFileName = $"video-frames-{Guid.NewGuid():N}.zip";
        var zipPath = Path.Combine(tempDir, zipFileName);

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

        return zipPath;
    }
}