using ECommerce.Business.DTOs.Category;

namespace ECommerce.Business.Contracts;

public interface ICategoryService
{
    Task<ServiceResult<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryDto>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryDto>> UpdateAsync(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
