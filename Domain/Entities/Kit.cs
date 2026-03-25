using Domain.Common;

namespace Domain.Entities;

public class Kit : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public List<KitItem> Items { get; private set; } = new();
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Kit() { }

    public Kit(string name, string description, decimal price)
    {
        // Security: Input validation
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Kit name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Kit name cannot exceed 200 characters", nameof(name));

        if (price <= 0)
            throw new ArgumentException("Price must be greater than 0", nameof(price));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, int quantity)
    {
        // Security: Input validation
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (quantity <= 0 || quantity > 1000)
            throw new ArgumentException("Quantity must be between 1 and 1,000", nameof(quantity));

        // Check kit limit
        if (Items.Count >= 100)
            throw new InvalidOperationException("Kit cannot contain more than 100 components");

        // Prevent duplicates
        if (Items.Any(i => i.ProductId == productId))
            throw new InvalidOperationException("Product already exists in kit");

        if (!IsActive)
            throw new InvalidOperationException("Cannot modify inactive kit");

        var item = new KitItem(productId, quantity);
        Items.Add(item);
    }

    public void RemoveItem(Guid productId)
    {
        // Security: Validation
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (!IsActive)
            throw new InvalidOperationException("Cannot modify inactive kit");

        if (!Items.Any(i => i.ProductId == productId))
            throw new InvalidOperationException("Product not found in kit");

        Items.RemoveAll(i => i.ProductId == productId);
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public class KitItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // EF Core constructor
    private KitItem() { }

    public KitItem(Guid productId, int quantity)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
    }
}
