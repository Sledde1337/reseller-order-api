using Microsoft.AspNetCore.Mvc;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Services;
using Microsoft.AspNetCore.Authorization;

namespace ResellerOrderApi.Api.Controllers;

[ApiController]
[Route("products/{productId:guid}")]
[Authorize(Roles = "Admin,SalesRep")]
public class StockController : ControllerBase
{
    private readonly IStockService _stock;

    public StockController(IStockService stock)
    {
        _stock = stock;
    }

    [HttpPost("restock")]
    public async Task<ActionResult<ProductDto>> Restock(Guid productId, RestockRequest request)
    {
        var product = await _stock.RestockAsync(productId, request.Quantity);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("stock-history")]
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetHistory(
        Guid productId, [FromQuery] StockHistoryQuery query)
    {
        var history = await _stock.GetHistoryAsync(productId, query);
        return history is null ? NotFound() : Ok(history);
    }
}