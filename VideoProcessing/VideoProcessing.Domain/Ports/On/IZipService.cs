namespace VideoProcessing.Domain.Ports.On;

public interface IZipService
{
    Task<string> CreateZipAsync(List<string> files);
}
