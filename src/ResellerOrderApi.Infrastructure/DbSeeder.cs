using Bogus;
using ResellerOrderApi.Core.Entities;

namespace ResellerOrderApi.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Products.Any() || db.Resellers.Any()) return;

        Randomizer.Seed = new Random(12345);
        var faker = new Faker();

        var products = BuildProducts(faker);
        var resellers = BuildResellers(faker);

        var movements = products
            .Where(p => p.StockQuantity > 0)
            .Select(p => new StockMovement
            {
                StockMovementId = Guid.NewGuid(),
                ProductId = p.ProductId,
                Change = p.StockQuantity,
                Reason = StockMovementReason.Restock,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            })
            .ToList();

        db.Products.AddRange(products);
        db.Resellers.AddRange(resellers);
        db.StockMovements.AddRange(movements);
        await db.SaveChangesAsync();
    }

    private static List<Product> BuildProducts(Faker faker)
    {
        var products = new List<Product>();
        var usedEans = new HashSet<string>();

        void Add(ProductCategory category, string name, decimal minPrice, decimal maxPrice)
        {
            string ean;
            do { ean = faker.Commerce.Ean13(); } while (!usedEans.Add(ean));

            products.Add(new Product
            {
                ProductId = Guid.NewGuid(),
                Ean = ean,
                Name = name,
                Category = category,
                NetPrice = Math.Round(faker.Random.Decimal(minPrice, maxPrice), 2),
                // Roughly 1 in 10 products is out of stock, and 1 in 20 is discontinued
                StockQuantity = faker.Random.Bool(0.1f) ? 0 : faker.Random.Int(5, 200),
                IsActive = !faker.Random.Bool(0.05f)
            });
        }

        foreach (var name in new[]
        {
            "MakerOne Mini", "MakerOne Core", "MakerOne Core XL", "MakerOne Pro",
            "FormLab Starter", "FormLab Studio", "FormLab Enclosed", "FormLab Resin 4K"
        })
            Add(ProductCategory.Printer, $"{name} 3D Printer", 199, 1999);

        foreach (var material in new[] { "PLA", "PETG", "ABS", "TPU", "ASA" })
            foreach (var color in new[] { "Black", "White", "Red", "Blue" })
                Add(ProductCategory.Filament, $"{material} Filament 1.75mm 1kg - {color}", 15, 35);

        foreach (var name in new[]
        {
            "Creality Pika", "Creality CR-Scan Ferret Pro", "3DMakerpro Fox", "Creality CR-Scan Raptor",
            "Shining 3D Einstar Rockit", "Creality CR-Scan Otter", "Creality CR-Scan Raptor Pro", "Creality CR-Scan Ferret SE",
            "Creality Raptor X", "Shining 3D Einstar 2"
        })
            Add(ProductCategory.Scanner, name, 8, 120);

        foreach (var name in new[]
        {
            "Hotend Assembly", "Brass Nozzle 0.4mm (5-pack)", "PTFE Tube 1m", "Heated Bed Plate",
            "Extruder Gear Kit", "Cooling Fan 4010", "Stepper Motor", "Belt Kit"
        })
            Add(ProductCategory.SparePart, name, 5, 90);

        foreach (var name in new[]
        {
            "Filament Dryer Box", "Enclosure Tent", "Spool Holder", "Textured PEI Build Sheet"
        })
            Add(ProductCategory.Accessory, name, 10, 150);

        return products;
    }

    private static List<Reseller> BuildResellers(Faker faker)
    {
        var countries = new[] { "Sweden", "Norway", "Denmark", "Finland", "Germany", "Netherlands", "Poland", "France" };
        var counter = 0;

        return new Faker<Reseller>()
            .RuleFor(r => r.ResellerId, _ => Guid.NewGuid())
            .RuleFor(r => r.Name, f => f.Company.CompanyName())
            .RuleFor(r => r.Email, f => $"orders{++counter}@{f.Internet.DomainName()}")
            .RuleFor(r => r.Country, f => f.PickRandom(countries))
            .Generate(20);
    }
}