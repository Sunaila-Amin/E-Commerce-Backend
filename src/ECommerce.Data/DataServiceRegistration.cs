using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Data.BackgroundJobs;
using ECommerce.Data.Cache;
using ECommerce.Data.Persistence;
using ECommerce.Data.RealTime;
using ECommerce.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Data;

public static class DataServiceRegistration
{
    public static IServiceCollection AddDataLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name)));

        var cacheProvider = configuration["Cache:Provider"] ?? "InMemory";
        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "ECommerce:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IBackgroundJobService, BackgroundJobService>();

        return services;
    }
}
