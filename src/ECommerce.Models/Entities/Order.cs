using ECommerce.Models.Common;
using ECommerce.Models.Enums;

namespace ECommerce.Models.Entities;

public class Order : AuditableEntity
{
    public string OrderNumber { get; set; } = null!;
    public int UserId { get; set; }
    public int? ShippingAddressId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? PlacedAt { get; set; }

    public User User { get; set; } = null!;
    public Address? ShippingAddress { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
