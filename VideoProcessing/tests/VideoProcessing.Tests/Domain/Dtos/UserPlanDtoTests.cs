using VideoProcessing.Domain.Dtos;

namespace VideoProcessing.Tests.Domain.Dtos;

public class UserPlanDtoTests
{
    [Fact]
    public void Constructor_AssignsProperties()
    {
        var dto = new UserPlanDto("Basic", 9.99m, 80, "10", "60", "4");

        Assert.Equal("Basic", dto.Name);
        Assert.Equal(9.99m, dto.Price);
        Assert.Equal(80, dto.ImageQuality);
        Assert.Equal("10", dto.MaxSizeInMegaBytes);
        Assert.Equal("60", dto.MaxDurationInSeconds);
        Assert.Equal("4", dto.Threads);
    }

    [Fact]
    public void Records_AreValueEqual()
    {
        var a = new UserPlanDto("Basic", 9.99m, 80, "10", "60", "4");
        var b = new UserPlanDto("Basic", 9.99m, 80, "10", "60", "4");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void WithExpression_CreatesNewInstance()
    {
        var a = new UserPlanDto("Basic", 9.99m, 80, "10", "60", "4");
        var b = a with { Price = 19.99m };

        Assert.NotSame(a, b);
        Assert.Equal(19.99m, b.Price);
        Assert.Equal(a.Name, b.Name);
    }
}