using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs.Rooms;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Services;

public class RoomTypeService : IRoomTypeService
{
    private readonly AppDbContext _db;

    public RoomTypeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<RoomTypeResponseDto>> GetByHotelIdAsync(int hotelId)
    {
        return await _db.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.HotelId == hotelId && rt.IsActive)
            .Select(rt => new RoomTypeResponseDto
            {
                Id = rt.Id,
                HotelId = rt.HotelId,
                HotelName = rt.Hotel.Name,
                Name = rt.Name,
                Description = rt.Description,
                MaxCapacity = rt.MaxCapacity,
                BasePrice = rt.BasePrice,
                IsActive = rt.IsActive
            })
            .ToListAsync();
    }

    public async Task<RoomTypeResponseDto?> GetByIdAsync(int id)
    {
        return await _db.RoomTypes
            .AsNoTracking()
            .Where(rt => rt.Id == id && rt.IsActive)
            .Select(rt => new RoomTypeResponseDto
            {
                Id = rt.Id,
                HotelId = rt.HotelId,
                HotelName = rt.Hotel.Name,
                Name = rt.Name,
                Description = rt.Description,
                MaxCapacity = rt.MaxCapacity,
                BasePrice = rt.BasePrice,
                IsActive = rt.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<RoomTypeResponseDto?> CreateAsync(CreateRoomTypeDto dto)
    {
        var hotelExists = await _db.Hotels
            .AnyAsync(h => h.Id == dto.HotelId && h.IsActive);

        if (!hotelExists)
            return null;

        var roomType = new RoomType
        {
            HotelId = dto.HotelId,
            Name = dto.Name,
            Description = dto.Description,
            MaxCapacity = dto.MaxCapacity,
            BasePrice = dto.BasePrice,
            IsActive = true
        };

        _db.RoomTypes.Add(roomType);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(roomType.Id);
    }

    public async Task<RoomTypeResponseDto?> UpdateAsync(
        int id, UpdateRoomTypeDto dto)
    {
        var roomType = await _db.RoomTypes
            .FirstOrDefaultAsync(rt => rt.Id == id && rt.IsActive);

        if (roomType == null)
            return null;

        roomType.Name = dto.Name;
        roomType.Description = dto.Description;
        roomType.MaxCapacity = dto.MaxCapacity;
        roomType.BasePrice = dto.BasePrice;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var roomType = await _db.RoomTypes
            .FirstOrDefaultAsync(rt => rt.Id == id && rt.IsActive);

        if (roomType == null)
            return false;

        // Prevent deactivation while active rooms remain.
        var hasActiveRooms = await _db.Rooms
            .AnyAsync(r => r.RoomTypeId == id && r.IsActive);

        if (hasActiveRooms)
            return false;

        roomType.IsActive = false;
        await _db.SaveChangesAsync();

        return true;
    }
}