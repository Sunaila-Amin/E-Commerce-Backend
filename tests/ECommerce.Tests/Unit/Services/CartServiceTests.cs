using AutoMapper;
using ECommerce.Business.Abstractions;
using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Business.DTOs.Cart;
using ECommerce.Business.Services.Carts;
using ECommerce.Models.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Unit.Services;

public class CartServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CartService _sut;

    public CartServiceTests()
    {
        _sut = new CartService(_uow.Object, _mapper.Object);
    }

    private Mock<ICartRepository> SetupCarts()
    {
        var carts = new Mock<ICartRepository>();
        _uow.SetupGet(u => u.Carts).Returns(carts.Object);
        return carts;
    }

    private Mock<IProductRepository> SetupProducts()
    {
        var products = new Mock<IProductRepository>();
        _uow.SetupGet(u => u.Products).Returns(products.Object);
        return products;
    }

    [Fact]
    public async Task GetCartAsync_WhenNoCart_ReturnsFailure()
    {
        var carts = SetupCarts();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var result = await _sut.GetCartAsync(1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Cart not found.");
    }

    [Fact]
    public async Task GetCartAsync_WhenCartExists_ReturnsCardDto()
    {
        var carts = SetupCarts();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewCart());
        _mapper.Setup(m => m.Map<CartDto>(It.IsAny<Cart>()))
            .Returns(new CartDto { Id = 1, UserId = 1, ItemCount = 2, Total = 40m });

        var result = await _sut.GetCartAsync(1);

        result.Succeeded.Should().BeTrue();
        result.Data!.UserId.Should().Be(1);
        result.Data.ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task AddItemAsync_WhenProductInactive_ReturnsFailure()
    {
        var carts = SetupCarts();
        var products = SetupProducts();

        products.Setup(p => p.GetByIdWithInventoryAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = 5, IsActive = false });

        var result = await _sut.AddItemAsync(1, new AddToCartRequest { ProductId = 5, Quantity = 1 });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Product not found or is inactive.");
    }

    [Fact]
    public async Task AddItemAsync_WhenValid_AddsToExistingCart()
    {
        var carts = SetupCarts();
        var products = SetupProducts();

        products.Setup(p => p.GetByIdWithInventoryAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product
            {
                Id = 6, Name = "Hat", Sku = "S-2", Price = 15m, IsActive = true,
                Inventory = new Inventory { Quantity = 10, Reserved = 0 }
            });

        var cart = NewCart();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.AddItemAsync(1, new AddToCartRequest { ProductId = 6, Quantity = 2 });

        result.Succeeded.Should().BeTrue();
        cart.Items.Should().Contain(i => i.ProductId == 6 && i.Quantity == 2);
    }

    [Fact]
    public async Task AddItemAsync_WhenItemAlreadyInCart_IncrementsQuantity()
    {
        var carts = SetupCarts();
        var products = SetupProducts();

        products.Setup(p => p.GetByIdWithInventoryAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product
            {
                Id = 5, Name = "Shirt", Sku = "S-1", Price = 20m, IsActive = true,
                Inventory = new Inventory { Quantity = 10, Reserved = 0 }
            });

        var cart = NewCart();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.AddItemAsync(1, new AddToCartRequest { ProductId = 5, Quantity = 1 });

        result.Succeeded.Should().BeTrue();
        cart.Items.Should().Contain(i => i.ProductId == 5 && i.Quantity == 3);
    }

    [Fact]
    public async Task AddItemAsync_WhenQuantityExceedsAvailableStock_ReturnsFailure()
    {
        var carts = SetupCarts();
        var products = SetupProducts();

        products.Setup(p => p.GetByIdWithInventoryAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product
            {
                Id = 7, Name = "Phone", Sku = "S-3", Price = 100m, IsActive = true,
                Inventory = new Inventory { Quantity = 5, Reserved = 0 }
            });

        var cart = NewCart();
        cart.Items.Clear();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var result = await _sut.AddItemAsync(1, new AddToCartRequest { ProductId = 7, Quantity = 6 });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("available in stock");
    }

    [Fact]
    public async Task UpdateItemAsync_WhenItemMissing_ReturnsFailure()
    {
        var carts = SetupCarts();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewCart());

        var result = await _sut.UpdateItemAsync(1, 999, new UpdateCartItemRequest { Quantity = 3 });

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Cart item not found.");
    }

    [Fact]
    public async Task UpdateItemAsync_WhenValid_UpdatesQuantity()
    {
        var carts = SetupCarts();
        var cart = NewCart();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateItemAsync(1, 10, new UpdateCartItemRequest { Quantity = 5 });

        result.Succeeded.Should().BeTrue();
        cart.Items.Single().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task RemoveItemAsync_WhenItemMissing_ReturnsFailure()
    {
        var carts = SetupCarts();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewCart());

        var result = await _sut.RemoveItemAsync(1, 999);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Cart item not found.");
    }

    [Fact]
    public async Task RemoveItemAsync_WhenValid_RemovesItem()
    {
        var carts = SetupCarts();
        var cart = NewCart();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.RemoveItemAsync(1, 10);

        result.Succeeded.Should().BeTrue();
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearCartAsync_WhenNoCart_ReturnsFailure()
    {
        var carts = SetupCarts();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart?)null);

        var result = await _sut.ClearCartAsync(1);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Cart not found.");
    }

    [Fact]
    public async Task ClearCartAsync_WhenValid_ClearsItems()
    {
        var carts = SetupCarts();
        var cart = NewCart();
        carts.Setup(c => c.GetActiveWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.ClearCartAsync(1);

        result.Succeeded.Should().BeTrue();
        cart.Items.Should().BeEmpty();
    }

    private static Cart NewCart()
    {
        var shirt = new Product { Id = 5, Name = "Shirt", Sku = "S-1", Price = 20m, IsActive = true };
        return new Cart
        {
            Id = 1,
            UserId = 1,
            IsActive = true,
            Items = new List<CartItem>
            {
                new() { Id = 10, CartId = 1, ProductId = 5, Quantity = 2, Product = shirt }
            }
        };
    }
}
