using HotelBookingAPI.DTOs.Payments;

namespace HotelBookingAPI.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto> ProcessPaymentAsync(
        int bookingId,
        string userId,
        CreatePaymentDto dto);

    Task<List<PaymentResponseDto>> GetMyPaymentsAsync(
        string userId);
    Task<bool> RefundForCancellationAsync(
        int bookingId,
        string userId);
}