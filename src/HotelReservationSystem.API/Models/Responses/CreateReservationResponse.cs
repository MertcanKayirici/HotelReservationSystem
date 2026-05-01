namespace HotelReservationSystem.API.Models.Responses
{
    public class CreateReservationResponse
    {
        public string Status { get; set; } = "PENDING";
        public string RequestId { get; set; }
    }
}