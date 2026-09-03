using ECommerce.Business.DTOs.Auth;
using ECommerce.Business.DTOs.Cart;
using ECommerce.Business.DTOs.Inventory;
using ECommerce.Business.DTOs.Order;
using ECommerce.Business.DTOs.Product;
using ECommerce.Models.Enums;
using FluentAssertions;
using Xunit;

namespace ECommerce.Tests.Integration;

public class CartOrderFlowIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public CartOrderFlowIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddToCart_PlaceOrder_ComputesTotals_AndClearsCart()
    {
        var userId = await RegisterUserAsync();
        var product = await CreateProductAsync();

        var cart = await _fixture.Carts.AddItemAsync(userId, new AddToCartRequest
        {
            ProductId = product.Id,
            Quantity = 2
        });

        cart.Succeeded.Should().BeTrue();
        cart.Data!.ItemCount.Should().Be(2);

        var order = await _fixture.Orders.PlaceOrderAsync(userId, new PlaceOrderRequest());

        order.Succeeded.Should().BeTrue();
        order.Data!.UserId.Should().Be(userId);
        order.Data.Status.Should().Be(OrderStatus.Pending);
        order.Data.Subtotal.Should().Be(199.98m);
        order.Data.ShippingCost.Should().Be(10m);
        order.Data.Tax.Should().Be(15.9984m);
        order.Data.TotalAmount.Should().Be(225.9784m);
        order.Data.Items.Should().HaveCount(1);

        // Cart should be cleared after placing the order.
        var afterOrderCart = await _fixture.Carts.GetCartAsync(userId);
        afterOrderCart.Data!.ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task PlaceOrder_ReservesStock_AndCancelReleasesIt()
    {
        var userId = await RegisterUserAsync();
        var product = await CreateProductAsync(stock: 10);

        await _fixture.Carts.AddItemAsync(userId, new AddToCartRequest
        {
            ProductId = product.Id,
            Quantity = 3
        });

        // Adding to cart must NOT reserve stock yet.
        var inventoryAfterAdd = await _fixture.Inventory.GetByProductIdAsync(product.Id);
        inventoryAfterAdd.Data!.Available.Should().Be(10);

        var order = await _fixture.Orders.PlaceOrderAsync(userId, new PlaceOrderRequest());

        // Placing the order reserves the quantity.
        var inventoryAfterPlace = await _fixture.Inventory.GetByProductIdAsync(product.Id);
        inventoryAfterPlace.Data!.Available.Should().Be(7); // 10 - 3 reserved

        var cancel = await _fixture.Orders.CancelOrderAsync(userId, order.Data!.Id);
        cancel.Succeeded.Should().BeTrue();

        var orderAfterCancel = await _fixture.Orders.GetByUserAndIdAsync(userId, order.Data.Id);
        orderAfterCancel.Data!.Status.Should().Be(OrderStatus.Cancelled);

        var inventoryAfterCancel = await _fixture.Inventory.GetByProductIdAsync(product.Id);
        inventoryAfterCancel.Data!.Available.Should().Be(10); // reservation released
    }

    [Fact]
    public async Task PlaceOrder_WhenCartEmpty_ReturnsFailure()
    {
        var userId = await RegisterUserAsync();

        var order = await _fixture.Orders.PlaceOrderAsync(userId, new PlaceOrderRequest());

        order.Succeeded.Should().BeFalse();
        order.Message.Should().Be("Your cart is empty.");
    }

    [Fact]
    public async Task PlaceOrder_InsufficientStock_ReturnsFailure()
    {
        var userId = await RegisterUserAsync();
        var product = await CreateProductAsync(stock: 25);

        await _fixture.Carts.AddItemAsync(userId, new AddToCartRequest
        {
            ProductId = product.Id,
            Quantity = 10
        });

        // Reduce available stock below the cart quantity (e.g. a concurrent sale)
        // to trigger the order-level stock guard.
        await _fixture.Inventory.AdjustStockAsync(product.Id, new AdjustStockRequest { Delta = -20 });

        var order = await _fixture.Orders.PlaceOrderAsync(userId, new PlaceOrderRequest());

        order.Succeeded.Should().BeFalse();
        order.Message.Should().Contain("Insufficient stock");
    }

    private async Task<int> RegisterUserAsync()
    {
        var email = $"flow-{Guid.NewGuid():N}@test.com";
        var result = await _fixture.Auth.RegisterAsync(new RegisterRequest
        {
            FullName = "Flow User",
            Email = email,
            Password = "Passw0rd!123"
        });

        result.Succeeded.Should().BeTrue();
        return result.Data!.UserId;
    }

    private async Task<ProductDto> CreateProductAsync(int stock = 25)
    {
        var sku = $"FLOW-{Guid.NewGuid():N}";
        var result = await _fixture.Products.CreateAsync(new CreateProductRequest
        {
            Name = "Flow Product",
            Sku = sku,
            Price = 99.99m,
            CategoryId = 1,
            IsActive = true,
            InitialStock = stock
        });

        result.Succeeded.Should().BeTrue();
        return result.Data!;
    }
}
