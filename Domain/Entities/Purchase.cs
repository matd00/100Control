using Domain.Common;

namespace Domain.Entities;

public enum PurchaseType
{
    FactoryPurchase = 1,
    UsedPurchase = 2
}

public class Purchase : Entity
{
    public Guid SupplierId { get; private set; }
    public PurchaseType Type { get; private set; }
    public List<PurchaseItem> Items { get; private set; } = new();
    public decimal TotalAmount { get; private set; }
    public PurchaseStatus Status { get; private set; }
    public DateTime PurchaseDate { get; private set; }
    public DateTime? DeliveryDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Purchase() { }

    public Purchase(Guid supplierId, PurchaseType type)
    {
        // Security: Input validation
        if (supplierId == Guid.Empty)
            throw new ArgumentException("Supplier ID cannot be empty", nameof(supplierId));

        if (!Enum.IsDefined(typeof(PurchaseType), type))
            throw new ArgumentException("Invalid purchase type", nameof(type));

        Id = Guid.NewGuid();
        SupplierId = supplierId;
        Type = type;
        Status = PurchaseStatus.Pending;
        PurchaseDate = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, int quantity, decimal cost)
    {
        // Security: Input validation
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (quantity <= 0 || quantity > 1000000)
            throw new ArgumentException("Quantity must be between 1 and 1,000,000", nameof(quantity));

        if (cost <= 0)
            throw new ArgumentException("Cost must be greater than 0", nameof(cost));

        // Check purchase limit
        if (Items.Count >= 500)
            throw new InvalidOperationException("Purchase cannot contain more than 500 items");

        // Check total amount doesn't exceed limit
        decimal estimatedNewTotal = TotalAmount + (quantity * cost);
        if (estimatedNewTotal > 999999999.99m)
            throw new InvalidOperationException("Purchase total would exceed maximum allowed amount");

        // Check status allows modifications
        if (Status != PurchaseStatus.Pending)
            throw new InvalidOperationException("Cannot add items to non-pending purchase");

        var item = new PurchaseItem(productId, quantity, cost);
        Items.Add(item);
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.Subtotal);
    }

    public void MarkAsReceived(DateTime deliveryDate)
    {
        // Security: Status validation
        if (Status != PurchaseStatus.Pending)
            throw new InvalidOperationException("Can only mark pending purchases as received");

        if (deliveryDate == DateTime.MinValue)
            throw new ArgumentException("Delivery date must be set", nameof(deliveryDate));

        if (deliveryDate < PurchaseDate)
            throw new ArgumentException("Delivery date cannot be before purchase date", nameof(deliveryDate));

        Status = PurchaseStatus.Received;
        DeliveryDate = deliveryDate;
    }

    public void Cancel()
    {
        // Security: Status validation
        if (Status == PurchaseStatus.Received)
            throw new InvalidOperationException("Cannot cancel received purchase");

        if (Status == PurchaseStatus.Cancelled)
            throw new InvalidOperationException("Purchase is already cancelled");

        Status = PurchaseStatus.Cancelled;
    }
}

public enum PurchaseStatus
{
    Pending = 1,
    Received = 2,
    Cancelled = 3
}

public class PurchaseItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal Cost { get; private set; }
    public decimal Subtotal { get; private set; }

    // EF Core constructor
    private PurchaseItem() { }

    public PurchaseItem(Guid productId, int quantity, decimal cost)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        Cost = cost;
        Subtotal = quantity * cost;
    }
}
