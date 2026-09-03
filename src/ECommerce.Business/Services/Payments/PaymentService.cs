using ECommerce.Business.Abstractions;
using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Payment;
using ECommerce.Models.Entities;
using ECommerce.Models.Enums;
using AutoMapper;

namespace ECommerce.Business.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public PaymentService(
        IUnitOfWork uow,
        IMapper mapper,
        INotificationService notificationService)
    {
        _uow = uow;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<PaymentDto>> GetByReferenceAsync(
        string paymentReference,
        CancellationToken cancellationToken = default)
    {
        var payment = await _uow.Payments.GetByReferenceAsync(paymentReference, cancellationToken);

        if (payment is null)
        {
            return ServiceResult<PaymentDto>.Failure("Payment not found.");
        }

        var dto = _mapper.Map<PaymentDto>(payment);
        return ServiceResult<PaymentDto>.Success(dto);
    }

    public async Task<ServiceResult<PaymentDto>> ProcessPaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _uow.Orders.GetByIdWithDetailsAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return ServiceResult<PaymentDto>.Failure("Order not found.");
        }

        if (order.Status == OrderStatus.Paid)
        {
            return ServiceResult<PaymentDto>.Failure("Order is already paid.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return ServiceResult<PaymentDto>.Failure("Cannot pay for a cancelled order.");
        }

        var provider = ResolveProvider(request.Method);

        var payment = new Payment
        {
            PaymentReference = Guid.NewGuid().ToString("N"),
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = request.Method,
            Provider = provider,
            Status = PaymentStatus.Pending,
            CreatedBy = order.UserId.ToString()
        };

        // Simulate payment provider processing.
        var processResult = await SimulatePaymentAsync(payment, cancellationToken);

        payment.Status = processResult.Status;
        payment.TransactionId = processResult.TransactionId;
        payment.PaidAt = processResult.Status == PaymentStatus.Succeeded ? DateTime.UtcNow : null;
        payment.UpdatedBy = order.UserId.ToString();

        if (processResult.Status == PaymentStatus.Succeeded)
        {
            order.Status = OrderStatus.Paid;
            order.UpdatedBy = order.UserId.ToString();
        }

        await _uow.Payments.AddAsync(payment, cancellationToken);
        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyOrderStatusChangedAsync(
            order.UserId,
            order.Id,
            order.OrderNumber,
            order.Status,
            cancellationToken);

        var dto = _mapper.Map<PaymentDto>(payment);
        var message = processResult.Status == PaymentStatus.Succeeded
            ? "Payment processed successfully."
            : "Payment failed.";

        return ServiceResult<PaymentDto>.Success(dto, message);
    }

    public async Task<ServiceResult<PaymentDto>> RefundAsync(
        string paymentReference,
        CancellationToken cancellationToken = default)
    {
        var payment = await _uow.Payments.GetByReferenceAsync(paymentReference, cancellationToken);

        if (payment is null)
        {
            return ServiceResult<PaymentDto>.Failure("Payment not found.");
        }

        if (payment.Status != PaymentStatus.Succeeded)
        {
            return ServiceResult<PaymentDto>.Failure("Only successful payments can be refunded.");
        }

        payment.Status = PaymentStatus.Refunded;
        payment.UpdatedBy = "Admin";

        var order = await _uow.Orders.GetByIdWithDetailsAsync(payment.OrderId, cancellationToken);
        if (order is not null && order.Status != OrderStatus.Cancelled)
        {
            order.Status = OrderStatus.Refunded;
            order.UpdatedBy = "Admin";
            _uow.Orders.Update(order);
        }

        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync(cancellationToken);

        if (order is not null)
        {
            await _notificationService.NotifyOrderStatusChangedAsync(
                order.UserId,
                order.Id,
                order.OrderNumber,
                order.Status,
                cancellationToken);
        }

        var dto = _mapper.Map<PaymentDto>(payment);
        return ServiceResult<PaymentDto>.Success(dto, "Payment refunded.");
    }

    private static PaymentProvider ResolveProvider(PaymentMethod method) =>
        method switch
        {
            PaymentMethod.PayPal => PaymentProvider.PayPal,
            _ => PaymentProvider.Mock
        };

    private static Task<PaymentProcessResult> SimulatePaymentAsync(
        Payment payment,
        CancellationToken cancellationToken)
    {
        // Deterministic simulation: succeed for all except an explicit failure marker.
        // For a real provider this would call an external gateway.
        var status = PaymentStatus.Succeeded;

        return Task.FromResult(new PaymentProcessResult
        {
            Succeeded = status == PaymentStatus.Succeeded,
            Status = status,
            PaymentReference = payment.PaymentReference,
            TransactionId = status == PaymentStatus.Succeeded ? $"TXN-{Guid.NewGuid():N}" : null
        });
    }
}
