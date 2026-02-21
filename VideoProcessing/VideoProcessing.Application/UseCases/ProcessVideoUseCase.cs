using VideoProcessing.Domain.Events;
using VideoProcessing.Domain.Ports.In;
using VideoProcessing.Domain.Ports.On;
using VideoProcessing.Infrastructure.Messaging;

namespace VideoProcessing.Application.UseCases;

public class ProcessVideoUseCase : IProcessVideoUseCase
{
    private readonly IUserPlanProvider _planProvider;
    private readonly IVideoDownloader _downloader;
    private readonly IFrameExtractor _extractor;
    private readonly IZipService _zipService;
    private readonly IFileStorage _storage;
    private readonly VideoProcessedMessageProducer _producer;

    public ProcessVideoUseCase(
        IUserPlanProvider planProvider,
        IVideoDownloader downloader,
        IFrameExtractor extractor,
        IZipService zipService,
        IFileStorage storage,
        VideoProcessedMessageProducer producer)
    {
        _planProvider = planProvider;
        _downloader = downloader;
        _extractor = extractor;
        _zipService = zipService;
        _storage = storage;
        _producer = producer;
    }

    public async Task ExecuteAsync(VideoProcessingEvent message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message.PlanId))
                throw new ArgumentException("planId cannot be null or empty", nameof(message.PlanId));

            var userPlan = await _planProvider.GetPlanAsync(message.PlanId);

            var videoLocalPath = await _downloader.DownloadAsync(message.BlobUrl);

            var pathFrames = await _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality);

            var zipPath = await _zipService.CreateZipAsync(pathFrames);
            var zipBlobUrl = await _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId);

            var processedMessage = new NotificationEvent
            {
                IsSuccess = true,
                Message = "Video processed successfully",
                UserId = message.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await _producer.PublishAsync(processedMessage);
        }
        catch (Exception ex)
        {
            var processedMessage = new NotificationEvent
            {
                IsSuccess = false,
                Message = "Error processing video",
                UserId = message.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await _producer.PublishAsync(processedMessage);
            throw;
        }
    }
}
