using HotelBookingAPI.Data;
using HotelBookingAPI.DTOs.Bookings;
using HotelBookingAPI.Interfaces;
using HotelBookingAPI.Models;
using HotelBookingAPI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingAPI.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<AvailableRoomResponseDto>>
        SearchAvailableRoomsAsync(AvailableRoomQueryDto query)
    {
        if (query.CheckInDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException(
                "Check-in date cannot be in the past.");
        }

        if (query.CheckOutDate <= query.CheckInDate)
        {
            throw new ArgumentException(
                "Check-out date must be after check-in date.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        var numberOfNights =
            query.CheckOutDate.DayNumber - query.CheckInDate.DayNumber;

        var rooms = await _db.Rooms
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                r.Capacity >= query.Guests &&
                r.RoomType.IsActive &&
                r.RoomType.HotelId == query.HotelId &&
                r.RoomType.Hotel.IsActive &&
                !r.Bookings.Any(b =>
                    b.CheckInDate < query.CheckOutDate &&
                    b.CheckOutDate > query.CheckInDate &&
                    (
                        b.Status == BookingStatus.Confirmed ||
                        b.Status == BookingStatus.CheckedIn ||
                        (
                            b.Status == BookingStatus.Pending &&
                            b.ExpiresAt > now
                        )
                    )
                )
            )
            .Select(r => new AvailableRoomResponseDto
            {
                RoomId = r.Id,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                Capacity = r.Capacity,
                RoomTypeName = r.RoomType.Name,
                HotelName = r.RoomType.Hotel.Name,
                PricePerNight = r.PricePerNight,
                NumberOfNights = numberOfNights,
                EstimatedTotalPrice =
                    r.PricePerNight * numberOfNights
            })
            .ToListAsync();

        return rooms;
    }
    public async Task<BookingResponseDto> CreateBookingAsync(
    string userId,
    CreateBookingDto dto)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // 1. Validate booking dates
        if (dto.CheckInDate < today)
        {
            throw new ArgumentException(
                "Check-in date cannot be in the past.");
        }

        if (dto.CheckOutDate <= dto.CheckInDate)
        {
            throw new ArgumentException(
                "Check-out date must be after check-in date.");
        }

        // 2. Start database transaction
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        // 3. Lock the selected room row.
        // Another booking request for this room must wait
        // until this transaction finishes.
        var room = await _db.Rooms
            .FromSqlInterpolated(
                $"SELECT * FROM \"Rooms\" WHERE \"Id\" = {dto.RoomId} FOR UPDATE")
            .Include(r => r.RoomType)
            .ThenInclude(rt => rt.Hotel)
            .FirstOrDefaultAsync();

        if (room == null || !room.IsActive ||
            !room.RoomType.IsActive ||
            !room.RoomType.Hotel.IsActive)
        {
            throw new KeyNotFoundException(
                "Room not found or unavailable.");
        }

        // 4. Validate guest capacity
        if (dto.NumberOfGuests > room.Capacity)
        {
            throw new ArgumentException(
                $"This room can accommodate only {room.Capacity} guests.");
        }

        // 5. Expire old pending bookings for this room
        await _db.Bookings
            .Where(b =>
                b.RoomId == room.Id &&
                b.Status == BookingStatus.Pending &&
                b.ExpiresAt <= now)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    b => b.Status,
                    BookingStatus.Expired));

        // 6. Check overlapping bookings
        var hasConflict = await _db.Bookings.AnyAsync(b =>
            b.RoomId == room.Id &&
            b.CheckInDate < dto.CheckOutDate &&
            b.CheckOutDate > dto.CheckInDate &&
            (
                b.Status == BookingStatus.Confirmed ||
                b.Status == BookingStatus.CheckedIn ||
                (
                    b.Status == BookingStatus.Pending &&
                    b.ExpiresAt > now
                )
            ));

        if (hasConflict)
        {
            throw new InvalidOperationException(
                "This room is already booked for the selected dates.");
        }

        // 7. Calculate price on the server
        var numberOfNights =
            dto.CheckOutDate.DayNumber - dto.CheckInDate.DayNumber;

        var pricePerNight = room.PricePerNight;

        var totalPrice = pricePerNight * numberOfNights;

        // 8. Create pending booking
        var booking = new Booking
        {
            UserId = userId,
            RoomId = room.Id,
            CheckInDate = dto.CheckInDate,
            CheckOutDate = dto.CheckOutDate,
            NumberOfGuests = dto.NumberOfGuests,
            PricePerNight = pricePerNight,
            NumberOfNights = numberOfNights,
            TotalPrice = totalPrice,
            Currency = "USD",
            Status = BookingStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15)
        };

        _db.Bookings.Add(booking);

        await _db.SaveChangesAsync();

        // 9. Commit transaction
        await transaction.CommitAsync();

        // 10. Return booking information
        return await GetBookingResponseAsync(booking.Id);
    }

    public async Task<List<BookingResponseDto>> GetMyBookingsAsync(
    string userId)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookingResponseDto
            {
                Id = b.Id,
                UserId = b.UserId,
                BookingReference = b.BookingReference,
                HotelName = b.Room.RoomType.Hotel.Name,
                RoomNumber = b.Room.RoomNumber,
                RoomTypeName = b.Room.RoomType.Name,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                NumberOfGuests = b.NumberOfGuests,
                NumberOfNights = b.NumberOfNights,
                PricePerNight = b.PricePerNight,
                TotalPrice = b.TotalPrice,
                Currency = b.Currency,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                ExpiresAt = b.ExpiresAt
            })
            .ToListAsync();
    }
    public async Task<BookingResponseDto?> GetBookingByIdAsync(int id)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BookingResponseDto
            {
                Id = b.Id,
                UserId = b.UserId,
                BookingReference = b.BookingReference,
                HotelName = b.Room.RoomType.Hotel.Name,
                RoomNumber = b.Room.RoomNumber,
                RoomTypeName = b.Room.RoomType.Name,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                NumberOfGuests = b.NumberOfGuests,
                NumberOfNights = b.NumberOfNights,
                PricePerNight = b.PricePerNight,
                TotalPrice = b.TotalPrice,
                Currency = b.Currency,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                ExpiresAt = b.ExpiresAt
            })
            .FirstOrDefaultAsync();
    }

    public Task<bool> IsBookingOwnedByUserAsync(int id, string userId)
    {
        return _db.Bookings
            .AsNoTracking()
            .AnyAsync(b => b.Id == id && b.UserId == userId);
    }
    private async Task<BookingResponseDto> GetBookingResponseAsync(int id)
    {
        var booking = await GetBookingByIdAsync(id);

        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        return booking;
    }
    public async Task<bool> CancelBookingAsync(int id, string userId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var booking = await _db.Bookings
            .FromSqlInterpolated(
                $"SELECT * FROM \"Bookings\" WHERE \"Id\" = {id} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (booking == null || booking.UserId != userId)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Expire a pending booking if its hold has passed.
        if (booking.Status == BookingStatus.Pending &&
            booking.ExpiresAt <= now)
        {
            booking.Status = BookingStatus.Expired;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return false;
        }

        if (booking.Status != BookingStatus.Pending &&
            booking.Status != BookingStatus.Confirmed)
        {
            return false;
        }

        if (booking.CheckInDate <= today)
        {
            throw new InvalidOperationException(
                "A booking cannot be cancelled on or after its check-in date.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = now;
        booking.CancellationReason = "Cancelled by customer.";

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return true;
    }
    public async Task<bool> CheckInAsync(int bookingId)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            return false;
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException(
                "Only confirmed bookings can check in.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (booking.CheckInDate != today)
        {
            throw new InvalidOperationException(
                "Guest can only check in on the scheduled check-in date.");
        }

        booking.Status = BookingStatus.CheckedIn;
        booking.CheckedInAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CheckOutAsync(int bookingId)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            return false;
        }

        if (booking.Status != BookingStatus.CheckedIn)
        {
            throw new InvalidOperationException(
                "Only checked-in bookings can check out.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (today < booking.CheckOutDate)
        {
            throw new InvalidOperationException(
                "Guest cannot check out before the scheduled check-out date.");
        }

        booking.Status = BookingStatus.CheckedOut;
        booking.CheckedOutAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }
}
