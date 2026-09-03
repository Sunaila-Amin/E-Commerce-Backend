using ECommerce.Business.Contracts;
using ECommerce.Models.Enums;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.Data.RealTime;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyOrderStatusChangedAsync(
        int userId,
        int orderId,
        string orderNumber,
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("OrderStatusChanged", new
            {
                orderId,
                orderNumber,
                status = status.ToString()
            }, cancellationToken);
    }

    public async Task NotifyStockChangedAsync(
        int productId,
        string productName,
        int available,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group("stock-monitors")
            .SendAsync("StockChanged", new
            {
                productId,
                productName,
                available
            }, cancellationToken);
    }
}
