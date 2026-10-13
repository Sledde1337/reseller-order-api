using Microsoft.AspNetCore.Mvc;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Api.Controllers;

[ApiController]
[Route("orders")]
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
        return Ok(await _orders.GetAllAsync(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
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
}