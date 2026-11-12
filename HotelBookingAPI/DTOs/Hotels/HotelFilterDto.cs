namespace HotelBookingAPI.DTOs.Hotels;

public class HotelFilterDto
{
    public string? SearchTerm { get; set; }

    public string? City { get; set; }

    public string SortBy { get; set; } = "name";

    public string SortDirection { get; set; } = "asc";

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}