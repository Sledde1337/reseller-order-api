using ResellerOrderApi.Core.Dtos;

namespace ResellerOrderApi.Core.Services;

public interface IOrderService
{
    Task<PagedResult<OrderSummaryDto>> GetAllAsync(OrderQuery query);
    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<CreateOrderResult> CreateAsync(CreateOrderRequest request);
    Task<TransitionResult> ConfirmAsync(Guid id);
    Task<TransitionResult> ShipAsync(Guid id);
    Task<TransitionResult> DeliverAsync(Guid id);
    Task<TransitionResult> CancelAsync(Guid id);
}