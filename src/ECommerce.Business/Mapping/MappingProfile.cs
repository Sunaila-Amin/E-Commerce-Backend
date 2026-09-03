using ECommerce.Business.DTOs.Address;
using ECommerce.Business.DTOs.Cart;
using ECommerce.Business.DTOs.Category;
using ECommerce.Business.DTOs.Inventory;
using ECommerce.Business.DTOs.Order;
using ECommerce.Business.DTOs.Payment;
using ECommerce.Business.DTOs.Product;
using ECommerce.Models.Entities;
using ECommerce.Models.Enums;
using AutoMapper;

namespace ECommerce.Business.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null))
            .ForMember(d => d.AvailableStock, o => o.MapFrom(s => s.Inventory != null ? s.Inventory.Available : (int?)null));

        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.Children, o => o.MapFrom(s => s.Children.Where(c => c.IsActive)));

        CreateMap<Inventory, InventoryDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.IsLowStock, o => o.MapFrom(s => s.Available <= s.LowStockThreshold));

        CreateMap<CartItem, CartItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.Product.Price))
            .ForMember(d => d.LineTotal, o => o.MapFrom(s => s.Product.Price * s.Quantity));

        CreateMap<Cart, CartDto>()
            .ForMember(d => d.ItemCount, o => o.MapFrom(s => s.Items.Sum(i => i.Quantity)))
            .ForMember(d => d.Total, o => o.MapFrom(s => s.Items.Sum(i => i.Product.Price * i.Quantity)));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.LineTotal, o => o.MapFrom(s => s.UnitPrice * s.Quantity));

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.StatusName, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.PlacedAt, o => o.MapFrom(s => s.PlacedAt ?? s.CreatedAt));

        CreateMap<Payment, PaymentDto>();

        CreateMap<Address, AddressDto>();
    }
}
