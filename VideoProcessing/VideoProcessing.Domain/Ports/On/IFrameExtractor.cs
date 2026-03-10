namespace VideoProcessing.Domain.Ports.On;

public interface IFrameExtractor
{
    Task<List<string>> ExtractFramesAsync(string videoPath, int qualityImage);
}
