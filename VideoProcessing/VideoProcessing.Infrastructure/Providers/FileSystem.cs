using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

public class FileSystem : IFileSystem
{
    public bool DeleteFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }

        return false;
    }

    public bool DeleteFiles(IEnumerable<string>? filePaths)
    {
        if (filePaths is null) return false;
        if (!filePaths.Any()) return false;

        var allDeleted = true;
        foreach (var filePath in filePaths)
        {
            if (!DeleteFile(filePath))
            {
                allDeleted = false;
            }
        }

        return allDeleted;
    }
}