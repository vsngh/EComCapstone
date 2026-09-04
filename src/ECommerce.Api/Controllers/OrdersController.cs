using ECommerce.Application.Auth;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly IUserContext _userContext;

    public OrdersController(OrderService orderService, IUserContext userContext)
    {
        _orderService = orderService;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetMyOrders(
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetUserOrdersAsync(
            _userContext.GetRequiredUserId(), cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderAsync(
            _userContext.GetRequiredUserId(),
            orderId,
            _userContext.IsInRole("Admin"),
            cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost("{orderId:guid}/cancel")]
    public async Task<ActionResult<OrderResponse>> CancelOrder(
        Guid orderId,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CancelAsync(
            _userContext.GetRequiredUserId(),
            orderId,
            request.Reason,
            _userContext.IsInRole("Admin"),
            cancellationToken);

        return Ok(order);
    }
}