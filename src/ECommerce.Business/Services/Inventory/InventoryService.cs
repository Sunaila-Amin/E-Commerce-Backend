using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Inventory;
using AutoMapper;

namespace ECommerce.Business.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobService _backgroundJobService;

    public InventoryService(
        IUnitOfWork uow,
        IMapper mapper,
        INotificationService notificationService,
        IBackgroundJobService backgroundJobService)
    {
        _uow = uow;
        _mapper = mapper;
        _notificationService = notificationService;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<ServiceResult<InventoryDto>> GetByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _uow.Inventories.GetByProductIdAsync(productId, cancellationToken);

        if (inventory is null)
        {
            return ServiceResult<InventoryDto>.Failure("Inventory not found for this product.");
        }

        var dto = _mapper.Map<InventoryDto>(inventory);
        return ServiceResult<InventoryDto>.Success(dto);
    }

    public async Task<ServiceResult<IReadOnlyList<InventoryDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var inventories = await _uow.Inventories.GetAllAsync(cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<InventoryDto>>(inventories);

        return ServiceResult<IReadOnlyList<InventoryDto>>.Success(dtos);
    }

    public async Task<ServiceResult<IReadOnlyList<InventoryDto>>> GetLowStockAsync(
        CancellationToken cancellationToken = default)
    {
        var low = await _uow.Inventories.GetLowStockAsync(cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<InventoryDto>>(low);

        return ServiceResult<IReadOnlyList<InventoryDto>>.Success(dtos);
    }

    public async Task<ServiceResult<InventoryDto>> UpdateAsync(
        int productId,
        UpdateInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _uow.Inventories.GetByProductIdAsync(productId, cancellationToken);

        if (inventory is null)
        {
            return ServiceResult<InventoryDto>.Failure("Inventory not found for this product.");
        }

        inventory.Quantity = request.Quantity;
        inventory.LowStockThreshold = request.LowStockThreshold ?? inventory.LowStockThreshold;
        inventory.UpdatedBy = "Admin";

        _uow.Inventories.Update(inventory);
        await _uow.SaveChangesAsync(cancellationToken);

        await RaiseStockNotificationsAsync(inventory, cancellationToken);

        var dto = _mapper.Map<InventoryDto>(inventory);
        return ServiceResult<InventoryDto>.Success(dto, "Inventory updated.");
    }

    public async Task<ServiceResult<InventoryDto>> AdjustStockAsync(
        int productId,
        AdjustStockRequest request,
        CancellationToken cancellationToken = default)
    {
        var inventory = await _uow.Inventories.GetByProductIdAsync(productId, cancellationToken);

        if (inventory is null)
        {
            return ServiceResult<InventoryDto>.Failure("Inventory not found for this product.");
        }

        if (inventory.Quantity + request.Delta < 0)
        {
            return ServiceResult<InventoryDto>.Failure("Adjustment would result in negative stock.");
        }

        inventory.Quantity += request.Delta;
        inventory.UpdatedBy = "Admin";

        _uow.Inventories.Update(inventory);
        await _uow.SaveChangesAsync(cancellationToken);

        await RaiseStockNotificationsAsync(inventory, cancellationToken);

        var dto = _mapper.Map<InventoryDto>(inventory);
        return ServiceResult<InventoryDto>.Success(dto, "Stock adjusted.");
    }

    private async Task RaiseStockNotificationsAsync(
        Models.Entities.Inventory inventory,
        CancellationToken cancellationToken)
    {
        await _notificationService.NotifyStockChangedAsync(
            inventory.ProductId,
            inventory.Product?.Name ?? $"Product #{inventory.ProductId}",
            inventory.Available,
            cancellationToken);

        if (inventory.Available <= inventory.LowStockThreshold)
        {
            _backgroundJobService.ScheduleLowStockAlert(inventory.ProductId, inventory.Available);
        }
    }
}
