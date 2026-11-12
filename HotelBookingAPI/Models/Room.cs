namespace HotelBookingAPI.Models;

public class Room
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public int Floor { get; set; }

    public int Capacity { get; set; }

    public decimal PricePerNight { get; set; }

    public string OccupancyStatus { get; set; } = "Vacant";

    public bool IsActive { get; set; } = true;

    // Relationship
    public RoomType RoomType { get; set; } = null!;

    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();
}