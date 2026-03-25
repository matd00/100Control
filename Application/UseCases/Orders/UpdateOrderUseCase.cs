using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.Orders;

public class UpdateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderUseCase(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Execute(UpdateOrderCommand command)
    {
        try
        {
            if (command == null)
                return Result.Failure("Command cannot be null");

            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            
            if (order == null)
                return Result.Failure("Pedido não encontrado");

            if (order.Status != Domain.Entities.OrderStatus.Pending)
                return Result.Failure("Apenas pedidos pendentes podem ser editados");

            // Update Customer if changed
            if (order.CustomerId != command.CustomerId)
            {
                var customer = await _customerRepository.GetByIdAsync(command.CustomerId);
                if (customer == null)
                    return Result.Failure("Novo cliente não encontrado");
                    
                order.UpdateCustomer(command.CustomerId);
            }

            // Sync Items
            // For simplicity, we remove all existing items and add the new ones.
            var existingItems = order.Items.ToList();
            foreach (var item in existingItems)
            {
                order.RemoveItem(item.ProductId);
            }

            foreach (var itemCommand in command.Items)
            {
                var product = await _productRepository.GetByIdAsync(itemCommand.ProductId);
                if (product == null)
                    return Result.Failure($"Produto {itemCommand.ProductId} não encontrado");

                if (product.Stock < itemCommand.Quantity)
                    return Result.Failure($"Estoque insuficiente para o produto {product.Name}");

                order.AddItem(itemCommand.ProductId, itemCommand.Quantity, itemCommand.UnitPrice);
            }

            await _orderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception)
        {
            return Result.Failure("Erro ao atualizar o pedido.");
        }
    }
}

public class UpdateOrderCommand
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public List<UpdateOrderItemCommand> Items { get; set; } = new();
}

public class UpdateOrderItemCommand
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
