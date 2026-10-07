namespace ResellerOrderApi.Core.Entities;

public class Product
{
    public Guid ProductId { get; set; }
    public string Ean { get; set; } = string.Empty;
    public string Name { get; set;} = string.Empty;
    public ProductCategory Category { get; set; }
    public decimal NetPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public uint RowVersion { get; set; } 
}