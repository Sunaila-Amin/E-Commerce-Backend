using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Data.Persistence;
using ECommerce.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Repositories;

public class InventoryRepository : Repository<Inventory>, IInventoryRepository
{
    public InventoryRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<Inventory?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<Inventory>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(i => i.Product)
            .Where(i => i.Quantity - i.Reserved <= i.LowStockThreshold)
            .ToListAsync(cancellationToken);
    }
}
