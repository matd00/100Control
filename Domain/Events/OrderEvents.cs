using Domain.Common;

namespace Domain.Events;

public class OrderCreatedEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public DateTime OccurredOn { get; }

    public OrderCreatedEvent(Guid orderId, Guid customerId)
    {
        OrderId = orderId;
        CustomerId = customerId;
        OccurredOn = DateTime.UtcNow;
    }
}

public class ProductStockDecreasedEvent : IDomainEvent
{
    public Guid ProductId { get; }
    public int Quantity { get; }
    public int NewStock { get; }
    public DateTime OccurredOn { get; }

    public ProductStockDecreasedEvent(Guid productId, int quantity, int newStock)
    {
        ProductId = productId;
        Quantity = quantity;
        NewStock = newStock;
        OccurredOn = DateTime.UtcNow;
    }
}
