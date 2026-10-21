using Microsoft.AspNetCore.Mvc;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Services;
using Microsoft.AspNetCore.Authorization;

namespace ResellerOrderApi.Api.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetAll([FromQuery] OrderQuery query)
    {
        if (IsReseller)
        {
            if (CurrentResellerId is null) return Forbid();
            query.ResellerId = CurrentResellerId;
        }

        return Ok(await _orders.GetAllAsync(query));

    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order is null) return NotFound();

        if (IsReseller && order.ResellerId != CurrentResellerId) return NotFound();

        return Ok(order);

    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
        if (IsReseller)
        {
            if (CurrentResellerId is null) return Forbid();
            request.ResellerId = CurrentResellerId.Value;
        }

        var result = await _orders.CreateAsync(request);

        if (result.Order is null)
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid order",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });

        return CreatedAtAction(nameof(GetById), new { id = result.Order.OrderId }, result.Order);
    }

    [Authorize(Roles = "Admin,SalesRep")]
    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<OrderDto>> Confirm(Guid id) =>
        ToActionResult(await _orders.ConfirmAsync(id));

    [Authorize(Roles = "Admin,SalesRep")]
    [HttpPost("{id:guid}/ship")]
    public async Task<ActionResult<OrderDto>> Ship(Guid id) =>
        ToActionResult(await _orders.ShipAsync(id));

    [Authorize(Roles = "Admin,SalesRep")]
    [HttpPost("{id:guid}/deliver")]
    public async Task<ActionResult<OrderDto>> Deliver(Guid id) =>
        ToActionResult(await _orders.DeliverAsync(id));

    [Authorize(Roles = "Admin,SalesRep")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id) =>
        ToActionResult(await _orders.CancelAsync(id));

    private bool IsReseller => User.IsInRole("Reseller");

    private Guid? CurrentResellerId =>
        Guid.TryParse(User.FindFirst("resellerId")?.Value, out var id) ? id : null;

    private ActionResult<OrderDto> ToActionResult(TransitionResult result)
    {
        switch (result.Outcome)
        {
            case TransitionOutcome.Success:
                return Ok(result.Order);

            case TransitionOutcome.NotFound:
                return NotFound();

            case TransitionOutcome.InsufficientStock:
                return Conflict(new ProblemDetails
                {
                    Title = "Insufficient stock",
                    Detail = result.Error,
                    Status = StatusCodes.Status409Conflict
                });

            default:
                return Conflict(new ProblemDetails
                {
                    Title = "Invalid status change",
                    Detail = result.Error,
                    Status = StatusCodes.Status409Conflict
                });
        }
    }
}