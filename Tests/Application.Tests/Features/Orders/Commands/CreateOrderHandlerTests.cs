using Application.Features.Orders.Commands;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Moq;
using Xunit;

namespace Application.Tests.Features.Orders.Commands;

public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IInventoryMovementRepository> _inventoryMovementRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _inventoryMovementRepositoryMock = new Mock<IInventoryMovementRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateOrderHandler(
            _orderRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _productRepositoryMock.Object,
            _inventoryMovementRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var customer = new Customer("John Doe", "john@example.com", "551199999999", "12345678900");
        var product = new Product("Paintball Gun", "Equipamento", "A cool gun", 50m, 100m);
        product.IncreaseStock(10);
        
        _customerRepositoryMock.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Source = OrderSource.Direct,
            Items = new List<CreateOrderItemCommand>
            {
                new CreateOrderItemCommand { ProductId = productId, Quantity = 2 }
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _orderRepositoryMock.Verify(x => x.SaveAsync(It.IsAny<Order>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ShouldReturnFailure()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var customer = new Customer("John Doe", "john@example.com", "551199999999", "12345678900");
        var product = new Product("Paintball Gun", "Equipamento", "A cool gun", 50m, 100m);
        product.IncreaseStock(1); // Only 1 in stock
        
        _customerRepositoryMock.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);
        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);

        var command = new CreateOrderCommand
        {
            CustomerId = customerId,
            Source = OrderSource.Direct,
            Items = new List<CreateOrderItemCommand>
            {
                new CreateOrderItemCommand { ProductId = productId, Quantity = 2 } // Request 2
            }
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Insufficient stock", result.Error);
    }
}
