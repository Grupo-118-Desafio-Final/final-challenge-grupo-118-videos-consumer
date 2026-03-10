using VideoProcessing.Domain.Events;

namespace VideoProcessing.Domain.Ports.In;

public interface IProcessVideoUseCase
{
    Task ExecuteAsync(VideoProcessingEvent message);
}
