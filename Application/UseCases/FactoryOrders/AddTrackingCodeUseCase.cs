using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.FactoryOrders;

public class AddTrackingCodeUseCase
{
    private readonly IFactoryOrderRepository _repository;

    public AddTrackingCodeUseCase(IFactoryOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(Guid orderId, string trackingCode)
    {
        var order = await _repository.GetByIdAsync(orderId);
        if (order == null) throw new InvalidOperationException("Factory order not found");

        order.MarkAsSentByFactory(trackingCode);

        await _repository.UpdateAsync(order);
    }
}
