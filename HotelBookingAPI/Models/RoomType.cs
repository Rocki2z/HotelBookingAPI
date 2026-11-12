namespace HotelBookingAPI.Models;

public class RoomType
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int MaxCapacity { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;

    // Relationship
    public Hotel Hotel { get; set; } = null!;

    public ICollection<Room> Rooms { get; set; }
        = new List<Room>();
}