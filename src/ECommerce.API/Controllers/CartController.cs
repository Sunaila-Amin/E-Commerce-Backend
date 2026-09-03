using ECommerce.Business.Contracts;
using ECommerce.Business.DTOs.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ApiControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var result = await _cartService.GetCartAsync(CurrentUserId, cancellationToken);
        return FromServiceResult(result);
    }

    [Authorize]
    [HttpPost("items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddToCartRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cartService.AddItemAsync(CurrentUserId, request, cancellationToken);
        return FromServiceResult(result);
    }

    [Authorize]
    [HttpPut("items/{cartItemId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateItem(
        int cartItemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cartService.UpdateItemAsync(CurrentUserId, cartItemId, request, cancellationToken);
        return FromServiceResult(result);
    }

    [Authorize]
    [HttpDelete("items/{cartItemId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveItem(int cartItemId, CancellationToken cancellationToken)
    {
        var result = await _cartService.RemoveItemAsync(CurrentUserId, cartItemId, cancellationToken);
        return FromServiceResult(result);
    }

    [Authorize]
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var result = await _cartService.ClearCartAsync(CurrentUserId, cancellationToken);
        return FromServiceResult(result);
    }
}
