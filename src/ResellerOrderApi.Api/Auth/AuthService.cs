using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Services;
using ResellerOrderApi.Infrastructure;

namespace ResellerOrderApi.Api.Auth;

public class AuthService : IAuthService
{
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("timing-equalizer-not-a-real-password");

    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokens;

    public AuthService(AppDbContext db, JwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyHash);

        if (user is null || !passwordOk)
            return null;

        var (token, expiresAt) = _tokens.CreateToken(user);
        return new LoginResponse(token, expiresAt, user.Email, user.Role, user.ResellerId);
    }
}