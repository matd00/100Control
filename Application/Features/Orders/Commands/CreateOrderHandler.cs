using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Features.Orders.Commands;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(command.CustomerId);
            if (customer == null)
                return Result<Guid>.Failure("Customer not found");

            if (!customer.IsActive)
                return Result<Guid>.Failure("Customer account is inactive");

            var order = new Order(command.CustomerId, command.Source, command.IsDropshipping);

            foreach (var item in command.Items)
            {
                if (item.ProductId == Guid.Empty)
                    return Result<Guid>.Failure("Product ID cannot be empty");

                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    return Result<Guid>.Failure("Product not found");

                if (!product.IsActive)
                    return Result<Guid>.Failure($"Product '{product.Name}' is no longer available");

                if (!command.IsDropshipping)
                {
                    if (product.Stock < item.Quantity)
                        return Result<Guid>.Failure($"Insufficient stock for '{product.Name}'. Available: {product.Stock}, Requested: {item.Quantity}");

                    product.DecreaseStock(item.Quantity);
                    
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

            if (order.Items.Count > 100)
                return Result<Guid>.Failure("Order cannot contain more than 100 items");

            await _orderRepository.SaveAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result<Guid>.Success(order.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"An error occurred while creating the order: {ex.Message}");
        }
    }
}
