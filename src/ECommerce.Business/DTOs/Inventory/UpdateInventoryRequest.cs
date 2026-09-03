namespace ECommerce.Business.DTOs.Inventory;

public class UpdateInventoryRequest
{
    public int Quantity { get; set; }
    public int? LowStockThreshold { get; set; }
}
