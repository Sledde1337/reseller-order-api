using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Core.Dtos;
using ResellerOrderApi.Core.Entities;
using ResellerOrderApi.Core.Services;

namespace ResellerOrderApi.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync()
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();
        return users.Select(ToDto).ToList();
    }

    public async Task<UserCreateResult> CreateAsync(CreateUserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return new(UserCreateOutcome.DuplicateEmail, null, $"A user with email {email} already exists.");

        if (request.Role == UserRole.Reseller)
        {
            if (request.ResellerId is null)
                return Invalid("A Reseller user must be linked to a reseller (resellerId).");

            var resellerExists = await _db.Resellers.AnyAsync(r => r.ResellerId == request.ResellerId);
            if (!resellerExists)
                return Invalid($"Reseller {request.ResellerId} does not exist.");
        }
        else if (request.ResellerId is not null)
        {
            return Invalid("Only users with the Reseller role can be linked to a reseller.");
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            ResellerId = request.Role == UserRole.Reseller ? request.ResellerId : null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new(UserCreateOutcome.Created, ToDto(user), null);
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null) return false;

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static UserCreateResult Invalid(string error) => new(UserCreateOutcome.Invalid, null, error);

    private static UserDto ToDto(User u) => new(u.UserId, u.Email, u.Role, u.IsActive, u.ResellerId);
}