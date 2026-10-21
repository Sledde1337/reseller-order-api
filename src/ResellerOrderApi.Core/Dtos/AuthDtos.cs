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

public record UserDto(
    Guid UserId,
    string Email,
    UserRole Role,
    bool IsActive,
    Guid? ResellerId);

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(72, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }

    public Guid? ResellerId { get; set; }
}

public enum UserCreateOutcome
{
    Created,
    DuplicateEmail,
    Invalid
}

public record UserCreateResult(UserCreateOutcome Outcome, UserDto? User, string? Error);