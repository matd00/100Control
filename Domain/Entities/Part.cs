namespace Domain.Entities;

public class Part
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Stock { get; private set; }
    public decimal Cost { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Part()
    { }

    public Part(Guid productId, string name, string description, decimal cost)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Part name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Part name cannot exceed 200 characters", nameof(name));

        if (cost <= 0)
            throw new ArgumentException("Cost must be greater than 0", nameof(cost));

        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Cost = cost;
        Stock = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int quantity)
    {
        // Security: Input validation
        if (quantity < 0 || quantity > 1000000)
            throw new ArgumentException("Quantity must be between 0 and 1,000,000", nameof(quantity));

        if (!IsActive)
            throw new InvalidOperationException("Cannot update stock of inactive part");

        Stock = quantity;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}