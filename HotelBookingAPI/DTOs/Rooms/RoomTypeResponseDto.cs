namespace HotelBookingAPI.DTOs.Rooms;

public class RoomTypeResponseDto
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int MaxCapacity { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; }
}