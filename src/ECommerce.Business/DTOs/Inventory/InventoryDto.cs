namespace ECommerce.Business.DTOs.Inventory;

public class InventoryDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public int Reserved { get; set; }
    public int Available { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLowStock { get; set; }
}
