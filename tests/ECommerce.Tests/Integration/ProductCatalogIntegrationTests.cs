using ECommerce.Business.DTOs.Product;
using FluentAssertions;
using Xunit;

namespace ECommerce.Tests.Integration;

public class ProductCatalogIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public ProductCatalogIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    private static CreateProductRequest NewProduct(string sku) => new()
    {
        Name = $"Product-{sku}",
        Sku = sku,
        Price = 99.99m,
        Description = "Integration test product",
        CategoryId = 1,
        IsActive = true,
        InitialStock = 25
    };

    [Fact]
    public async Task Create_Then_GetById_ReturnsWithAvailableStock()
    {
        var sku = $"SKU-{Guid.NewGuid():N}";
        var created = await _fixture.Products.CreateAsync(NewProduct(sku));

        created.Succeeded.Should().BeTrue();
        created.Data!.Sku.Should().Be(sku);

        var byId = await _fixture.Products.GetByIdAsync(created.Data.Id);

        byId.Succeeded.Should().BeTrue();
        byId.Data!.AvailableStock.Should().Be(25);
        byId.Data.CategoryName.Should().Be("Electronics");
    }

    [Fact]
    public async Task Create_DuplicateSku_ReturnsFailure()
    {
        var sku = $"SKU-{Guid.NewGuid():N}";
        await _fixture.Products.CreateAsync(NewProduct(sku));

        var duplicate = await _fixture.Products.CreateAsync(NewProduct(sku));

        duplicate.Succeeded.Should().BeFalse();
        duplicate.Message.Should().Contain("SKU");
    }

    [Fact]
    public async Task Catalog_ContainsCreatedProduct()
    {
        var sku = $"SKU-{Guid.NewGuid():N}";
        await _fixture.Products.CreateAsync(NewProduct(sku));

        var catalog = await _fixture.Products.GetCatalogAsync(new ProductSearchQuery
        {
            Search = sku,
            Page = 1,
            PageSize = 10
        });

        catalog.Succeeded.Should().BeTrue();
        catalog.Data!.Items.Should().Contain(p => p.Sku == sku);
    }

    [Fact]
    public async Task Product_StockAdjusts_AndReflectsInInventory()
    {
        var sku = $"SKU-{Guid.NewGuid():N}";
        var created = await _fixture.Products.CreateAsync(NewProduct(sku));

        var adjust = await _fixture.Inventory.AdjustStockAsync(created.Data!.Id,
            new ECommerce.Business.DTOs.Inventory.AdjustStockRequest { Delta = -5, Reason = "damaged" });

        adjust.Succeeded.Should().BeTrue();
        adjust.Data!.Available.Should().Be(20);
    }
}
