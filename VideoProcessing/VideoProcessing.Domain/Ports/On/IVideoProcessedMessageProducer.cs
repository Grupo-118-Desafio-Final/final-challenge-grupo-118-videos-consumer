using VideoProcessing.Domain.Events;

namespace VideoProcessing.Domain.Ports.On;

public interface IVideoProcessedMessageProducer
{
    Task PublishAsync(NotificationEvent message);
}