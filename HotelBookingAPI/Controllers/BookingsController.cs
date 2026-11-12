using System.Security.Claims;
using HotelBookingAPI.DTOs.Bookings;
using HotelBookingAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using HotelBookingAPI.Data;
using Asp.Versioning;

namespace HotelBookingAPI.Controllers;

[ApiVersion(1.0)]
[EnableRateLimiting("ApiPolicy")]
[Authorize(Policy = "AdminOrCustomer")]
[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IPaymentService _paymentService;
    private readonly AppDbContext _context;

    public BookingsController(IBookingService bookingService, IPaymentService paymentService, AppDbContext context)
    {
        _bookingService = bookingService;
        _paymentService = paymentService;
        _context = context;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(
                "User ID not found in token.");
    }

    // POST: api/bookings
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateBooking(
        CreateBookingDto dto)
    {
        var userId = GetCurrentUserId();

        var booking = await _bookingService.CreateBookingAsync(
            userId,
            dto);

        return CreatedAtAction(
            nameof(GetBookingById),
            new { id = booking.Id },
            booking);
    }

    // GET: api/bookings/my
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = GetCurrentUserId();

        var bookings = await _bookingService.GetMyBookingsAsync(userId);

        return Ok(bookings);
    }

    // GET: api/bookings/{id}
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> GetBookingById(int id)
    {
        var booking = await _context.Bookings
       .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            return NotFound("Booking not found.");
        }

        // Admin can access every booking
        if (User.IsInRole("Admin"))
        {
            return Ok(booking);
        }

        // Only Customer can access their own booking
        var currentUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (currentUserId == null ||
            booking.UserId != currentUserId)
        {
            return Forbid();
        }

        return Ok(booking);
    }

    // PATCH: api/bookings/{id}/cancel
    [HttpPatch("{id:int}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var cancelled = await _paymentService.RefundForCancellationAsync(
    id,
    GetCurrentUserId());

        if (!cancelled)
        {
            return Conflict(new
            {
                message = "Booking could not be cancelled."
            });
        }

        return Ok(new
        {
            message = "Booking cancelled successfully. Refund eligibility was checked."
        });
    }
}