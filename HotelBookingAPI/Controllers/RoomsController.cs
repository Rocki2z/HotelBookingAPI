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
public class RoomsController : ControllerBase
{
    private readonly IRoomService _service;

    public RoomsController(IRoomService service)
    {
        _service = service;
    }

    // GET: api/room-types/1/rooms
    [HttpGet("room-types/{roomTypeId:int}/rooms")]
    public async Task<IActionResult> GetByRoomType(int roomTypeId)
    {
        var rooms = await _service.GetByRoomTypeIdAsync(roomTypeId);
        return Ok(rooms);
    }

    // GET: api/rooms/1
    [HttpGet("rooms/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var room = await _service.GetByIdAsync(id);

        if (room == null)
            return NotFound(new { message = "Room not found." });

        return Ok(room);
    }

    // POST: api/rooms
    [Authorize(Policy = "AdminOnly")]
    [HttpPost("rooms")]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        try
        {
            var room = await _service.CreateAsync(dto);

            if (room == null)
                return BadRequest(new
                {
                    message = "Room type does not exist or is inactive."
                });

            return CreatedAtAction(
                nameof(GetById),
                new { id = room.Id },
                room
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT: api/rooms/1
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("rooms/{id:int}")]
    public async Task<IActionResult> Update(
        int id, UpdateRoomDto dto)
    {
        try
        {
            var room = await _service.UpdateAsync(id, dto);

            if (room == null)
                return NotFound(new { message = "Room not found." });

            return Ok(room);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE: api/rooms/1
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("rooms/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return Conflict(new
            {
                message = "Room not found or it has booking history."
            });

        return NoContent();
    }
}