namespace HotelBookingAPI.DTOs.Rooms;

public class RoomResponseDto
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }

    public string RoomTypeName { get; set; } = string.Empty;

    public string HotelName { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public int Floor { get; set; }

    public int Capacity { get; set; }

    public decimal PricePerNight { get; set; }

    public string OccupancyStatus { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}