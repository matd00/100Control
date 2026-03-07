namespace Domain.Entities;

public enum ShipmentProvider
{
    SuperFrete = 1,
    Transportadora = 2,
    DropShipping = 3
}

public enum ShipmentStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public class Shipment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public ShipmentProvider Provider { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public string TrackingNumber { get; private set; } = string.Empty;
    public decimal ShippingCost { get; private set; }
    public List<ShipmentItem> Items { get; private set; } = new();
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Shipment() { }

    public Shipment(Guid orderId, ShipmentProvider provider)
    {
        // Security: Input validation
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

        if (!Enum.IsDefined(typeof(ShipmentProvider), provider))
            throw new ArgumentException("Invalid shipment provider", nameof(provider));

        Id = Guid.NewGuid();
        OrderId = orderId;
        Provider = provider;
        Status = ShipmentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, int quantity)
    {
        // Security: Input validation
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (quantity <= 0 || quantity > 1000)
            throw new ArgumentException("Quantity must be between 1 and 1,000", nameof(quantity));

        if (Status != ShipmentStatus.Pending && Status != ShipmentStatus.Processing)
            throw new InvalidOperationException("Cannot add items to shipped/delivered/cancelled shipment");

        var item = new ShipmentItem(productId, quantity);
        Items.Add(item);
    }

    public void GenerateLabel(string trackingNumber, decimal shippingCost)
    {
        // Security: Input validation
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException("Tracking number cannot be empty", nameof(trackingNumber));

        if (trackingNumber.Length > 50)
            throw new ArgumentException("Tracking number cannot exceed 50 characters", nameof(trackingNumber));

        if (shippingCost < 0)
            throw new ArgumentException("Shipping cost cannot be negative", nameof(shippingCost));

        if (shippingCost > 99999.99m)
            throw new ArgumentException("Shipping cost exceeds maximum allowed", nameof(shippingCost));

        if (Status != ShipmentStatus.Pending)
            throw new InvalidOperationException("Can only generate label for pending shipment");

        if (Items.Count == 0)
            throw new InvalidOperationException("Shipment must contain items");

        TrackingNumber = trackingNumber.Trim();
        ShippingCost = shippingCost;
        Status = ShipmentStatus.Processing;
    }

    public void MarkAsShipped()
    {
        // Security: Status validation
        if (Status != ShipmentStatus.Processing)
            throw new InvalidOperationException("Can only ship from processing state");

        if (string.IsNullOrWhiteSpace(TrackingNumber))
            throw new InvalidOperationException("Tracking number must be set before shipping");

        Status = ShipmentStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
    }

    public void MarkAsDelivered()
    {
        // Security: Status validation
        if (Status != ShipmentStatus.Shipped)
            throw new InvalidOperationException("Can only deliver from shipped state");

        if (!ShippedAt.HasValue)
            throw new InvalidOperationException("Shipment must be shipped before delivery");

        Status = ShipmentStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        // Security: Status validation
        if (Status == ShipmentStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel delivered shipment");

        if (Status == ShipmentStatus.Cancelled)
            throw new InvalidOperationException("Shipment is already cancelled");

        Status = ShipmentStatus.Cancelled;
    }
}

public class ShipmentItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // EF Core constructor
    private ShipmentItem() { }

    public ShipmentItem(Guid productId, int quantity)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
    }
}
