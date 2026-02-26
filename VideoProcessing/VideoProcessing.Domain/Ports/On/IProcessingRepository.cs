using VideoProcessing.Domain.Enums;

namespace VideoProcessing.Domain.Ports.On;

public interface IProcessingRepository
{
    Task UpdateProcessing(string processingId, ProcessingStatus status, string? zipBlobUrl = null);
}
