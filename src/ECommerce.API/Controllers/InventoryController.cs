using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Admin")]
public class InventoryController : ApiControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetAllAsync(cancellationToken);
        return FromServiceResult(result);
    }

    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductId(int productId, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetByProductIdAsync(productId, cancellationToken);

        return result.Succeeded
            ? Ok(new { succeeded = true, data = result.Data })
            : StatusCode(404, new { succeeded = false, message = result.Message });
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock(CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetLowStockAsync(cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPut("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int productId,
        [FromBody] UpdateInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.UpdateAsync(productId, request, cancellationToken);
        return FromServiceResult(result);
    }

    [HttpPatch("{productId:int}/stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdjustStock(
        int productId,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AdjustStockAsync(productId, request, cancellationToken);
        return FromServiceResult(result);
    }
}
