using HotelBookingAPI.DTOs.Auth;
using HotelBookingAPI.Controllers;
using HotelBookingAPI.DTOs.Hotels;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Services;
using HotelBookingAPI.Services.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HotelBookingAPI.Tests;

public class UnitTests
{
    [Fact]
    public void CancellationPolicy_AllowsFullRefundOutsideWindow()
    {
        var policy = new BookingCancellationPolicy(Configuration(48));
        Assert.True(policy.IsEligibleForFullRefund(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5))));
    }

    [Fact]
    public void CancellationPolicy_DeniesFullRefundInsideWindow()
    {
        var policy = new BookingCancellationPolicy(Configuration(48));
        Assert.False(policy.IsEligibleForFullRefund(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));
    }

    [Fact]
    public async Task MockGateway_ApprovesPaymentAndReturnsTransactionId()
    {
        var result = await new MockPaymentGateway().ProcessPaymentAsync("PAY-1", 125m, "USD");
        Assert.True(result.IsSuccessful);
        Assert.StartsWith("MOCK-", result.TransactionId);
    }

    [Fact]
    public async Task MockGateway_ApprovesRefundAndReturnsTransactionId()
    {
        var result = await new MockPaymentGateway().RefundPaymentAsync("TX-1", 125m, "USD");
        Assert.True(result.IsSuccessful);
        Assert.StartsWith("MOCK-REFUND-", result.TransactionId);
    }

    [Fact]
    public void LoginDto_RejectsMalformedEmail()
    {
        var dto = new LoginDto { Email = "not-an-email", Password = "secret" };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        Assert.False(System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, results, true));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(dto.Email)));
    }

    [Fact]
    public async Task HotelsController_GetById_ReturnsNotFoundWhenServiceHasNoHotel()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.GetByIdAsync(7)).ReturnsAsync((HotelResponseDto?)null);
        var controller = new HotelsController(service.Object);

        var result = await controller.GetById(7);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task HotelsController_GetById_ReturnsHotelWhenFound()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new HotelResponseDto { Id = 7, Name = "Test hotel" });
        var controller = new HotelsController(service.Object);

        var result = await controller.GetById(7);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(7, Assert.IsType<HotelResponseDto>(ok.Value).Id);
    }

    [Fact]
    public async Task HotelsController_Delete_ReturnsNoContentWhenDeleted()
    {
        var service = new Mock<IHotelService>();
        service.Setup(x => x.DeleteAsync(7)).ReturnsAsync(true);
        var controller = new HotelsController(service.Object);

        var result = await controller.Delete(7);

        Assert.IsType<NoContentResult>(result);
    }

    private static IConfiguration Configuration(int refundHours) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BookingPolicy:FreeCancellationHours"] = refundHours.ToString()
        }).Build();
}
