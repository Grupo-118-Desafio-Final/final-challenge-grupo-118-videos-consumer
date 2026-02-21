namespace VideoProcessing.Infrastructure.Messaging.Configuration;

public class RabbitMqSettings
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string VirtualHost { get; set; } = default!;
    public string ProcessImagesQueue { get; set; }
    public string NotificationQueue { get; set; }
}
