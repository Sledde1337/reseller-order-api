using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ResellerOrderApi.Tests;

[Collection("api")]
public class ValidationTests
{
    private readonly ApiFixture _fx;

    public ValidationTests(ApiFixture fx)
    {
        _fx = fx;
    }

    private static string NewEan() =>
        Random.Shared.NextInt64(1_000_000_000_000, 9_999_999_999_999).ToString();

    [Fact]
    public async Task Duplicate_ean_is_rejected_with_409()
    {
        var admin = await _fx.AdminAsync();
        var body = new { ean = NewEan(), name = "First", category = "Accessory", netPrice = 5 };

        var first = await admin.PostAsJsonAsync("/products", body);
        var second = await admin.PostAsJsonAsync("/products", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("123", "Accessory", 5)]
    [InlineData("1234567890123", "Nonsense", 5)]
    [InlineData("1234567890123", "Accessory", 0)]
    public async Task Invalid_product_requests_are_rejected_with_400(string ean, string category, decimal price)
    {
        var admin = await _fx.AdminAsync();

        var response = await admin.PostAsJsonAsync("/products",
            new { ean, name = "Invalid", category, netPrice = price });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reseller_with_orders_cannot_be_deleted()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 5);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 1);

        var response = await admin.DeleteAsync($"/resellers/{reseller}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Reseller_without_orders_can_be_deleted()
    {
        var admin = await _fx.AdminAsync();
        var reseller = await ApiHelpers.CreateResellerAsync(admin);

        var delete = await admin.DeleteAsync($"/resellers/{reseller}");
        var get = await admin.GetAsync($"/resellers/{reseller}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Duplicate_order_lines_for_one_product_are_merged()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);

        var response = await admin.PostAsJsonAsync("/orders", new
        {
            resellerId = reseller,
            lines = new[]
            {
                new { productId = product, quantity = 2 },
                new { productId = product, quantity = 3 }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        var lines = order.GetProperty("lines");
        Assert.Equal(1, lines.GetArrayLength());
        Assert.Equal(5, lines[0].GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Simultaneous_duplicate_reseller_emails_give_one_201_and_one_409()
    {
        var admin = await _fx.AdminAsync();
        var body = new { name = "Race Reseller", email = $"{Guid.NewGuid():N}@race.test", country = "Sweden" };

        var responses = await Task.WhenAll(
            admin.PostAsJsonAsync("/resellers", body),
            admin.PostAsJsonAsync("/resellers", body));

        var codes = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();
        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }, codes);
    }

    [Fact]
    public async Task Paging_limits_the_number_of_items()
    {
        var admin = await _fx.AdminAsync();
        await ApiHelpers.CreateResellerAsync(admin);
        await ApiHelpers.CreateResellerAsync(admin);

        var page = await admin.GetFromJsonAsync<JsonElement>("/resellers?page=1&pageSize=1");

        Assert.Equal(1, page.GetProperty("items").GetArrayLength());
        Assert.True(page.GetProperty("totalCount").GetInt32() >= 2);
    }
}