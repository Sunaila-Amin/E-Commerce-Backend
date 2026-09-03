using ECommerce.Business.Contracts;
using ECommerce.Data.Persistence;
using ECommerce.Models.Entities;
using ECommerce.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Shared integration-test environment: builds the real DI graph and seeds baseline data
/// (roles, a category) so services that depend on them resolve correctly.
/// </summary>
public sealed class IntegrationFixture : IAsyncDisposable
{
    public ServiceProvider Provider { get; }

    public IntegrationFixture()
    {
        Provider = IntegrationTestHost.Build($"ECommerceTests-{Guid.NewGuid():N}");
        SeedBaseline();
    }

    private void SeedBaseline()
    {
        using var scope = Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        var userRole = new Role { Name = RoleName.User };
        var adminRole = new Role { Name = RoleName.Admin };
        db.Roles.AddRange(userRole, adminRole);

        if (!db.Categories.Any())
        {
            db.Categories.Add(new Category
            {
                Id = 1,
                Name = "Electronics",
                Slug = "electronics",
                IsActive = true,
                CreatedBy = "seed"
            });
        }

        db.SaveChanges();
    }

    public IServiceProvider Services => Provider;

    public IAuthService Auth => Provider.GetRequiredService<IAuthService>();
    public IProductService Products => Provider.GetRequiredService<IProductService>();
    public ICategoryService Categories => Provider.GetRequiredService<ICategoryService>();
    public ICartService Carts => Provider.GetRequiredService<ICartService>();
    public IOrderService Orders => Provider.GetRequiredService<IOrderService>();
    public IInventoryService Inventory => Provider.GetRequiredService<IInventoryService>();

    public async ValueTask DisposeAsync()
    {
        using var scope = Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureDeleted();
        await Provider.DisposeAsync();
    }
}
