using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace VideoProcessing.Infrastructure.Messaging.Configuration;

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

        ConnectionFactory factory;

        factory = new ConnectionFactory
        {
            Uri = new Uri(_settings.ConnectionUri),
            AutomaticRecoveryEnabled = true
        };

        var maxAttempts = Math.Max(1, _settings.ConnectionRetryCount);
        var delay = Math.Max(100, _settings.ConnectionRetryDelayMs);
        var attempt = 0;

        while (true)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync();
                return _connection;
            }
            catch (RabbitMQ.Client.Exceptions.AuthenticationFailureException authEx)
            {
                // Authentication failed - these credentials or virtual host are likely incorrect.
                throw new InvalidOperationException(
                    "RabbitMQ authentication failed. Check Host, UserName, Password and VirtualHost configuration.",
                    authEx);
            }
            catch (Exception)
            {
                attempt++;
                if (attempt >= maxAttempts)
                    throw;

                await Task.Delay(delay);
                // exponential backoff but cap it to a reasonable value
                delay = Math.Min(30000, delay * 2);
            }
        }
    }
}