using System.Collections.ObjectModel;
using System.Windows.Input;
using Application.UseCases.FactoryOrders;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Integrations.SuperFrete.Interfaces;

namespace Desktop.Features.Orders;

public class FactoryOrdersViewModel : ViewModelBase
{
    private readonly IFactoryOrderRepository _repository;
    private readonly ICustomerRepository _customerRepository;
    private readonly CreateFactoryOrderUseCase _createUseCase;
    private readonly UpdateFactoryOrderStatusUseCase _updateStatusUseCase;
    private readonly AddTrackingCodeUseCase _addTrackingUseCase;

    private FactoryOrderViewModel? _selectedOrder;
    private string _statusMessage = string.Empty;
    private decimal _metricsTotalRevenue;
    private decimal _metricsTotalCost;
    private decimal _metricsAverageMargin;
    private int _metricsTotalOrders;

    // Customer picker
    private CustomerForFactoryOrderViewModel? _selectedCustomer;
    private string _customerSearchText = string.Empty;

    // Search / filter
    private string _searchText = string.Empty;
    private FactoryOrderStatus? _statusFilter;

    // Tracking code input
    private string _trackingCodeInput = string.Empty;

    public ObservableCollection<FactoryOrderViewModel> AllOrders { get; } = new();
    public ObservableCollection<FactoryOrderViewModel> Orders { get; } = new();
    public ObservableCollection<CustomerForFactoryOrderViewModel> AllCustomers { get; } = new();
    public ObservableCollection<CustomerForFactoryOrderViewModel> FilteredCustomers { get; } = new();

