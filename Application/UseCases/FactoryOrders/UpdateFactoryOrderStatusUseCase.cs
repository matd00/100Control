using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.FactoryOrders;

public class UpdateFactoryOrderStatusUseCase
{
    private readonly IFactoryOrderRepository _repository;

    public UpdateFactoryOrderStatusUseCase(IFactoryOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(Guid orderId, FactoryOrderStatus newStatus)
    {
        var order = await _repository.GetByIdAsync(orderId);
        if (order == null) throw new InvalidOperationException("Factory order not found");

        switch (newStatus)
        {
            case FactoryOrderStatus.Confirmado:
                order.Confirm();
                break;
            case FactoryOrderStatus.Entregue:
                order.MarkAsDelivered();
                break;
            case FactoryOrderStatus.Cancelado:
                order.Cancel();
                break;
            case FactoryOrderStatus.EnviadoPelaFabrica:
                throw new InvalidOperationException("Use AddTrackingCodeUseCase to mark as sent by factory");
            case FactoryOrderStatus.Pendente:
                throw new InvalidOperationException("Cannot revert to pending status");
        }

        await _repository.UpdateAsync(order);
    }
}
