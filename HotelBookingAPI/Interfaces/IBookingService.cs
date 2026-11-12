using HotelBookingAPI.DTOs.Bookings;

namespace HotelBookingAPI.Interfaces;

public interface IBookingService
{
    Task<List<AvailableRoomResponseDto>> SearchAvailableRoomsAsync(
        AvailableRoomQueryDto query);

    Task<BookingResponseDto> CreateBookingAsync(
        string userId,
        CreateBookingDto dto);

    Task<List<BookingResponseDto>> GetMyBookingsAsync(
        string userId);

    Task<BookingResponseDto?> GetBookingByIdAsync(
        int id);

    Task<bool> IsBookingOwnedByUserAsync(
        int id,
        string userId);

    Task<bool> CancelBookingAsync(
        int id,
        string userId);
    Task<bool> CheckInAsync(int bookingId); 

    Task<bool> CheckOutAsync(int bookingId);
}
