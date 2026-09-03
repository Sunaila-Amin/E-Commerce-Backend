using ECommerce.Models.Enums;

namespace ECommerce.Business.DTOs.Order;

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
