using FluentAssertions;
using Microsoft.Extensions.Logging;
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
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ProcessVideoUseCase> _logger;

    public ProcessVideoUseCaseTests()
    {
        _planProvider = Substitute.For<IUserPlanProvider>();
        _downloader = Substitute.For<IVideoDownloader>();
        _extractor = Substitute.For<IFrameExtractor>();
        _zipService = Substitute.For<IZipService>();
        _storage = Substitute.For<IFileStorage>();
        _producer = Substitute.For<IVideoProcessedMessageProducer>();
        _processingRepository = Substitute.For<IProcessingRepository>();
        _fileSystem = Substitute.For<IFileSystem>();
        _logger = Substitute.For<ILogger<ProcessVideoUseCase>>();

        _useCase = new ProcessVideoUseCase(
            _planProvider,
            _downloader,
            _extractor,
            _zipService,
            _storage,
            _producer,
            _processingRepository,
            _fileSystem,
            _logger
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality, userPlan.DesiredFrames).Returns(frames);
        _zipService.CreateZipAsync(frames).Returns(zipPath);
        _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId).Returns(zipBlobUrl);

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        await _planProvider.Received(1).GetPlanAsync(message.PlanId);
        await _downloader.Received(1).DownloadAsync(message.BlobUrl);
        await _extractor.Received(1).ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality, userPlan.DesiredFrames);
        await _zipService.Received(1).CreateZipAsync(frames);
        await _storage.Received(1).UploadAsync(zipPath, message.UserId, message.ProcessingId);
        await _processingRepository.Received(1)
            .UpdateProcessing(message.ProcessingId, ProcessingStatus.Processed, zipBlobUrl);

        var blobUrlForNotification = $@"<a href='{zipBlobUrl}'>Download de Zip Here</a>";
        
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == true &&
            n.Message == $"Video processed successfully. To download the zip file, click in the link: {blobUrlForNotification}" &&
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);
        await _producer.Received(1).PublishAsync(Arg.Is<NotificationEvent>(n =>
            n.IsSuccess == false &&
            n.Message == "Error processing video" &&
            n.UserId == message.UserId
        ));
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
        
        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Throws(new Exception("Download failed"));

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality, userPlan.DesiredFrames)
            .Throws(new Exception("Frame extraction failed"));

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality, userPlan.DesiredFrames).Returns(frames);
        _zipService.CreateZipAsync(frames).Throws(new Exception("Zip creation failed"));

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality, userPlan.DesiredFrames).Returns(frames);
        _zipService.CreateZipAsync(frames).Returns(zipPath);
        _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId)
            .Throws(new Exception("Upload failed"));

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).ThrowsAsync(new Exception("Plan not found"));

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Failed);
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).ThrowsAsync(new Exception("Plan not found"));

        // Act
        var result = await _useCase.ExecuteAsync(message);

        // Assert
        result.Should().BeFalse();
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        
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

        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality, userPlan.DesiredFrames).Returns(x =>
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, expectedQuality, userPlan.DesiredFrames).Returns(frames);
        _zipService.CreateZipAsync(frames).Returns(zipPath);
        _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId).Returns(zipBlobUrl);

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        await _extractor.Received(1).ExtractFramesAsync(videoLocalPath, expectedQuality, userPlan.DesiredFrames);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStatusIsProcessed_ShouldNotProcessAgain()
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.Processed);

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        await _planProvider.DidNotReceive().GetPlanAsync(Arg.Any<string>());
        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<string>());
        await _processingRepository.DidNotReceive().UpdateProcessing(Arg.Any<string>(), Arg.Any<ProcessingStatus>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenStatusIsFailed_ShouldNotProcessAgain()
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.Failed);

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        await _planProvider.DidNotReceive().GetPlanAsync(Arg.Any<string>());
        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<string>());
        await _processingRepository.DidNotReceive().UpdateProcessing(Arg.Any<string>(), Arg.Any<ProcessingStatus>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenStatusIsProcessing_ShouldNotProcessAgain()
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.Processing);

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        await _planProvider.DidNotReceive().GetPlanAsync(Arg.Any<string>());
        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<string>());
        await _processingRepository.DidNotReceive().UpdateProcessing(Arg.Any<string>(), Arg.Any<ProcessingStatus>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_ShouldUpdateToProcessingBeforeStarting()
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

        _processingRepository.GetProcessingStatus(message.ProcessingId).Returns(ProcessingStatus.NotStarted);
        _planProvider.GetPlanAsync(message.PlanId).Returns(userPlan);
        _downloader.DownloadAsync(message.BlobUrl).Returns(videoLocalPath);
        _extractor.ExtractFramesAsync(videoLocalPath, userPlan.ImageQuality, userPlan.DesiredFrames).Returns(frames);
        _zipService.CreateZipAsync(frames).Returns(zipPath);
        _storage.UploadAsync(zipPath, message.UserId, message.ProcessingId).Returns(zipBlobUrl);

        // Act
        await _useCase.ExecuteAsync(message);

        // Assert
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Processing);
        await _processingRepository.Received(1).UpdateProcessing(message.ProcessingId, ProcessingStatus.Processed, zipBlobUrl);
    }
}