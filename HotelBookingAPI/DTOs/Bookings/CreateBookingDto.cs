using System.ComponentModel.DataAnnotations;

namespace HotelBookingAPI.DTOs.Bookings;

public class CreateBookingDto
{
    [Range(1, int.MaxValue)]
    public int RoomId { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    [Range(1, 20)]
    public int NumberOfGuests { get; set; }
}