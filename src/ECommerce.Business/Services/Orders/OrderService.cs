using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Order;
using ECommerce.Models.Entities;
using ECommerce.Models.Enums;
using AutoMapper;

namespace ECommerce.Business.Services.Orders;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly IBackgroundJobService _backgroundJobService;

    public OrderService(
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

    public async Task<ServiceResult<OrderDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdWithDetailsAsync(id, cancellationToken);

        if (order is null)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        var dto = _mapper.Map<OrderDto>(order);
        return ServiceResult<OrderDto>.Success(dto);
    }

    public async Task<ServiceResult<PaginatedResult<OrderDto>>> GetByUserAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

        var orders = await _uow.Orders.GetByUserAsync(userId, page, pageSize, cancellationToken);
        var total = await _uow.Orders.CountByUserAsync(userId, cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<OrderDto>>(orders);

        var result = PaginatedResult<OrderDto>.Create(dtos, page, pageSize, total);
        return ServiceResult<PaginatedResult<OrderDto>>.Success(result);
    }

    public async Task<ServiceResult<OrderDto>> GetByUserAndIdAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId, cancellationToken);

        if (order is null || order.UserId != userId)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        var dto = _mapper.Map<OrderDto>(order);
        return ServiceResult<OrderDto>.Success(dto);
    }

    public async Task<ServiceResult<OrderDto>> PlaceOrderAsync(
        int userId,
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var cart = await _uow.Carts.GetActiveWithItemsAsync(userId, cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            return ServiceResult<OrderDto>.Failure("Your cart is empty.");
        }

        if (request.ShippingAddressId.HasValue)
        {
            var ownsAddress = await _uow.Addresses.AnyAsync(
                a => a.Id == request.ShippingAddressId.Value && a.UserId == userId,
                cancellationToken);

            if (!ownsAddress)
            {
                return ServiceResult<OrderDto>.Failure("Shipping address not found.");
            }
        }

        var createRequest = new CreateOrderRequest
        {
            ShippingAddressId = request.ShippingAddressId,
            Items = cart.Items.Select(i => new OrderLineRequest
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        var result = await CreateOrderCoreAsync(userId, createRequest, cancellationToken);

        if (result.Succeeded)
        {
            await ClearCartAsync(cart, cancellationToken);
        }

        return result;
    }

    public async Task<ServiceResult<OrderDto>> CreateOrderAsync(
        int userId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CreateOrderCoreAsync(userId, request, cancellationToken);
    }

    public async Task<ServiceResult> CancelOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId, cancellationToken);

        if (order is null || order.UserId != userId)
        {
            return ServiceResult.Failure("Order not found.");
        }

        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            return ServiceResult.Failure($"Order cannot be cancelled in status {order.Status}.");
        }

        foreach (var item in order.Items)
        {
            var inventory = await _uow.Inventories.GetByProductIdAsync(item.ProductId, cancellationToken);
            if (inventory is not null)
            {
                inventory.Reserved = Math.Max(0, inventory.Reserved - item.Quantity);
                _uow.Inventories.Update(inventory);
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedBy = userId.ToString();

        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyOrderStatusChangedAsync(
            userId,
            order.Id,
            order.OrderNumber,
            order.Status,
            cancellationToken);

        return ServiceResult.Success("Order cancelled.");
    }

    public async Task<ServiceResult<OrderDto>> UpdateStatusAsync(
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId, cancellationToken);

        if (order is null)
        {
            return ServiceResult<OrderDto>.Failure("Order not found.");
        }

        if (order.Status == request.Status)
        {
            return ServiceResult<OrderDto>.Failure("Order is already in that status.");
        }

        order.Status = request.Status;
        order.UpdatedBy = "Admin";

        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyOrderStatusChangedAsync(
            order.UserId,
            order.Id,
            order.OrderNumber,
            order.Status,
            cancellationToken);

        var dto = _mapper.Map<OrderDto>(order);
        return ServiceResult<OrderDto>.Success(dto, "Order status updated.");
    }

    private async Task<ServiceResult<OrderDto>> CreateOrderCoreAsync(
        int userId,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return ServiceResult<OrderDto>.Failure("Order must contain at least one item.");
        }

        var orderNumber = await _uow.Orders.GenerateOrderNumberAsync(cancellationToken);
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            ShippingAddressId = request.ShippingAddressId,
            Status = OrderStatus.Pending,
            CreatedBy = userId.ToString()
        };

        var orderItems = new List<OrderItem>();
        decimal subtotal = 0;

        foreach (var line in request.Items)
        {
            var product = await _uow.Products.GetByIdWithInventoryAsync(line.ProductId, cancellationToken);

            if (product is null || !product.IsActive)
            {
                return ServiceResult<OrderDto>.Failure($"Product with id {line.ProductId} was not found or is inactive.");
            }

            var inventory = await _uow.Inventories.GetByProductIdAsync(line.ProductId, cancellationToken);

            if (inventory is null || inventory.Available < line.Quantity)
            {
                return ServiceResult<OrderDto>.Failure($"Insufficient stock for {product.Name}.");
            }

            inventory.Reserved += line.Quantity;
            _uow.Inventories.Update(inventory);

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSku = product.Sku,
                Quantity = line.Quantity,
                UnitPrice = product.Price
            });

            subtotal += product.Price * line.Quantity;
        }

        const decimal shippingCost = 10.00m;
        const decimal taxRate = 0.08m;
        var tax = subtotal * taxRate;
        var total = subtotal + shippingCost + tax;

        order.Subtotal = subtotal;
        order.ShippingCost = shippingCost;
        order.Tax = tax;
        order.TotalAmount = total;
        order.PlacedAt = DateTime.UtcNow;

        foreach (var item in orderItems)
        {
            order.Items.Add(item);
        }

        await _uow.Orders.AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _backgroundJobService.EnqueueOrderProcessing(order.Id);

        var result = await _uow.Orders.GetByIdWithDetailsAsync(order.Id, cancellationToken);
        var dto = _mapper.Map<OrderDto>(result);

        return ServiceResult<OrderDto>.Success(dto, "Order placed successfully.");
    }

    private async Task ClearCartAsync(Cart cart, CancellationToken cancellationToken)
    {
        cart.Items.Clear();
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
