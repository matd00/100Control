using Domain.Common;

namespace Domain.Entities;

public enum FactoryOrderStatus
{
    Pendente = 1,
    Confirmado = 2,
    EnviadoPelaFabrica = 3,
    Entregue = 4,
    Cancelado = 5
}

public class FactoryOrder : Entity
{
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerContact { get; private set; } = string.Empty;
    public string DeliveryAddress { get; private set; } = string.Empty;
    public string SupplierName { get; private set; } = string.Empty;
    public string SupplierContact { get; private set; } = string.Empty;
    
    public List<FactoryOrderItem> Items { get; private set; } = new();
    
    public decimal TotalCost { get; private set; }
    public decimal TotalSalePrice { get; private set; }
    public decimal Margin { get; private set; }
    
    public FactoryOrderStatus Status { get; private set; }
    public OrderSource Channel { get; private set; }
    public string? TrackingCode { get; private set; }
    public string? Notes { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private FactoryOrder() { }

    public FactoryOrder(
        string customerName, 
        string customerContact, 
        string deliveryAddress, 
        string supplierName, 
        string supplierContact, 
        OrderSource channel,
        string? notes = null)
    {
        Id = Guid.NewGuid();
        CustomerName = customerName;
        CustomerContact = customerContact;
        DeliveryAddress = deliveryAddress;
        SupplierName = supplierName;
        SupplierContact = supplierContact;
        Channel = channel;
        Notes = notes;

        Status = FactoryOrderStatus.Pendente;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddItem(string description, int quantity, decimal unitCost, decimal unitSalePrice)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
            
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));
            
        if (unitCost < 0)
            throw new ArgumentException("Unit cost must be positive", nameof(unitCost));
            
        if (unitSalePrice < 0)
            throw new ArgumentException("Unit sale price must be positive", nameof(unitSalePrice));

        if (Status != FactoryOrderStatus.Pendente)
            throw new InvalidOperationException("Cannot add items to a non-pending order");

        var item = new FactoryOrderItem(description, quantity, unitCost, unitSalePrice);
        Items.Add(item);
        
        RecalculateTotals();
    }

    public void RemoveItem(Guid itemId)
    {
        if (Status != FactoryOrderStatus.Pendente)
            throw new InvalidOperationException("Cannot remove items from a non-pending order");

        Items.RemoveAll(i => i.Id == itemId);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        TotalCost = Items.Sum(i => i.SubtotalCost);
        TotalSalePrice = Items.Sum(i => i.SubtotalSalePrice);
        
        if (TotalSalePrice > 0)
        {
            Margin = ((TotalSalePrice - TotalCost) / TotalSalePrice) * 100;
        }
        else
        {
            Margin = 0;
        }
    }

    public void Confirm()
    {
        if (Status != FactoryOrderStatus.Pendente)
            throw new InvalidOperationException("Only pending orders can be confirmed");

        Status = FactoryOrderStatus.Confirmado;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSentByFactory(string trackingCode)
    {
        if (Status != FactoryOrderStatus.Confirmado)
            throw new InvalidOperationException("Order must be confirmed before marking as sent");

        if (string.IsNullOrWhiteSpace(trackingCode))
            throw new ArgumentException("Tracking code is required when marking as sent");

        TrackingCode = trackingCode;
        Status = FactoryOrderStatus.EnviadoPelaFabrica;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDelivered()
    {
        if (Status != FactoryOrderStatus.EnviadoPelaFabrica)
            throw new InvalidOperationException("Order must be sent before it can be marked as delivered");

        Status = FactoryOrderStatus.Entregue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == FactoryOrderStatus.Entregue || Status == FactoryOrderStatus.Cancelado)
            throw new InvalidOperationException($"Cannot cancel order in status: {Status}");

        Status = FactoryOrderStatus.Cancelado;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class FactoryOrderItem
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal UnitSalePrice { get; private set; }
    
    public decimal SubtotalCost { get; private set; }
    public decimal SubtotalSalePrice { get; private set; }

    // EF Core constructor
    private FactoryOrderItem() { }

    public FactoryOrderItem(string description, int quantity, decimal unitCost, decimal unitSalePrice)
    {
        Id = Guid.NewGuid();
        Description = description;
        Quantity = quantity;
        UnitCost = unitCost;
        UnitSalePrice = unitSalePrice;
        
        SubtotalCost = quantity * unitCost;
        SubtotalSalePrice = quantity * unitSalePrice;
    }
}
