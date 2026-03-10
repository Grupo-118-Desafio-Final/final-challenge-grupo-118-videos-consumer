using System.Diagnostics.CodeAnalysis;

namespace VideoProcessing.Infrastructure.Messaging.Configuration;

[ExcludeFromCodeCoverage]
public class RabbitMqSettings
{
    public string ProcessImagesQueue { get; set; }
    public string NotificationQueue { get; set; }
    public string? ConnectionUri { get; set; }
    public int ConnectionRetryCount { get; set; }
    public int ConnectionRetryDelayMs { get; set; }
}
