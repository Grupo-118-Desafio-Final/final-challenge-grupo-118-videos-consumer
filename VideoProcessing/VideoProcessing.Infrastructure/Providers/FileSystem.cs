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

        var filePathsList = filePaths.ToList();
        if (!filePathsList.Any()) return false;

        var directory = Path.GetDirectoryName(filePathsList.First());
        if (string.IsNullOrWhiteSpace(directory)) return false;

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