using AutoMapper;
using ECommerce.Business.Abstractions;
using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Order;
using ECommerce.Business.Services.Orders;
using ECommerce.Models.Entities;
using ECommerce.Models.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Unit.Services;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly IMapper _mapper = TestMapper.Create();
    private readonly Mock<INotificationService> _notification = new();
    private readonly Mock<IBackgroundJobService> _backgroundJobs = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_uow.Object, _mapper, _notification.Object, _backgroundJobs.Object);
    }

    private Mock<IOrderRepository> SetupOrders()
    {
        var orders = new Mock<IOrderRepository>();
        _uow.SetupGet(u => u.Orders).Returns(orders.Object);
        return orders;
    }

    [Fact]
    public async Task GetByUserAndIdAsync_WhenNotOwned_ReturnsFailure()
    {
        var orders = SetupOrders();
        orders.Setup(o => o.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewOrder(userId: 2));

        var result = await _sut.GetByUserAndIdAsync(userId: 1, orderId: 1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Order not found.");
    }

    [Fact]
    public async Task GetByUserAndIdAsync_WhenOwned_ReturnsOrder()
    {
        var orders = SetupOrders();
        orders.Setup(o => o.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewOrder(userId: 1));

        var result = await _sut.GetByUserAndIdAsync(userId: 1, orderId: 1);

        result.Succeeded.Should().BeTrue();
        result.Data!.OrderNumber.Should().Be("ORD-100");
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenCartEmpty_ReturnsFailure()
    {
        var carts = new Mock<ICartRepository>();
        _uow.SetupGet(u => u.Carts).Returns(carts.Object);
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cart { Id = 1, UserId = 1, IsActive = true, Items = new List<CartItem>() });

        var result = await _sut.PlaceOrderAsync(1, new PlaceOrderRequest());

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Your cart is empty.");
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenValid_CreatesOrderClearsCart()
    {
        var orders = SetupOrders();
        var carts = new Mock<ICartRepository>();
        var products = new Mock<IProductRepository>();
        var inventories = new Mock<IInventoryRepository>();
        _uow.SetupGet(u => u.Carts).Returns(carts.Object);
        _uow.SetupGet(u => u.Products).Returns(products.Object);
        _uow.SetupGet(u => u.Inventories).Returns(inventories.Object);

        var product = NewProduct(quantity: 10, reserved: 0);
        var cart = new Cart
        {
            Id = 1,
            UserId = 1,
            IsActive = true,
            Items = new List<CartItem>
            {
                new() { Id = 10, CartId = 1, ProductId = 1, Quantity = 2, Product = product }
            }
        };

        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        orders.Setup(o => o.GenerateOrderNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("ORD-100");
        products.Setup(p => p.GetByIdWithInventoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        inventories.Setup(i => i.GetByProductIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product.Inventory);
        orders.Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Order, CancellationToken>((o, _) => o.Id = 1);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        orders.Setup(o => o.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => NewOrder(userId: 1, id: id));

        var result = await _sut.PlaceOrderAsync(1, new PlaceOrderRequest());

        result.Succeeded.Should().BeTrue();
        cart.Items.Should().BeEmpty();
        _backgroundJobs.Verify(b => b.EnqueueOrderProcessing(1), Times.Once);
        product.Inventory!.Reserved.Should().Be(2);
    }

    [Fact]
    public async Task CreateOrderCoreAsync_WhenInsufficientStock_ReturnsFailure()
    {
        var orders = SetupOrders();
        var products = new Mock<IProductRepository>();
        var inventories = new Mock<IInventoryRepository>();
        _uow.SetupGet(u => u.Products).Returns(products.Object);
        _uow.SetupGet(u => u.Inventories).Returns(inventories.Object);

        var product = NewProduct(quantity: 1, reserved: 0);
        orders.Setup(o => o.GenerateOrderNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("ORD-100");
        products.Setup(p => p.GetByIdWithInventoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        inventories.Setup(i => i.GetByProductIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product.Inventory);

        var request = new CreateOrderRequest
        {
            Items = new List<OrderLineRequest> { new() { ProductId = 1, Quantity = 5 } }
        };

        var result = await _sut.CreateOrderAsync(1, request);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Insufficient stock");
        _backgroundJobs.Verify(b => b.EnqueueOrderProcessing(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_WhenShipped_ReturnsFailure()
    {
        var orders = SetupOrders();
        orders.Setup(o => o.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewOrder(userId: 1, status: OrderStatus.Shipped));

        var result = await _sut.CancelOrderAsync(1, 1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("cannot be cancelled");
    }

    [Fact]
    public async Task CancelOrderAsync_WhenValid_ReleasesStockAndNotifies()
    {
        var orders = SetupOrders();
        var inventories = new Mock<IInventoryRepository>();
        _uow.SetupGet(u => u.Inventories).Returns(inventories.Object);

        var inventory = new Inventory { ProductId = 1, Quantity = 10, Reserved = 2, LowStockThreshold = 5 };
        var order = NewOrder(userId: 1);
        order.Status = OrderStatus.Pending;

        orders.Setup(o => o.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        inventories.Setup(i => i.GetByProductIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CancelOrderAsync(1, 1);

        result.Succeeded.Should().BeTrue();
        inventory.Reserved.Should().Be(0);
        order.Status.Should().Be(OrderStatus.Cancelled);
        _notification.Verify(n => n.NotifyOrderStatusChangedAsync(
            1, 1, "ORD-100", OrderStatus.Cancelled, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenSameStatus_ReturnsFailure()
    {
        var orders = SetupOrders();
        var order = NewOrder(userId: 1, status: OrderStatus.Paid);
        orders.Setup(o => o.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.UpdateStatusAsync(1, new UpdateOrderStatusRequest { Status = OrderStatus.Paid });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Order is already in that status.");
    }

    private static Product NewProduct(int quantity, int reserved) => new()
    {
        Id = 1,
        Name = "Laptop",
        Slug = "laptop",
        Sku = "SKU-1",
        Price = 1000m,
        IsActive = true,
        Inventory = new Inventory
        {
            ProductId = 1,
            Quantity = quantity,
            Reserved = reserved,
            LowStockThreshold = 5
        }
    };

    private static Order NewOrder(int userId, OrderStatus status = OrderStatus.Pending, int id = 1)
    {
        var product = NewProduct(quantity: 10, reserved: 0);
        return new Order
        {
            Id = id,
            OrderNumber = "ORD-100",
            UserId = userId,
            Status = status,
            Subtotal = 2000m,
            ShippingCost = 10m,
            Tax = 160m,
            TotalAmount = 2170m,
            PlacedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = 1,
                    OrderId = id,
                    ProductId = 1,
                    ProductName = "Laptop",
                    ProductSku = "SKU-1",
                    Quantity = 2,
                    UnitPrice = 1000m,
                    Product = product
                }
            }
        };
    }
}
