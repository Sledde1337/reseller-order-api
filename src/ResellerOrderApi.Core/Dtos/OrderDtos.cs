using System.ComponentModel.DataAnnotations;
using ResellerOrderApi.Core.Entities;

namespace ResellerOrderApi.Core.Dtos;

public record OrderLineDto(
    Guid OrderLineId,
    Guid ProductId,
    string ProductName,
    string Ean,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record OrderDto(
    Guid OrderId,
    Guid ResellerId,
    string ResellerName,
    OrderStatus Status,
    DateTime CreatedAt,
    decimal TotalAmount,
    IReadOnlyList<OrderLineDto> Lines);

public record OrderSummaryDto(
    Guid OrderId,
    Guid ResellerId,
    string ResellerName,
    OrderStatus Status,
    DateTime CreatedAt,
    decimal TotalAmount,
    int LineCount);

public class CreateOrderLineRequest
{
    public Guid ProductId { get; set; }

    [Range(1, 10000)]
    public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    public Guid ResellerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "An order must have at least one line.")]
    public List<CreateOrderLineRequest> Lines { get; set; } = new();
}

public class OrderQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? ResellerId { get; set; }
    public OrderStatus? Status { get; set; }
}

public record CreateOrderResult(OrderDto? Order, string? Error);