using ECommerce.Models.Enums;

namespace ECommerce.Business.DTOs.Payment;

public class PaymentProcessResult
{
    public bool Succeeded { get; set; }
    public PaymentStatus Status { get; set; }
    public string PaymentReference { get; set; } = null!;
    public string? TransactionId { get; set; }
}
