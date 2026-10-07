namespace ResellerOrderApi.Core.Entities;

public class StockMovement
{
    public Guid StockMovementId { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Change { get; set; }
    public StockMovementReason Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}