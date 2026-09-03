using ECommerce.Models.Entities;

namespace ECommerce.Business.Abstractions.Repositories;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Cart?> GetActiveWithItemsAsync(int userId, CancellationToken cancellationToken = default);
}
