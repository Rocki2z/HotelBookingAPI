using System.ComponentModel.DataAnnotations;

namespace HotelBookingAPI.DTOs.Bookings;

public class AvailableRoomQueryDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int HotelId { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    [Required]
    [Range(1, 20)]
    public int Guests { get; set; }
}