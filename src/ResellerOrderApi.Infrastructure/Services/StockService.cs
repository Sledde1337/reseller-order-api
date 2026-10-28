using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Entities;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly AppDbContext _db;

    public StockService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProductDto?> RestockAsync(Guid productId, int quantity)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product is null) return null;

        product.StockQuantity += quantity;

        _db.StockMovements.Add(new StockMovement
        {
            StockMovementId = Guid.NewGuid(),
            ProductId = productId,
            Change = quantity,
            Reason = StockMovementReason.Restock,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return new ProductDto(
            product.ProductId, product.Ean, product.Name, product.Category,
            product.NetPrice, product.StockQuantity, product.IsActive);
    }

    public async Task<PagedResult<StockMovementDto>?> GetHistoryAsync(
        Guid productId, StockHistoryQuery query)
    {
        var exists = await _db.Products.AnyAsync(p => p.ProductId == productId);
        if (!exists) return null;

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var movements = _db.StockMovements
            .AsNoTracking()
            .Where(m => m.ProductId == productId);

        var totalCount = await movements.CountAsync();

        var items = await movements
            .OrderByDescending(m => m.CreatedAt).ThenBy(m => m.StockMovementId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new StockMovementDto(
                m.StockMovementId, m.ProductId, m.Change, m.Reason, m.CreatedAt))
            .ToListAsync();

        return new PagedResult<StockMovementDto>(items, page, pageSize, totalCount);
    }
}