using ECommerce.Models.Entities;

namespace ECommerce.Business.Abstractions.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetCatalogAsync(
        string? search,
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdWithInventoryAsync(int id, CancellationToken cancellationToken = default);
}
