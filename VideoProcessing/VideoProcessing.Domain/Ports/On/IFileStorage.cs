namespace VideoProcessing.Domain.Ports.On;

public interface IFileStorage
{
    Task<string> UploadAsync(string filePath);
}
