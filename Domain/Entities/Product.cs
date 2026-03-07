namespace Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Stock { get; private set; }
    public decimal Cost { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private Product() { }

    public Product(string name, string description, decimal cost, decimal price)
    {
        // Security: Input validation
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(name));

        if (cost <= 0)
            throw new ArgumentException("Cost must be greater than 0", nameof(cost));

        if (price <= 0)
            throw new ArgumentException("Price must be greater than 0", nameof(price));

        if (price < cost)
            throw new ArgumentException("Selling price cannot be less than cost", nameof(price));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Cost = cost;
        Price = price;
        Stock = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > 1000000)  // Reasonable limit
            throw new ArgumentException("Quantity exceeds maximum allowed", nameof(quantity));

        if (Stock < quantity)
            throw new InvalidOperationException("Insufficient stock available");

        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > 1000000)  // Reasonable limit
            throw new ArgumentException("Quantity exceeds maximum allowed", nameof(quantity));

        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be greater than 0", nameof(newPrice));

        if (!IsActive)
            throw new InvalidOperationException("Cannot update price of inactive product");

        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
