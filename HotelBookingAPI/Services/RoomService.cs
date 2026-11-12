using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs.Rooms;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<RoomResponseDto>> GetByRoomTypeIdAsync(int roomTypeId)
    {
        return await _db.Rooms
            .AsNoTracking()
            .Where(r => r.RoomTypeId == roomTypeId && r.IsActive)
            .Select(r => new RoomResponseDto
            {
                Id = r.Id,
                RoomTypeId = r.RoomTypeId,
                RoomTypeName = r.RoomType.Name,
                HotelName = r.RoomType.Hotel.Name,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                Capacity = r.Capacity,
                PricePerNight = r.PricePerNight,
                OccupancyStatus = r.OccupancyStatus,
                IsActive = r.IsActive
            })
            .ToListAsync();
    }

    public async Task<RoomResponseDto?> GetByIdAsync(int id)
    {
        return await _db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == id && r.IsActive)
            .Select(r => new RoomResponseDto
            {
                Id = r.Id,
                RoomTypeId = r.RoomTypeId,
                RoomTypeName = r.RoomType.Name,
                HotelName = r.RoomType.Hotel.Name,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                Capacity = r.Capacity,
                PricePerNight = r.PricePerNight,
                OccupancyStatus = r.OccupancyStatus,
                IsActive = r.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<RoomResponseDto?> CreateAsync(CreateRoomDto dto)
    {
        var roomType = await _db.RoomTypes
            .FirstOrDefaultAsync(rt =>
                rt.Id == dto.RoomTypeId && rt.IsActive);

        if (roomType == null)
            return null;

        // Capacity cannot exceed the room type's maximum.
        if (dto.Capacity > roomType.MaxCapacity)
            throw new ArgumentException(
                $"Room capacity cannot exceed {roomType.MaxCapacity} guests.");

        var roomNumberExists = await _db.Rooms.AnyAsync(r =>
            r.RoomTypeId == dto.RoomTypeId &&
            r.RoomNumber == dto.RoomNumber &&
            r.IsActive);

        if (roomNumberExists)
            throw new InvalidOperationException(
                "This room number already exists in this room type.");

        var room = new Room
        {
            RoomTypeId = dto.RoomTypeId,
            RoomNumber = dto.RoomNumber,
            Floor = dto.Floor,
            Capacity = dto.Capacity,
            PricePerNight = dto.PricePerNight,
            OccupancyStatus = "Vacant",
            IsActive = true
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(room.Id);
    }

    public async Task<RoomResponseDto?> UpdateAsync(
        int id, UpdateRoomDto dto)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (room == null)
            return null;

        if (dto.Capacity > room.RoomType.MaxCapacity)
            throw new ArgumentException(
                $"Room capacity cannot exceed {room.RoomType.MaxCapacity} guests.");

        var duplicate = await _db.Rooms.AnyAsync(r =>
            r.Id != id &&
            r.RoomTypeId == room.RoomTypeId &&
            r.RoomNumber == dto.RoomNumber &&
            r.IsActive);

        if (duplicate)
            throw new InvalidOperationException(
                "This room number already exists in this room type.");

        room.RoomNumber = dto.RoomNumber;
        room.Floor = dto.Floor;
        room.Capacity = dto.Capacity;
        room.PricePerNight = dto.PricePerNight;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var room = await _db.Rooms
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (room == null)
            return false;

        room.IsActive = false;
        await _db.SaveChangesAsync();

        return true;
    }
}
