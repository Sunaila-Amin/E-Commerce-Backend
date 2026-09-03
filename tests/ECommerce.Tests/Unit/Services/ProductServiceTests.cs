using AutoMapper;
using ECommerce.Business.Abstractions;
using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Product;
using ECommerce.Business.Services.Products;
using ECommerce.Models.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Unit.Services;

public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly IMapper _mapper = TestMapper.Create();
    private readonly Mock<ICacheService> _cache = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_uow.Object, _mapper, _cache.Object);
    }

    private Mock<IProductRepository> SetupProducts()
    {
        var products = new Mock<IProductRepository>();
        _uow.SetupGet(u => u.Products).Returns(products.Object);
        return products;
    }

    private Mock<ICategoryRepository> SetupCategories()
    {
        var categories = new Mock<ICategoryRepository>();
        _uow.SetupGet(u => u.Categories).Returns(categories.Object);
        return categories;
    }

    [Fact]
    public async Task GetCatalogAsync_RunsFactoryWhenCacheEmpty_ReturnsPaginatedResult()
    {
        var products = SetupProducts();
        var items = new List<Product>
        {
            new()
            {
                Id = 1,
                Name = "Laptop",
                Slug = "laptop",
                Sku = "SKU-1",
                Price = 1000m,
                IsActive = true
            }
        };

        products.Setup(p => p.GetCatalogAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        products.Setup(p => p.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _cache.Setup(c => c.GetOrSetAsync<PaginatedResult<ProductDto>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PaginatedResult<ProductDto>?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<Task<PaginatedResult<ProductDto>?>> factory, TimeSpan? _, CancellationToken _) => factory());

        var result = await _sut.GetCatalogAsync(new ProductSearchQuery { Page = 1, PageSize = 10 });

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Name.Should().Be("Laptop");
        result.Data.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsCachedValue()
    {
        SetupProducts();

        var cached = PaginatedResult<ProductDto>.Create(
            new List<ProductDto> { new() { Id = 7, Name = "Cached", Slug = "cached", Sku = "S", Price = 1m } },
            1,
            10,
            1);

        _cache.Setup(c => c.GetOrSetAsync<PaginatedResult<ProductDto>>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<PaginatedResult<ProductDto>?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _sut.GetCatalogAsync(new ProductSearchQuery { Page = 1, PageSize = 10 });

        result.Data!.Items[0].Name.Should().Be("Cached");
        _uow.Verify(p => p.Products.GetCatalogAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsFailure()
    {
        var products = SetupProducts();
        products.Setup(p => p.GetByIdWithInventoryAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _cache.Setup(c => c.GetOrSetAsync<ProductDto>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<ProductDto?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<Task<ProductDto?>> factory, TimeSpan? _, CancellationToken _) => factory());

        var result = await _sut.GetByIdAsync(99);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Product not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedProduct()
    {
        var products = SetupProducts();
        products.Setup(p => p.GetByIdWithInventoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product
            {
                Id = 1,
                Name = "Phone",
                Slug = "phone",
                Sku = "SKU-2",
                Price = 500m,
                IsActive = true,
                Inventory = new Inventory { Quantity = 10, Reserved = 0, LowStockThreshold = 5 }
            });

        _cache.Setup(c => c.GetOrSetAsync<ProductDto>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<ProductDto?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<Task<ProductDto?>> factory, TimeSpan? _, CancellationToken _) => factory());

        var result = await _sut.GetByIdAsync(1);

        result.Succeeded.Should().BeTrue();
        result.Data!.Name.Should().Be("Phone");
        result.Data.AvailableStock.Should().Be(10);
    }

    [Fact]
    public async Task CreateAsync_WhenSkuExists_ReturnsFailure()
    {
        var products = SetupProducts();
        products.Setup(p => p.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(CreateRequest());

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("SKU");
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryInvalid_ReturnsFailure()
    {
        var products = SetupProducts();
        var categories = SetupCategories();

        products.Setup(p => p.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        categories.Setup(c => c.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = CreateRequest();
        request.CategoryId = 555;

        var result = await _sut.CreateAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Category not found.");
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsSuccessAndClearsCatalog()
    {
        var products = SetupProducts();
        var categories = SetupCategories();

        products.Setup(p => p.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        categories.Setup(c => c.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        products.Setup(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Product, CancellationToken>((p, _) => p.Id = 1);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        products.Setup(p => p.GetByIdWithInventoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Product
            {
                Id = id,
                Name = "Mouse",
                Slug = "mouse",
                Sku = "SKU-3",
                Price = 25m,
                IsActive = true
            });

        var result = await _sut.CreateAsync(CreateRequest());

        result.Succeeded.Should().BeTrue();
        _cache.Verify(c => c.RemoveAsync("products:catalog", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFailure()
    {
        var products = SetupProducts();
        products.Setup(p => p.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.DeleteAsync(99);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Product not found.");
    }

    private static CreateProductRequest CreateRequest() => new()
    {
        Name = "Mouse",
        Sku = "SKU-3",
        Price = 25m,
        IsActive = true
    };
}
