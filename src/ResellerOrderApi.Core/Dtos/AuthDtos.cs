using System.ComponentModel.DataAnnotations;
using ResellerOrderApi.Core.Entities;

namespace ResellerOrderApi.Core.Dtos;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string Email,
    UserRole Role,
    Guid? ResellerId);