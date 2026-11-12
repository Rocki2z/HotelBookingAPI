using HotelBookingAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;


namespace HotelBookingAPI.Controllers;

[ApiVersion(1.0)]
[EnableRateLimiting("ApiPolicy")]
[ApiController]
[Route("api/admin/bookings")]
[Authorize(Roles = "Admin")]
public class AdminBookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public AdminBookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // PATCH: api/admin/bookings/1/check-in
    [HttpPatch("{id:int}/check-in")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var result = await _bookingService.CheckInAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }

        return Ok(new
        {
            message = "Guest checked in successfully."
        });
    }

    // PATCH: api/admin/bookings/1/check-out
    [HttpPatch("{id:int}/check-out")]
    public async Task<IActionResult> CheckOut(int id)
    {
        var result = await _bookingService.CheckOutAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }

        return Ok(new
        {
            message = "Guest checked out successfully."
        });
    }
}