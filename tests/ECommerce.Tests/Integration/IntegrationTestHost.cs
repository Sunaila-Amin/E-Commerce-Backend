using ECommerce.Business;
using ECommerce.Business.Contracts;
using ECommerce.Data;
using ECommerce.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Builds the real application DI graph (Data + Business layers) and swaps only the
/// external-infrastructure pieces for test-friendly alternatives:
///   - SQL Server DbContext        -> EF Core InMemory
///   - Redis IDistributedCache      -> In-memory distributed cache
///   - Hangfire background jobs     -> no-op fake
///   - SignalR notifications        -> no-op fake
/// </summary>
public static class IntegrationTestHost
{
    public static ServiceProvider Build(string? databaseName = null)
    {
        var configuration = BuildConfiguration();
        var dbName = databaseName ?? $"ECommerceTests-{Guid.NewGuid():N}";

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDataLayer(configuration);
        services.AddBusinessLayer(configuration);

        OverrideDbContext(services, dbName);
        OverrideDistributedCache(services);
        OverrideBackgroundJobService(services);
        OverrideNotificationService(services);

        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=ECommerceTests;Integrated Security=True;",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Jwt:Key"] = "IntegrationTestSigningKey_AtLeastThirtyTwoCharactersLong_123456",
                ["Jwt:Issuer"] = "ECommerceTests",
                ["Jwt:Audience"] = "ECommerceTestsClient",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();

    private static void OverrideDbContext(IServiceCollection services, string dbName)
    {
        services.RemoveAll<ApplicationDbContext>();
        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
    }

    private static void OverrideDistributedCache(IServiceCollection services)
    {
        services.RemoveAll<IDistributedCache>();
        services.AddDistributedMemoryCache();
    }

    private static void OverrideBackgroundJobService(IServiceCollection services)
    {
        services.RemoveAll<IBackgroundJobService>();
        services.AddScoped<IBackgroundJobService>(_ => Mock.Of<IBackgroundJobService>());
    }

    private static void OverrideNotificationService(IServiceCollection services)
    {
        services.RemoveAll<INotificationService>();
        services.AddScoped<INotificationService>(_ => Mock.Of<INotificationService>());
    }
}
