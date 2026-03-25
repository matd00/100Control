using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.FactoryOrders;

public class AddTrackingCodeUseCase
{
    private readonly IFactoryOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddTrackingCodeUseCase(IFactoryOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> Execute(Guid orderId, string trackingCode)
    {
        try
        {
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null) return Result.Failure("Factory order not found");

            order.MarkAsSentByFactory(trackingCode);

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
