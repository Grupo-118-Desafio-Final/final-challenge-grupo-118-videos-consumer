using System.Text.Json;
using VideoProcessing.Domain.Events;

namespace VideoProcessing.Tests.Domain.Events;

public class NotificationEventTests
{
    [Fact]
    public void Properties_CanBeAssignedAndRead()
    {
        var evt = new NotificationEvent
        {
            IsSuccess = true,
            UserId = "user-1",
            Message = "done",
            ExceptionMessage = "none",
            CreatedAt = new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero)
        };

        Assert.True(evt.IsSuccess);
        Assert.Equal("user-1", evt.UserId);
        Assert.Equal("done", evt.Message);
        Assert.Equal("none", evt.ExceptionMessage);
        Assert.Equal(new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero), evt.CreatedAt);
    }

    [Fact]
    public void JsonSerialization_UsesConfiguredPropertyNames()
    {
        var createdAt = new DateTimeOffset(new DateTime(2024, 1, 1, 12, 0, 0), TimeSpan.Zero);
        var evt = new NotificationEvent
        {
            IsSuccess = false,
            UserId = "u1",
            Message = "msg",
            ExceptionMessage = "ex",
            CreatedAt = createdAt
        };

        var json = JsonSerializer.Serialize(evt);

        Assert.Contains("\"isSuccess\":", json);
        Assert.Contains("\"userId\":", json);
        Assert.Contains("\"message\":", json);
        Assert.Contains("\"exceptionMessage\":", json);
        Assert.Contains("\"createdAt\":", json);
        Assert.Contains("\"u1\"", json);
        Assert.Contains("msg", json);
    }
}
