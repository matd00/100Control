using Domain.Interfaces.Repositories;

namespace Application.UseCases.Orders;

public class UpdateOrderStatusUseCase
{
    private readonly IOrderRepository _orderRepository;

    public UpdateOrderStatusUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task Execute(Guid orderId, int status)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new InvalidOperationException("Order not found");

        switch (status)
        {
            case 1: // Pending - no change needed
                break;
            case 2: // Processing
                order.MarkAsProcessing();
                break;
            case 3: // Completed
                order.MarkAsCompleted();
                break;
            case 4: // Cancelled
                order.Cancel();
                break;
        }

        await _orderRepository.UpdateAsync(order);
    }
}
