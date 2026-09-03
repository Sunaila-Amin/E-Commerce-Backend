using ECommerce.Models.Entities;
using ECommerce.Models.Enums;

namespace ECommerce.Business.Abstractions.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default);
}
