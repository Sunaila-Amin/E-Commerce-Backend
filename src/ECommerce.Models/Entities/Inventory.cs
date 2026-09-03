using ECommerce.Models.Common;

namespace ECommerce.Models.Entities;

public class Inventory : AuditableEntity
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int Reserved { get; set; }
    public int LowStockThreshold { get; set; } = 5;

    public Product Product { get; set; } = null!;

    public int Available => Quantity - Reserved;
}
