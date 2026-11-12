using System.ComponentModel.DataAnnotations;

namespace HotelBookingAPI.DTOs.Rooms;

public class CreateRoomTypeDto
{
    [Required]
    public int HotelId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 20)]
    public int MaxCapacity { get; set; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal BasePrice { get; set; }
}