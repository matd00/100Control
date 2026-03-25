using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.FactoryOrders;

public class CreateFactoryOrderUseCase
{
    private readonly IFactoryOrderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFactoryOrderUseCase(IFactoryOrderRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Guid>> Execute(CreateFactoryOrderCommand command)
    {
        try
        {
            if (command == null)
                return Result<Guid>.Failure("Command cannot be null");

            if (string.IsNullOrWhiteSpace(command.CustomerName))
                return Result<Guid>.Failure("Customer name is required");

            if (string.IsNullOrWhiteSpace(command.SupplierName))
                return Result<Guid>.Failure("Supplier name is required");

            if (command.Items == null || command.Items.Count == 0)
                return Result<Guid>.Failure("Order must contain at least one item");

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
            await _unitOfWork.SaveChangesAsync();
            
            return Result<Guid>.Success(order.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
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
