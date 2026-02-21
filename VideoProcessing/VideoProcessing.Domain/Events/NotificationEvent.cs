namespace VideoProcessing.Domain.Events;

public class NotificationEvent
{
    public bool IsSuccess { get; set; }
    public string UserId { get; set; }
    public string Message { get; set; }
    public string ExceptionMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
