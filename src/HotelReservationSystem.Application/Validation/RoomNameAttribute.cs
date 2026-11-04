using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Application.Validation
{
    public class RoomNameAttribute : ValidationAttribute
    {
        public RoomNameAttribute()
        {
            ErrorMessage = "Room name cannot be empty or contain only whitespace.";
        }

        public override bool IsValid(object? value)
        {
            return value is string name && !string.IsNullOrWhiteSpace(name);
        }
    }
}