using Domain.Interfaces.Repositories;

namespace Application.UseCases.Orders;

public class UpdateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;

    public UpdateOrderUseCase(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
    }

    public async Task<UpdateOrderResult> Execute(UpdateOrderCommand command)
    {
        try
        {
            if (command == null)
                return UpdateOrderResult.Failure("Command cannot be null");

            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            
            if (order == null)
                return UpdateOrderResult.Failure("Pedido não encontrado");

            if (order.Status != Domain.Entities.OrderStatus.Pending)
                return UpdateOrderResult.Failure("Apenas pedidos pendentes podem ser editados");

            // Update Customer if changed
            if (order.CustomerId != command.CustomerId)
            {
                var customer = await _customerRepository.GetByIdAsync(command.CustomerId);
                if (customer == null)
                    return UpdateOrderResult.Failure("Novo cliente não encontrado");
                    
                order.UpdateCustomer(command.CustomerId);
            }

            // Sync Items
            // For simplicity, we remove all existing items and add the new ones.
            // A more complex implementation would calculate the diff.
            var existingItems = order.Items.ToList();
            foreach (var item in existingItems)
            {
                order.RemoveItem(item.ProductId);
            }

            foreach (var itemCommand in command.Items)
            {
                var product = await _productRepository.GetByIdAsync(itemCommand.ProductId);
                if (product == null)
                    return UpdateOrderResult.Failure($"Produto {itemCommand.ProductId} não encontrado");

                if (product.Stock < itemCommand.Quantity)
                    return UpdateOrderResult.Failure($"Estoque insuficiente para o produto {product.Name}");

                order.AddItem(itemCommand.ProductId, itemCommand.Quantity, itemCommand.UnitPrice);
            }

            await _orderRepository.UpdateAsync(order);
            return UpdateOrderResult.SuccessResult();
        }
        catch (ArgumentException ex)
        {
            return UpdateOrderResult.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return UpdateOrderResult.Failure(ex.Message);
        }
        catch (Exception)
        {
            return UpdateOrderResult.Failure("Erro ao atualizar o pedido.");
        }
    }
}

public class UpdateOrderResult
{
    public bool Success { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;

    public static UpdateOrderResult SuccessResult() => new() { Success = true };
    public static UpdateOrderResult Failure(string error) => new() { Success = false, ErrorMessage = error };
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
