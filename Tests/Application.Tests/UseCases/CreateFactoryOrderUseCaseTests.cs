using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.UseCases.FactoryOrders;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Moq;
using Xunit;

namespace Application.Tests.UseCases;

public class CreateFactoryOrderUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ValidCommand_ShouldSaveAndReturnId()
    {
        // Arrange
        var mockRepo = new Mock<IFactoryOrderRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var command = new CreateFactoryOrderCommand
        {
            CustomerName = "Cliente A",
            CustomerContact = "Contato A",
            DeliveryAddress = "Endereco A", 
            SupplierName = "Fábrica B",
            SupplierContact = "Contato B",
            Notes = "Notas",
            Channel = (Domain.Entities.OrderSource)1,
            Items = new List<CreateFactoryOrderItemCommand>
            {
                new CreateFactoryOrderItemCommand { Description = "Produto X", Quantity = 1, UnitCost = 10m, UnitSalePrice = 20m }
            }
        };
        
        var useCase = new CreateFactoryOrderUseCase(mockRepo.Object, mockUnitOfWork.Object);

        // Act
        var resultId = await useCase.Execute(command);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId.Value);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<FactoryOrder>()), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyItems_ShouldReturnFailure()
    {
        // Arrange
        var mockRepo = new Mock<IFactoryOrderRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var command = new CreateFactoryOrderCommand
        {
            CustomerName = "Cliente A",
            CustomerContact = "Contato A",
            DeliveryAddress = "Endereco A", 
            SupplierName = "Fábrica B",
            SupplierContact = "Contato B",
            Notes = "Notas",
            Channel = (Domain.Entities.OrderSource)1,
            Items = new List<CreateFactoryOrderItemCommand>() // Empty list
        };
        
        var useCase = new CreateFactoryOrderUseCase(mockRepo.Object, mockUnitOfWork.Object);

        // Act
        var result = await useCase.Execute(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order must contain at least one item", result.Error);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<FactoryOrder>()), Times.Never);
    }
}
