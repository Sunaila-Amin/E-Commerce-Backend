using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Product;
using ECommerce.Models.Entities;
using AutoMapper;
using InventoryEntity = ECommerce.Models.Entities.Inventory;

namespace ECommerce.Business.Services.Products;

public class ProductService : IProductService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public ProductService(IUnitOfWork uow, IMapper mapper, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ServiceResult<PaginatedResult<ProductDto>>> GetCatalogAsync(
        ProductSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = _cache.BuildKey(
            "products:catalog",
            query.Search ?? string.Empty,
            query.CategoryId ?? 0,
            query.Page,
            query.PageSize);

        var result = await _cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var items = await _uow.Products.GetCatalogAsync(
                    query.Search,
                    query.CategoryId,
                    query.Page,
                    query.PageSize,
                    cancellationToken);

                var total = await _uow.Products.CountAsync(
                    p => p.IsActive &&
                        (string.IsNullOrEmpty(query.Search) || p.Name.Contains(query.Search) || p.Description!.Contains(query.Search)) &&
                        (!query.CategoryId.HasValue || p.CategoryId == query.CategoryId),
                    cancellationToken);

                var dtos = _mapper.Map<IReadOnlyList<ProductDto>>(items);
                return PaginatedResult<ProductDto>.Create(dtos, query.Page, query.PageSize, total);
            },
            CacheDuration,
            cancellationToken);

        return ServiceResult<PaginatedResult<ProductDto>>.Success(result!);
    }

    public async Task<ServiceResult<ProductDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = _cache.BuildKey("products:by-id", id);

        var dto = await _cache.GetOrSetAsync<ProductDto>(
            cacheKey,
            async () =>
            {
                var product = await _uow.Products.GetByIdWithInventoryAsync(id, cancellationToken);
                return product is null ? null : _mapper.Map<ProductDto>(product);
            },
            CacheDuration,
            cancellationToken);

        if (dto is null)
        {
            return ServiceResult<ProductDto>.Failure("Product not found.");
        }

        return ServiceResult<ProductDto>.Success(dto);
    }

    public async Task<ServiceResult<ProductDto>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var skuExists = await _uow.Products.AnyAsync(
            p => p.Sku == request.Sku,
            cancellationToken);

        if (skuExists)
        {
            return ServiceResult<ProductDto>.Failure("A product with this SKU already exists.");
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _uow.Categories.AnyAsync(
                c => c.Id == request.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                return ServiceResult<ProductDto>.Failure("Category not found.");
            }
        }

        var product = new Product
        {
            Name = request.Name,
            Slug = Slugify(request.Name),
            Description = request.Description,
            Sku = request.Sku,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            IsActive = request.IsActive,
            CreatedBy = "Admin"
        };

        if (request.InitialStock > 0)
        {
            product.Inventory = new InventoryEntity
            {
                Quantity = request.InitialStock,
                Reserved = 0
            };
        }

        await _uow.Products.AddAsync(product, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await InvalidateCatalogCacheAsync(cancellationToken);

        var created = await _uow.Products.GetByIdWithInventoryAsync(product.Id, cancellationToken);
        var dto = _mapper.Map<ProductDto>(created);

        return ServiceResult<ProductDto>.Success(dto, "Product created.");
    }

    public async Task<ServiceResult<ProductDto>> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Product not found.");
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _uow.Categories.AnyAsync(
                c => c.Id == request.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                return ServiceResult<ProductDto>.Failure("Category not found.");
            }
        }

        product.Name = request.Name;
        product.Slug = Slugify(request.Name);
        product.Description = request.Description;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;
        product.UpdatedBy = "Admin";

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        await InvalidateCatalogCacheAsync(cancellationToken);
        await _cache.RemoveAsync(_cache.BuildKey("products:by-id", id), cancellationToken);

        var updated = await _uow.Products.GetByIdWithInventoryAsync(id, cancellationToken);
        var dto = _mapper.Map<ProductDto>(updated);

        return ServiceResult<ProductDto>.Success(dto, "Product updated.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _uow.Products.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ServiceResult.Failure("Product not found.");
        }

        product.IsActive = false;
        product.UpdatedBy = "Admin";

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        await InvalidateCatalogCacheAsync(cancellationToken);
        await _cache.RemoveAsync(_cache.BuildKey("products:by-id", id), cancellationToken);

        return ServiceResult.Success("Product deactivated.");
    }

    private async Task InvalidateCatalogCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync("products:catalog", cancellationToken);
    }

    private static string Slugify(string value) =>
        value.Trim().ToLower().Replace(" ", "-");
}
