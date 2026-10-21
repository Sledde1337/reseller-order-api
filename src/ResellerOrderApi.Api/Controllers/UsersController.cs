using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll()
    {
        return Ok(await _users.GetAllAsync());
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var result = await _users.CreateAsync(request);

        return result.Outcome switch
        {
            UserCreateOutcome.Created => StatusCode(StatusCodes.Status201Created, result.User),
            UserCreateOutcome.DuplicateEmail => Conflict(new ProblemDetails
            {
                Title = "Duplicate email",
                Detail = result.Error,
                Status = StatusCodes.Status409Conflict
            }),
            _ => BadRequest(new ProblemDetails
            {
                Title = "Invalid user",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            })
        };
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        if (User.FindFirst("sub")?.Value == id.ToString())
            return Conflict(new ProblemDetails
            {
                Title = "Cannot deactivate yourself",
                Detail = "Ask another admin to deactivate your account.",
                Status = StatusCodes.Status409Conflict
            });

        var deactivated = await _users.DeactivateAsync(id);
        return deactivated ? NoContent() : NotFound();
    }
}