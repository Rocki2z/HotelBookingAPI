using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs.Hotels;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Models;
using Microsoft.EntityFrameworkCore;
using HotelBookingAPI.DTOs.Common;
using HotelBookingAPI.Helpers;
using Microsoft.Extensions.Logging;
using HotelBookingAPI.Middleware;


namespace HotelBookingAPI.Services;

public class HotelService : IHotelService
{
    private readonly AppDbContext _db;
    private readonly AppDbContext _context;
    private readonly ILogger<HotelService> _logger;

    public HotelService(AppDbContext db,
        AppDbContext context,
        ILogger<HotelService> logger)
    {
        _db = db;
        _context = context;
        _logger = logger;
    }

    public async Task<List<HotelResponseDto>> GetAllAsync()
    {
        return await _db.Hotels
            .AsNoTracking()
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name)
            .Select(h => new HotelResponseDto
            {
                Id = h.Id,
                Name = h.Name,
                Description = h.Description,
                Address = h.Address,
                City = h.City,
                Country = h.Country,
                PhoneNumber = h.PhoneNumber,
                Email = h.Email,
                StarRating = h.StarRating,
                IsActive = h.IsActive,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<HotelResponseDto?> GetByIdAsync(int id)
    {
        return await _db.Hotels
            .AsNoTracking()
            .Where(h => h.Id == id && h.IsActive)
            .Select(h => new HotelResponseDto
            {
                Id = h.Id,
                Name = h.Name,
                Description = h.Description,
                Address = h.Address,
                City = h.City,
                Country = h.Country,
                PhoneNumber = h.PhoneNumber,
                Email = h.Email,
                StarRating = h.StarRating,
                IsActive = h.IsActive,
                CreatedAt = h.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<HotelResponseDto> CreateAsync(
        CreateHotelDto dto)
    {
        var hotel = new Hotel
        {
            Name = dto.Name,
            Description = dto.Description,
            Address = dto.Address,
            City = dto.City,
            Country = dto.Country,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            StarRating = dto.StarRating,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Hotels.Add(hotel);

        await _db.SaveChangesAsync();

        return new HotelResponseDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Description = hotel.Description,
            Address = hotel.Address,
            City = hotel.City,
            Country = hotel.Country,
            PhoneNumber = hotel.PhoneNumber,
            Email = hotel.Email,
            StarRating = hotel.StarRating,
            IsActive = hotel.IsActive,
            CreatedAt = hotel.CreatedAt
        };
    }

    public async Task<HotelResponseDto?> UpdateAsync(
        int id,
        UpdateHotelDto dto)
    {
        var hotel = await _db.Hotels
            .FirstOrDefaultAsync(h => h.Id == id && h.IsActive);

        if (hotel == null)
            return null;

        hotel.Name = dto.Name;
        hotel.Description = dto.Description;
        hotel.Address = dto.Address;
        hotel.City = dto.City;
        hotel.Country = dto.Country;
        hotel.PhoneNumber = dto.PhoneNumber;
        hotel.Email = dto.Email;
        hotel.StarRating = dto.StarRating;
        hotel.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var hotel = await _db.Hotels
            .FirstOrDefaultAsync(h => h.Id == id && h.IsActive);

        if (hotel == null)
            return false;

        // Soft delete: preserve hotel history.
        hotel.IsActive = false;
        hotel.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }
    public async Task<PaginationResponseDto<HotelResponseDto>>
    GetHotelsAsync(int pageNumber, int pageSize)
    {
        pageNumber = PaginationHelper.ValidatePageNumber(pageNumber);
        pageSize = PaginationHelper.ValidatePageSize(pageSize);

        var query = _context.Hotels
            .AsNoTracking()
            .Where(h => h.IsActive);

        var totalRecords = await query.CountAsync();

        var hotels = await query
            .OrderBy(h => h.Name)
            .Skip(PaginationHelper.GetSkip(pageNumber, pageSize))
            .Take(pageSize)
            .Select(h => new HotelResponseDto
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City
            })
            .ToListAsync();

        return new PaginationResponseDto<HotelResponseDto>
        {
            Items = hotels,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(
                (double)totalRecords / pageSize)
        };
    }
    public async Task<PaginationResponseDto<HotelResponseDto>>
     GetFilteredHotelsAsync(HotelFilterDto filter)
    {
        _logger.LogInformation(
            "Starting hotel filtering operation."
        );

        filter.PageNumber =
            PaginationHelper.ValidatePageNumber(filter.PageNumber);

        filter.PageSize =
            PaginationHelper.ValidatePageSize(filter.PageSize);

        var query = _context.Hotels
            .AsNoTracking()
            .Where(h => h.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim();

            query = query.Where(h =>
                EF.Functions.ILike(h.Name, $"%{search}%") ||
                EF.Functions.ILike(h.City, $"%{search}%")
            );
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim();

            query = query.Where(h =>
                EF.Functions.ILike(h.City, $"%{city}%")
            );
        }

        var sortBy = (filter.SortBy ?? "name").Trim().ToLower();
        var sortDirection =
            (filter.SortDirection ?? "asc").Trim().ToLower();

        query = sortBy switch
        {
            "city" => sortDirection == "desc"
                ? query.OrderByDescending(h => h.City).ThenBy(h => h.Id)
                : query.OrderBy(h => h.City).ThenBy(h => h.Id),

            _ => sortDirection == "desc"
                ? query.OrderByDescending(h => h.Name).ThenBy(h => h.Id)
                : query.OrderBy(h => h.Name).ThenBy(h => h.Id)
        };

        var totalRecords = await query.CountAsync();

        var hotels = await query
            .Skip(PaginationHelper.GetSkip(
                filter.PageNumber,
                filter.PageSize
            ))
            .Take(filter.PageSize)
            .Select(h => new HotelResponseDto
            {
                Id = h.Id,
                Name = h.Name,
                City = h.City
            })
            .ToListAsync();

        _logger.LogInformation(
            "Hotel filtering completed. Records found: {TotalRecords}, Page: {PageNumber}, PageSize: {PageSize}",
            totalRecords,
            filter.PageNumber,
            filter.PageSize
        );

        return new PaginationResponseDto<HotelResponseDto>
        {
            Items = hotels,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(
                (double)totalRecords / filter.PageSize
            )
        };
    }

}
