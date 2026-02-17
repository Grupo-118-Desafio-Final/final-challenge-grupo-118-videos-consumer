using System.Net.Http.Json;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VideoProcessing.Domain.Ports.On;
using VideoProcessing.Domain.Dtos;

namespace VideoProcessing.Infrastructure.Providers;

public class UserPlanProvider : IUserPlanProvider
{
    private static readonly ActivitySource Activity = new("VideoProcessing.UserPlanProvider");

    private readonly HttpClient _httpClient;
    private readonly ILogger<UserPlanProvider> _logger;

    public UserPlanProvider(HttpClient httpClient, ILogger<UserPlanProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserPlanDto> GetPlanAsync(string planId)
    {
        var requestUri = $"/plan/GetById?id={Uri.EscapeDataString(planId)}";

        using var activity = Activity.StartActivity("GetUserPlan", ActivityKind.Client);
        activity?.SetTag("user.id", planId);
        activity?.SetTag("http.url", requestUri);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(requestUri);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to call user plan API for {UserId}", planId);
            throw new HttpRequestException("Failed to call user plan API", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var message = $"User plan API returned status {(int)response.StatusCode}";
            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetStatus(ActivityStatusCode.Error, message);
            _logger.LogWarning("{Message} for {UserId}", message, planId);
            throw new HttpRequestException(message);
        }

        return await response.Content.ReadFromJsonAsync<UserPlanDto>();
    }
}
