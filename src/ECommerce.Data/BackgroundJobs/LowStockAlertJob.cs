using ECommerce.Business.Abstractions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Data.BackgroundJobs;

public class LowStockAlertJob
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LowStockAlertJob> _logger;

    public LowStockAlertJob(IUnitOfWork uow, ILogger<LowStockAlertJob> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task ExecuteAsync(int productId, int available, CancellationToken cancellationToken)
    {
        var inventory = await _uow.Inventories.GetByProductIdAsync(productId, cancellationToken);

        if (inventory is null)
        {
            _logger.LogWarning("LowStockAlert: inventory for product {ProductId} not found.", productId);
            return;
        }

        _logger.LogWarning(
            "LowStockAlert: product {ProductId} ({ProductName}) has only {Available} units available (threshold {Threshold}).",
            productId,
            inventory.Product?.Name,
            available,
            inventory.LowStockThreshold);
    }
}
