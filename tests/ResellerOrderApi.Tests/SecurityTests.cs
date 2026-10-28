using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ResellerOrderApi.Tests;

[Collection("api")]
public class SecurityTests
{
    private readonly ApiFixture _fx;

    public SecurityTests(ApiFixture fx)
    {
        _fx = fx;
    }

    [Fact]
    public async Task Requests_without_a_token_are_rejected()
    {
        var client = _fx.Factory.CreateClient();

        var response = await client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_rejected()
    {
        var client = _fx.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login",
            new { email = ApiFixture.AdminEmail, password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_gives_the_same_answer()
    {
        var client = _fx.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login",
            new { email = "nobody@test.com", password = "whatever" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reseller_users_cannot_manage_products_or_resellers()
    {
        var admin = await _fx.AdminAsync();
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var resellerClient = await ApiHelpers.CreateResellerUserClientAsync(_fx, admin, reseller);

        var read = await resellerClient.GetAsync("/products");
        var create = await resellerClient.PostAsJsonAsync("/products",
            new { ean = "1234567890123", name = "Nope", category = "Accessory", netPrice = 5 });
        var listResellers = await resellerClient.GetAsync("/resellers");
        var listUsers = await resellerClient.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, listResellers.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, listUsers.StatusCode);
    }

    [Fact]
    public async Task Sales_reps_cannot_manage_users()
    {
        var sales = await _fx.LoginAsync(ApiFixture.SalesEmail, ApiFixture.SalesPassword);

        var response = await sales.GetAsync("/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Resellers_cannot_confirm_orders()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var reseller = await ApiHelpers.CreateResellerAsync(admin);
        var resellerClient = await ApiHelpers.CreateResellerUserClientAsync(_fx, admin, reseller);
        var orderId = await ApiHelpers.CreateOrderAsync(resellerClient, reseller, product, quantity: 1);

        var response = await resellerClient.PostAsync($"/orders/{orderId}/confirm", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reseller_cannot_see_another_resellers_order()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var resellerA = await ApiHelpers.CreateResellerAsync(admin);
        var resellerB = await ApiHelpers.CreateResellerAsync(admin);
        var clientA = await ApiHelpers.CreateResellerUserClientAsync(_fx, admin, resellerA);

        var orderOfB = await ApiHelpers.CreateOrderAsync(admin, resellerB, product, quantity: 1);

        var response = await clientA.GetAsync($"/orders/{orderOfB}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var adminResponse = await admin.GetAsync($"/orders/{orderOfB}");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Reseller_order_list_only_contains_their_own_orders()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var resellerA = await ApiHelpers.CreateResellerAsync(admin);
        var resellerB = await ApiHelpers.CreateResellerAsync(admin);
        var clientA = await ApiHelpers.CreateResellerUserClientAsync(_fx, admin, resellerA);

        await ApiHelpers.CreateOrderAsync(admin, resellerA, product, quantity: 1);
        await ApiHelpers.CreateOrderAsync(admin, resellerB, product, quantity: 1);

        var list = await clientA.GetFromJsonAsync<JsonElement>($"/orders?resellerId={resellerB}");
        var owners = list.GetProperty("items").EnumerateArray()
            .Select(o => o.GetProperty("resellerId").GetGuid())
            .Distinct()
            .ToList();

        Assert.Equal(new[] { resellerA }, owners);
    }

    [Fact]
    public async Task Reseller_cannot_create_orders_for_someone_else()
    {
        var admin = await _fx.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin, stock: 10);
        var resellerA = await ApiHelpers.CreateResellerAsync(admin);
        var resellerB = await ApiHelpers.CreateResellerAsync(admin);
        var clientA = await ApiHelpers.CreateResellerUserClientAsync(_fx, admin, resellerA);

        var orderId = await ApiHelpers.CreateOrderAsync(clientA, resellerB, product, quantity: 1);

        var order = await admin.GetFromJsonAsync<JsonElement>($"/orders/{orderId}");
        Assert.Equal(resellerA, order.GetProperty("resellerId").GetGuid());
    }

    [Fact]
    public async Task Deactivated_users_cannot_log_in()
    {
        var admin = await _fx.AdminAsync();
        var email = $"{Guid.NewGuid():N}@user.test";

        var create = await admin.PostAsJsonAsync("/users",
            new { email, password = ApiHelpers.UserPassword, role = "SalesRep" });
        create.EnsureSuccessStatusCode();
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("userId").GetGuid();

        var deactivate = await admin.PostAsync($"/users/{userId}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var login = await _fx.Factory.CreateClient().PostAsJsonAsync("/auth/login",
            new { email, password = ApiHelpers.UserPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_is_public_and_healthy()
    {
        var client = _fx.Factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}