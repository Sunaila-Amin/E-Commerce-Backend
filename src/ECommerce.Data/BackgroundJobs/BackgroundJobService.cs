using ECommerce.Business.Contracts;
using Hangfire;

namespace ECommerce.Data.BackgroundJobs;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _client;

    public BackgroundJobService(IBackgroundJobClient client)
    {
        _client = client;
    }

    public void EnqueueOrderProcessing(int orderId)
    {
        _client.Enqueue<OrderProcessingJob>(job => job.ProcessAsync(orderId, CancellationToken.None));
    }

    public void ScheduleLowStockAlert(int productId, int available)
    {
        _client.Schedule<LowStockAlertJob>(job => job.ExecuteAsync(productId, available, CancellationToken.None), TimeSpan.FromSeconds(5));
    }
}
