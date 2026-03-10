using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using VideoProcessing.Domain.Events;
using VideoProcessing.Domain.Ports.In;
using VideoProcessing.Infrastructure.Messaging.Configuration;

namespace VideoProcessing.Infrastructure.Messaging;

public class VideoProcessingMessageConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions =new() { PropertyNameCaseInsensitive = true };

    private readonly RabbitMqConnectionFactory _factory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _settings;

    public VideoProcessingMessageConsumer(RabbitMqConnectionFactory factory, IServiceScopeFactory scopeFactory, IOptions<RabbitMqSettings> options)
    {
        _factory = factory;
        _scopeFactory = scopeFactory;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        Console.WriteLine("Consumer iniciando...");

        var connection = await _factory.CreateAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.BasicQosAsync(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            await HandleMessageAsync(channel, ea);
        };

        await channel.BasicConsumeAsync(_settings.ProcessImagesQueue, autoAck: false, consumer);

        Console.WriteLine("Consumer conectado e aguardando mensagens...");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    protected virtual async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            Console.WriteLine($"Mensagem recebida: {json}");

            var message = await DeserializeMessageAsync(json);

            await ProcessMessageAsync(message);

            await channel.BasicAckAsync(ea.DeliveryTag, false);

            Console.WriteLine("Mensagem processada com sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar mensagem: {ex}");
            await channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }
    }

    protected virtual Task<VideoProcessingEvent> DeserializeMessageAsync(string json)
    {
        var message = JsonSerializer.Deserialize<VideoProcessingEvent>(json, JsonOptions);

        if (message == null)
            throw new InvalidOperationException("Mensagem inválida ou vazia");

        return Task.FromResult(message);
    }

    protected virtual async Task ProcessMessageAsync(VideoProcessingEvent message)
    {
        using var scope = _scopeFactory.CreateScope();

        var useCase = scope.ServiceProvider.GetRequiredService<IProcessVideoUseCase>();

        await useCase.ExecuteAsync(message);
    }
}
