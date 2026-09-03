using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Data.Persistence;
using ECommerce.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetActiveWithChildrenAsync(CancellationToken cancellationToken = default)
    {
        var categories = await DbSet
            .AsNoTracking()
            .Include(c => c.Children)
            .Where(c => c.ParentId == null && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories;
    }
}
