using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.Shipments;

public class GenerateShipmentUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateShipmentUseCase(
        IShipmentRepository shipmentRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _shipmentRepository = shipmentRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Execute(GenerateShipmentCommand command)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            if (order == null)
                return Result<Guid>.Failure("Order not found");

            var shipment = new Shipment(command.OrderId, command.Provider);

            foreach (var item in order.Items)
            {
                shipment.AddItem(item.ProductId, item.Quantity);
            }

            await _shipmentRepository.SaveAsync(shipment);
            await _unitOfWork.SaveChangesAsync();
            
            return Result<Guid>.Success(shipment.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

public class GenerateShipmentCommand
{
    public Guid OrderId { get; set; }
    public ShipmentProvider Provider { get; set; }
}
