using ResellerOrderApi.Core.Dtos;

namespace ResellerOrderApi.Core.Services;

public interface IOrderService
{
    Task<PagedResult<OrderSummaryDto>> GetAllAsync(OrderQuery query);
    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<CreateOrderResult> CreateAsync(CreateOrderRequest request);
}