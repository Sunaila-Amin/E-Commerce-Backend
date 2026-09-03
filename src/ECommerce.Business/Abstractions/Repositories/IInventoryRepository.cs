using ECommerce.Models.Entities;

namespace ECommerce.Business.Abstractions.Repositories;

public interface IInventoryRepository : IRepository<Inventory>
{
    Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inventory>> GetLowStockAsync(CancellationToken cancellationToken = default);
}
