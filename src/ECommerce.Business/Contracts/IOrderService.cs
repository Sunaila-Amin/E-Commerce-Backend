using ECommerce.Business.DTOs.Order;

namespace ECommerce.Business.Contracts;

public interface IOrderService
{
    Task<ServiceResult<OrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<PaginatedResult<OrderDto>>> GetByUserAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<OrderDto>> GetByUserAndIdAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<OrderDto>> PlaceOrderAsync(
        int userId,
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<OrderDto>> CreateOrderAsync(
        int userId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<OrderDto>> UpdateStatusAsync(
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);
}
