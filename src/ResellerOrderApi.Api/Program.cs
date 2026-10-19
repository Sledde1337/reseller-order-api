using ResellerOrderApi.Api.ErrorHandling;
using System.Text.Json.Serialization;
using ResellerOrderApi.Core.Services;
using Microsoft.EntityFrameworkCore;
using ResellerOrderApi.Infrastructure;
using ResellerOrderApi.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(o =>
         o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
     .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IResellerService, ResellerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

app.Run();
