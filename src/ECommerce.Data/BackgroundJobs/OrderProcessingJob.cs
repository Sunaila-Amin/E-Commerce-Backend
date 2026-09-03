using ECommerce.Business.Abstractions;
using ECommerce.Models.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Data.BackgroundJobs;

public class OrderProcessingJob
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderProcessingJob> _logger;

    public OrderProcessingJob(IUnitOfWork uow, ILogger<OrderProcessingJob> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task ProcessAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning("OrderProcessing: order {OrderId} not found.", orderId);
            return;
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogInformation("OrderProcessing: order {OrderId} already {Status}.", orderId, order.Status);
            return;
        }

        order.Status = OrderStatus.Processing;
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OrderProcessing: order {OrderId} moved to Processing.", orderId);
    }
}
