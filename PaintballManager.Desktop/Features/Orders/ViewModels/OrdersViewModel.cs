using PaintballManager.Application.UseCases.Orders;
using PaintballManager.Desktop.Infrastructure.MVVM;

namespace PaintballManager.Desktop.Features.Orders.ViewModels;

public class OrdersViewModel : ViewModelBase
{
    private readonly CreateOrderUseCase _createOrderUseCase;
    private readonly UpdateOrderStatusUseCase _updateOrderStatusUseCase;

    private List<OrderItemViewModel> _orders;
    public List<OrderItemViewModel> Orders
    {
        get => _orders;
        set => SetProperty(ref _orders, value);
    }

    private OrderItemViewModel _selectedOrder;
    public OrderItemViewModel SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    public RelayCommand LoadOrdersCommand { get; }
    public RelayCommand<OrderItemViewModel> MarkAsProcessingCommand { get; }
    public RelayCommand<OrderItemViewModel> MarkAsCompletedCommand { get; }

    public OrdersViewModel(
        CreateOrderUseCase createOrderUseCase,
        UpdateOrderStatusUseCase updateOrderStatusUseCase)
    {
        _createOrderUseCase = createOrderUseCase;
        _updateOrderStatusUseCase = updateOrderStatusUseCase;
        Orders = new List<OrderItemViewModel>();

        LoadOrdersCommand = new RelayCommand(async _ => await LoadOrders());
        MarkAsProcessingCommand = new RelayCommand<OrderItemViewModel>(async order => await MarkAsProcessing(order));
        MarkAsCompletedCommand = new RelayCommand<OrderItemViewModel>(async order => await MarkAsCompleted(order));
    }

    private async Task LoadOrders()
    {
        // TODO: Load orders from repository
        await Task.CompletedTask;
    }

    private async Task MarkAsProcessing(OrderItemViewModel order)
    {
        await _updateOrderStatusUseCase.Execute(order.Id, 2);
        order.Status = "Processing";
    }

    private async Task MarkAsCompleted(OrderItemViewModel order)
    {
        await _updateOrderStatusUseCase.Execute(order.Id, 3);
        order.Status = "Completed";
    }
}

public class OrderItemViewModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
