using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Address;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/addresses")]
public class AddressesController : ApiControllerBase
{
    private readonly IAddressService _addressService;

    public AddressesController(IAddressService addressService)
    {
        _addressService = addressService;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAddresses(CancellationToken cancellationToken)
    {
        var result = await _addressService.GetByUserAsync(CurrentUserId, cancellationToken);
        return FromServiceResult(result);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addressService.CreateAsync(CurrentUserId, request, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetMyAddresses), new { succeeded = true, data = result.Data })
            : FromServiceResult(result);
    }

    [Authorize]
    [HttpPut("{addressId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int addressId,
        [FromBody] UpdateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addressService.UpdateAsync(CurrentUserId, addressId, request, cancellationToken);
        return FromServiceResult(result);
    }

    [Authorize]
    [HttpDelete("{addressId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int addressId, CancellationToken cancellationToken)
    {
        var result = await _addressService.DeleteAsync(CurrentUserId, addressId, cancellationToken);
        return FromServiceResult(result);
    }
}
