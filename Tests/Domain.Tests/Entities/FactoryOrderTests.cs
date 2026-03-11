using System;
using System.Linq;
using Domain.Entities;
using Xunit;

namespace Domain.Tests.Entities;

public class FactoryOrderTests
{
    private FactoryOrder CreateOrder()
    {
        return new FactoryOrder(
            "Cliente 1", "email@test.com", "Rua X", 
            "Fornecedor Z", "4444", (Domain.Entities.OrderSource)1, "Notas"
        );
    }

    [Fact]
    public void Constructor_Initialization_ShouldSetEmptyListsAndPendenteStatus()
    {
        var order = CreateOrder();

        Assert.Equal("Cliente 1", order.CustomerName);
        Assert.Equal(FactoryOrderStatus.Pendente, order.Status);
        Assert.Empty(order.Items);
        Assert.Equal(0, order.TotalCost);
        Assert.Equal(0, order.TotalSalePrice);
        Assert.Equal(0, order.Margin);
        Assert.Null(order.TrackingCode);
    }

    [Fact]
    public void AddItem_ShouldRecalculateTotalsAndMargin()
    {
        var order = CreateOrder();
        
        // Cost 50, Sale 100, Qty 2 -> TotalCost 100, TotalSale 200, Profit 100, Margin 50%
        order.AddItem("Item A", 2, 50m, 100m);

        Assert.Single(order.Items);
        Assert.Equal(100m, order.TotalCost);
        Assert.Equal(200m, order.TotalSalePrice);
        Assert.Equal(50m, order.Margin);
    }

    [Fact]
    public void RemoveItem_ShouldRecalculateTotals()
    {
        var order = CreateOrder();
        
        order.AddItem("Item A", 1, 10m, 20m);
        order.AddItem("Item B", 1, 30m, 60m);
        
        Assert.Equal(40m, order.TotalCost);
        Assert.Equal(80m, order.TotalSalePrice);

        var firstItem = order.Items.First();
        order.RemoveItem(firstItem.Id);

        Assert.Single(order.Items);
        Assert.Equal(30m, order.TotalCost);
        Assert.Equal(60m, order.TotalSalePrice);
        Assert.Equal(50m, order.Margin);
    }
    
    [Fact]
    public void Confirm_WhenPendente_ShouldChangeStatusToConfirmado()
    {
        var order = CreateOrder();
        order.Confirm();
        Assert.Equal(FactoryOrderStatus.Confirmado, order.Status);
    }

    [Fact]
    public void Confirm_WhenNotPendente_ShouldThrowException()
    {
        var order = CreateOrder();
        order.Confirm(); // status is now Confirmado

        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    [Fact]
    public void MarkAsSentByFactory_WhenConfirmado_ShouldUpdateStatusAndTrackingCode()
    {
        var order = CreateOrder();
        order.Confirm();
        order.MarkAsSentByFactory("TRACK123");

        Assert.Equal(FactoryOrderStatus.EnviadoPelaFabrica, order.Status);
        Assert.Equal("TRACK123", order.TrackingCode);
    }

    [Fact]
    public void MarkAsDelivered_WhenSent_ShouldUpdateStatus()
    {
        var order = CreateOrder();
        order.Confirm();
        order.MarkAsSentByFactory("TRACK123");
        order.MarkAsDelivered();

        Assert.Equal(FactoryOrderStatus.Entregue, order.Status);
    }

    [Fact]
    public void Cancel_WhenPendente_ShouldUpdateStatusToCancelado()
    {
        var order = CreateOrder();
        order.Cancel();

        Assert.Equal(FactoryOrderStatus.Cancelado, order.Status);
    }
}
