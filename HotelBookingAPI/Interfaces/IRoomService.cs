using HotelBookingAPI.DTOs.Rooms;

namespace HotelBookingAPI.Interfaces;

public interface IRoomService
{
    Task<List<RoomResponseDto>> GetByRoomTypeIdAsync(int roomTypeId);
    Task<RoomResponseDto?> GetByIdAsync(int id);
    Task<RoomResponseDto?> CreateAsync(CreateRoomDto dto);
    Task<RoomResponseDto?> UpdateAsync(int id, UpdateRoomDto dto);
    Task<bool> DeleteAsync(int id);
}