namespace ECommerce.Business.DTOs.Order;

public class CreateOrderRequest
{
    public int? ShippingAddressId { get; set; }
    public IReadOnlyList<OrderLineRequest> Items { get; set; } = new List<OrderLineRequest>();
}

public class OrderLineRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
