using HotelBookingAPI.DTOs.Bookings;
using HotelBookingAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

namespace HotelBookingAPI.Controllers;

[ApiVersion(1.0)]
[EnableRateLimiting("ApiPolicy")]
[ApiController]
[Route("api/rooms")]
public class RoomAvailabilityController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public RoomAvailabilityController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // GET: api/rooms/available
    [HttpGet("available")]
    public async Task<IActionResult> SearchAvailableRooms(
        [FromQuery] AvailableRoomQueryDto query)
    {
        var rooms = await _bookingService.SearchAvailableRoomsAsync(query);

        return Ok(rooms);
    }
}