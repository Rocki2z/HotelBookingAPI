using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs.Auth;
using HotelBookingAPI.DTOs.Bookings;
using HotelBookingAPI.DTOs.Payments;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Interfaces.Payments;
using HotelBookingAPI.Models;
using HotelBookingAPI.Models.Enums;
using HotelBookingAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit.Sdk;

namespace HotelBookingAPI.Tests;

[Collection(PostgresCollection.Name)]
public sealed class BookingPaymentIntegrationTests(PostgresApiFixture fixture)
{
    [PostgresFact]
    public async Task UnauthenticatedRequest_IsRejected()
    {
        using var client = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/hotels")).StatusCode);
    }

    [PostgresFact]
    public async Task CustomerCanRegisterLoginAndReadOwnBookings_ButCannotUseAdminEndpoint()
    {
        using var client = fixture.CreateClient();
        var email = $"new-{Guid.NewGuid():N}@example.test";
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Test Customer", email, phoneNumber = "+15551234567", password = "Customer123!" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = email, Password = "Customer123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(token);
        Assert.Contains("Customer", token.Roles);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/bookings/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch,
                "/api/admin/bookings/1/check-in"))).StatusCode);
    }

    [PostgresFact]
    public async Task InvalidCredentials_AreRejected()
    {
        using var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = "missing@example.test", Password = "Wrong123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [PostgresFact]
    public async Task BookingCreation_ValidatesDatesAndCalculatesPriceFromDatabase()
    {
        var (roomId, userId) = await CreateRoomAndCustomerAsync();
        using var scope = fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var booking = await service.CreateBookingAsync(userId, new CreateBookingDto
        {
            RoomId = roomId, CheckInDate = checkIn, CheckOutDate = checkIn.AddDays(3), NumberOfGuests = 2
        });

        Assert.Equal(3, booking.NumberOfNights);
        Assert.Equal(125m, booking.PricePerNight);
        Assert.Equal(375m, booking.TotalPrice);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [PostgresFact]
    public async Task BookingCreation_RejectsInvalidDatesAndCapacity()
    {
        var (roomId, userId) = await CreateRoomAndCustomerAsync(capacity: 1);
        using var scope = fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBookingAsync(userId,
            new CreateBookingDto { RoomId = roomId, CheckInDate = checkIn, CheckOutDate = checkIn, NumberOfGuests = 1 }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateBookingAsync(userId,
            new CreateBookingDto { RoomId = roomId, CheckInDate = checkIn, CheckOutDate = checkIn.AddDays(1), NumberOfGuests = 2 }));
    }

    [PostgresFact]
    public async Task BookingCreation_RejectsOverlappingActiveBooking()
    {
        var (roomId, userId) = await CreateRoomAndCustomerAsync();
        using var scope = fixture.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25));
        var dto = new CreateBookingDto { RoomId = roomId, CheckInDate = checkIn, CheckOutDate = checkIn.AddDays(2), NumberOfGuests = 2 };
        await service.CreateBookingAsync(userId, dto);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateBookingAsync(userId, dto));
    }

    [PostgresFact]
    public async Task PaymentSuccess_ConfirmsBookingAndPersistsTransaction()
    {
        var (roomId, userId) = await CreateRoomAndCustomerAsync();
        using var scope = fixture.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(35));
        var booking = await bookingService.CreateBookingAsync(userId, new CreateBookingDto
        {
            RoomId = roomId, CheckInDate = checkIn, CheckOutDate = checkIn.AddDays(2), NumberOfGuests = 2
        });
        var payment = await paymentService.ProcessPaymentAsync(booking.Id, userId,
            new CreatePaymentDto { Method = PaymentMethod.Card });

        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(250m, payment.Amount);
        var persisted = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Bookings.SingleAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Confirmed, persisted.Status);
        Assert.Null(persisted.ExpiresAt);
    }

    [PostgresFact]
    public async Task Payment_RejectsAnotherUsersBooking()
    {
        var (roomId, ownerId) = await CreateRoomAndCustomerAsync();
        var strangerId = await fixture.CreateCustomerAsync();
        using var scope = fixture.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45));
        var booking = await bookingService.CreateBookingAsync(ownerId, new CreateBookingDto
        {
            RoomId = roomId, CheckInDate = checkIn, CheckOutDate = checkIn.AddDays(1), NumberOfGuests = 1
        });
        await Assert.ThrowsAsync<KeyNotFoundException>(() => paymentService.ProcessPaymentAsync(
            booking.Id, strangerId, new CreatePaymentDto { Method = PaymentMethod.Card }));
    }

    [PostgresFact]
    public async Task PaymentFailure_LeavesBookingPendingAndRecordsReason()
    {
        var (roomId, userId) = await CreateRoomAndCustomerAsync();
        using var scope = fixture.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(50));
        var booking = await bookingService.CreateBookingAsync(userId, new CreateBookingDto
        {
            RoomId = roomId, CheckInDate = checkIn, CheckOutDate = checkIn.AddDays(1), NumberOfGuests = 1
        });
        var gateway = new Mock<IPaymentGateway>();
        gateway.Setup(g => g.ProcessPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentGatewayResult { IsSuccessful = false, FailureReason = "Card declined." });
        var service = new PaymentService(db, gateway.Object,
            scope.ServiceProvider.GetRequiredService<IConfiguration>());

        var payment = await service.ProcessPaymentAsync(booking.Id, userId,
            new CreatePaymentDto { Method = PaymentMethod.Card });

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(BookingStatus.Pending, (await db.Bookings.SingleAsync(b => b.Id == booking.Id)).Status);
        Assert.Equal("Card declined.", (await db.Payments.SingleAsync(p => p.BookingId == booking.Id)).FailureReason);
    }

    [PostgresFact]
    public async Task CancellationWithinFreeRefundWindow_RefundsPaymentAndCancelsBooking()
    {
        var (roomId, userId) = await CreateRoomAndCustomerAsync();
        using var scope = fixture.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var booking = await bookingService.CreateBookingAsync(userId, new CreateBookingDto
        {
            RoomId = roomId, CheckInDate = checkIn, CheckOutDate = checkIn.AddDays(1), NumberOfGuests = 1
        });
        await paymentService.ProcessPaymentAsync(booking.Id, userId,
            new CreatePaymentDto { Method = PaymentMethod.Card });

        Assert.True(await paymentService.RefundForCancellationAsync(booking.Id, userId));
        Assert.Equal(BookingStatus.Cancelled, (await db.Bookings.SingleAsync(b => b.Id == booking.Id)).Status);
        var payment = await db.Payments.SingleAsync(p => p.BookingId == booking.Id);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.NotNull(payment.RefundedAt);
    }

    private async Task<(int RoomId, string UserId)> CreateRoomAndCustomerAsync(int capacity = 3)
    {
        var userId = await fixture.CreateCustomerAsync();
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var hotel = new Hotel
        {
            Name = $"Hotel-{suffix}", Description = "Test hotel", Address = "1 Test St",
            City = "Test City", Country = "Test Country", PhoneNumber = "+15551234567",
            Email = $"hotel-{suffix}@example.test", StarRating = 4
        };
        var roomType = new RoomType { Hotel = hotel, Name = "Standard", Description = "Test room", MaxCapacity = capacity, BasePrice = 125m };
        var room = new Room { RoomType = roomType, RoomNumber = suffix[..8], Floor = 1, Capacity = capacity, PricePerNight = 125m };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return (room.Id, userId);
    }
}


