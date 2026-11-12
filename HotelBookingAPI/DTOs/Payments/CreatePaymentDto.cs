using System.ComponentModel.DataAnnotations;
using HotelBookingAPI.Models.Enums;

namespace HotelBookingAPI.DTOs.Payments;

public class CreatePaymentDto
{
    [Required]
    public PaymentMethod Method { get; set; }
}