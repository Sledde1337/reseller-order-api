namespace ResellerOrderApi.Core.Entities;

public class User
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ResellerId { get; set; }
    public Reseller? Reseller { get; set; }
}