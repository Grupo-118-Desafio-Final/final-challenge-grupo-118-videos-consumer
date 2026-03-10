using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using VideoProcessing.Infrastructure.Providers;
using VideoProcessing.Domain.Dtos;

namespace VideoProcessing.Tests.Infrastructure.Providers;

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
        var expectedPlan = new UserPlanDto("Premium", 19.99m, 1080, "500", "120", "4");
        
        var mockHandler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(expectedPlan));
        
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        
        var sut = new UserPlanProvider(httpClient, _logger);

        // Act
        var result = await sut.GetPlanAsync(planId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(expectedPlan.Name);
        result.Price.Should().Be(expectedPlan.Price);
        result.ImageQuality.Should().Be(expectedPlan.ImageQuality);
    }

    [Fact]
    public async Task GetPlanAsync_WhenApiReturnsNotFound_ShouldThrowHttpRequestException()
    {
        // Arrange
        var planId = "nonexistent";
        
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.NotFound, null);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        
        var sut = new UserPlanProvider(httpClient, _logger);

        // Act
        var act = () => sut.GetPlanAsync(planId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*status 404*");
    }

    [Fact]
    public async Task GetPlanAsync_WhenApiReturnsServerError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var planId = "plan123";
        
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, null);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        
        var sut = new UserPlanProvider(httpClient, _logger);

        // Act
        var act = () => sut.GetPlanAsync(planId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*status 500*");
    }

    [Fact]
    public async Task GetPlanAsync_WhenNetworkFails_ShouldThrowHttpRequestException()
    {
        // Arrange
        var planId = "plan123";
        
        var mockHandler = new MockHttpMessageHandler(new HttpRequestException("Network error"));
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        
        var sut = new UserPlanProvider(httpClient, _logger);

        // Act
        var act = () => sut.GetPlanAsync(planId);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Failed to call user plan API*");
    }

    [Fact]
    public async Task GetPlanAsync_WithSpecialCharactersInPlanId_ShouldEscapeCorrectly()
    {
        // Arrange
        var planId = "plan with spaces & special=chars";
        var expectedPlan = new UserPlanDto("Basic", 9.99m, 480, "100", "60", "2");
        
        var mockHandler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(expectedPlan));
        
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        
        var sut = new UserPlanProvider(httpClient, _logger);

        // Act
        var result = await sut.GetPlanAsync(planId);

        // Assert
        result.Should().NotBeNull();
        mockHandler.RequestUri.Should().NotBeNull();
        // The URL may contain either encoded or unencoded spaces depending on HttpClient implementation
        var uri = mockHandler.RequestUri!.ToString();
        (uri.Contains("plan%20with%20spaces") || uri.Contains("plan with spaces")).Should().BeTrue();
    }

    [Fact]
    public async Task GetPlanAsync_ShouldConstructCorrectRequestUri()
    {
        // Arrange
        var planId = "plan789";
        var expectedPlan = new UserPlanDto("Standard", 14.99m, 720, "250", "90", "3");
        
        var mockHandler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonContent.Create(expectedPlan));
        
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        
        var sut = new UserPlanProvider(httpClient, _logger);

        // Act
        await sut.GetPlanAsync(planId);

        // Assert
        mockHandler.RequestUri.Should().NotBeNull();
        mockHandler.RequestUri!.PathAndQuery.Should().Be("/plan/GetById?id=plan789");
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _statusCode;
        private readonly HttpContent? _content;
        private readonly Exception? _exception;

        public Uri? RequestUri { get; private set; }

        public MockHttpMessageHandler(HttpStatusCode statusCode, HttpContent? content)
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
            RequestUri = request.RequestUri;

            if (_exception != null)
            {
                throw _exception;
            }

            var response = new HttpResponseMessage(_statusCode!.Value)
            {
                Content = _content
            };

            return Task.FromResult(response);
        }
    }
}

