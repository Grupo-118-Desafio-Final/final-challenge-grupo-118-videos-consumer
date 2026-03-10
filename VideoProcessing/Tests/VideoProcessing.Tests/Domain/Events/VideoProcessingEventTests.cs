using FluentAssertions;
using VideoProcessing.Domain.Events;

namespace VideoProcessing.Tests.Domain.Events;

public class VideoProcessingEventTests
{
    [Fact]
    public void VideoProcessingEvent_ShouldSetAndGetProperties()
    {
        // Arrange
        var userId = "user123";
        var planId = "plan456";
        var processingId = "proc789";
        var blobUrl = "https://blob.example.com/video.mp4";
        var eventAt = DateTime.UtcNow;

        // Act
        var sut = new VideoProcessingEvent
        {
            UserId = userId,
            PlanId = planId,
            ProcessingId = processingId,
            BlobUrl = blobUrl,
            EventAt = eventAt
        };

        // Assert
        sut.UserId.Should().Be(userId);
        sut.PlanId.Should().Be(planId);
        sut.ProcessingId.Should().Be(processingId);
        sut.BlobUrl.Should().Be(blobUrl);
        sut.EventAt.Should().Be(eventAt);
    }

    [Fact]
    public void VideoProcessingEvent_DefaultValues_ShouldBeNull()
    {
        // Act
        var sut = new VideoProcessingEvent();

        // Assert
        sut.UserId.Should().BeNull();
        sut.PlanId.Should().BeNull();
        sut.ProcessingId.Should().BeNull();
        sut.BlobUrl.Should().BeNull();
        sut.EventAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void VideoProcessingEvent_WithAllProperties_ShouldBeValid()
    {
        // Arrange & Act
        var sut = new VideoProcessingEvent
        {
            UserId = "user-abc-123",
            PlanId = "plan-premium-001",
            ProcessingId = "processing-xyz-789",
            BlobUrl = "https://storage.blob.core.windows.net/videos/user123/video.mp4",
            EventAt = new DateTime(2024, 3, 10, 12, 0, 0, DateTimeKind.Utc)
        };

        // Assert
        sut.Should().NotBeNull();
        sut.UserId.Should().NotBeNullOrEmpty();
        sut.PlanId.Should().NotBeNullOrEmpty();
        sut.ProcessingId.Should().NotBeNullOrEmpty();
        sut.BlobUrl.Should().NotBeNullOrEmpty();
        sut.EventAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void VideoProcessingEvent_Properties_ShouldBeMutable()
    {
        // Arrange
        var sut = new VideoProcessingEvent
        {
            UserId = "user1",
            PlanId = "plan1"
        };

        // Act
        sut.UserId = "user2";
        sut.PlanId = "plan2";

        // Assert
        sut.UserId.Should().Be("user2");
        sut.PlanId.Should().Be("plan2");
    }
}

