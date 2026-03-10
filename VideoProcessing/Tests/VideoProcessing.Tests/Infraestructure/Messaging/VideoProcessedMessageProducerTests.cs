using FluentAssertions;
using Microsoft.Extensions.Options;
using VideoProcessing.Domain.Events;
using VideoProcessing.Infrastructure.Messaging;
using VideoProcessing.Infrastructure.Messaging.Configuration;
using System.Text;
using System.Text.Json;

namespace VideoProcessing.Tests.Infraestructure.Messaging;

public class VideoProcessedMessageProducerTests
{
    private readonly IOptions<RabbitMqSettings> _options;
    private readonly RabbitMqSettings _settings;

    public VideoProcessedMessageProducerTests()
    {
        _settings = new RabbitMqSettings
        {
            NotificationQueue = "test-notification-queue",
            ProcessImagesQueue = "test-process-queue",
            ConnectionUri = "amqp://localhost",
            ConnectionRetryCount = 3,
            ConnectionRetryDelayMs = 1000
        };

        _options = Options.Create(_settings);
    }

    [Fact]
    public void Constructor_WithValidSettings_ShouldInitialize()
    {
        // Arrange
        var factory = new RabbitMqConnectionFactory(_options);

        // Act
        var producer = new VideoProcessedMessageProducer(factory, _options);

        // Assert
        producer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithDifferentQueueNames_ShouldAcceptConfiguration()
    {
        // Arrange
        var customSettings = new RabbitMqSettings
        {
            NotificationQueue = "custom-queue",
            ProcessImagesQueue = "process-queue",
            ConnectionUri = "amqp://localhost",
            ConnectionRetryCount = 5,
            ConnectionRetryDelayMs = 2000
        };

        var customOptions = Options.Create(customSettings);
        var factory = new RabbitMqConnectionFactory(customOptions);

        // Act
        var producer = new VideoProcessedMessageProducer(factory, customOptions);

        // Assert
        producer.Should().NotBeNull();
    }

    [Fact]
    public void NotificationEvent_ShouldSerializeToJsonCorrectly()
    {
        // Arrange
        var notification = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user123",
            Message = "Video processed successfully",
            ExceptionMessage = string.Empty,
            CreatedAt = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var body = Encoding.UTF8.GetBytes(json);

        // Assert
        body.Should().NotBeEmpty();
        json.Should().Contain("user123");
        json.Should().Contain("Video processed successfully");
    }

    [Fact]
    public void NotificationEvent_WithFailureStatus_ShouldSerializeCorrectly()
    {
        // Arrange
        var notification = new NotificationEvent
        {
            IsSuccess = false,
            UserId = "user456",
            Message = "Processing failed",
            ExceptionMessage = "Error: Invalid video format",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var deserialized = JsonSerializer.Deserialize<NotificationEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.IsSuccess.Should().BeFalse();
        deserialized.UserId.Should().Be("user456");
        deserialized.Message.Should().Be("Processing failed");
        deserialized.ExceptionMessage.Should().Contain("Invalid video format");
    }

    [Fact]
    public void NotificationEvent_WithSpecialCharacters_ShouldSerializeCorrectly()
    {
        // Arrange
        var notification = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user789",
            Message = "Message with special chars: é, ñ, 中文, 🎉",
            ExceptionMessage = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var deserialized = JsonSerializer.Deserialize<NotificationEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Message.Should().Contain("special chars");
        deserialized.Message.Should().Contain("🎉");
    }

    [Fact]
    public void NotificationEvent_WithLongMessage_ShouldSerializeCorrectly()
    {
        // Arrange
        var longMessage = new string('A', 5000);
        var notification = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user999",
            Message = longMessage,
            ExceptionMessage = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Assert
        bytes.Length.Should().BeGreaterThan(5000);
        json.Should().Contain(longMessage);
    }

    [Fact]
    public void NotificationEvent_WithEmptyExceptionMessage_ShouldSerializeCorrectly()
    {
        // Arrange
        var notification = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user111",
            Message = "Success",
            ExceptionMessage = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var deserialized = JsonSerializer.Deserialize<NotificationEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ExceptionMessage.Should().BeEmpty();
    }

    [Fact]
    public void NotificationEvent_WithStackTrace_ShouldSerializeCorrectly()
    {
        // Arrange
        var notification = new NotificationEvent
        {
            IsSuccess = false,
            UserId = "user222",
            Message = "Processing failed",
            ExceptionMessage = "System.Exception: Error\n  at Method1()\n  at Method2()\n  at Method3()",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var deserialized = JsonSerializer.Deserialize<NotificationEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ExceptionMessage.Should().Contain("System.Exception");
        deserialized.ExceptionMessage.Should().Contain("Method1");
    }

    [Fact]
    public void NotificationEvent_WithDateTimeOffset_ShouldSerializeCorrectly()
    {
        // Arrange
        var specificDate = new DateTimeOffset(2024, 6, 15, 10, 30, 45, TimeSpan.FromHours(-3));
        var notification = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user333",
            Message = "Test",
            ExceptionMessage = string.Empty,
            CreatedAt = specificDate
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var deserialized = JsonSerializer.Deserialize<NotificationEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CreatedAt.Should().Be(specificDate);
    }

    [Fact]
    public void Utf8Encoding_ShouldHandleAllCharacters()
    {
        // Arrange
        var notification = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user444",
            Message = "UTF-8 test: ñ, é, ü, 中文, 日本語, 한국어, العربية, עברית, 🎉",
            ExceptionMessage = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var bytes = Encoding.UTF8.GetBytes(json);
        var decodedJson = Encoding.UTF8.GetString(bytes);
        var deserialized = JsonSerializer.Deserialize<NotificationEvent>(decodedJson);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Message.Should().Contain("UTF-8 test");
        deserialized.Message.Should().Contain("🎉");
    }

    [Fact]
    public void RabbitMqSettings_ShouldHaveCorrectQueueNames()
    {
        // Arrange & Act
        var settings = _options.Value;

        // Assert
        settings.NotificationQueue.Should().Be("test-notification-queue");
        settings.ProcessImagesQueue.Should().Be("test-process-queue");
        settings.ConnectionUri.Should().Be("amqp://localhost");
        settings.ConnectionRetryCount.Should().Be(3);
        settings.ConnectionRetryDelayMs.Should().Be(1000);
    }

    [Fact]
    public void MessageSerialization_ShouldPreserveAllProperties()
    {
        // Arrange
        var notification = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user555",
            Message = "Test message",
            ExceptionMessage = "Test exception",
            CreatedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var deserialized = JsonSerializer.Deserialize<NotificationEvent>(json);

        // Assert
        deserialized.Should().BeEquivalentTo(notification);
    }

    [Fact]
    public void MessageSerialization_ShouldHandleMultipleMessages()
    {
        // Arrange
        var notifications = new List<NotificationEvent>
        {
            new NotificationEvent
            {
                IsSuccess = true,
                UserId = "user1",
                Message = "Message 1",
                ExceptionMessage = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new NotificationEvent
            {
                IsSuccess = false,
                UserId = "user2",
                Message = "Message 2",
                ExceptionMessage = "Error 2",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new NotificationEvent
            {
                IsSuccess = true,
                UserId = "user3",
                Message = "Message 3",
                ExceptionMessage = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Act & Assert
        foreach (var notification in notifications)
        {
            var json = JsonSerializer.Serialize(notification);
            var deserialized = JsonSerializer.Deserialize<NotificationEvent>(json);

            deserialized.Should().NotBeNull();
            deserialized!.UserId.Should().Be(notification.UserId);
            deserialized.Message.Should().Be(notification.Message);
        }
    }

    [Fact]
    public void ByteArrayConversion_ShouldBeReversible()
    {
        // Arrange
        var notification = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user666",
            Message = "Reversibility test",
            ExceptionMessage = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(notification);
        var bytes = Encoding.UTF8.GetBytes(json);
        var recoveredJson = Encoding.UTF8.GetString(bytes);
        var deserialized = JsonSerializer.Deserialize<NotificationEvent>(recoveredJson);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.UserId.Should().Be("user666");
        deserialized.Message.Should().Be("Reversibility test");
    }

    [Fact]
    public void EmptyExchange_ConfigurationScenario()
    {
        // Arrange - Simular o que o producer faz
        var exchange = string.Empty;
        var routingKey = _settings.NotificationQueue;

        // Act & Assert
        exchange.Should().BeEmpty();
        routingKey.Should().Be("test-notification-queue");
    }
}