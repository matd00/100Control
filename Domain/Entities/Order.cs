using Domain.Common;
using Domain.Events;

namespace Domain.Entities;

public enum OrderSource
{
    MercadoLivre = 1,
    Shopee = 2,
    Instagram = 3,
    WhatsApp = 4,
    Direct = 5
}

public class Order : Entity
{
    public Guid CustomerId { get; private set; }
    public OrderSource Source { get; private set; }
    public bool IsDropshipping { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private Order() { }

    public Order(Guid customerId, OrderSource source = OrderSource.Direct, bool isDropshipping = false)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Source = source;
        IsDropshipping = isDropshipping;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderCreatedEvent(Id, CustomerId));
    }

    public void AddItem(Guid productId, int quantity, decimal price)
    {
        // Security: Input validation
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > 1000)  // Reasonable limit per order
            throw new ArgumentException("Quantity exceeds maximum allowed per item", nameof(quantity));

        if (price <= 0)
            throw new ArgumentException("Price must be greater than 0", nameof(price));

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to non-pending order");

        var item = new OrderItem(productId, quantity, price);
        Items.Add(item);
        RecalculateTotal();
    }

    public void RemoveItem(Guid productId)
    {
        Items.RemoveAll(i => i.ProductId == productId);
        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCustomer(Guid customerId)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot update customer of non-pending order");

        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));

        CustomerId = customerId;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.Subtotal);
    }

    public void MarkAsProcessing()
    {
        Status = OrderStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsShipped()
    {
        Status = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted()
    {
        Status = OrderStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Cancelled = 4,
    Shipped = 5
}

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Subtotal { get; private set; }

    // EF Core constructor
    private OrderItem() { }

    public OrderItem(Guid productId, int quantity, decimal price)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = price;
        Subtotal = quantity * price;
    }
}
