using FluentAssertions;
using VideoProcessing.Domain.Events;

namespace VideoProcessing.Tests.Domain.Events;

public class NotificationEventTests
{
    [Fact]
    public void NotificationEvent_ShouldSetAndGetProperties()
    {
        // Arrange
        var isSuccess = true;
        var userId = "user123";
        var message = "Video processed successfully";
        var exceptionMessage = "No errors";
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var sut = new NotificationEvent
        {
            IsSuccess = isSuccess,
            UserId = userId,
            Message = message,
            ExceptionMessage = exceptionMessage,
            CreatedAt = createdAt
        };

        // Assert
        sut.IsSuccess.Should().Be(isSuccess);
        sut.UserId.Should().Be(userId);
        sut.Message.Should().Be(message);
        sut.ExceptionMessage.Should().Be(exceptionMessage);
        sut.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void NotificationEvent_DefaultValues_ShouldBeDefault()
    {
        // Act
        var sut = new NotificationEvent();

        // Assert
        sut.IsSuccess.Should().BeFalse();
        sut.UserId.Should().BeNull();
        sut.Message.Should().BeNull();
        sut.ExceptionMessage.Should().BeNull();
        sut.CreatedAt.Should().Be(default(DateTimeOffset));
    }

    [Fact]
    public void NotificationEvent_SuccessScenario_ShouldHaveCorrectValues()
    {
        // Act
        var sut = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user456",
            Message = "Video processed successfully",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.Message.Should().Be("Video processed successfully");
        sut.ExceptionMessage.Should().BeNull();
    }

    [Fact]
    public void NotificationEvent_FailureScenario_ShouldHaveCorrectValues()
    {
        // Act
        var sut = new NotificationEvent
        {
            IsSuccess = false,
            UserId = "user789",
            Message = "Error processing video",
            ExceptionMessage = "File not found exception",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        sut.IsSuccess.Should().BeFalse();
        sut.Message.Should().Be("Error processing video");
        sut.ExceptionMessage.Should().Be("File not found exception");
    }

    [Fact]
    public void NotificationEvent_Properties_ShouldBeMutable()
    {
        // Arrange
        var sut = new NotificationEvent
        {
            IsSuccess = false,
            UserId = "user1",
            Message = "Initial message"
        };

        // Act
        sut.IsSuccess = true;
        sut.UserId = "user2";
        sut.Message = "Updated message";

        // Assert
        sut.IsSuccess.Should().BeTrue();
        sut.UserId.Should().Be("user2");
        sut.Message.Should().Be("Updated message");
    }

    [Fact]
    public void NotificationEvent_CreatedAt_ShouldAcceptDifferentTimezones()
    {
        // Arrange
        var utcTime = new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.Zero);
        var localTime = new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.FromHours(-3));

        // Act
        var utcEvent = new NotificationEvent { CreatedAt = utcTime };
        var localEvent = new NotificationEvent { CreatedAt = localTime };

        // Assert
        utcEvent.CreatedAt.Should().Be(utcTime);
        localEvent.CreatedAt.Should().Be(localTime);
        utcEvent.CreatedAt.Should().NotBe(localEvent.CreatedAt);
    }

    [Fact]
    public void NotificationEvent_WithLongMessage_ShouldStoreCorrectly()
    {
        // Arrange
        var longMessage = new string('x', 1000);

        // Act
        var sut = new NotificationEvent
        {
            Message = longMessage
        };

        // Assert
        sut.Message.Should().HaveLength(1000);
        sut.Message.Should().Be(longMessage);
    }
}

