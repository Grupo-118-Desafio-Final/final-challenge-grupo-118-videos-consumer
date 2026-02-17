namespace VideoProcessing.Domain.Events;

public class VideoProcessedEvent
{
    public string UserId { get; set; }
    public string ProcessingId { get; set; }
    public bool Success { get; set; }
    public string BlobUrl { get; set; }
    public DateTime EventAt { get; set; }
}
