using VideoProcessing.Domain.Dtos;
using VideoProcessing.Domain.Enums;

namespace VideoProcessing.Domain.Ports.On;

public interface IUserPlanProvider
{
    Task<UserPlanDto> GetPlanAsync(string planId);
}
