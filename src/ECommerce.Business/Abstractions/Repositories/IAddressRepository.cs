using ECommerce.Models.Entities;

namespace ECommerce.Business.Abstractions.Repositories;

public interface IAddressRepository : IRepository<Address>
{
    Task<IReadOnlyList<Address>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
}
