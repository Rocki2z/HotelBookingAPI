using HotelBookingAPI.DTOs.Rooms;
using HotelBookingAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

namespace HotelBookingAPI.Controllers;

[ApiVersion(1.0)]
[EnableRateLimiting("ApiPolicy")]
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api")]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomTypeService _service;

    public RoomTypesController(IRoomTypeService service)
    {
        _service = service;
    }

    // GET: api/hotels/1/room-types
    [HttpGet("hotels/{hotelId:int}/room-types")]
    public async Task<IActionResult> GetByHotel(int hotelId)
    {
        var result = await _service.GetByHotelIdAsync(hotelId);
        return Ok(result);
    }

    // GET: api/room-types/1
    [HttpGet("room-types/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound(new { message = "Room type not found." });

        return Ok(result);
    }

    // POST: api/room-types
    [HttpPost("room-types")]
    public async Task<IActionResult> Create(CreateRoomTypeDto dto)
    {
        var result = await _service.CreateAsync(dto);

        if (result == null)
            return BadRequest(new { message = "Hotel does not exist or is inactive." });

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result
        );
    }

    // PUT: api/room-types/1
    [HttpPut("room-types/{id:int}")]
    public async Task<IActionResult> Update(
        int id, UpdateRoomTypeDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);

        if (result == null)
            return NotFound(new { message = "Room type not found." });

        return Ok(result);
    }

    // DELETE: api/room-types/1
    [HttpDelete("room-types/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return Conflict(new
            {
                message = "Room type not found or still contains active rooms."
            });

        return NoContent();
    }
}