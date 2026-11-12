using HotelBookingAPI.Models.Enums;

namespace HotelBookingAPI.Models;

public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }

    public string PaymentReference { get; set; } =
        Guid.NewGuid().ToString("N").ToUpper();

    public string? ProviderTransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; } =
        PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public DateTime? RefundedAt { get; set; }

    public string? FailureReason { get; set; }

    public Booking Booking { get; set; } = null!;
}