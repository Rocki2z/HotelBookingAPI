using HotelBookingAPI.Interfaces.Payments;

namespace HotelBookingAPI.Services.Payments;

public class MockPaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayResult> ProcessPaymentAsync(
        string paymentReference,
        decimal amount,
        string currency)
    {
        // DEVELOPMENT ONLY:
        // Simulate a successful payment.

        return Task.FromResult(new PaymentGatewayResult
        {
            IsSuccessful = true,
            TransactionId = "MOCK-" + Guid.NewGuid().ToString("N")
        });
    }

    public Task<PaymentGatewayResult> RefundPaymentAsync(
        string transactionId,
        decimal amount,
        string currency)
    {
        // DEVELOPMENT ONLY:
        // Simulate a successful refund.

        return Task.FromResult(new PaymentGatewayResult
        {
            IsSuccessful = true,
            TransactionId = "MOCK-REFUND-" + Guid.NewGuid().ToString("N")
        });
    }
}