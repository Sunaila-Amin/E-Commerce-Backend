using ECommerce.Models.Common;
using ECommerce.Models.Enums;

namespace ECommerce.Models.Entities;

public class Role : BaseEntity
{
    public RoleName Name { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
