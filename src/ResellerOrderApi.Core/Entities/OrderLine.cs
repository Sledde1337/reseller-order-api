namespace ResellerOrderApi.Core.Entities;

public class OrderLine
{
    public Guid OrderLineId { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
}