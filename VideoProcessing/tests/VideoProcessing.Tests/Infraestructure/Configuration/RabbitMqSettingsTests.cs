using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using VideoProcessing.Infrastructure.Messaging.Configuration;
using Xunit;

namespace VideoProcessing.Tests.Infraestructure.Configuration;

public class RabbitMqSettingsTests
{
    [Fact]
    public void PropertyAssignment_ShouldRetainValues()
    {
        // Arrange & Act
        var settings = new RabbitMqSettings
        {
            ProcessImagesQueue = "process-queue",
            NotificationQueue = "notification-queue",
            ConnectionUri = "amqp://localhost",
            ConnectionRetryCount = 5,
            ConnectionRetryDelayMs = 2000
        };

        // Assert
        settings.ProcessImagesQueue.Should().Be("process-queue");
        settings.NotificationQueue.Should().Be("notification-queue");
        settings.ConnectionUri.Should().Be("amqp://localhost");
        settings.ConnectionRetryCount.Should().Be(5);
        settings.ConnectionRetryDelayMs.Should().Be(2000);
    }

    [Fact]
    public void ConfigurationBinding_ShouldPopulateProperties_FromInMemoryConfiguration()
    {
        // Arrange
        var inMemory = new Dictionary<string, string?>
        {
            ["ProcessImagesQueue"] = "cfg-process-queue",
            ["NotificationQueue"] = "cfg-notification-queue",
            ["ConnectionUri"] = "amqp://cfg-host",
            ["ConnectionRetryCount"] = "2",
            ["ConnectionRetryDelayMs"] = "1500"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        // Act
        var bound = configuration.Get<RabbitMqSettings>();

        // Assert
        bound.Should().NotBeNull();
        bound!.ProcessImagesQueue.Should().Be("cfg-process-queue");
        bound.NotificationQueue.Should().Be("cfg-notification-queue");
        bound.ConnectionUri.Should().Be("amqp://cfg-host");
        bound.ConnectionRetryCount.Should().Be(2);
        bound.ConnectionRetryDelayMs.Should().Be(1500);
    }
}