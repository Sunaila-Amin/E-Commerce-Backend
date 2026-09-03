using ECommerce.Business.DTOs.Inventory;

namespace ECommerce.Business.Contracts;

public interface IInventoryService
{
    Task<ServiceResult<InventoryDto>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<ServiceResult<IReadOnlyList<InventoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<IReadOnlyList<InventoryDto>>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<InventoryDto>> UpdateAsync(
        int productId,
        UpdateInventoryRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<InventoryDto>> AdjustStockAsync(
        int productId,
        AdjustStockRequest request,
        CancellationToken cancellationToken = default);
}
