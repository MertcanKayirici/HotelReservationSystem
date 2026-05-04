namespace HotelReservationSystem.Domain.Entities;

public class Room
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool Amenities_HasAirConditioning { get; set; }

    public bool Amenities_HasBalcony { get; set; }

    public bool Amenities_HasFreeWifi { get; set; }

    public bool Amenities_HasJacuzzi { get; set; }

    public bool Amenities_HasKingBed { get; set; }

    public bool Amenities_HasMiniBar { get; set; }

    public bool Amenities_HasSeaView { get; set; }

    public bool Amenities_HasSmartSystem { get; set; }

    public int AvailableCount { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool HasDiscount { get; set; }

    public bool IsAiRecommended { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsLastFewAvailable { get; set; }

    public decimal OriginalPrice { get; set; }

    public decimal PricePerNight { get; set; }

    public string ThumbnailUrl { get; set; } = string.Empty;

    public int Type { get; set; }
}