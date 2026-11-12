namespace HotelBookingAPI.DTOs.Hotels;

public class HotelResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int StarRating { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}