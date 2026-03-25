using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.FactoryOrders;

public class UpdateFactoryOrderStatusUseCase
{
    private readonly IFactoryOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFactoryOrderStatusUseCase(IFactoryOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> Execute(Guid orderId, FactoryOrderStatus newStatus)
    {
        try
        {
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null) return Result.Failure("Factory order not found");

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
                    return Result.Failure("Use AddTrackingCodeUseCase to mark as sent by factory");
                case FactoryOrderStatus.Pendente:
                    return Result.Failure("Cannot revert to pending status");
            }

            await _repository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
