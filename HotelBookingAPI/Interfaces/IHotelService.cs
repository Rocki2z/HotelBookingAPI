using HotelBookingAPI.DTOs.Hotels;
using HotelBookingAPI.DTOs.Common;

namespace HotelBookingAPI.Interfaces;

public interface IHotelService
{
    Task<List<HotelResponseDto>> GetAllAsync();

    Task<HotelResponseDto?> GetByIdAsync(int id);

    Task<HotelResponseDto> CreateAsync(CreateHotelDto dto);

    Task<HotelResponseDto?> UpdateAsync(
        int id,
        UpdateHotelDto dto
    );

    Task<bool> DeleteAsync(int id);
    Task<PaginationResponseDto<HotelResponseDto>>
    GetHotelsAsync(int pageNumber, int pageSize);
    
    Task<PaginationResponseDto<HotelResponseDto>>
    GetFilteredHotelsAsync(HotelFilterDto filter);
}