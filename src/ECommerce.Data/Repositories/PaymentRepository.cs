using ECommerce.Business.Abstractions.Repositories;
using ECommerce.Data.Persistence;
using ECommerce.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<Payment?> GetByReferenceAsync(string paymentReference, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentReference == paymentReference, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }
}
