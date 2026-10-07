namespace ResellerOrderApi.Core.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    public Guid ResellerId { get; set; }
    public Reseller? Reseller { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }

    public List<OrderLine> Lines { get; set; } = new();
}