
namespace HotelReservationSystem.Application.Exceptions;

public sealed class IdempotencyKeyAlreadyExistsException : Exception
{
    public IdempotencyKeyAlreadyExistsException(Exception innerException)
        : base("An idempotency key already exists.", innerException)
    {
    }
}
