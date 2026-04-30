public class ReservationService : IReservationService
{
    private readonly IReservationRepository _repository;

    public ReservationService(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task CreateReservationAsync(CreateReservationDto dto)
    {
        var reservation = new Reservation
        {
            RoomId = dto.RoomId,
            UserId = dto.UserId,
            CheckInDate = dto.CheckInDate,
            CheckOutDate = dto.CheckOutDate,
            CreatedAt = DateTime.Now
        };

        await _repository.AddAsync(reservation);
    }
}