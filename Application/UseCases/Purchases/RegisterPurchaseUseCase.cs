using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.Purchases;

public class RegisterPurchaseUseCase
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;

    public RegisterPurchaseUseCase(
        IPurchaseRepository purchaseRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository)
    {
        _purchaseRepository = purchaseRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
    }

    public async Task Execute(RegisterPurchaseCommand command)
    {
        try
        {
            // Security: Input validation
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (command.SupplierId == Guid.Empty)
                throw new ArgumentException("Supplier ID is required");

            if (command.Items == null || command.Items.Count == 0)
                throw new ArgumentException("Purchase must contain at least one item");

            var supplier = await _supplierRepository.GetByIdAsync(command.SupplierId);
            if (supplier == null)
                throw new InvalidOperationException("Supplier not found");

            if (!supplier.IsActive)
                throw new InvalidOperationException("Supplier is inactive");

            var purchase = new Purchase(command.SupplierId, command.Type);

            foreach (var item in command.Items)
            {
                if (item.ProductId == Guid.Empty)
                    throw new ArgumentException("Product ID cannot be empty");

                if (item.Quantity <= 0 || item.Quantity > 1000000)
                    throw new ArgumentException("Invalid quantity");

                if (item.Cost <= 0)
                    throw new ArgumentException("Cost must be greater than 0");

                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new InvalidOperationException($"Product not found");

                // Add purchase item
                purchase.AddItem(item.ProductId, item.Quantity, item.Cost);

                // Increase stock
                product.IncreaseStock(item.Quantity);

                // Log inventory movement
                var movement = new InventoryMovement(
                    item.ProductId,
                    InventoryMovementType.Purchase,
                    item.Quantity,
                    purchase.Id.ToString(),
                    $"Purchase from {supplier.Name}"
                );

                await _productRepository.UpdateAsync(product);
                await _inventoryMovementRepository.SaveAsync(movement);
            }

            // Limit items per purchase
            if (purchase.Items.Count > 500)
                throw new InvalidOperationException("Purchase cannot contain more than 500 items");

            await _purchaseRepository.SaveAsync(purchase);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Security: Don't expose internal exception details
            throw new InvalidOperationException("An error occurred while registering the purchase. Please try again later.");
        }
    }
}

public class RegisterPurchaseCommand
{
    public Guid SupplierId { get; set; }
    public PurchaseType Type { get; set; }
    public List<PurchaseItemCommand> Items { get; set; } = new();
}

public class PurchaseItemCommand
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Cost { get; set; }
}
