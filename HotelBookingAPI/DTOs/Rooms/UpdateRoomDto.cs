using System.ComponentModel.DataAnnotations;

namespace HotelBookingAPI.DTOs.Rooms;

public class UpdateRoomDto
{
    [Required]
    [StringLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(0, 100)]
    public int Floor { get; set; }

    [Range(1, 20)]
    public int Capacity { get; set; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal PricePerNight { get; set; }
}