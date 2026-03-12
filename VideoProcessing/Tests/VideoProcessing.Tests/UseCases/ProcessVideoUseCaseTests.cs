using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using VideoProcessing.Application.UseCases;
using VideoProcessing.Domain.Dtos;
using VideoProcessing.Domain.Enums;
using VideoProcessing.Domain.Events;
using VideoProcessing.Domain.Ports.On;

namespace VideoProcessing.Tests.UseCases;

public class ProcessVideoUseCaseTests
{
    private readonly IUserPlanProvider _planProvider;
    private readonly IVideoDownloader _downloader;
    private readonly IFrameExtractor _extractor;
    private readonly IZipService _zipService;
    private readonly IFileStorage _storage;
    private readonly IVideoProcessedMessageProducer _producer;
    private readonly IProcessingRepository _processingRepository;
    private readonly ProcessVideoUseCase _useCase;

    public ProcessVideoUseCaseTests()
    {
        _planProvider = Substitute.For<IUserPlanProvider>();
        _downloader = Substitute.For<IVideoDownloader>();
        _extractor = Substitute.For<IFrameExtractor>();
        _zipService = Substitute.For<IZipService>();
        _storage = Substitute.For<IFileStorage>();
        _producer = Substitute.For<IVideoProcessedMessageProducer>();
        _processingRepository = Substitute.For<IProcessingRepository>();

        _useCase = new ProcessVideoUseCase(
            _planProvider,
            _downloader,
            _extractor,
            _zipService,
            _storage,
            _producer,
            _processingRepository
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_ShouldProcessVideoAndPublishSuccessMessage()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        var userPlan = new UserPlanDto("Premium", 29.99m, 1080, "100", "300", 10);
        var videoLocalPath = "/tmp/video.mp4";
        var frames = new List<string> { "/tmp/frame1.jpg", "/tmp/frame2.jpg" };
        var zipPath = "/tmp/frames.zip";
        var zipBlobUrl = "https://blob.com/frames.zip";

        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality).Returns(frames);
        _zipService.CreateZipAsync(frames).Returns(zipPath);
        _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId).Returns(zipBlobUrl);

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        await _planProvider.Received(1).GetPlanAsync(message.PlanId);
        await _downloader.Received(1).DownloadAsync(message.BlobUrl);
        await _extractor.Received(1).ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality);
        await _zipService.Received(1).CreateZipAsync(frames);
        await _storage.Received(1).UploadAsync(zipPath, message.UserId, message.ProcessingId);
        await _processingRepository.Received(1)
            .UpdateProcessing(message.ProcessingId, ProcessingStatus.Processed, zipBlobUrl);

        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == true &&
            n.Message == "Video processed successfully" &&
            n.UserId == message.UserId
        ));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlanIdIsNull_ShouldThrowArgumentException()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = null!,
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("planId cannot be null or empty*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlanIdIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("planId cannot be null or empty*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlanIdIsWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "   ",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("planId cannot be null or empty*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDownloadFails_ShouldUpdateProcessingToFailedAndPublishErrorMessage()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        var userPlan = new UserPlanDto("Premium", 29.99m, 1080, "100", "300", 10);
        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Throws(new Exception("Download failed"));

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Download failed");
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
    }

    [Fact]
    public async Task ExecuteAsync_WhenFrameExtractionFails_ShouldUpdateProcessingToFailedAndPublishErrorMessage()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        var userPlan = new UserPlanDto("Premium", 29.99m, 1080, "100", "300", 10);
        var videoLocalPath = "/tmp/video.mp4";

        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality)
            .Throws(new Exception("Frame extraction failed"));

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Frame extraction failed");
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
    }

    [Fact]
    public async Task ExecuteAsync_WhenZipCreationFails_ShouldUpdateProcessingToFailedAndPublishErrorMessage()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        var userPlan = new UserPlanDto("Premium", 29.99m, 1080, "100", "300", 10);
        var videoLocalPath = "/tmp/video.mp4";
        var frames = new List<string> { "/tmp/frame1.jpg", "/tmp/frame2.jpg" };

        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality).Returns(frames);
        _zipService.CreateZipAsync(frames).Throws(new Exception("Zip creation failed"));

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Zip creation failed");
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageUploadFails_ShouldUpdateProcessingToFailedAndPublishErrorMessage()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        var userPlan = new UserPlanDto("Premium", 29.99m, 1080, "100", "300", 10);
        var videoLocalPath = "/tmp/video.mp4";
        var frames = new List<string> { "/tmp/frame1.jpg", "/tmp/frame2.jpg" };
        var zipPath = "/tmp/frames.zip";

        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality).Returns(frames);
        _zipService.CreateZipAsync(frames).Returns(zipPath);
        _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId)
            .Throws(new Exception("Upload failed"));

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Upload failed");
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
    }

    [Fact]
    public async Task ExecuteAsync_WhenGetPlanFails_ShouldUpdateProcessingToFailedAndPublishErrorMessage()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        _planProvider.GetPlanAsync(message.PlanId).ThrowsAsync(new Exception("Plan not found"));

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Plan not found");
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed, null);
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessingRepositoryUpdateFails_ShouldStillPublishErrorMessage()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        _planProvider.GetPlanAsync(message.PlanId).ThrowsAsync(new Exception("Plan not found"));

        // Act
        var act = async () => await _useCase.ExecuteAsync(message);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_ShouldCallServicesInCorrectOrder()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        var userPlan = new UserPlanDto("Premium", 29.99m, 1080, "100", "300", 10);
        var videoLocalPath = "/tmp/video.mp4";
        var frames = new List<string> { "/tmp/frame1.jpg", "/tmp/frame2.jpg" };
        var zipPath = "/tmp/frames.zip";
        var zipBlobUrl = "https://blob.com/frames.zip";

        var callOrder = new List<string>();

        _planProvider.GetPlanAsync(message.PlanId).Returns(x =>
        {
            callOrder.Add("GetPlan");
            return userPlan;
        });

        _downloader.DownloadAsync(message.BlobUrl).Returns(x =>
        {
            callOrder.Add("Download");
            return videoLocalPath;
        });

        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality).Returns(x =>
        {
            callOrder.Add("ExtractFrames");
            return frames;
        });

        _zipService.CreateZipAsync(frames).Returns(x =>
        {
            callOrder.Add("CreateZip");
            return zipPath;
        });

        _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId).Returns(x =>
        {
            callOrder.Add("Upload");
            return zipBlobUrl;
        });

        _processingRepository.UpdateProcessing(message.ProcessingId, ProcessingStatus.Processed, zipBlobUrl)
            .Returns(x =>
            {
                callOrder.Add("UpdateProcessing");
                return Task.CompletedTask;
            });

        _producer.PublishAsync(Arg.Any<NotificationEvent>()).Returns(x =>
        {
            callOrder.Add("PublishMessage");
            return Task.CompletedTask;
        });

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        callOrder.Should().Equal(
            "GetPlan",
            "Download",
            "ExtractFrames",
            "CreateZip",
            "Upload",
            "UpdateProcessing",
            "PublishMessage"
        );
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_ShouldUseCorrectImageQualityFromPlan()
    {
        // Arrange
        var message = new VideoProcessingEvent
        {
            UserId = "user123",
            PlanId = "plan123",
            ProcessingId = "processing123",
            BlobUrl = "https://blob.com/video.mp4",
            EventAt = DateTime.UtcNow
        };

        var expectedQuality = 720;
        var userPlan = new UserPlanDto("Basic", 9.99m, expectedQuality, "50", "180", 10);
        var videoLocalPath = "/tmp/video.mp4";
        var frames = new List<string> { "/tmp/frame1.jpg" };
        var zipPath = "/tmp/frames.zip";
        var zipBlobUrl = "https://blob.com/frames.zip";

        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, expectedQuality).Returns(frames);
        _zipService.CreateZipAsync(frames).Returns(zipPath);
        _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId).Returns(zipBlobUrl);

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        await _extractor.Received(1).ExtractFramesAsync(videoLocalPath, expectedQuality);
    }
}