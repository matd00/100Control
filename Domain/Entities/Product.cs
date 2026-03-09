namespace Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public int Stock { get; private set; }
    public decimal Cost { get; private set; }
    public decimal Price { get; private set; }

    // Shipping dimensions (cm)
    public decimal Weight { get; private set; }  // kg
    public int Width { get; private set; }       // cm
    public int Height { get; private set; }      // cm
    public int Length { get; private set; }      // cm

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
        SKU = GenerateSKU(name);
        Cost = cost;
        Price = price;
        Stock = 0;

        // Default shipping dimensions (minimum for Correios)
        Weight = 0.3m;  // 300g
        Width = 11;     // cm
        Height = 2;     // cm
        Length = 16;    // cm

        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    private static string GenerateSKU(string name)
    {
        var prefix = name.Length >= 3 ? name[..3].ToUpperInvariant() : name.ToUpperInvariant();
        var random = new Random().Next(1000, 9999);
        return $"{prefix}-{random}";
    }

    public void UpdateShippingDimensions(decimal weight, int width, int height, int length)
    {
        if (weight <= 0 || weight > 30)
            throw new ArgumentException("Weight must be between 0.01 and 30 kg", nameof(weight));

        if (width < 11 || width > 100)
            throw new ArgumentException("Width must be between 11 and 100 cm", nameof(width));

        if (height < 2 || height > 100)
            throw new ArgumentException("Height must be between 2 and 100 cm", nameof(height));

        if (length < 16 || length > 100)
            throw new ArgumentException("Length must be between 16 and 100 cm", nameof(length));

        Weight = weight;
        Width = width;
        Height = height;
        Length = length;
        UpdatedAt = DateTime.UtcNow;
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

    public void UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePricing(decimal cost, decimal price)
    {
        if (cost < 0)
            throw new ArgumentException("Cost cannot be negative", nameof(cost));

        if (price <= 0)
            throw new ArgumentException("Price must be greater than 0", nameof(price));

        Cost = cost;
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
