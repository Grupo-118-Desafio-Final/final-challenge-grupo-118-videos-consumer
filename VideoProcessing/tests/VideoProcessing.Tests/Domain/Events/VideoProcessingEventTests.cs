using FluentAssertions;
using VideoProcessing.Domain.Events;

namespace VideoProcessing.Tests.Domain.Events
{
    public class VideoProcessingEventTests
    {
        [Fact]
        public void ShouldCreateInstanceAndAllowSetGetProperties()
        {
            // Arrange & Act
            var now = new DateTime(2026, 3, 11, 10, 30, 0, DateTimeKind.Utc);
            var evt = new VideoProcessingEvent
            {
                UserId = "user123",
                PlanId = "plan456",
                ProcessingId = "proc789",
                BlobUrl = "https://example.com/video.mp4",
                EventAt = now
            };

            // Assert
            evt.UserId.Should().Be("user123");
            evt.PlanId.Should().Be("plan456");
            evt.ProcessingId.Should().Be("proc789");
            evt.BlobUrl.Should().Be("https://example.com/video.mp4");
            evt.EventAt.Should().Be(now);
        }
    }
}