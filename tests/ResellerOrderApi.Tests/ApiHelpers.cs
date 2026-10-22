using System.Net.Http.Json;
using System.Text.Json;

namespace ResellerOrderApi.Tests;

public static class ApiHelpers
{
    public const string UserPassword = "Password123!";

    public static async Task<Guid> CreateProductAsync(HttpClient admin, int stock, decimal price = 10m)
    {
        var ean = Random.Shared.NextInt64(1_000_000_000_000, 9_999_999_999_999).ToString();

        var response = await admin.PostAsJsonAsync("/products",
            new { ean, name = $"Test {ean}", category = "Accessory", netPrice = price });
        response.EnsureSuccessStatusCode();

        var id = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("productId").GetGuid();

        if (stock > 0)
        {
            var restock = await admin.PostAsJsonAsync($"/products/{id}/restock", new { quantity = stock });
            restock.EnsureSuccessStatusCode();
        }

        return id;
    }

    public static async Task<Guid> CreateResellerAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync("/resellers", new
        {
            name = "Test Reseller",
            email = $"{Guid.NewGuid():N}@reseller.test",
            country = "Sweden"
        });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("resellerId").GetGuid();
    }

    public static async Task<Guid> CreateOrderAsync(
        HttpClient client, Guid resellerId, Guid productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/orders", new
        {
            resellerId,
            lines = new[] { new { productId, quantity } }
        });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("orderId").GetGuid();
    }

    public static async Task<HttpClient> CreateResellerUserClientAsync(
        ApiFixture fx, HttpClient admin, Guid resellerId)
    {
        var email = $"{Guid.NewGuid():N}@user.test";

        var response = await admin.PostAsJsonAsync("/users", new
        {
            email,
            password = UserPassword,
            role = "Reseller",
            resellerId
        });
        response.EnsureSuccessStatusCode();

        return await fx.LoginAsync(email, UserPassword);
    }

    public static async Task<int> GetStockAsync(HttpClient client, Guid productId)
    {
        var product = await client.GetFromJsonAsync<JsonElement>($"/products/{productId}");
        return product.GetProperty("stockQuantity").GetInt32();
    }

    public static async Task<string?> GetStatusAsync(HttpClient client, Guid orderId)
    {
        var order = await client.GetFromJsonAsync<JsonElement>($"/orders/{orderId}");
        return order.GetProperty("status").GetString();
    }
}