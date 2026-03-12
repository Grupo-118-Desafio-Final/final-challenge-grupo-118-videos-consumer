namespace VideoProcessing.Domain.Ports.On;

public interface IFileSystem
{
    bool DeleteFile(string filePath);

    bool DeleteFiles(IEnumerable<string> filePaths);
}