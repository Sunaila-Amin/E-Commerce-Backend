using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Category;
using ECommerce.Models.Entities;
using AutoMapper;

namespace ECommerce.Business.Services.Categories;

public class CategoryService : ICategoryService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public CategoryService(IUnitOfWork uow, IMapper mapper, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var cacheKey = _cache.BuildKey("categories:active");

        var dtos = await _cache.GetOrSetAsync<IReadOnlyList<CategoryDto>>(
            cacheKey,
            async () =>
            {
                var categories = await _uow.Categories.GetActiveWithChildrenAsync(cancellationToken);
                return _mapper.Map<IReadOnlyList<CategoryDto>>(categories);
            },
            CacheDuration,
            cancellationToken);

        return ServiceResult<IReadOnlyList<CategoryDto>>.Success(dtos!);
    }

    public async Task<ServiceResult<CategoryDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _uow.Categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return ServiceResult<CategoryDto>.Failure("Category not found.");
        }

        var dto = _mapper.Map<CategoryDto>(category);
        return ServiceResult<CategoryDto>.Success(dto);
    }

    public async Task<ServiceResult<CategoryDto>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ParentId.HasValue)
        {
            var parentExists = await _uow.Categories.AnyAsync(
                c => c.Id == request.ParentId.Value,
                cancellationToken);

            if (!parentExists)
            {
                return ServiceResult<CategoryDto>.Failure("Parent category not found.");
            }
        }

        var category = new Category
        {
            Name = request.Name,
            Slug = Slugify(request.Name),
            Description = request.Description,
            ParentId = request.ParentId,
            IsActive = request.IsActive,
            CreatedBy = "Admin"
        };

        await _uow.Categories.AddAsync(category, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await InvalidateCacheAsync(cancellationToken);

        var dto = _mapper.Map<CategoryDto>(category);
        return ServiceResult<CategoryDto>.Success(dto, "Category created.");
    }

    public async Task<ServiceResult<CategoryDto>> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _uow.Categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return ServiceResult<CategoryDto>.Failure("Category not found.");
        }

        category.Name = request.Name;
        category.Slug = Slugify(request.Name);
        category.Description = request.Description;
        category.ParentId = request.ParentId;
        category.IsActive = request.IsActive;
        category.UpdatedBy = "Admin";

        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync(cancellationToken);

        await InvalidateCacheAsync(cancellationToken);

        var dto = _mapper.Map<CategoryDto>(category);
        return ServiceResult<CategoryDto>.Success(dto, "Category updated.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _uow.Categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return ServiceResult.Failure("Category not found.");
        }

        var hasProducts = await _uow.Products.AnyAsync(
            p => p.CategoryId == id,
            cancellationToken);

        if (hasProducts)
        {
            return ServiceResult.Failure("Cannot delete a category that has products.");
        }

        _uow.Categories.Remove(category);
        await _uow.SaveChangesAsync(cancellationToken);

        await InvalidateCacheAsync(cancellationToken);

        return ServiceResult.Success("Category deleted.");
    }

    private async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(_cache.BuildKey("categories:active"), cancellationToken);
    }

    private static string Slugify(string value) =>
        value.Trim().ToLower().Replace(" ", "-");
}
