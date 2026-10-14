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
    Delivered,
    Cancelled
}

public enum StockMovementReason
{
    Restock,
    OrderConfirmed,
    OrderCancelled,
    Correction
}