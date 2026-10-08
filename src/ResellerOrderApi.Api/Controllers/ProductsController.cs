using Microsoft.AspNetCore.Mvc;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Api.Controllers;

[ApiController]
[Route("products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll([FromQuery] ProductQuery query)
    {
        return Ok(await _products.GetAllAsync(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _products.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request)
    {
        if (await _products.EanExistsAsync(request.Ean))
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate EAN",
                Detail = $"A product with EAN {request.Ean} already exists.",
                Status = StatusCodes.Status409Conflict
            });

        var created = await _products.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductRequest request)
    {
        var updated = await _products.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deactivated = await _products.DeactivateAsync(id);
        return deactivated ? NoContent() : NotFound();
    }
}