using ECommerce.Application.Auth;
using ECommerce.Application.Carts;
using ECommerce.Application.Carts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly CartService _cartService;
    private readonly IUserContext _userContext;

    public CartController(CartService cartService, IUserContext userContext)
    {
        _cartService = cartService;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCart(
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetCartAsync(_userContext.GetRequiredUserId(), cancellationToken);

        if (cart is null)
        {
            return NotFound();
        }

        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartResponse>> AddItem(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.AddItemAsync(
            _userContext.GetRequiredUserId(), request, cancellationToken);
        return Ok(cart);
    }

    [HttpPut("items/{productId:guid}")]
    public async Task<ActionResult<CartResponse>> UpdateItemQuantity(
        Guid productId,
        [FromBody] UpdateCartItemQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.UpdateItemQuantityAsync(
            _userContext.GetRequiredUserId(), productId, request.Quantity, cancellationToken);
        return Ok(cart);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<ActionResult<CartResponse>> RemoveItem(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.RemoveItemAsync(
            _userContext.GetRequiredUserId(), productId, cancellationToken);
        return Ok(cart);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(
        CancellationToken cancellationToken)
    {
        await _cartService.ClearAsync(_userContext.GetRequiredUserId(), cancellationToken);
        return NoContent();
    }
}
