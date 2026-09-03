using ECommerce.Models.Common;

namespace ECommerce.Models.Entities;

public class Address : AuditableEntity
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string? City { get; set; }
    public string? State { get; set; }
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
