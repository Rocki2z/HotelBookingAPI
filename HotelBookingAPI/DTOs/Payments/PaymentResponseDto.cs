using HotelBookingAPI.Models.Enums;

namespace HotelBookingAPI.DTOs.Payments;

public class PaymentResponseDto
{
    public int Id { get; set; }

    public string PaymentReference { get; set; } = string.Empty;

    public int BookingId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? RefundedAt { get; set; }
}