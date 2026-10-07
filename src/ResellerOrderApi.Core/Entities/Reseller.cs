namespace ResellerOrderApi.Core.Entities;

public class Reseller
{
    public Guid ResellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    public List<Order> Orders { get; set; } = new();
}