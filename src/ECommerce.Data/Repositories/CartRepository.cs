using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Data.Persistence;
using ECommerce.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Repositories;

public class CartRepository : Repository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<Cart?> GetActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive, cancellationToken);
    }

    public async Task<Cart?> GetActiveWithItemsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive, cancellationToken);
    }
}
