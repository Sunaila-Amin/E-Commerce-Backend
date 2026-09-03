using ECommerce.Models.Enums;

namespace ECommerce.Business.DTOs.Payment;

public class CreatePaymentRequest
{
    public int OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public object? ProviderData { get; set; }
}
