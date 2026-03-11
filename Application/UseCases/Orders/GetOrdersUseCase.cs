using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.Orders;

public class GetOrdersUseCase
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<Order>> Execute()
    {
        return await _orderRepository.GetAllAsync();
    }
}
