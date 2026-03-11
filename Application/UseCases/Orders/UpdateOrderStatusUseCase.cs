using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.Orders;

public class UpdateOrderStatusUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;

    public UpdateOrderStatusUseCase(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
    }

    public async Task Execute(Guid orderId, int status)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new InvalidOperationException("Order not found");

        var oldStatus = order.Status;
        var newStatus = (OrderStatus)status;

        if (oldStatus == newStatus) return;

        // Transitions that should decrease stock: Pending -> (Processing, Shipped, Completed)
        bool shouldDecreaseStock = oldStatus == OrderStatus.Pending && 
                                   (newStatus == OrderStatus.Processing || 
                                    newStatus == OrderStatus.Shipped || 
                                    newStatus == OrderStatus.Completed);

        // Transitions that should increase stock (Return): (Processing, Shipped, Completed) -> Cancelled
        bool shouldIncreaseStock = (oldStatus == OrderStatus.Processing || 
                                    oldStatus == OrderStatus.Shipped || 
                                    oldStatus == OrderStatus.Completed) && 
                                   newStatus == OrderStatus.Cancelled;

        switch (newStatus)
        {
            case OrderStatus.Pending:
                // Cannot go back to pending normally in this simplified flow
                break;
            case OrderStatus.Processing:
                order.MarkAsProcessing();
                break;
            case OrderStatus.Shipped:
                order.MarkAsShipped();
                break;
            case OrderStatus.Completed:
                order.MarkAsCompleted();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }

        if (shouldDecreaseStock)
        {
            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.DecreaseStock(item.Quantity);
                    await _productRepository.UpdateAsync(product);
                    
                    var movement = new InventoryMovement(
                        product.Id, 
                        InventoryMovementType.Sale, 
                        item.Quantity, 
                        $"Order {order.Id}", 
                        "Stock decreased due to order processing");
                    await _inventoryMovementRepository.SaveAsync(movement);
                }
            }
        }
        else if (shouldIncreaseStock)
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
                        "Stock returned due to order cancellation");
                    await _inventoryMovementRepository.SaveAsync(movement);
                }
            }
        }

        await _orderRepository.UpdateAsync(order);
    }
}
