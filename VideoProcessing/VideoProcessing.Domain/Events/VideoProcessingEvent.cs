namespace VideoProcessing.Domain.Events;

public class VideoProcessingEvent
{
    public string UserId { get; set; } 
    public string PlanId { get; set; }
    public string ProcessingId { get; set; }
    public string BlobUrl { get; set; }
    public DateTime EventAt { get; set; }
}
