using ECommerce.Business.Contracts;
using ECommerce.Business.Services.Addresses;
using ECommerce.Business.Services.Auth;
using ECommerce.Business.Services.Carts;
using ECommerce.Business.Services.Categories;
using ECommerce.Business.Services.Inventory;
using ECommerce.Business.Services.Orders;
using ECommerce.Business.Services.Payments;
using ECommerce.Business.Services.Products;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Business;

public static class BusinessServiceRegistration
{
    public static IServiceCollection AddBusinessLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAutoMapper(config =>
        {
            config.AddMaps(typeof(BusinessServiceRegistration).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(BusinessServiceRegistration).Assembly);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAddressService, AddressService>();

        return services;
    }
}
