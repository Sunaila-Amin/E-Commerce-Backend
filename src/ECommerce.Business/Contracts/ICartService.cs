using ECommerce.Business.DTOs.Cart;

namespace ECommerce.Business.Contracts;

public interface ICartService
{
    Task<ServiceResult<CartDto>> GetCartAsync(int userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<CartDto>> AddItemAsync(
        int userId,
        AddToCartRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<CartDto>> UpdateItemAsync(
        int userId,
        int cartItemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<CartDto>> RemoveItemAsync(
        int userId,
        int cartItemId,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> ClearCartAsync(int userId, CancellationToken cancellationToken = default);
}
