using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.Shipments;

public class GenerateShipmentUseCase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;

    public GenerateShipmentUseCase(
        IShipmentRepository shipmentRepository,
        IOrderRepository orderRepository)
    {
        _shipmentRepository = shipmentRepository;
        _orderRepository = orderRepository;
    }

    public async Task Execute(GenerateShipmentCommand command)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId);
        if (order == null)
            throw new InvalidOperationException("Order not found");

        var shipment = new Shipment(command.OrderId, command.Provider);

        foreach (var item in order.Items)
        {
            shipment.AddItem(item.ProductId, item.Quantity);
        }

        await _shipmentRepository.SaveAsync(shipment);
    }
}

public class GenerateShipmentCommand
{
    public Guid OrderId { get; set; }
    public ShipmentProvider Provider { get; set; }
}
