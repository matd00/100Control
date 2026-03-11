using System.Collections.ObjectModel;
using System.Windows.Input;
using Application.UseCases.FactoryOrders;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Desktop.Features.Orders;

public class FactoryOrdersViewModel : ViewModelBase
{
    private readonly IFactoryOrderRepository _repository;
    private readonly CreateFactoryOrderUseCase _createUseCase;
    private readonly UpdateFactoryOrderStatusUseCase _updateStatusUseCase;
    private readonly AddTrackingCodeUseCase _addTrackingUseCase;

    private FactoryOrderViewModel? _selectedOrder;
    private string _statusMessage = string.Empty;
    private decimal _metricsTotalRevenue;
    private decimal _metricsTotalCost;
    private decimal _metricsAverageMargin;
    private int _metricsTotalOrders;

    public ObservableCollection<FactoryOrderViewModel> Orders { get; } = new();

    public FactoryOrderViewModel? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                OnPropertyChanged(nameof(HasSelectedOrder));
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
    public ICommand UpdateStatusCommand { get; }
    public ICommand AddTrackingCodeCommand { get; }

    public FactoryOrdersViewModel(
        IFactoryOrderRepository repository,
        CreateFactoryOrderUseCase createUseCase,
        UpdateFactoryOrderStatusUseCase updateStatusUseCase,
        AddTrackingCodeUseCase addTrackingUseCase)
    {
        _repository = repository;
        _createUseCase = createUseCase;
        _updateStatusUseCase = updateStatusUseCase;
        _addTrackingUseCase = addTrackingUseCase;

        RefreshOrdersCommand = new AsyncRelayCommand(LoadOrdersAsync);
        SelectOrderCommand = new RelayCommand<FactoryOrderViewModel>(SelectOrder);
        NewOrderCommand = new RelayCommand(NewOrder);
        SaveOrderCommand = new AsyncRelayCommand(SaveOrderAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        AddItemCommand = new RelayCommand(AddItem);
        RemoveItemCommand = new RelayCommand<FactoryOrderItemViewModel>(RemoveItem);
        UpdateStatusCommand = new AsyncRelayCommand<FactoryOrderStatus>(UpdateStatusAsync);
        AddTrackingCodeCommand = new AsyncRelayCommand<string>(AddTrackingCodeAsync);

        _ = LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        StatusMessage = "Carregando pedidos fábrica...";
        
        try
        {
            var dbOrders = await _repository.GetAllAsync();
            
            Orders.Clear();
            foreach (var o in dbOrders.OrderByDescending(x => x.CreatedAt))
            {
                var vm = new FactoryOrderViewModel();
                vm.LoadFromEntity(o);
                Orders.Add(vm);
            }

            CalculateMetrics();
            
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private void CalculateMetrics()
    {
        MetricsTotalOrders = Orders.Count;
        MetricsTotalRevenue = Orders.Where(o => o.Status != FactoryOrderStatus.Cancelado).Sum(o => o.TotalSalePrice);
        MetricsTotalCost = Orders.Where(o => o.Status != FactoryOrderStatus.Cancelado).Sum(o => o.TotalCost);
        
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
        if (IsEditing) return; // Ignore selection while editing
        SelectedOrder = order;
    }

    private void NewOrder()
    {
        SelectedOrder = new FactoryOrderViewModel
        {
            Channel = OrderSource.Direct
        };
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
    }

    private async Task SaveOrderAsync()
    {
        if (SelectedOrder == null) return;
        
        try
        {
            StatusMessage = "Salvando pedido...";
            
            // Only create is currently implemented via UI
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
            await LoadOrdersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao salvar: {ex.Message}";
        }
    }

    private async Task UpdateStatusAsync(FactoryOrderStatus newStatus)
    {
        if (SelectedOrder == null || SelectedOrder.IsNew) return;
        
        try
        {
            await _updateStatusUseCase.Execute(SelectedOrder.Id, newStatus);
            await LoadOrdersAsync(); // reload to reflect changes
            StatusMessage = "Status atualizado!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }

    private async Task AddTrackingCodeAsync(string code)
    {
        if (SelectedOrder == null || SelectedOrder.IsNew) return;
        
        try
        {
            await _addTrackingUseCase.Execute(SelectedOrder.Id, code);
            await LoadOrdersAsync(); // reload to reflect changes
            StatusMessage = "Rastreio atualizado com sucesso!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }
}
