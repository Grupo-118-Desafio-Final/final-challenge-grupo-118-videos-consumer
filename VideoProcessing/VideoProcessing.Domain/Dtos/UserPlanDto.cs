using System.Diagnostics.CodeAnalysis;

namespace VideoProcessing.Domain.Dtos;

[ExcludeFromCodeCoverage]
public sealed record UserPlanDto(string Name, decimal Price, int ImageQuality, string MaxSizeInMegaBytes, string MaxDurationInSeconds, int DesiredFrames);
