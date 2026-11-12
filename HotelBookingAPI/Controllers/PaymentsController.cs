using System.Security.Claims;
using HotelBookingAPI.DTOs.Payments;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Interfaces.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

namespace HotelBookingAPI.Controllers;

[ApiVersion(1.0)]
[EnableRateLimiting("ApiPolicy")]
[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Customer")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(
                "User ID not found.");
    } 

    // POST: api/payments/bookings/1
    [HttpPost("bookings/{bookingId:int}")]
    public async Task<IActionResult> PayForBooking(
        int bookingId,
        CreatePaymentDto dto)
    {
        var payment = await _paymentService.ProcessPaymentAsync(
            bookingId,
            GetUserId(),
            dto);

        return Ok(payment);
    }

    // GET: api/payments/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyPayments()
    {
        var payments = await _paymentService.GetMyPaymentsAsync(
            GetUserId());

        return Ok(payments);
    }
}