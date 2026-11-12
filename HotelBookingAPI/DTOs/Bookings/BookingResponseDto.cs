using HotelBookingAPI.Models.Enums;

namespace HotelBookingAPI.DTOs.Bookings;

public class BookingResponseDto
{
    public int Id { get; set; }

    public string BookingReference { get; set; } = string.Empty;

    public string HotelName { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public string RoomTypeName { get; set; } = string.Empty;

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int NumberOfGuests { get; set; }

    public int NumberOfNights { get; set; }

    public decimal PricePerNight { get; set; }

    public decimal TotalPrice { get; set; }

    public string Currency { get; set; } = "USD";

    public BookingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public String UserId { get; set; } = string.Empty;
}