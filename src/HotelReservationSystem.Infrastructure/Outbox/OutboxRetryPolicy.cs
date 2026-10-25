namespace HotelReservationSystem.Infrastructure.Outbox;

public static class OutboxRetryPolicy
{
    public const int MaxRetries = 5;

    public static DateTime GetNextRetryAt(int retryCount, DateTime utcNow)
    {
        if (retryCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(retryCount));
        }

        var delaySeconds = Math.Pow(2, retryCount);

        return utcNow.AddSeconds(delaySeconds);
    }
}
