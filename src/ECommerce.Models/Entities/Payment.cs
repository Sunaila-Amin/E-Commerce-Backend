using ECommerce.Models.Common;
using ECommerce.Models.Enums;

namespace ECommerce.Models.Entities;

public class Payment : AuditableEntity
{
    public string PaymentReference { get; set; } = null!;
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentProvider Provider { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = null!;
}
