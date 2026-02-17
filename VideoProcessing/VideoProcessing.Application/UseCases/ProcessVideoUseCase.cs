using VideoProcessing.Domain.Dtos;
using VideoProcessing.Domain.Events;
using VideoProcessing.Domain.Ports.In;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Application.UseCases;

public class ProcessVideoUseCase : IProcessVideoUseCase
{
    private readonly IUserPlanProvider _planProvider;
    private readonly IVideoDownloader _downloader;
    private readonly IFrameExtractor _extractor;
    private readonly IZipService _zipService;
    private readonly IFileStorage _storage;
    private readonly IProcessingPublisher _publisher;

    public ProcessVideoUseCase(
        IUserPlanProvider planProvider,
        IVideoDownloader downloader,
        IFrameExtractor extractor,
        IZipService zipService,
        IFileStorage storage,
        IProcessingPublisher publisher)
    {
        _planProvider = planProvider;
        _downloader = downloader;
        _extractor = extractor;
        _zipService = zipService;
        _storage = storage;
        _publisher = publisher;
    }

    public async Task ExecuteAsync(VideoProcessingEvent message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message.PlanId))
                throw new ArgumentException("planId cannot be null or empty", nameof(message.PlanId));

            //var userPlan = await _planProvider.GetPlanAsync(message.PlanId);
            var userPlan = new UserPlanDto("Basic", 10, 480, "10", "10", "10");
            var videoLocalPath = await _downloader.DownloadAsync(message.BlobUrl);

            var pathFrames = await _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality);

            var zipPath = await _zipService.CreateZipAsync(pathFrames);
            var zipBlobUrl = await _storage.UploadAsync(zipPath);

            //await _publisher.PublishSuccessAsync(message.ProcessingId, zipBlobUrl);
        }
        catch (Exception ex)
        {
            await _publisher.PublishErrorAsync(message.ProcessingId, ex.Message);
            throw;
        }
    }
}
