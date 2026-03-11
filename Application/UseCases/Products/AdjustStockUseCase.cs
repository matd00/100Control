using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.Products;

public class AdjustStockUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;

    public AdjustStockUseCase(
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository)
    {
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
    }

    public async Task Execute(Guid productId, int quantity, string notes)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
            throw new InvalidOperationException("Product not found");

        if (quantity > 0)
            product.IncreaseStock(quantity);
        else if (quantity < 0)
            product.DecreaseStock(Math.Abs(quantity));
        else
            return;

        await _productRepository.UpdateAsync(product);

        var movement = new InventoryMovement(
            product.Id,
            InventoryMovementType.Adjustment,
            Math.Abs(quantity),
            "Manual Adjustment",
            notes);

        await _inventoryMovementRepository.SaveAsync(movement);
    }
}
