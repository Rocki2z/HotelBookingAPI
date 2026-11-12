using HotelBookingAPI.Models.Enums;

namespace HotelBookingAPI.Models;

public class Booking
{
    public int Id { get; set; }

    public string BookingReference { get; set; } =
        Guid.NewGuid().ToString("N").ToUpper();

    // Customer who made this booking
    public string UserId { get; set; } = string.Empty;

    // Physical room being reserved
    public int RoomId { get; set; }

    // Booking dates (hotel calendar dates)
    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int NumberOfGuests { get; set; }

    // Price snapshots at booking time
    public decimal PricePerNight { get; set; }

    public int NumberOfNights { get; set; }

    public decimal TotalPrice { get; set; }

    public string Currency { get; set; } = "USD";

    public BookingStatus Status { get; set; } =
        BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime? CheckedInAt { get; set; }

    public DateTime? CheckedOutAt { get; set; }

    public string? CancellationReason { get; set; }

    // Relationships
    public ApplicationUser User { get; set; } = null!;

    public Room Room { get; set; } = null!;

    // Payments associated with this booking
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}