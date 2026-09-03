namespace ECommerce.Business.Contracts;

public interface IBackgroundJobService
{
    void EnqueueOrderProcessing(int orderId);
    void ScheduleLowStockAlert(int productId, int available);
}
