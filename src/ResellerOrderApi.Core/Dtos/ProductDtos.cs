using System.ComponentModel.DataAnnotations;
using ResellerOrderApi.Core.Entities;

namespace ResellerOrderApi.Core.Dtos;

public record ProductDto(
    Guid ProductId,
    string Ean,
    string Name,
    ProductCategory Category,
    decimal NetPrice,
    int StockQuantity,
    bool IsActive);

public class CreateProductRequest
{
    [Required]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "EAN must be exactly 13 digits.")]
    public string Ean { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(ProductCategory))]
    public ProductCategory Category { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal NetPrice { get; set; }
}

public class UpdateProductRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(ProductCategory))]
    public ProductCategory Category { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal NetPrice { get; set; }
}

public class ProductQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public ProductCategory? Category { get; set; }
    public string? Search { get; set; }
    public bool IncludeInactive { get; set; } = false;
}