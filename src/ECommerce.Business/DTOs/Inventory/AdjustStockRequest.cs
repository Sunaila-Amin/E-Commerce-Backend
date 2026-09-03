namespace ECommerce.Business.DTOs.Inventory;

public class AdjustStockRequest
{
    public int Delta { get; set; }
    public string? Reason { get; set; }
}
