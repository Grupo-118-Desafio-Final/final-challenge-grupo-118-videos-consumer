using VideoProcessing.Domain.Dtos;

namespace VideoProcessing.Domain.Ports.On;

public interface IUserPlanProvider
{
    Task<UserPlanDto> GetPlanAsync(string planId);
}
