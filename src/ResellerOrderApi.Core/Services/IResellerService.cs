using ResellerOrderApi.Core.Dtos;

namespace ResellerOrderApi.Core.Services;

public enum DeleteResult
{
    Deleted,
    NotFound,
    HasOrders
}

public interface IResellerService
{
    Task<PagedResult<ResellerDto>> GetAllAsync(ResellerQuery query);
    Task<ResellerDto?> GetByIdAsync(Guid id);
    Task<bool> EmailExistsAsync(string email, Guid? excludeResellerId = null);
    Task<ResellerDto> CreateAsync(CreateResellerRequest request);
    Task<ResellerDto?> UpdateAsync(Guid id, UpdateResellerRequest request);
    Task<DeleteResult> DeleteAsync(Guid id);
}