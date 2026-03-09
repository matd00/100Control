using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Shipments;
using Integrations.SuperFrete.Interfaces;

namespace Desktop.Features.Shipments;

public class ShipmentsViewModel : ViewModelBase
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly GenerateShipmentUseCase _generateShipmentUseCase;
    private readonly ISuperFreteService _superFreteService;

    private OrderForShipmentViewModel? _selectedOrder;
    private ShipmentProvider _selectedProvider = ShipmentProvider.SuperFrete;
    private ShipmentItemViewModel? _selectedShipment;
    private string _trackingNumber = string.Empty;
    private decimal _shippingCost;
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    #region Properties

    public OrderForShipmentViewModel? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    public ShipmentProvider SelectedProvider
    {
        get => _selectedProvider;
        set => SetProperty(ref _selectedProvider, value);
    }

    public ShipmentItemViewModel? SelectedShipment
    {
        get => _selectedShipment;
        set => SetProperty(ref _selectedShipment, value);
    }

    public string TrackingNumber
    {
        get => _trackingNumber;
        set => SetProperty(ref _trackingNumber, value);
    }

    public decimal ShippingCost
    {
        get => _shippingCost;
        set => SetProperty(ref _shippingCost, value);
    }

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

    #endregion

    public ObservableCollection<ShipmentItemViewModel> Shipments { get; } = new();
    public ObservableCollection<OrderForShipmentViewModel> PendingOrders { get; } = new();
    public ObservableCollection<ShipmentProvider> Providers { get; } = new()
    {
        ShipmentProvider.SuperFrete,
        ShipmentProvider.Transportadora,
        ShipmentProvider.DropShipping
    };

    public ICommand CreateShipmentCommand { get; }
    public ICommand GenerateLabelCommand { get; }
    public ICommand MarkAsShippedCommand { get; }
    public ICommand MarkAsDeliveredCommand { get; }
    public ICommand CancelShipmentCommand { get; }
    public ICommand TrackShipmentCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectShipmentCommand { get; }

    public ShipmentsViewModel(
        IShipmentRepository shipmentRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        GenerateShipmentUseCase generateShipmentUseCase,
        ISuperFreteService superFreteService)
    {
        _shipmentRepository = shipmentRepository ?? throw new ArgumentNullException(nameof(shipmentRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _generateShipmentUseCase = generateShipmentUseCase ?? throw new ArgumentNullException(nameof(generateShipmentUseCase));
        _superFreteService = superFreteService ?? throw new ArgumentNullException(nameof(superFreteService));

        CreateShipmentCommand = new AsyncRelayCommand(CreateShipmentAsync, () => !IsLoading && SelectedOrder != null);
        GenerateLabelCommand = new AsyncRelayCommand(GenerateLabelAsync, () => !IsLoading && SelectedShipment != null);
        MarkAsShippedCommand = new AsyncRelayCommand(MarkAsShippedAsync, () => !IsLoading && SelectedShipment != null);
        MarkAsDeliveredCommand = new AsyncRelayCommand(MarkAsDeliveredAsync, () => !IsLoading && SelectedShipment != null);
        CancelShipmentCommand = new AsyncRelayCommand(CancelShipmentAsync, () => !IsLoading && SelectedShipment != null);
        TrackShipmentCommand = new AsyncRelayCommand(TrackShipmentAsync, () => !IsLoading && SelectedShipment != null);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        SelectShipmentCommand = new RelayCommand<ShipmentItemViewModel>(SelectShipment);

        _ = LoadDataAsync();
    }

    private void SelectShipment(ShipmentItemViewModel? shipment)
    {
        if (shipment != null)
        {
            SelectedShipment = shipment;
            TrackingNumber = shipment.TrackingNumber;
            ShippingCost = shipment.ShippingCost;
        }
    }

    private async Task CreateShipmentAsync()
    {
        if (SelectedOrder == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Criando envio...";

            var shipment = new Shipment(SelectedOrder.Id, SelectedProvider);
            
            // Add items from order
            var order = await _orderRepository.GetByIdAsync(SelectedOrder.Id);
            if (order != null)
            {
                foreach (var item in order.Items)
                {
                    shipment.AddItem(item.ProductId, item.Quantity);
                }
            }

            await _shipmentRepository.SaveAsync(shipment);

            StatusMessage = "✅ Envio criado! Gere a etiqueta.";
            await LoadDataAsync();
            
            // Select the new shipment
            SelectedShipment = Shipments.FirstOrDefault(s => s.Id == shipment.Id);
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

    private async Task GenerateLabelAsync()
    {
        if (SelectedShipment == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Gerando etiqueta...";

            var shipment = await _shipmentRepository.GetByIdAsync(SelectedShipment.Id);
            if (shipment == null)
            {
                StatusMessage = "❌ Envio não encontrado.";
                return;
            }

            // Generate label with SuperFrete or manual
            if (string.IsNullOrWhiteSpace(TrackingNumber))
            {
                // Auto-generate tracking number
                TrackingNumber = $"BR{Guid.NewGuid().ToString()[..8].ToUpper()}";
            }

            shipment.GenerateLabel(TrackingNumber, ShippingCost);
            await _shipmentRepository.UpdateAsync(shipment);

            StatusMessage = $"✅ Etiqueta gerada! Código: {TrackingNumber}";
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

    private async Task MarkAsShippedAsync()
    {
        if (SelectedShipment == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Marcando como enviado...";

            var shipment = await _shipmentRepository.GetByIdAsync(SelectedShipment.Id);
            if (shipment == null)
            {
                StatusMessage = "❌ Envio não encontrado.";
                return;
            }

            shipment.MarkAsShipped();
            await _shipmentRepository.UpdateAsync(shipment);

            StatusMessage = "✅ Envio marcado como enviado!";
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

    private async Task MarkAsDeliveredAsync()
    {
        if (SelectedShipment == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Marcando como entregue...";

            var shipment = await _shipmentRepository.GetByIdAsync(SelectedShipment.Id);
            if (shipment == null)
            {
                StatusMessage = "❌ Envio não encontrado.";
                return;
            }

            shipment.MarkAsDelivered();
            await _shipmentRepository.UpdateAsync(shipment);

            StatusMessage = "✅ Envio marcado como entregue!";
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

    private async Task CancelShipmentAsync()
    {
        if (SelectedShipment == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Cancelando envio...";

            var shipment = await _shipmentRepository.GetByIdAsync(SelectedShipment.Id);
            if (shipment == null)
            {
                StatusMessage = "❌ Envio não encontrado.";
                return;
            }

            shipment.Cancel();
            await _shipmentRepository.UpdateAsync(shipment);

            StatusMessage = "✅ Envio cancelado!";
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

    private async Task TrackShipmentAsync()
    {
        if (SelectedShipment == null || string.IsNullOrWhiteSpace(SelectedShipment.TrackingNumber)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Rastreando envio...";

            var tracking = await _superFreteService.TrackShipmentAsync(SelectedShipment.TrackingNumber);
            
            StatusMessage = $"📦 Status: {tracking.Status} - {tracking.Location} ({tracking.LastUpdate:dd/MM/yyyy HH:mm})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao rastrear: {ex.Message}";
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

            // Load shipments
            var shipments = await _shipmentRepository.GetAllAsync();
            Shipments.Clear();
            foreach (var shipment in shipments.OrderByDescending(s => s.CreatedAt))
            {
                Shipments.Add(new ShipmentItemViewModel
                {
                    Id = shipment.Id,
                    OrderId = shipment.OrderId,
                    Provider = shipment.Provider,
                    Status = shipment.Status,
                    TrackingNumber = shipment.TrackingNumber,
                    ShippingCost = shipment.ShippingCost,
                    ItemCount = shipment.Items.Count,
                    CreatedAt = shipment.CreatedAt,
                    ShippedAt = shipment.ShippedAt,
                    DeliveredAt = shipment.DeliveredAt
                });
            }

            // Load pending orders (orders that don't have shipments yet)
            var orders = await _orderRepository.GetByStatusAsync(OrderStatus.Processing);
            var ordersWithShipments = shipments.Select(s => s.OrderId).ToHashSet();
            
            PendingOrders.Clear();
            foreach (var order in orders.Where(o => !ordersWithShipments.Contains(o.Id)))
            {
                PendingOrders.Add(new OrderForShipmentViewModel
                {
                    Id = order.Id,
                    TotalAmount = order.TotalAmount,
                    ItemCount = order.Items.Count,
                    CreatedAt = order.CreatedAt
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
}

public class ShipmentItemViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public ShipmentProvider Provider { get; set; }
    public ShipmentStatus Status { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public string CostFormatted => $"R$ {ShippingCost:N2}";
    public string ProviderText => Provider switch
    {
        ShipmentProvider.SuperFrete => "SuperFrete",
        ShipmentProvider.Transportadora => "Transportadora",
        ShipmentProvider.DropShipping => "Drop Shipping",
        _ => "Desconhecido"
    };
    public string StatusText => Status switch
    {
        ShipmentStatus.Pending => "Pendente",
        ShipmentStatus.Processing => "Processando",
        ShipmentStatus.Shipped => "Enviado",
        ShipmentStatus.Delivered => "Entregue",
        ShipmentStatus.Cancelled => "Cancelado",
        _ => "Desconhecido"
    };
    public string DateFormatted => CreatedAt.ToString("dd/MM/yyyy");
}

public class OrderForShipmentViewModel
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TotalFormatted => $"R$ {TotalAmount:N2}";
    public string Description => $"Pedido de {CreatedAt:dd/MM} - {ItemCount} item(s) - {TotalFormatted}";
}
