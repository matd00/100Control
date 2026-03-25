using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.Purchases;

public class RegisterPurchaseUseCase
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPurchaseUseCase(
        IPurchaseRepository purchaseRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IUnitOfWork unitOfWork)
    {
        _purchaseRepository = purchaseRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Execute(RegisterPurchaseCommand command)
    {
        try
        {
            if (command == null)
                return Result.Failure("Command cannot be null");

            if (command.SupplierId == Guid.Empty)
                return Result.Failure("Supplier ID is required");

            if (command.Items == null || command.Items.Count == 0)
                return Result.Failure("Purchase must contain at least one item");

            var supplier = await _supplierRepository.GetByIdAsync(command.SupplierId);
            if (supplier == null)
                return Result.Failure("Supplier not found");

            if (!supplier.IsActive)
                return Result.Failure("Supplier is inactive");

            var purchase = new Purchase(command.SupplierId, command.Type);

            foreach (var item in command.Items)
            {
                if (item.ProductId == Guid.Empty)
                    return Result.Failure("Product ID cannot be empty");

                if (item.Quantity <= 0 || item.Quantity > 1000000)
                    return Result.Failure("Invalid quantity");

                if (item.Cost <= 0)
                    return Result.Failure("Cost must be greater than 0");

                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    return Result.Failure($"Product not found");

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

            if (purchase.Items.Count > 500)
                return Result.Failure("Purchase cannot contain more than 500 items");

            await _purchaseRepository.SaveAsync(purchase);
            await _unitOfWork.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure("An error occurred while registering the purchase. Please try again later.");
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
