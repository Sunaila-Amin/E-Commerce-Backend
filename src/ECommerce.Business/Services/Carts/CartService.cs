using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Cart;
using ECommerce.Models.Entities;
using AutoMapper;

namespace ECommerce.Business.Services.Carts;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CartService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ServiceResult<CartDto>> GetCartAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _uow.Carts.GetActiveWithItemsAsync(userId, cancellationToken);

        if (cart is null)
        {
            return ServiceResult<CartDto>.Failure("Cart not found.");
        }

        var dto = _mapper.Map<CartDto>(cart);
        return ServiceResult<CartDto>.Success(dto);
    }

    public async Task<ServiceResult<CartDto>> AddItemAsync(
        int userId,
        AddToCartRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await _uow.Products.GetByIdWithInventoryAsync(request.ProductId, cancellationToken);

        if (product is null || !product.IsActive)
        {
            return ServiceResult<CartDto>.Failure("Product not found or is inactive.");
        }

        if (request.Quantity <= 0)
        {
            return ServiceResult<CartDto>.Failure("Quantity must be greater than zero.");
        }

        var cart = await GetOrCreateCartAsync(userId, cancellationToken);

        var existing = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        var newQuantity = (existing?.Quantity ?? 0) + request.Quantity;
        var available = product.Inventory?.Available ?? 0;

        if (newQuantity > available)
        {
            return ServiceResult<CartDto>.Failure(
                $"Only {available} unit(s) of {product.Name} available in stock.");
        }

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CartId = cart.Id
            });
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var updated = await _uow.Carts.GetActiveWithItemsAsync(userId, cancellationToken);
        var dto = _mapper.Map<CartDto>(updated);

        return ServiceResult<CartDto>.Success(dto, "Item added to cart.");
    }

    public async Task<ServiceResult<CartDto>> UpdateItemAsync(
        int userId,
        int cartItemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var cart = await _uow.Carts.GetActiveWithItemsAsync(userId, cancellationToken);

        if (cart is null)
        {
            return ServiceResult<CartDto>.Failure("Cart not found.");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

        if (item is null)
        {
            return ServiceResult<CartDto>.Failure("Cart item not found.");
        }

        item.Quantity = request.Quantity;
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<CartDto>(cart);
        return ServiceResult<CartDto>.Success(dto, "Cart item updated.");
    }

    public async Task<ServiceResult<CartDto>> RemoveItemAsync(
        int userId,
        int cartItemId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _uow.Carts.GetActiveWithItemsAsync(userId, cancellationToken);

        if (cart is null)
        {
            return ServiceResult<CartDto>.Failure("Cart not found.");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

        if (item is null)
        {
            return ServiceResult<CartDto>.Failure("Cart item not found.");
        }

        cart.Items.Remove(item);
        await _uow.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<CartDto>(cart);
        return ServiceResult<CartDto>.Success(dto, "Item removed from cart.");
    }

    public async Task<ServiceResult> ClearCartAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cart = await _uow.Carts.GetActiveWithItemsAsync(userId, cancellationToken);

        if (cart is null)
        {
            return ServiceResult.Failure("Cart not found.");
        }

        cart.Items.Clear();
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success("Cart cleared.");
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId, CancellationToken cancellationToken)
    {
        var cart = await _uow.Carts.GetActiveWithItemsAsync(userId, cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            UserId = userId,
            IsActive = true
        };

        await _uow.Carts.AddAsync(cart, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return cart;
    }
}
