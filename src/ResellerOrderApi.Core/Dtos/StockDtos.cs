using System.ComponentModel.DataAnnotations;
using ResellerOrderApi.Core.Entities;

namespace ResellerOrderApi.Core.Dtos;

public class RestockRequest
{
    [Range(1, 100000)]
    public int Quantity { get; set; }
}

public record StockMovementDto(
    Guid StockMovementId,
    Guid ProductId,
    int Change,
    StockMovementReason Reason,
    DateTime CreatedAt);

public class StockHistoryQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}