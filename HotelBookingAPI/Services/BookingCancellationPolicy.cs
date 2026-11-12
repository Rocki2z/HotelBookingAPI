namespace HotelBookingAPI.Services;

public class BookingCancellationPolicy
{
    private readonly IConfiguration _configuration;

    public BookingCancellationPolicy(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsEligibleForFullRefund(DateOnly checkInDate)
    {
        var freeCancellationHours =
            _configuration.GetValue<int>(
                "BookingPolicy:FreeCancellationHours");

        var hoursUntilCheckIn =
            (checkInDate.ToDateTime(TimeOnly.MinValue) -
             DateTime.UtcNow).TotalHours;

        return hoursUntilCheckIn >= freeCancellationHours;
    }
}