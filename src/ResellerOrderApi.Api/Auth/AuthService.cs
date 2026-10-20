using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Services;
using ResellerOrderApi.Infrastructure;

namespace ResellerOrderApi.Api.Auth;

public class AuthService : IAuthService
{
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

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var (token, expiresAt) = _tokens.CreateToken(user);
        return new LoginResponse(token, expiresAt, user.Email, user.Role, user.ResellerId);
    }
}