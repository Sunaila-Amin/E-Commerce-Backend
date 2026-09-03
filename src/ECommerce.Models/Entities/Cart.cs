using ECommerce.Models.Common;

namespace ECommerce.Models.Entities;

public class Cart : BaseEntity
{
    public int UserId { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
