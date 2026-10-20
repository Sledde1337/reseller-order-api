using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Entities;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Infrastructure.Services;

public class ResellerService : IResellerService
{
    private readonly AppDbContext _db;

    public ResellerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ResellerDto>> GetAllAsync(ResellerQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var resellers = _db.Resellers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim().ToLower();
            resellers = resellers.Where(r => r.Country.ToLower() == country);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            resellers = resellers.Where(r =>
                r.Name.ToLower().Contains(term) || r.Email.ToLower().Contains(term));
        }

        var totalCount = await resellers.CountAsync();

        var entities = await resellers
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ResellerDto>(
            entities.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<ResellerDto?> GetByIdAsync(Guid id)
    {
        var reseller = await _db.Resellers.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResellerId == id);
        return reseller is null ? null : ToDto(reseller);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeResellerId = null)
    {
        var normalized = Normalize(email);
        return await _db.Resellers.AnyAsync(r =>
            r.Email == normalized &&
            (excludeResellerId == null || r.ResellerId != excludeResellerId));
    }

    public async Task<ResellerDto> CreateAsync(CreateResellerRequest request)
    {
        var reseller = new Reseller
        {
            ResellerId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = Normalize(request.Email),
            Country = request.Country.Trim()
        };

        _db.Resellers.Add(reseller);
        await _db.SaveChangesAsync();
        return ToDto(reseller);
    }

    public async Task<ResellerDto?> UpdateAsync(Guid id, UpdateResellerRequest request)
    {
        var reseller = await _db.Resellers.FirstOrDefaultAsync(r => r.ResellerId == id);
        if (reseller is null) return null;

        reseller.Name = request.Name.Trim();
        reseller.Email = Normalize(request.Email);
        reseller.Country = request.Country.Trim();

        await _db.SaveChangesAsync();
        return ToDto(reseller);
    }

    public async Task<DeleteResult> DeleteAsync(Guid id)
    {
        var reseller = await _db.Resellers.FirstOrDefaultAsync(r => r.ResellerId == id);
        if (reseller is null) return DeleteResult.NotFound;

        if (await _db.Orders.AnyAsync(o => o.ResellerId == id) ||
            await _db.Users.AnyAsync(u => u.ResellerId == id))
            return DeleteResult.HasOrders;

        _db.Resellers.Remove(reseller);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static ResellerDto ToDto(Reseller r) =>
        new(r.ResellerId, r.Name, r.Email, r.Country);
}