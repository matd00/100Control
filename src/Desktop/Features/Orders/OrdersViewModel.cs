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
    private CustomerForOrderViewModel? _selectedCustomer;
    private ProductForOrderViewModel? _selectedProduct;
    private int _quantity = 1;
    private OrderSource _selectedSource = OrderSource.Direct;

    #region Properties

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

    public CustomerForOrderViewModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public ProductForOrderViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public OrderSource SelectedSource
    {
        get => _selectedSource;
        set => SetProperty(ref _selectedSource, value);
    }

    #endregion

    public ObservableCollection<OrderItemViewModel> Orders { get; } = new();
    public ObservableCollection<OrderItemViewModel> PendingOrders { get; } = new();
    public ObservableCollection<CustomerForOrderViewModel> Customers { get; } = new();
    public ObservableCollection<ProductForOrderViewModel> Products { get; } = new();
    public ObservableCollection<OrderSource> OrderSources { get; } = new()
    {
        OrderSource.Direct,
        OrderSource.MercadoLivre,
        OrderSource.Shopee,
        OrderSource.Instagram,
        OrderSource.WhatsApp
    };

    public ICommand RefreshCommand { get; }
    public ICommand CreateOrderCommand { get; }
    public ICommand AddItemToOrderCommand { get; }
    public ICommand MarkAsProcessingCommand { get; }
    public ICommand MarkAsCompletedCommand { get; }
    public ICommand CancelOrderCommand { get; }
    public ICommand DeleteOrderCommand { get; }
    public ICommand SelectOrderCommand { get; }

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

        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        CreateOrderCommand = new AsyncRelayCommand(CreateOrderAsync, () => !IsLoading && SelectedCustomer != null);
        AddItemToOrderCommand = new AsyncRelayCommand(AddItemToOrderAsync, () => !IsLoading && SelectedOrder != null && SelectedProduct != null);
        MarkAsProcessingCommand = new AsyncRelayCommand(MarkAsProcessingAsync, () => SelectedOrder != null);
        MarkAsCompletedCommand = new AsyncRelayCommand(MarkAsCompletedAsync, () => SelectedOrder != null);
        CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync, () => SelectedOrder != null);
        DeleteOrderCommand = new AsyncRelayCommand(DeleteOrderAsync, () => SelectedOrder != null);
        SelectOrderCommand = new RelayCommand<OrderItemViewModel>(SelectOrder);

        _ = LoadDataAsync();
    }

    private void SelectOrder(OrderItemViewModel? order)
    {
        if (order != null)
        {
            SelectedOrder = order;
        }
    }

    private async Task CreateOrderAsync()
    {
        if (SelectedCustomer == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Criando pedido...";

            var order = new Order(SelectedCustomer.Id, SelectedSource);
            await _orderRepository.SaveAsync(order);

            StatusMessage = "✅ Pedido criado! Adicione itens.";
            await LoadDataAsync();

            // Select the new order
            SelectedOrder = Orders.FirstOrDefault(o => o.Id == order.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddItemToOrderAsync()
    {
        if (SelectedOrder == null || SelectedProduct == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Adicionando item...";

            var order = await _orderRepository.GetByIdAsync(SelectedOrder.Id);
            if (order == null)
            {
                StatusMessage = "❌ Pedido não encontrado.";
                return;
            }

            order.AddItem(SelectedProduct.Id, Quantity, SelectedProduct.Price);
            await _orderRepository.UpdateAsync(order);

            StatusMessage = "✅ Item adicionado!";
            Quantity = 1;
            await LoadDataAsync();

            // Reload selected order
            SelectedOrder = Orders.FirstOrDefault(o => o.Id == order.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteOrderAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Removendo pedido...";

            await _orderRepository.DeleteAsync(SelectedOrder.Id);

            StatusMessage = "✅ Pedido removido!";
            SelectedOrder = null;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDataAsync()
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
                    Status = order.Status,
                    Source = order.Source,
                    ItemsCount = order.Items.Count,
                    CreatedAt = order.CreatedAt
                };

                Orders.Add(vm);

                if (order.Status == OrderStatus.Pending)
                    PendingOrders.Add(vm);
            }

            // Load customers
            var customers = await _customerRepository.GetActiveAsync();
            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(new CustomerForOrderViewModel
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Email = customer.Email
                });
            }

            // Load products
            var products = await _productRepository.GetActiveAsync();
            Products.Clear();
            foreach (var product in products)
            {
                Products.Add(new ProductForOrderViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Stock = product.Stock
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao carregar dados: {ex.Message}";
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
            StatusMessage = "✅ Pedido em processamento!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
    }

    private async Task MarkAsCompletedAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            await _updateOrderStatusUseCase.Execute(SelectedOrder.Id, (int)OrderStatus.Completed);
            StatusMessage = "✅ Pedido concluído!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
    }

    private async Task CancelOrderAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            await _updateOrderStatusUseCase.Execute(SelectedOrder.Id, (int)OrderStatus.Cancelled);
            StatusMessage = "✅ Pedido cancelado!";
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
    }
}

public class OrderItemViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public OrderSource Source { get; set; }
    public int ItemsCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TotalFormatted => $"R$ {TotalAmount:N2}";
    public string StatusText => Status switch
    {
        OrderStatus.Pending => "Pendente",
        OrderStatus.Processing => "Processando",
        OrderStatus.Completed => "Concluído",
        OrderStatus.Cancelled => "Cancelado",
        _ => "Desconhecido"
    };
    public string SourceText => Source switch
    {
        OrderSource.Direct => "Direto",
        OrderSource.MercadoLivre => "Mercado Livre",
        OrderSource.Shopee => "Shopee",
        OrderSource.Instagram => "Instagram",
        OrderSource.WhatsApp => "WhatsApp",
        _ => "Desconhecido"
    };
    public string DateFormatted => CreatedAt.ToString("dd/MM/yyyy HH:mm");
}

public class CustomerForOrderViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ProductForOrderViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public string PriceFormatted => $"R$ {Price:N2}";
    public string StockText => $"{Stock} em estoque";
}
