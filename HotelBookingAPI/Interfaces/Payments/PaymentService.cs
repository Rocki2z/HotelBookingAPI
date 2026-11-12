using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs.Payments;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Interfaces.Payments;
using HotelBookingAPI.Models;
using HotelBookingAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly IConfiguration _configuration; // Dependency injection

    public PaymentService( // Dependency injection
        AppDbContext db,
        IPaymentGateway gateway,
        IConfiguration configuration)
    {
        _db = db;
        _gateway = gateway;
        _configuration = configuration;
    } 

    public async Task<PaymentResponseDto> ProcessPaymentAsync(
        int bookingId,
        string userId,
        CreatePaymentDto dto)
    {
        // 1. Start transaction
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        // 2. Lock booking row
        var booking = await _db.Bookings
            .FromSqlInterpolated(
                $"SELECT * FROM \"Bookings\" WHERE \"Id\" = {bookingId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (booking == null || booking.UserId != userId)
        {
            throw new KeyNotFoundException(
                "Booking not found.");
        }

        // 3. Check booking status
        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending bookings can be paid.");
        }

        if (booking.ExpiresAt <= DateTime.UtcNow)
        {
            booking.Status = BookingStatus.Expired;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            throw new InvalidOperationException(
                "Booking has expired. Please make a new booking.");
        }

        // 4. Prevent paying twice for the same booking
        var existingSuccessfulPayment =
            await _db.Payments.AnyAsync(p =>
                p.BookingId == bookingId &&
                p.Status == PaymentStatus.Succeeded);

        if (existingSuccessfulPayment)
        {
            throw new InvalidOperationException(
                "This booking has already been paid.");
        }

        // 5. Create payment record
        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalPrice,
            Currency = booking.Currency,
            Method = dto.Method,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);

        await _db.SaveChangesAsync();

        // 6. Process payment through gateway
        var result = await _gateway.ProcessPaymentAsync(
            payment.PaymentReference,
            payment.Amount,
            payment.Currency);

        // 7. Update payment and booking
        if (result.IsSuccessful)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.ProviderTransactionId = result.TransactionId;
            payment.PaidAt = DateTime.UtcNow;

            booking.Status = BookingStatus.Confirmed;
            booking.ExpiresAt = null;
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = result.FailureReason;
        }

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return MapPayment(payment);
    }

    public async Task<List<PaymentResponseDto>> GetMyPaymentsAsync(
        string userId)
    {
        return await _db.Payments
            .AsNoTracking()
            .Where(p => p.Booking.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentResponseDto
            {
                Id = p.Id,
                PaymentReference = p.PaymentReference,
                BookingId = p.BookingId,
                Amount = p.Amount,
                Currency = p.Currency,
                Method = p.Method,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt,
                RefundedAt = p.RefundedAt
            })
            .ToListAsync();
    }

    private static PaymentResponseDto MapPayment(Payment payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            PaymentReference = payment.PaymentReference,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Method = payment.Method,
            Status = payment.Status,
            CreatedAt = payment.CreatedAt,
            PaidAt = payment.PaidAt,
            RefundedAt = payment.RefundedAt
        };
    }
    public async Task<bool> RefundForCancellationAsync(
    int bookingId,
    string userId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var booking = await _db.Bookings
            .FromSqlInterpolated(
                $"SELECT * FROM \"Bookings\" WHERE \"Id\" = {bookingId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (booking == null || booking.UserId != userId)
        {
            return false;
        }

        if (booking.Status != BookingStatus.Pending &&
            booking.Status != BookingStatus.Confirmed)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (booking.CheckInDate <= today)
        {
            throw new InvalidOperationException(
                "This booking cannot be cancelled on or after check-in.");
        }

        var latestSuccessfulPayment = await _db.Payments
            .Where(p =>
                p.BookingId == bookingId &&
                p.Status == PaymentStatus.Succeeded)
            .OrderByDescending(p => p.PaidAt)
            .FirstOrDefaultAsync();

        // If payment has not succeeded, cancellation does not need a refund.
        if (latestSuccessfulPayment != null)
        {
            var policy = new BookingCancellationPolicy(
                _configuration);

            if (policy.IsEligibleForFullRefund(booking.CheckInDate))
            {
                latestSuccessfulPayment.Status = PaymentStatus.RefundPending;

                await _db.SaveChangesAsync();

                if (string.IsNullOrWhiteSpace(
                    latestSuccessfulPayment.ProviderTransactionId))
                {
                    throw new InvalidOperationException(
                        "Payment provider transaction ID is missing.");
                }

                var refundResult = await _gateway.RefundPaymentAsync(
                    latestSuccessfulPayment.ProviderTransactionId,
                    latestSuccessfulPayment.Amount,
                    latestSuccessfulPayment.Currency);

                if (!refundResult.IsSuccessful)
                {
                    throw new InvalidOperationException(
                        "Refund failed. Please contact the hotel.");
                }

                latestSuccessfulPayment.Status = PaymentStatus.Refunded;
                latestSuccessfulPayment.RefundedAt = DateTime.UtcNow;
            }
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = "Cancelled by customer.";

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return true;
    }
}