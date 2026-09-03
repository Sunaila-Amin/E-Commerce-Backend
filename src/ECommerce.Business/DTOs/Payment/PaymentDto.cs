using ECommerce.Models.Enums;

namespace ECommerce.Business.DTOs.Payment;

public class PaymentDto
{
    public int Id { get; set; }
    public string PaymentReference { get; set; } = null!;
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentProvider Provider { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
}
