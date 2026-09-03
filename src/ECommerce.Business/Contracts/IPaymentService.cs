using ECommerce.Business.DTOs.Payment;

namespace ECommerce.Business.Contracts;

public interface IPaymentService
{
    Task<ServiceResult<PaymentDto>> GetByReferenceAsync(string paymentReference, CancellationToken cancellationToken = default);
    Task<ServiceResult<PaymentDto>> ProcessPaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<PaymentDto>> RefundAsync(
        string paymentReference,
        CancellationToken cancellationToken = default);
}