    public FactoryOrderViewModel? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                OnPropertyChanged(nameof(HasSelectedOrder));
                OnPropertyChanged(nameof(CanConfirm));
                OnPropertyChanged(nameof(CanMarkSent));
                OnPropertyChanged(nameof(CanMarkDelivered));
                OnPropertyChanged(nameof(CanCancel));
            }
        }
    }

    public bool HasSelectedOrder => _selectedOrder != null;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    // Metrics
    public decimal MetricsTotalRevenue
    {
        get => _metricsTotalRevenue;
        set => SetProperty(ref _metricsTotalRevenue, value);
    }

    public decimal MetricsTotalCost
    {
        get => _metricsTotalCost;
        set => SetProperty(ref _metricsTotalCost, value);
    }

    public decimal MetricsAverageMargin
    {
        get => _metricsAverageMargin;
        set => SetProperty(ref _metricsAverageMargin, value);
    }

    public int MetricsTotalOrders
    {
        get => _metricsTotalOrders;
        set => SetProperty(ref _metricsTotalOrders, value);
    }

    // Customer picker
    public CustomerForFactoryOrderViewModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                if (value != null && SelectedOrder != null)
                {
                    SelectedOrder.CustomerName = value.Name;
                    SelectedOrder.CustomerContact = !string.IsNullOrEmpty(value.Phone) ? value.Phone : value.Email;
                    SelectedOrder.DeliveryAddress = value.FullAddress;
                }
                OnPropertyChanged(nameof(HasSelectedCustomer));
            }
        }
    }
    public bool HasSelectedCustomer => _selectedCustomer != null;

    public string CustomerSearchText
    {
        get => _customerSearchText;
        set
        {
            if (SetProperty(ref _customerSearchText, value))
            {
                FilterCustomers();
            }
        }
    }

    // Search and filter
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public FactoryOrderStatus? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    // Tracking code
    public string TrackingCodeInput
    {
        get => _trackingCodeInput;
        set => SetProperty(ref _trackingCodeInput, value);
    }

    // Smart status buttons
    public bool CanConfirm => SelectedOrder != null && !SelectedOrder.IsNew && SelectedOrder.Status == FactoryOrderStatus.Pendente;
    public bool CanMarkSent => SelectedOrder != null && !SelectedOrder.IsNew && SelectedOrder.Status == FactoryOrderStatus.Confirmado;
    public bool CanMarkDelivered => SelectedOrder != null && !SelectedOrder.IsNew && SelectedOrder.Status == FactoryOrderStatus.EnviadoPelaFabrica;
    public bool CanCancel => SelectedOrder != null && !SelectedOrder.IsNew && SelectedOrder.Status != FactoryOrderStatus.Entregue && SelectedOrder.Status != FactoryOrderStatus.Cancelado;

    // Creating/Editing Flags
    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    // Commands
    public ICommand RefreshOrdersCommand { get; }
    public ICommand SelectOrderCommand { get; }
    public ICommand NewOrderCommand { get; }
    public ICommand SaveOrderCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand ConfirmOrderCommand { get; }
    public ICommand MarkSentCommand { get; }
    public ICommand MarkDeliveredCommand { get; }
    public ICommand CancelOrderCommand { get; }
    public ICommand AddTrackingCodeCommand { get; }
    public ICommand SyncWithSuperFreteCommand { get; }
    public ICommand DeleteOrderCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand ClearStatusFilterCommand { get; }
    public ICommand SelectCustomerCommand { get; }

    public FactoryOrdersViewModel(
        IFactoryOrderRepository repository,
        ICustomerRepository customerRepository,
        CreateFactoryOrderUseCase createUseCase,
        UpdateFactoryOrderStatusUseCase updateStatusUseCase,
        AddTrackingCodeUseCase addTrackingUseCase,
        ISuperFreteService superFreteService)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _createUseCase = createUseCase;
        _updateStatusUseCase = updateStatusUseCase;
        _addTrackingUseCase = addTrackingUseCase;
        _superFreteService = superFreteService;

        RefreshOrdersCommand = new AsyncRelayCommand(LoadOrdersAsync);
        SelectOrderCommand = new RelayCommand<FactoryOrderViewModel>(SelectOrder);
        NewOrderCommand = new RelayCommand(NewOrder);
        SaveOrderCommand = new AsyncRelayCommand(SaveOrderAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        AddItemCommand = new RelayCommand(AddItem);
        RemoveItemCommand = new RelayCommand<FactoryOrderItemViewModel>(RemoveItem);
        ConfirmOrderCommand = new AsyncRelayCommand(ConfirmOrderAsync);
        MarkSentCommand = new AsyncRelayCommand(MarkSentAsync);
        MarkDeliveredCommand = new AsyncRelayCommand(MarkDeliveredAsync);
        CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync);
        AddTrackingCodeCommand = new AsyncRelayCommand(AddTrackingCodeAsync);
        SyncWithSuperFreteCommand = new AsyncRelayCommand<FactoryOrderViewModel>(SyncWithSuperFreteAsync, (o) => o != null && !string.IsNullOrEmpty(o.TrackingCode));
        DeleteOrderCommand = new AsyncRelayCommand<FactoryOrderViewModel>(DeleteOrderAsync);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        ClearStatusFilterCommand = new RelayCommand(() => StatusFilter = null);
        SelectCustomerCommand = new RelayCommand<CustomerForFactoryOrderViewModel>(c => SelectedCustomer = c);

        _ = LoadDataAsync();
    }

    private readonly ISuperFreteService _superFreteService;

    private async Task SyncWithSuperFreteAsync(FactoryOrderViewModel? orderVm)
    {
        if (orderVm == null || string.IsNullOrEmpty(orderVm.TrackingCode)) return;

        try
        {
            StatusMessage = $"Sincronizando rastreio {orderVm.TrackingCode}...";
            var tracking = await _superFreteService.TrackShipmentAsync(orderVm.TrackingCode);
            
            StatusMessage = $"✅ Status SuperFrete: {tracking.Status}";
            
            if (tracking.Status.Contains("Entregue", StringComparison.OrdinalIgnoreCase))
            {
                await _updateStatusUseCase.Execute(orderVm.Id, FactoryOrderStatus.Entregue);
                await LoadOrdersAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao sincronizar: {ex.Message}";
        }
    }

    private async Task DeleteOrderAsync(FactoryOrderViewModel? orderVm)
    {
        if (orderVm == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Deseja excluir o pedido fábrica de {orderVm.CustomerName}?\nEsta ação removerá o registro permanentemente.",
            "Confirmar Exclusão",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                await _repository.DeleteAsync(orderVm.Id);
                StatusMessage = "✅ Pedido removido.";
                await LoadOrdersAsync();
                if (SelectedOrder?.Id == orderVm.Id) SelectedOrder = null;
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erro ao remover: {ex.Message}";
            }
        }
    }

    private async Task LoadDataAsync()
    {
        await LoadCustomersAsync();
        await LoadOrdersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customers = await _customerRepository.GetAllAsync();
            AllCustomers.Clear();
            FilteredCustomers.Clear();
            foreach (var c in customers.Where(c => c.IsActive).OrderBy(c => c.Name))
            {
                var vm = new CustomerForFactoryOrderViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Email = c.Email,
                    Document = c.Document,
                    FullAddress = BuildFullAddress(c)
                };
                AllCustomers.Add(vm);
                FilteredCustomers.Add(vm);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading customers: {ex.Message}");
        }
    }

    private static string BuildFullAddress(Customer c)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(c.Address)) parts.Add(c.Address);
        if (!string.IsNullOrWhiteSpace(c.Number)) parts.Add(c.Number);
        if (!string.IsNullOrWhiteSpace(c.Complement)) parts.Add(c.Complement);
        if (!string.IsNullOrWhiteSpace(c.District)) parts.Add(c.District);
        if (!string.IsNullOrWhiteSpace(c.City) && !string.IsNullOrWhiteSpace(c.State))
            parts.Add($"{c.City}/{c.State}");
        if (!string.IsNullOrWhiteSpace(c.ZipCode)) parts.Add($"CEP {c.ZipCode}");
        return string.Join(", ", parts);
    }

    private void FilterCustomers()
    {
        FilteredCustomers.Clear();
        var search = CustomerSearchText?.ToLowerInvariant() ?? "";
        foreach (var c in AllCustomers)
        {
            if (string.IsNullOrEmpty(search) ||
                c.Name.ToLowerInvariant().Contains(search) ||
                c.Phone.ToLowerInvariant().Contains(search) ||
                c.Email.ToLowerInvariant().Contains(search))
            {
                FilteredCustomers.Add(c);
            }
        }
    }

    private async Task LoadOrdersAsync()
    {
        StatusMessage = "Carregando pedidos fábrica...";
        
        try
        {
            var dbOrders = await _repository.GetAllAsync();
            
            AllOrders.Clear();
            foreach (var o in dbOrders.OrderByDescending(x => x.CreatedAt))
            {
                var vm = new FactoryOrderViewModel();
                vm.LoadFromEntity(o);
                AllOrders.Add(vm);
            }

            ApplyFilters();
            CalculateMetrics();
            
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private void ApplyFilters()
    {
        Orders.Clear();
        var search = SearchText?.ToLowerInvariant() ?? "";

        foreach (var o in AllOrders)
        {
            // Status filter
            if (StatusFilter.HasValue && o.Status != StatusFilter.Value) continue;

            // Text search
            if (!string.IsNullOrEmpty(search))
            {
                if (!o.CustomerName.ToLowerInvariant().Contains(search) &&
                    !o.SupplierName.ToLowerInvariant().Contains(search) &&
                    !(o.TrackingCode?.ToLowerInvariant().Contains(search) ?? false))
                    continue;
            }

            Orders.Add(o);
        }
    }

    private void CalculateMetrics()
    {
        MetricsTotalOrders = AllOrders.Count;
        MetricsTotalRevenue = AllOrders.Where(o => o.Status != FactoryOrderStatus.Cancelado).Sum(o => o.TotalSalePrice);
        MetricsTotalCost = AllOrders.Where(o => o.Status != FactoryOrderStatus.Cancelado).Sum(o => o.TotalCost);
        
        if (MetricsTotalRevenue > 0)
        {
            MetricsAverageMargin = ((MetricsTotalRevenue - MetricsTotalCost) / MetricsTotalRevenue) * 100;
        }
        else
        {
            MetricsAverageMargin = 0;
        }
    }

    private void SelectOrder(FactoryOrderViewModel? order)
    {
        if (IsEditing) return;
        SelectedOrder = order;
    }

    private void NewOrder()
    {
        SelectedOrder = new FactoryOrderViewModel
        {
            Channel = OrderSource.Direct
        };
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        IsEditing = true;
    }

    private void AddItem()
    {
        if (SelectedOrder == null) return;
        SelectedOrder.Items.Add(new FactoryOrderItemViewModel
        {
            Quantity = 1,
            UnitCost = 0,
            UnitSalePrice = 0
        });
    }

    private void RemoveItem(FactoryOrderItemViewModel? item)
    {
        if (SelectedOrder == null || item == null) return;
        SelectedOrder.Items.Remove(item);
    }

    private void CancelEdit()
    {
        IsEditing = false;
        SelectedOrder = null;
        SelectedCustomer = null;
    }

    private async Task SaveOrderAsync()
    {
        if (SelectedOrder == null) return;
        
        try
        {
            StatusMessage = "Salvando pedido...";
            
            if (SelectedOrder.IsNew)
            {
                var command = new CreateFactoryOrderCommand
                {
                    CustomerName = SelectedOrder.CustomerName,
                    CustomerContact = SelectedOrder.CustomerContact,
                    DeliveryAddress = SelectedOrder.DeliveryAddress,
                    SupplierName = SelectedOrder.SupplierName,
                    SupplierContact = SelectedOrder.SupplierContact,
                    Channel = SelectedOrder.Channel,
                    Notes = SelectedOrder.Notes,
                    Items = SelectedOrder.Items.Select(i => new CreateFactoryOrderItemCommand
                    {
                        Description = i.Description,
                        Quantity = i.Quantity,
                        UnitCost = i.UnitCost,
                        UnitSalePrice = i.UnitSalePrice
                    }).ToList()
                };

                await _createUseCase.Execute(command);
            }

            StatusMessage = "Pedido salvo com sucesso!";
            IsEditing = false;
            SelectedOrder = null;
            SelectedCustomer = null;
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao salvar: {ex.Message}";
        }
    }

    private async Task ConfirmOrderAsync()
    {
        if (SelectedOrder == null || SelectedOrder.IsNew) return;
        try
        {
            await _updateStatusUseCase.Execute(SelectedOrder.Id, FactoryOrderStatus.Confirmado);
            StatusMessage = "✅ Pedido confirmado!";
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private async Task MarkSentAsync()
    {
        if (SelectedOrder == null || SelectedOrder.IsNew) return;
        if (string.IsNullOrWhiteSpace(TrackingCodeInput))
        {
            StatusMessage = "⚠️ Informe o código de rastreio para marcar como enviado.";
            return;
        }
        try
        {
            await _addTrackingUseCase.Execute(SelectedOrder.Id, TrackingCodeInput.Trim());
            StatusMessage = "📦 Pedido marcado como enviado pela fábrica!";
            TrackingCodeInput = string.Empty;
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private async Task MarkDeliveredAsync()
    {
        if (SelectedOrder == null || SelectedOrder.IsNew) return;
        try
        {
            await _updateStatusUseCase.Execute(SelectedOrder.Id, FactoryOrderStatus.Entregue);
            StatusMessage = "✅ Pedido entregue!";
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private async Task CancelOrderAsync()
    {
        if (SelectedOrder == null || SelectedOrder.IsNew) return;
        try
        {
            await _updateStatusUseCase.Execute(SelectedOrder.Id, FactoryOrderStatus.Cancelado);
            StatusMessage = "Pedido cancelado.";
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private async Task AddTrackingCodeAsync()
    {
        if (SelectedOrder == null || SelectedOrder.IsNew) return;
        if (string.IsNullOrWhiteSpace(TrackingCodeInput))
        {
            StatusMessage = "Informe o código de rastreio.";
            return;
        }
        try
        {
            await _addTrackingUseCase.Execute(SelectedOrder.Id, TrackingCodeInput.Trim());
            StatusMessage = "Rastreio atualizado com sucesso!";
            TrackingCodeInput = string.Empty;
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }
}
