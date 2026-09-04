using ECommerce.Application.Auth;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly CheckoutService _checkoutService;
    private readonly IUserContext _userContext;

    public CheckoutController(CheckoutService checkoutService, IUserContext userContext)
    {
        _checkoutService = checkoutService;
        _userContext = userContext;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _checkoutService.CheckoutAsync(
            _userContext.GetRequiredUserId(), request, cancellationToken);
        return Ok(order);
    }
}