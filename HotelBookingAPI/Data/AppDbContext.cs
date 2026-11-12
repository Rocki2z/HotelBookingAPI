using HotelBookingAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using HotelBookingAPI.Models.Enums;

namespace HotelBookingAPI.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Hotel> Hotels => Set<Hotel>(); // Add DbSet for Hotel entity

    public DbSet<RoomType> RoomTypes => Set<RoomType>(); // Add DbSet for RoomType entity

    public DbSet<Room> Rooms => Set<Room>(); // Add DbSet for Room entity
    public DbSet<Booking> Bookings => Set<Booking>(); // Add DbSet for Booking entity
    public DbSet<Payment> Payments => Set<Payment>(); // Add DbSet for Payment entity

    protected override void OnModelCreating(ModelBuilder modelBuilder) // Configure entity relationships and constraints
    {
        base.OnModelCreating(modelBuilder);

        // Hotel -> RoomType (One-to-Many)
        modelBuilder.Entity<RoomType>()
            .HasOne(rt => rt.Hotel)
            .WithMany(h => h.RoomTypes)
            .HasForeignKey(rt => rt.HotelId)
            .OnDelete(DeleteBehavior.Restrict);

        // RoomType -> Room (One-to-Many)
        modelBuilder.Entity<Room>()
            .HasOne(r => r.RoomType)
            .WithMany(rt => rt.Rooms)
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimal precision
        modelBuilder.Entity<RoomType>()
            .Property(rt => rt.BasePrice)
            .HasPrecision(12, 2);

        modelBuilder.Entity<Room>()
            .Property(r => r.PricePerNight)
            .HasPrecision(12, 2);

        // Hotel name and room number constraints
        modelBuilder.Entity<Hotel>()
            .Property(h => h.Name)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<Room>()
            .Property(r => r.RoomNumber)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Room>()
            .HasIndex(r => new { r.RoomTypeId, r.RoomNumber })
            .IsUnique();
        // Booking relationships
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking reference must be unique
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingReference)
            .IsUnique();

        // Store enum as readable text
        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Price precision
        modelBuilder.Entity<Booking>()
            .Property(b => b.PricePerNight)
            .HasPrecision(12, 2);

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalPrice)
            .HasPrecision(12, 2);

        // Indexes for booking searches
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new
            {
                b.UserId,
                b.CheckInDate,
                b.CheckOutDate
            });

        modelBuilder.Entity<Booking>()
            .HasIndex(b => new
            {
                b.RoomId,
                b.Status,
                b.CheckInDate,
                b.CheckOutDate
            });

        // Payment relationship
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique payment reference
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.PaymentReference)
            .IsUnique();

        // Provider transaction ID should be unique when present
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.ProviderTransactionId)
            .IsUnique()
            .HasFilter("\"ProviderTransactionId\" IS NOT NULL");

        // Payment amount
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(12, 2);

        // Store enums as text
        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<Payment>()
            .Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(30);
        // Hotel indexes
        modelBuilder.Entity<Hotel>()
            .HasIndex(h => h.City);

        modelBuilder.Entity<Hotel>()
            .HasIndex(h => h.Name);

        // Room indexes
        modelBuilder.Entity<RoomType>()
            .HasIndex(r => r.HotelId);

        // Booking indexes
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.RoomId);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.UserId);
    }
}