using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VideoProcessing.Domain.Events;
using VideoProcessing.Infrastructure.Messaging.Configuration;

namespace VideoProcessing.Infrastructure.Messaging;

public class VideoProcessedMessageProducer
{
    private readonly RabbitMqConnectionFactory _factory;
    private readonly RabbitMqSettings _settings;

    public VideoProcessedMessageProducer(RabbitMqConnectionFactory factory, IOptions<RabbitMqSettings> options)
    {
        _factory = factory;
        _settings = options.Value;
    }

    public async Task PublishAsync(NotificationEvent message)
    {
        var connection = await _factory.CreateAsync();
        var channel = await connection.CreateChannelAsync();

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: _settings.NotificationQueue, body: body);

        await Task.CompletedTask;
    }
}
