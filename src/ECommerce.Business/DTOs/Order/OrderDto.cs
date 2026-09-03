using ECommerce.Models.Enums;

namespace ECommerce.Business.DTOs.Order;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public int UserId { get; set; }
    public int? ShippingAddressId { get; set; }
    public OrderStatus Status { get; set; }
    public string StatusName { get; set; } = null!;
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime PlacedAt { get; set; }
    public IReadOnlyList<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
}
