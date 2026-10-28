using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResellerOrderApi.Core.Entities;
using ResellerOrderApi.Infrastructure;
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace ResellerOrderApi.Tests;

public class ApiFixture : IDisposable
{
    public const string AdminEmail = "admin@test.com";
    public const string AdminPassword = "Admin123!";
    public const string SalesEmail = "sales@test.com";
    public const string SalesPassword = "Sales123!";

    private readonly PostgreSqlContainer _postgres;

    public WebApplicationFactory<Program> Factory { get; }

    public ApiFixture()
    {
        _postgres = new PostgreSqlBuilder().WithImage("postgres:16").Build();
        _postgres.StartAsync().GetAwaiter().GetResult();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
            builder.UseSetting("Jwt:Key", "test-signing-key-at-least-32-characters-long-0123456789");
        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        db.Users.AddRange(
            new User
            {
                UserId = Guid.NewGuid(),
                Email = AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
                Role = UserRole.Admin
            },
            new User
            {
                UserId = Guid.NewGuid(),
                Email = SalesEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(SalesPassword),
                Role = UserRole.SalesRep
            });
        db.SaveChanges();
    }

    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return client;
    }

    public Task<HttpClient> AdminAsync() => LoginAsync(AdminEmail, AdminPassword);

    public void Dispose()
    {
        Factory.Dispose();
        _postgres.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture> { }