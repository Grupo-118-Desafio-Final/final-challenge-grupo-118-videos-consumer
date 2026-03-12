using VideoProcessing.Domain.Enums;
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
    private readonly IVideoProcessedMessageProducer _producer;
    private readonly IProcessingRepository _processingRepository;
    private readonly IFileSystem _fileSystem;

    public ProcessVideoUseCase(
        IUserPlanProvider planProvider,
        IVideoDownloader downloader,
        IFrameExtractor extractor,
        IZipService zipService,
        IFileStorage storage,
        IVideoProcessedMessageProducer producer,
        IProcessingRepository processingRepository,
        IFileSystem fileSystem)
    {
        _planProvider = planProvider;
        _downloader = downloader;
        _extractor = extractor;
        _zipService = zipService;
        _storage = storage;
        _producer = producer;
        _processingRepository = processingRepository;
        _fileSystem = fileSystem;
    }

    public async Task ExecuteAsync(VideoProcessingEvent message)
    {
        string videoLocalPath, zipPath;
        videoLocalPath = zipPath = string.Empty;

        var pathFrames = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(message.PlanId))
                throw new ArgumentException("planId cannot be null or empty", nameof(message.PlanId));

            var userPlan = await _planProvider.GetPlanAsync(message.PlanId);

            videoLocalPath = await _downloader.DownloadAsync(message.BlobUrl);

            pathFrames =
                await _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality, userPlan.DesiredFrames);

            zipPath = await _zipService.CreateZipAsync(pathFrames);
            var zipBlobUrl = await _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId);

            await _processingRepository.UpdateProcessing(message.ProcessingId, ProcessingStatus.Processed, zipBlobUrl);

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
            await _processingRepository.UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);

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
        finally
        {
            _fileSystem.DeleteFile(zipPath);
            _fileSystem.DeleteFile(videoLocalPath);
            _fileSystem.DeleteFiles(pathFrames);
        }
    }
}