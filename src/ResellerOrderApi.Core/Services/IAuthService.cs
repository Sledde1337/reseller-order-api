using ResellerOrderApi.Core.Dtos;

namespace ResellerOrderApi.Core.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}