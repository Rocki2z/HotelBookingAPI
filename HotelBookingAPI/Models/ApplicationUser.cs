using Microsoft.AspNetCore.Identity;

namespace HotelBookingAPI.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Booking> Bookings { get; set; } =
    new List<Booking>();
}