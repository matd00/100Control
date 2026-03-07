using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Orders;

namespace Desktop.Features.Orders;

public class OrdersViewModel : ViewModelBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly CreateOrderUseCase _createOrderUseCase;
    private readonly UpdateOrderStatusUseCase _updateOrderStatusUseCase;

    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private OrderItemViewModel? _selectedOrder;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public OrderItemViewModel? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    public ObservableCollection<OrderItemViewModel> Orders { get; } = new();
    public ObservableCollection<OrderItemViewModel> PendingOrders { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand MarkAsProcessingCommand { get; }
    public ICommand MarkAsCompletedCommand { get; }
    public ICommand CancelOrderCommand { get; }

    public OrdersViewModel(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        CreateOrderUseCase createOrderUseCase,
        UpdateOrderStatusUseCase updateOrderStatusUseCase)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _createOrderUseCase = createOrderUseCase ?? throw new ArgumentNullException(nameof(createOrderUseCase));
        _updateOrderStatusUseCase = updateOrderStatusUseCase ?? throw new ArgumentNullException(nameof(updateOrderStatusUseCase));

        RefreshCommand = new AsyncRelayCommand(LoadOrdersAsync);
        MarkAsProcessingCommand = new AsyncRelayCommand(MarkAsProcessingAsync, () => SelectedOrder != null);
        MarkAsCompletedCommand = new AsyncRelayCommand(MarkAsCompletedAsync, () => SelectedOrder != null);
        CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync, () => SelectedOrder != null);

        _ = LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            IsLoading = true;
            var orders = await _orderRepository.GetAllAsync();

            Orders.Clear();
            PendingOrders.Clear();

            foreach (var order in orders.OrderByDescending(o => o.CreatedAt))
            {
                var vm = new OrderItemViewModel
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status.ToString(),
                    Source = order.Source.ToString(),
                    ItemsCount = order.Items.Count,
                    CreatedAt = order.CreatedAt
                };

                Orders.Add(vm);

                if (order.Status == OrderStatus.Pending)
                    PendingOrders.Add(vm);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao carregar pedidos: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task MarkAsProcessingAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            await _updateOrderStatusUseCase.Execute(SelectedOrder.Id, (int)OrderStatus.Processing);
            SelectedOrder.Status = OrderStatus.Processing.ToString();
            StatusMessage = "Pedido marcado como em processamento!";
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private async Task MarkAsCompletedAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            await _updateOrderStatusUseCase.Execute(SelectedOrder.Id, (int)OrderStatus.Completed);
            SelectedOrder.Status = OrderStatus.Completed.ToString();
            StatusMessage = "Pedido concluído!";
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private async Task CancelOrderAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            await _updateOrderStatusUseCase.Execute(SelectedOrder.Id, (int)OrderStatus.Cancelled);
            SelectedOrder.Status = OrderStatus.Cancelled.ToString();
            StatusMessage = "Pedido cancelado!";
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }
}

public class OrderItemViewModel : ViewModelBase
{
    private string _status = string.Empty;

    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
    public string Source { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
