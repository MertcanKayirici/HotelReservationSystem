using System;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    public interface IUnitOfWork
    {
        Task ExecuteAsync(Func<Task> action);
    }
}