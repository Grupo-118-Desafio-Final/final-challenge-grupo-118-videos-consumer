namespace VideoProcessing.Domain.Ports.On;

public interface IProcessingPublisher
{
    Task PublishSuccessAsync(string processingId, string zipUrl);
    Task PublishErrorAsync(string processingId, string error);
}
