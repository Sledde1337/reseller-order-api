using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ResellerOrderApi.Tests;

[Collection("api")]
public class OrderLifecycleTests
{
    private readonly ApiFixture _fx;

    public OrderLifecycleTests(ApiFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public async Task New_order_is_pending_with_calculated_total()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10, price: 12.50m);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);

        var orderId = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 3);

        var order = await admin.GetFromJsonAsync<JsonElement>($"/orders/{orderId}");
        Assert.Equal("Pending", order.GetProperty("status").GetString());
        Assert.Equal(37.50m, order.GetProperty("totalAmount").GetDecimal());
    }

    [Fact]
    public async Task Order_keeps_the_price_from_when_it_was_created()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10, price: 12.50m);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var orderId = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 2);

        var update = await admin.PutAsJsonAsync($"/products/{product}",
            new { name = "Renamed", category = "Accessory", netPrice = 99m });
        update.EnsureSuccessStatusCode();

        var order = await admin.GetFromJsonAsync<JsonElement>($"/orders/{orderId}");
        var line = order.GetProperty("lines")[0];
        Assert.Equal(12.50m, line.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(25.00m, order.GetProperty("totalAmount").GetDecimal());
    }

    [Fact]
    public async Task Cannot_order_a_deactivated_product()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);

        var delete = await admin.DeleteAsync($"/products/{product}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var response = await admin.PostAsJsonAsync("/orders", new
        {
            resellerId = reseller,
            lines = new[] { new { productId = product, quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Order_without_lines_is_rejected()
    {
        var admin = await _fx.AdminAsync();
        var reseller = await ApiHelpers.CreateResellerAsync(admin);

        var response = await admin.PostAsJsonAsync("/orders",
            new { resellerId = reseller, lines = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Confirming_reduces_stock_and_cancelling_restores_it()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 20);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var orderId = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 5);

        Assert.Equal(20, await ApiHelpers.GetStockAsync(admin, product));

        var confirm = await admin.PostAsync($"/orders/{orderId}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal(15, await ApiHelpers.GetStockAsync(admin, product));

        var cancel = await admin.PostAsync($"/orders/{orderId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(20, await ApiHelpers.GetStockAsync(admin, product));
        Assert.Equal("Cancelled", await ApiHelpers.GetStatusAsync(admin, orderId));
    }

    [Fact]
    public async Task Cancelling_a_pending_order_does_not_change_stock()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 20);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var orderId = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 5);

        var cancel = await admin.PostAsync($"/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(20, await ApiHelpers.GetStockAsync(admin, product));
    }

    [Fact]
    public async Task Confirming_with_insufficient_stock_fails_and_changes_nothing()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 3);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var orderId = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 10);

        var confirm = await admin.PostAsync($"/orders/{orderId}/confirm", null);

        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("Pending", await ApiHelpers.GetStatusAsync(admin, orderId));
        Assert.Equal(3, await ApiHelpers.GetStockAsync(admin, product));
    }

    [Fact]
    public async Task Order_goes_through_the_full_lifecycle()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var orderId = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 1);

        foreach (var (action, expected) in new[]
        {
            ("confirm", "Confirmed"), ("ship", "Shipped"), ("deliver", "Delivered")
        })
        {
            var response = await admin.PostAsync($"/orders/{orderId}/{action}", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, await ApiHelpers.GetStatusAsync(admin, orderId));
        }
    }

    [Fact]
    public async Task Illegal_status_changes_are_rejected()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var orderId = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 1);

        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsync($"/orders/{orderId}/ship", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsync($"/orders/{orderId}/deliver", null)).StatusCode);

        await admin.PostAsync($"/orders/{orderId}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsync($"/orders/{orderId}/confirm", null)).StatusCode);

        await admin.PostAsync($"/orders/{orderId}/ship", null);
        await admin.PostAsync($"/orders/{orderId}/deliver", null);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsync($"/orders/{orderId}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task Stock_always_equals_the_sum_of_its_movements()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 20);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);

        var orderId = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 5);
        await admin.PostAsync($"/orders/{orderId}/confirm", null);
        await admin.PostAsync($"/orders/{orderId}/cancel", null);

        var history = await admin.GetFromJsonAsync<JsonElement>($"/products/{product}/stock-history");
        var sum = history.GetProperty("items").EnumerateArray()
            .Sum(m => m.GetProperty("change").GetInt32());

        Assert.Equal(await ApiHelpers.GetStockAsync(admin, product), sum);
    }

    [Fact]
    public async Task Two_orders_cannot_both_take_the_last_units()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var order1 = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 10);
        var order2 = await ApiHelpers.CreateOrderAsync(admin, reseller, product, quantity: 10);

        var responses = await Task.WhenAll(
            admin.PostAsync($"/orders/{order1}/confirm", null),
            admin.PostAsync($"/orders/{order2}/confirm", null));

        var codes = responses.Select(r => r.StatusCode).OrderBy(c => c).ToArray();
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }, codes);
        Assert.Equal(0, await ApiHelpers.GetStockAsync(admin, product));
    }
}