public interface IReservationRepository
{
    Task AddAsync(Reservation reservation);
}