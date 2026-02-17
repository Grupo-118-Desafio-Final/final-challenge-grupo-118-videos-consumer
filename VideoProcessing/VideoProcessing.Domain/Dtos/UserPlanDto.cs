namespace VideoProcessing.Domain.Dtos;

public sealed record UserPlanDto(string Name, decimal Price, int ImageQuality, string MaxSizeInMegaBytes, string MaxDurationInSeconds, string Threads);
