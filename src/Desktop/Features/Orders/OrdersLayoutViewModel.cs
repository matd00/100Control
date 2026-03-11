using Desktop.Infrastructure.MVVM;

namespace Desktop.Features.Orders;

public class OrdersLayoutViewModel : ViewModelBase
{
    public OrdersViewModel NormalOrders { get; }
    public FactoryOrdersViewModel FactoryOrders { get; }

    public OrdersLayoutViewModel(OrdersViewModel normalOrders, FactoryOrdersViewModel factoryOrders)
    {
        NormalOrders = normalOrders;
        FactoryOrders = factoryOrders;
    }
}
