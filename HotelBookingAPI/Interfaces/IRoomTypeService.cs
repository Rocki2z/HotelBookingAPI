using HotelBookingAPI.DTOs.Rooms;

namespace HotelBookingAPI.Interfaces;

public interface IRoomTypeService
{
    Task<List<RoomTypeResponseDto>> GetByHotelIdAsync(int hotelId);
    Task<RoomTypeResponseDto?> GetByIdAsync(int id);
    Task<RoomTypeResponseDto?> CreateAsync(CreateRoomTypeDto dto);
    Task<RoomTypeResponseDto?> UpdateAsync(int id, UpdateRoomTypeDto dto);
    Task<bool> DeleteAsync(int id);
}