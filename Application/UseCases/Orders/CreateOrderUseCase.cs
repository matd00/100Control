using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.Orders;

public class CreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;

    public CreateOrderUseCase(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
    }

    public async Task Execute(CreateOrderCommand command)
    {
        try
        {
            // Security: Input validation
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (command.CustomerId == Guid.Empty)
                throw new ArgumentException("Customer ID is required");

            if (command.Items == null || command.Items.Count == 0)
                throw new ArgumentException("Order must contain at least one item");

            // Validate customer exists
            var customer = await _customerRepository.GetByIdAsync(command.CustomerId);
            if (customer == null)
                throw new InvalidOperationException("Customer not found");

            if (!customer.IsActive)
                throw new InvalidOperationException("Customer account is inactive");

            var order = new Order(command.CustomerId, command.Source, command.IsDropshipping);

            // Add items with validation and stock check
            foreach (var item in command.Items)
            {
                if (item.ProductId == Guid.Empty)
                    throw new ArgumentException("Product ID cannot be empty");

                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new InvalidOperationException("Product not found");

                if (!product.IsActive)
                    throw new InvalidOperationException($"Product '{product.Name}' is no longer available");

                // Stock management: only for non-dropshipping orders
                if (!command.IsDropshipping)
                {
                    // SECURITY FIX: Check stock availability
                    if (product.Stock < item.Quantity)
                        throw new InvalidOperationException(
                            $"Insufficient stock for '{product.Name}'. Available: {product.Stock}, Requested: {item.Quantity}");

                    // Decrease stock
                    product.DecreaseStock(item.Quantity);
                    
                    // Log inventory movement
                    var movement = new InventoryMovement(
                        product.Id,
                        InventoryMovementType.Sale,
                        item.Quantity,
                        order.Id.ToString(),
                        $"Venda (Pedido {order.Id}) para {customer.Name}"
                    );

                    await _productRepository.UpdateAsync(product);
                    await _inventoryMovementRepository.SaveAsync(movement);
                }

                order.AddItem(item.ProductId, item.Quantity, product.Price);
            }

            // Limit total items per order
            if (order.Items.Count > 100)
                throw new InvalidOperationException("Order cannot contain more than 100 items");

            await _orderRepository.SaveAsync(order);
        }
        catch (ArgumentException)
        {
            throw;  // Re-throw validation errors
        }
        catch (InvalidOperationException)
        {
            throw;  // Re-throw business logic errors
        }
        catch (Exception ex)
        {
            // Security: Don't expose internal exception details
            throw new InvalidOperationException("An error occurred while creating the order. Please try again later.");
        }
    }
}

public class CreateOrderCommand
{
    public Guid CustomerId { get; set; }
    public OrderSource Source { get; set; }
    public bool IsDropshipping { get; set; }
    public List<CreateOrderItemCommand> Items { get; set; } = new();
}

public class CreateOrderItemCommand
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
