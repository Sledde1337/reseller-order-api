using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Entities;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<OrderSummaryDto>> GetAllAsync(OrderQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var orders = _db.Orders.AsNoTracking().AsQueryable();

        if (query.ResellerId.HasValue)
            orders = orders.Where(o => o.ResellerId == query.ResellerId.Value);

        if (query.Status.HasValue)
            orders = orders.Where(o => o.Status == query.Status.Value);

        var totalCount = await orders.CountAsync();

        var items = await orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.OrderId,
                o.ResellerId,
                o.Reseller!.Name,
                o.Status,
                o.CreatedAt,
                o.TotalAmount,
                o.Lines.Count))
            .ToListAsync();

        return new PagedResult<OrderSummaryDto>(items, page, pageSize, totalCount);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Reseller)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        return order is null ? null : ToDto(order);
    }

    public async Task<CreateOrderResult> CreateAsync(CreateOrderRequest request)
    {
        if (request.Lines.Count == 0)
            return Fail("An order must have at least one line.");

        var resellerExists = await _db.Resellers.AnyAsync(r => r.ResellerId == request.ResellerId);
        if (!resellerExists)
            return Fail($"Reseller {request.ResellerId} does not exist.");

        var requested = request.Lines
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToList();

        var productIds = requested.Select(r => r.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            ResellerId = request.ResellerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in requested)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return Fail($"Product {item.ProductId} does not exist.");

            if (!product.IsActive)
                return Fail($"Product '{product.Name}' is no longer available.");

            order.Lines.Add(new OrderLine
            {
                OrderLineId = Guid.NewGuid(),
                OrderId = order.OrderId,
                ProductId = product.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.NetPrice
            });
        }

        order.TotalAmount = Math.Round(
            order.Lines.Sum(l => l.Quantity * l.UnitPrice), 2);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var created = await GetByIdAsync(order.OrderId);
        return new CreateOrderResult(created, null);
    }

    private static CreateOrderResult Fail(string error) => new(null, error);

    private static OrderDto ToDto(Order o) => new(
        o.OrderId,
        o.ResellerId,
        o.Reseller?.Name ?? string.Empty,
        o.Status,
        o.CreatedAt,
        o.TotalAmount,
        o.Lines
            .OrderBy(l => l.Product?.Name)
            .Select(l => new OrderLineDto(
                l.OrderLineId,
                l.ProductId,
                l.Product?.Name ?? string.Empty,
                l.Product?.Ean ?? string.Empty,
                l.Quantity,
                l.UnitPrice,
                Math.Round(l.Quantity * l.UnitPrice, 2)))
            .ToList());
}