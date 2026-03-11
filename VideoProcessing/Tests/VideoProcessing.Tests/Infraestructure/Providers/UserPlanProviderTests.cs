using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using VideoProcessing.Domain.Dtos;
using VideoProcessing.Infrastructure.Providers;

namespace VideoProcessing.Tests.Infraestructure.Providers;

public class UserPlanProviderTests
{
    private readonly ILogger<UserPlanProvider> _logger;

    public UserPlanProviderTests()
    {
        _logger = Substitute.For<ILogger<UserPlanProvider>>();
    }

    [Fact]
    public async Task GetPlanAsync_WithValidPlanId_ShouldReturnUserPlanDto()
    {
        // Arrange
        var planId = "plan123";
        var expectedPlan = new UserPlanDto("Premium", 29.99m, 1080, "100", "600", "4");
        
        var messageHandler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(expectedPlan));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        var result = await provider.GetPlanAsync(planId);

        // Assert
        result.Should().NotBeNull();
        result.ImageQuality.Should().Be(1080);
    }

    [Fact]
    public async Task GetPlanAsync_ShouldCallCorrectEndpoint()
    {
        // Arrange
        var planId = "plan123";
        var expectedPlan = new UserPlanDto("Basic", 9.99m, 720, "50", "300", "2");
        
        var messageHandler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(expectedPlan));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        await provider.GetPlanAsync(planId);

        // Assert
        messageHandler.LastRequestUri.Should().NotBeNull();
        messageHandler.LastRequestUri!.ToString().Should().Contain($"/plan/{planId}");
    }

    [Fact]
    public async Task GetPlanAsync_WithSpecialCharactersInPlanId_ShouldEscapeCorrectly()
    {
        // Arrange
        var planId = "plan+with spaces&special=chars";
        var expectedPlan = new UserPlanDto("Premium", 29.99m, 1080, "100", "600", "4");
        
        var messageHandler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(expectedPlan));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        await provider.GetPlanAsync(planId);

        // Assert
        messageHandler.LastRequestUri.Should().NotBeNull();
        // The actual escaping might use + for spaces (form encoding) rather than %20
        messageHandler.LastRequestUri!.ToString().Should().Contain("plan%2B");
        messageHandler.LastRequestUri!.ToString().Should().Contain("%26special%3Dchars");
    }

    [Fact]
    public async Task GetPlanAsync_WhenApiReturnsNotFound_ShouldThrowHttpRequestException()
    {
        // Arrange
        var planId = "nonexistent";
        
        var messageHandler = new MockHttpMessageHandler(
            HttpStatusCode.NotFound,
            new StringContent("Not found"));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        var act = async () => await provider.GetPlanAsync(planId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*status 404*");
    }

    [Fact]
    public async Task GetPlanAsync_WhenApiReturnsInternalServerError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var planId = "plan123";
        
        var messageHandler = new MockHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            new StringContent("Server error"));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        var act = async () => await provider.GetPlanAsync(planId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*status 500*");
    }

    [Fact]
    public async Task GetPlanAsync_WhenHttpClientThrowsException_ShouldThrowHttpRequestException()
    {
        // Arrange
        var planId = "plan123";
        
        var messageHandler = new MockHttpMessageHandler(new HttpRequestException("Network error"));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        var act = async () => await provider.GetPlanAsync(planId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Failed to call user plan API*");
    }

    [Fact]
    public async Task GetPlanAsync_WhenApiCallFails_ShouldLogError()
    {
        // Arrange
        var planId = "plan123";
        
        var messageHandler = new MockHttpMessageHandler(new HttpRequestException("Network error"));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        try
        {
            await provider.GetPlanAsync(planId);
        }
        catch
        {
            // Expected
        }

        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to call user plan API")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task GetPlanAsync_WhenApiReturnsNonSuccessStatus_ShouldLogWarning()
    {
        // Arrange
        var planId = "plan123";
        
        var messageHandler = new MockHttpMessageHandler(
            HttpStatusCode.BadRequest,
            new StringContent("Bad request"));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        try
        {
            await provider.GetPlanAsync(planId);
        }
        catch
        {
            // Expected
        }

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("status 400")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task GetPlanAsync_WithDifferentMaxResolutions_ShouldDeserializeCorrectly()
    {
        // Arrange
        var planId = "plan123";
        var expectedPlan = new UserPlanDto("Ultra", 49.99m, 2160, "200", "1200", "8");
        
        var messageHandler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(expectedPlan));
        
        var httpClient = new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        
        var provider = new UserPlanProvider(httpClient, _logger);

        // Act
        var result = await provider.GetPlanAsync(planId);

        // Assert
        result.ImageQuality.Should().Be(2160);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _statusCode;
        private readonly HttpContent? _content;
        private readonly Exception? _exception;

        public Uri? LastRequestUri { get; private set; }

        public MockHttpMessageHandler(HttpStatusCode statusCode, HttpContent content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        public MockHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            if (_exception != null)
            {
                throw _exception;
            }

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode!.Value,
                Content = _content
            });
        }
    }
}

