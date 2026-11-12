using HotelBookingAPI.DTOs.Hotels;
using HotelBookingAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

namespace HotelBookingAPI.Controllers;

[ApiVersion(1.0)]
[EnableRateLimiting("ApiPolicy")]
[Authorize(Policy = "AdminOnly")]
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class HotelsController : ControllerBase
{
    private readonly IHotelService _hotelService;

    public HotelsController(IHotelService hotelService)
    {
        _hotelService = hotelService;
    }

    // GET: api/hotels
    /// <summary>
    /// Retrieves all available hotels.
    /// </summary>
    /// <returns>A list of hotels.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var hotels = await _hotelService.GetAllAsync();

        return Ok(hotels);
    }

    // GET: api/hotels/1
    /// <summary>
    /// Retrieves a hotel by its ID.
    /// </summary>
    /// <param name="id">The hotel ID.</param>
    /// <returns>The requested hotel.</returns>
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var hotel = await _hotelService.GetByIdAsync(id);

        if (hotel == null)
            return NotFound(new { message = "Hotel not found." });

        return Ok(hotel);
    }

    // GET: api/hotels/paginated?pageNumber=1&pageSize=10
    [HttpGet("paginated")]
    public async Task<IActionResult> GetHotels(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
    {
        var result = await _hotelService.GetHotelsAsync(
            pageNumber,
            pageSize
        );

        return Ok(result);
    }
    // GET: api/hotels/filter
    [HttpGet("filter")]
    public async Task<IActionResult> GetFilteredHotels(
        [FromQuery] HotelFilterDto filter)
    {
        var result = await _hotelService.GetFilteredHotelsAsync(filter);

        return Ok(result);
    }

    // POST: api/hotels
    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateHotelDto dto)
    {
        var hotel = await _hotelService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = hotel.Id },
            hotel
        );
    }

    // PUT: api/hotels/1
    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(
        int id,
        UpdateHotelDto dto)
    {
        var hotel = await _hotelService.UpdateAsync(id, dto);

        if (hotel == null)
            return NotFound(new { message = "Hotel not found." });

        return Ok(hotel);
    }

    // DELETE: api/hotels/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _hotelService.DeleteAsync(id);

        if (!deleted)
            return NotFound(new { message = "Hotel not found." });

        return NoContent();
    }

}
