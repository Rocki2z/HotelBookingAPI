using System.ComponentModel.DataAnnotations;

namespace HotelBookingAPI.DTOs.Rooms;

public class CreateRoomDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int RoomTypeId { get; set; }

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(0, 100)]
    public int Floor { get; set; }

    [Range(1, 20)]
    public int Capacity { get; set; }

    [Range(typeof(decimal), "0.01", "999999")]
    public decimal PricePerNight { get; set; }
}