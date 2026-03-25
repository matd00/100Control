using Domain.Common;

namespace Domain.Entities;

public enum InventoryMovementType
{
    Purchase = 1,
    Sale = 2,
    Adjustment = 3,
    KitUsage = 4,
    Return = 5
}

public class InventoryMovement : Entity
{
    public Guid ProductId { get; private set; }
    public InventoryMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private InventoryMovement() { }

    public InventoryMovement(Guid productId, InventoryMovementType type, int quantity, string reference, string notes = "")
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        Reference = reference;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }
}
