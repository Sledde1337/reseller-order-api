using Microsoft.AspNetCore.Mvc;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Api.Controllers;

[ApiController]
[Route("resellers")]
public class ResellersController : ControllerBase
{
    private readonly IResellerService _resellers;

    public ResellersController(IResellerService resellers)
    {
        _resellers = resellers;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ResellerDto>>> GetAll([FromQuery] ResellerQuery query)
    {
        return Ok(await _resellers.GetAllAsync(query));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResellerDto>> GetById(Guid id)
    {
        var reseller = await _resellers.GetByIdAsync(id);
        return reseller is null ? NotFound() : Ok(reseller);
    }

    [HttpPost]
    public async Task<ActionResult<ResellerDto>> Create(CreateResellerRequest request)
    {
        if (await _resellers.EmailExistsAsync(request.Email))
            return DuplicateEmail(request.Email);

        var created = await _resellers.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.ResellerId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResellerDto>> Update(Guid id, UpdateResellerRequest request)
    {
        if (await _resellers.EmailExistsAsync(request.Email, excludeResellerId: id))
            return DuplicateEmail(request.Email);

        var updated = await _resellers.UpdateAsync(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _resellers.DeleteAsync(id);

        return result switch
        {
            DeleteResult.Deleted => NoContent(),
            DeleteResult.NotFound => NotFound(),
            DeleteResult.HasOrders => Conflict(new ProblemDetails
            {
                Title = "Reseller has orders",
                Detail = "A reseller with existing orders cannot be deleted.",
                Status = StatusCodes.Status409Conflict
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private ConflictObjectResult DuplicateEmail(string email) =>
        Conflict(new ProblemDetails
        {
            Title = "Duplicate email",
            Detail = $"A reseller with email {email} already exists.",
            Status = StatusCodes.Status409Conflict
        });
}