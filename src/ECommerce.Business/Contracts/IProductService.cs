using ECommerce.Business.DTOs.Product;

namespace ECommerce.Business.Contracts;

public interface IProductService
{
    Task<ServiceResult<PaginatedResult<ProductDto>>> GetCatalogAsync(
        ProductSearchQuery query,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductDto>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<ProductDto>> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
