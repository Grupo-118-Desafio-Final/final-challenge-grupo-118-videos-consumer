using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace VideoProcessing.Domain.Events;

[ExcludeFromCodeCoverage]
public class NotificationEvent
{
    [JsonPropertyName(("isSuccess"))] public bool IsSuccess { get; set; }
    [JsonPropertyName(("userId"))] public string UserId { get; set; }

    [JsonPropertyName(("message"))] public string Message { get; set; }

    [JsonPropertyName(("exceptionMessage"))]
    public string ExceptionMessage { get; set; }

    [JsonPropertyName(("createdAt"))] public DateTimeOffset CreatedAt { get; set; }
}