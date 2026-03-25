using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.Products;

public class AdjustStockUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustStockUseCase(
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Execute(Guid productId, int quantity, string notes)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                return Result.Failure("Product not found");

            if (quantity > 0)
                product.IncreaseStock(quantity);
            else if (quantity < 0)
                product.DecreaseStock(Math.Abs(quantity));
            else
                return Result.Success();

            await _productRepository.UpdateAsync(product);

            var movement = new InventoryMovement(
                product.Id,
                InventoryMovementType.Adjustment,
                Math.Abs(quantity),
                "Manual Adjustment",
                notes);

            await _inventoryMovementRepository.SaveAsync(movement);
            await _unitOfWork.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
