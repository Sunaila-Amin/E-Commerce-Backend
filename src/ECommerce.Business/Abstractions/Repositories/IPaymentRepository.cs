using ECommerce.Models.Entities;

namespace ECommerce.Business.Abstractions.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByReferenceAsync(string paymentReference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
}
