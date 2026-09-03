using ECommerce.Models.Entities;

namespace ECommerce.Business.Abstractions.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetActiveWithChildrenAsync(CancellationToken cancellationToken = default);
}
