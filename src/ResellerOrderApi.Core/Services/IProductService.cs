using ResellerOrderApi.Core.Dtos;

namespace ResellerOrderApi.Core.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(ProductQuery query);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<bool> EanExistsAsync(string ean, Guid? excludeProductId = null);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request);
    Task<bool> DeactivateAsync(Guid id);
}