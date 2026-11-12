namespace HotelBookingAPI.DTOs.Bookings;

public class AvailableRoomResponseDto
{
    public int RoomId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public int Floor { get; set; }

    public int Capacity { get; set; }

    public string RoomTypeName { get; set; } = string.Empty;

    public string HotelName { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public int NumberOfNights { get; set; }

    public decimal EstimatedTotalPrice { get; set; }
}