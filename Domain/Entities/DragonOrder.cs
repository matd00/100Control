using Domain.Common;

namespace Domain.Entities;

public enum DragonOrderStatus
{
    Pendente = 1,
    Confirmado = 2,
    EnviadoPelaFabrica = 3,
    Entregue = 4,
    Cancelado = 5
}

public enum BoxType
{
    DragonFire = 1,
    Special = 2,
    WhiteBox = 3,
    UltraBox = 4
}

public enum DragonPaymentMethod
{
    Pix = 1,
    Transferencia = 2,
    Dinheiro = 3,
    Credito = 4
}

public static class CashbackConfig
{
    // Cashback por caixa (ajuste os valores reais aqui)
    public static decimal GetCashbackPerBox(BoxType boxType) => boxType switch
    {
        BoxType.DragonFire => 5.00m,
        BoxType.Special => 5.00m,
        BoxType.WhiteBox => 5.00m,
        BoxType.UltraBox => 8.00m,
        _ => 0m
    };
}

public class DragonOrder : Entity
{
    public Guid? CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerContact { get; private set; } = string.Empty;

    public bool IsOwnOrder { get; private set; }
    public bool IsClubeDragon { get; private set; }

    public List<DragonOrderItem> Items { get; private set; } = new();
    public List<DragonPayment> Payments { get; private set; } = new();

    // Pricing
    public decimal TotalAmount { get; private set; }
    public decimal FactoryCost { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal Margin { get; private set; }

    // Cashback
    public decimal CashbackAmount { get; private set; }

    // Payment tracking
    public decimal TotalPaid { get; private set; }
    public decimal RemainingBalance { get; private set; }
    public bool IsFullyPaid { get; private set; }

    // Factory payment
    public bool IsFactoryPaid { get; private set; }
    public DateTime? FactoryPaidAt { get; private set; }

    public DragonOrderStatus Status { get; private set; }
    public string? TrackingCode { get; private set; }
    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private DragonOrder() { }

    public DragonOrder(
        string customerName,
        string customerContact,
        bool isClubeDragon,
        bool isOwnOrder,
        decimal shippingCost,
        Guid? customerId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Nome do cliente é obrigatório", nameof(customerName));

        if (shippingCost < 0)
            throw new ArgumentException("Frete não pode ser negativo", nameof(shippingCost));

        Id = Guid.NewGuid();
        CustomerName = customerName.Trim();
        CustomerContact = customerContact?.Trim() ?? string.Empty;
        CustomerId = customerId;
        IsClubeDragon = isClubeDragon;
        IsOwnOrder = isOwnOrder;
        ShippingCost = shippingCost;
        Notes = notes;

        Status = DragonOrderStatus.Pendente;
        IsFactoryPaid = false;
        IsFullyPaid = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddItem(string description, int quantity, decimal unitCost, decimal unitPrice, BoxType boxType)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Descrição é obrigatória", nameof(description));
        if (quantity <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero", nameof(quantity));
        if (unitCost < 0)
            throw new ArgumentException("Custo unitário não pode ser negativo", nameof(unitCost));
        if (unitPrice < 0)
            throw new ArgumentException("Preço unitário não pode ser negativo", nameof(unitPrice));

        var item = new DragonOrderItem(description, quantity, unitCost, unitPrice, boxType);
        Items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItem(Guid itemId)
    {
        Items.RemoveAll(i => i.Id == itemId);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPayment(decimal amount, DragonPaymentMethod method, string? notes = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Valor do pagamento deve ser maior que zero", nameof(amount));

        if (IsFullyPaid)
            throw new InvalidOperationException("Este pedido já está totalmente pago");

        var payment = new DragonPayment(Id, amount, method, notes);
        Payments.Add(payment);
        RecalculatePayments();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemovePayment(Guid paymentId)
    {
        Payments.RemoveAll(p => p.Id == paymentId);
        RecalculatePayments();
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        FactoryCost = Items.Sum(i => i.SubtotalCost);
        TotalAmount = Items.Sum(i => i.SubtotalPrice);
        TotalCost = FactoryCost + ShippingCost;

        if (TotalAmount > 0)
            Margin = ((TotalAmount - TotalCost) / TotalAmount) * 100;
        else
            Margin = 0;

        // Auto-calculate cashback based on items
        if (IsClubeDragon)
            CashbackAmount = Items.Sum(i => i.Quantity * CashbackConfig.GetCashbackPerBox(i.BoxType));
        else
            CashbackAmount = 0;

        RecalculatePayments();
    }

    private void RecalculatePayments()
    {
        TotalPaid = Payments.Sum(p => p.Amount);
        // Cashback acts as credit — reduces what client needs to pay
        RemainingBalance = Math.Max(0, TotalAmount - TotalPaid - CashbackAmount);
        IsFullyPaid = RemainingBalance <= 0 && TotalAmount > 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateShippingCost(decimal shippingCost)
    {
        if (shippingCost < 0)
            throw new ArgumentException("Frete não pode ser negativo", nameof(shippingCost));

        ShippingCost = shippingCost;
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }



    public void Confirm()
    {
        if (Status != DragonOrderStatus.Pendente)
            throw new InvalidOperationException("Apenas pedidos pendentes podem ser confirmados");
        if (!Items.Any())
            throw new InvalidOperationException("O pedido precisa ter pelo menos um item");

        Status = DragonOrderStatus.Confirmado;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSentByFactory(string? trackingCode = null)
    {
        if (Status != DragonOrderStatus.Confirmado)
            throw new InvalidOperationException("Pedido precisa estar confirmado");

        TrackingCode = trackingCode;
        Status = DragonOrderStatus.EnviadoPelaFabrica;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDelivered()
    {
        if (Status != DragonOrderStatus.EnviadoPelaFabrica)
            throw new InvalidOperationException("Pedido precisa ter sido enviado pela fábrica");

        Status = DragonOrderStatus.Entregue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFactoryAsPaid()
    {
        if (IsFactoryPaid)
            throw new InvalidOperationException("Fábrica já foi paga para este pedido");

        IsFactoryPaid = true;
        FactoryPaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnmarkFactoryAsPaid()
    {
        IsFactoryPaid = false;
        FactoryPaidAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == DragonOrderStatus.Cancelado)
            throw new InvalidOperationException("Pedido já está cancelado");

        Status = DragonOrderStatus.Cancelado;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class DragonOrderItem
{
    public Guid Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public BoxType BoxType { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal SubtotalCost { get; private set; }
    public decimal SubtotalPrice { get; private set; }

    // EF Core constructor
    private DragonOrderItem() { }

    public DragonOrderItem(string description, int quantity, decimal unitCost, decimal unitPrice, BoxType boxType)
    {
        Id = Guid.NewGuid();
        Description = description;
        BoxType = boxType;
        Quantity = quantity;
        UnitCost = unitCost;
        UnitPrice = unitPrice;
        SubtotalCost = quantity * unitCost;
        SubtotalPrice = quantity * unitPrice;
    }
}

public class DragonPayment
{
    public Guid Id { get; private set; }
    public Guid DragonOrderId { get; private set; }
    public decimal Amount { get; private set; }
    public DragonPaymentMethod Method { get; private set; }
    public string? Notes { get; private set; }
    public DateTime PaidAt { get; private set; }

    // EF Core constructor
    private DragonPayment() { }

    public DragonPayment(Guid dragonOrderId, decimal amount, DragonPaymentMethod method, string? notes = null)
    {
        Id = Guid.NewGuid();
        DragonOrderId = dragonOrderId;
        Amount = amount;
        Method = method;
        Notes = notes;
        PaidAt = DateTime.UtcNow;
    }
}
