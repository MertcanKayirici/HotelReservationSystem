
using HotelReservationSystem.Infrastructure.Outbox;
using Xunit;

namespace HotelReservationSystem.Tests;

public class OutboxRetryPolicyTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    [InlineData(5, 32)]
    public void GetNextRetryAt_ReturnsExpectedDelay(
        int retryCount,
        int expectedSeconds)
    {
        var now = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

        var result = OutboxRetryPolicy.GetNextRetryAt(retryCount, now);

        Assert.Equal(now.AddSeconds(expectedSeconds), result);
    }

    [Fact]
    public void MaxRetries_IsFive()
    {
        Assert.Equal(5, OutboxRetryPolicy.MaxRetries);
    }

    [Fact]
    public void GetNextRetryAt_Throws_WhenRetryCountIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OutboxRetryPolicy.GetNextRetryAt(0, DateTime.UtcNow));
    }

    [Fact]
    public void GetNextRetryAt_Throws_WhenRetryCountIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OutboxRetryPolicy.GetNextRetryAt(-1, DateTime.UtcNow));
    }
}
