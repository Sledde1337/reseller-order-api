using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Entities;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var products = _db.Products.AsNoTracking().AsQueryable();

        if (!query.IncludeInactive)
            products = products.Where(p => p.IsActive);

        if (query.Category.HasValue)
            products = products.Where(p => p.Category == query.Category.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            products = products.Where(p => p.Name.ToLower().Contains(term) || p.Ean.Contains(term));
        }

        var totalCount = await products.CountAsync();

        var entities = await products
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProductDto>(
            entities.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == id);
        return product is null ? null : ToDto(product);
    }

    public async Task<bool> EanExistsAsync(string ean, Guid? excludeProductId = null)
    {
        return await _db.Products.AnyAsync(p =>
            p.Ean == ean && (excludeProductId == null || p.ProductId != excludeProductId));
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Ean = request.Ean,
            Name = request.Name.Trim(),
            Category = request.Category,
            NetPrice = request.NetPrice,
            StockQuantity = 0,
            IsActive = true
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return ToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (product is null) return null;

        product.Name = request.Name.Trim();
        product.Category = request.Category;
        product.NetPrice = request.NetPrice;

        await _db.SaveChangesAsync();
        return ToDto(product);
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == id);
        if (product is null) return false;

        product.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static ProductDto ToDto(Product p) =>
        new(p.ProductId, p.Ean, p.Name, p.Category, p.NetPrice, p.StockQuantity, p.IsActive);
}