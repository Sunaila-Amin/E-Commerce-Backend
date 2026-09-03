using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.ProcessPaymentAsync(request, cancellationToken);
        return FromServiceResult(result);
    }

    [Authorize]
    [HttpGet("{paymentReference}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByReference(string paymentReference, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetByReferenceAsync(paymentReference, cancellationToken);

        return result.Succeeded
            ? Ok(new { succeeded = true, data = result.Data })
            : StatusCode(404, new { succeeded = false, message = result.Message });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{paymentReference}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refund(string paymentReference, CancellationToken cancellationToken)
    {
        var result = await _paymentService.RefundAsync(paymentReference, cancellationToken);
        return FromServiceResult(result);
    }
}
