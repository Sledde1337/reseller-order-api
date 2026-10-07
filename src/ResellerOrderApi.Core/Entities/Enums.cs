namespace ResellerOrderApi.Core.Entities;

public enum ProductCategory
{
    Printer,
    Filament,
    Scanner,
    SparePart,
    Accessory
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Cancelled
}

public enum StockMovementReason
{
    Restock,
    OrderConfirmed,
    OrderCancelled,
    Correction
}