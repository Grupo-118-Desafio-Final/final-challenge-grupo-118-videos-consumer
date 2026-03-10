namespace VideoProcessing.Domain.Ports.On;

public interface IVideoDownloader
{
    Task<string> DownloadAsync(string blobUrl);
}
