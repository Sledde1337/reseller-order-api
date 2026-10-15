using ResellerOrderApi.Core.Dtos;

namespace ResellerOrderApi.Core.Services;

public interface IStockService
{
    Task<ProductDto?> RestockAsync(Guid productId, int quantity);
    Task<PagedResult<StockMovementDto>?> GetHistoryAsync(Guid productId, StockHistoryQuery query);
}