using ResellerOrderApi.Core.Dtos;

namespace ResellerOrderApi.Core.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync();
    Task<UserCreateResult> CreateAsync(CreateUserRequest request);
    Task<bool> DeactivateAsync(Guid id);
}