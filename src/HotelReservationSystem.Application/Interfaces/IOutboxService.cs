using HotelReservationSystem.Domain.Entities;
using System.Threading.Tasks;

public interface IOutboxService
{
    Task AddAsync(OutboxMessage message);
}