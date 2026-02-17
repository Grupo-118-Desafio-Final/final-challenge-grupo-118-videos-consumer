using Microsoft.Extensions.Logging;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Infrastructure.Providers;

public class LoggingProcessingPublisher : IProcessingPublisher
{
    private readonly ILogger<LoggingProcessingPublisher> _logger;

    public LoggingProcessingPublisher(ILogger<LoggingProcessingPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishSuccessAsync(string processingId, string zipUrl)
    {
        _logger.LogInformation("Processing {ProcessingId} finished successfully. Zip URL: {ZipUrl}", processingId, zipUrl);
        return Task.CompletedTask;
    }

    public Task PublishErrorAsync(string processingId, string error)
    {
        _logger.LogError("Processing {ProcessingId} failed. Error: {Error}", processingId, error);
        return Task.CompletedTask;
    }
}
