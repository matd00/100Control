using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.Orders;

public class DeleteOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOrderUseCase(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Execute(Guid orderId)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return Result.Failure("Order not found");

            // If the order was in a status where stock was decreased, return it before deleting
            if (order.Status == OrderStatus.Processing || 
                order.Status == OrderStatus.Shipped || 
                order.Status == OrderStatus.Completed)
            {
                foreach (var item in order.Items)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.IncreaseStock(item.Quantity);
                        await _productRepository.UpdateAsync(product);
                        
                        var movement = new InventoryMovement(
                            product.Id, 
                            InventoryMovementType.Return, 
                            item.Quantity, 
                            $"Order {order.Id}", 
                            "Stock returned due to order deletion");
                        await _inventoryMovementRepository.SaveAsync(movement);
                    }
                }
            }

            await _orderRepository.DeleteAsync(orderId);
            await _unitOfWork.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
