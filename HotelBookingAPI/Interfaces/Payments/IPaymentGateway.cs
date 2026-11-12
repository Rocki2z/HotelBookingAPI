namespace HotelBookingAPI.Interfaces.Payments;

public class PaymentGatewayResult
{
    public bool IsSuccessful { get; set; }

    public string? TransactionId { get; set; }

    public string? FailureReason { get; set; }
}

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(
        string paymentReference,
        decimal amount,
        string currency);

    Task<PaymentGatewayResult> RefundPaymentAsync(
        string transactionId,
        decimal amount,
        string currency);
}