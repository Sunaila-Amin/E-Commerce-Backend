using ECommerce.Models.Enums;

namespace ECommerce.Business.Contracts;

public interface INotificationService
{
    Task NotifyOrderStatusChangedAsync(int userId, int orderId, string orderNumber, OrderStatus status, CancellationToken cancellationToken = default);
    Task NotifyStockChangedAsync(int productId, string productName, int available, CancellationToken cancellationToken = default);
}
