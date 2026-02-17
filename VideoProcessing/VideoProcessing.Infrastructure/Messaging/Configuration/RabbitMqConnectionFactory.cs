using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using VideoProcessing.Infrastructure.Messaging.Configuration;

public class RabbitMqConnectionFactory
{
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;

    public RabbitMqConnectionFactory(
        IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<IConnection> CreateAsync()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            Ssl = new SslOption
            {
                Enabled = true,
                ServerName = _settings.Host
            }
        };

        _connection = await factory.CreateConnectionAsync();

        return _connection;
    }
}
