using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.FactoryOrders;

public class CreateFactoryOrderUseCase
{
    private readonly IFactoryOrderRepository _repository;

    public CreateFactoryOrderUseCase(IFactoryOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Execute(CreateFactoryOrderCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (string.IsNullOrWhiteSpace(command.CustomerName)) throw new ArgumentException("Customer name is required");
        if (string.IsNullOrWhiteSpace(command.SupplierName)) throw new ArgumentException("Supplier name is required");
        if (command.Items == null || command.Items.Count == 0) throw new ArgumentException("Order must contain at least one item");

        var order = new FactoryOrder(
            command.CustomerName,
            command.CustomerContact,
            command.DeliveryAddress,
            command.SupplierName,
            command.SupplierContact,
            command.Channel,
            command.Notes
        );

        foreach (var item in command.Items)
        {
            order.AddItem(item.Description, item.Quantity, item.UnitCost, item.UnitSalePrice);
        }

        await _repository.AddAsync(order);
        
        return order.Id;
    }
}

public class CreateFactoryOrderCommand
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerContact { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierContact { get; set; } = string.Empty;
    public OrderSource Channel { get; set; }
    public string? Notes { get; set; }
    public List<CreateFactoryOrderItemCommand> Items { get; set; } = new();
}

public class CreateFactoryOrderItemCommand
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitSalePrice { get; set; }
}
