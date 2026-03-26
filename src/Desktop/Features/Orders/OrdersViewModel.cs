using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using Application.UseCases.Orders;
using Application.UseCases.Products;
using Integrations.SuperFrete.Configuration;
using Integrations.SuperFrete.Interfaces;
using Integrations.SuperFrete.Models;
using MaterialDesignThemes.Wpf;

namespace Desktop.Features.Orders
{
    public class OrdersViewModel : ViewModelBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IProductRepository _productRepository;
        private readonly IShipmentRepository _shipmentRepository;
        private readonly ISuperFreteService _superFreteService;
        private readonly SuperFreteSettings _superFreteSettings;
        private readonly IUnitOfWork _unitOfWork;

        private int _currentStep = 1;
        private OrderItemViewModel? _selectedOrder;
        private CustomerForOrderViewModel? _selectedCustomer;
        private ShippingQuoteViewModel? _selectedShipping;
        private bool _isCalculatingShipping;
        private string _statusMessage = string.Empty;
        private string _searchText = string.Empty;
        private decimal _calculatedWeight;
        private int _calculatedWidth;
        private int _calculatedHeight;
        private int _calculatedLength;
        private string _shippingRuleApplied = string.Empty;
        private bool _isManualDimensionsMode;
        private string _originPostalCode = string.Empty;
        private string _categoryFilter = "Todas Categorias";

        public string CategoryFilter
        {
            get => _categoryFilter;
            set
            {
                if (SetProperty(ref _categoryFilter, value))
                {
                    FilterOrders();
                }
            }
        }

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    OnPropertyChanged(nameof(IsStep1));
                    OnPropertyChanged(nameof(IsStep2));
                    OnPropertyChanged(nameof(IsStep3));
                }
            }
        }

        public bool IsStep1 => CurrentStep == 1;
        public bool IsStep2 => CurrentStep == 2;
        public bool IsStep3 => CurrentStep == 3;
        public bool Step1Completed => SelectedCustomer != null;
        public bool Step2Completed => OrderItems.Count > 0;
        public bool Step3Completed => SelectedShipping != null;
        public bool HasSelectedOrder => _selectedOrder != null;
        public bool HasSelectedCustomer => _selectedCustomer != null;
        public bool HasOrderItems => OrderItems.Count > 0;
        public bool HasSelectedShipping => _selectedShipping != null;

        public OrderItemViewModel? SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (SetProperty(ref _selectedOrder, value))
                {
                    OnPropertyChanged(nameof(HasSelectedOrder));
                    if (value != null) LoadOrderDetails(value);
                }
            }
        }

        public CustomerForOrderViewModel? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    OnPropertyChanged(nameof(HasSelectedCustomer));
                    OnPropertyChanged(nameof(Step1Completed));
                }
            }
        }

        public ShippingQuoteViewModel? SelectedShipping
        {
            get => _selectedShipping;
            set
            {
                if (SetProperty(ref _selectedShipping, value))
                {
                    OnPropertyChanged(nameof(HasSelectedShipping));
                    OnPropertyChanged(nameof(Step3Completed));
                    OnPropertyChanged(nameof(GrandTotalFormatted));
                }
            }
        }

        public bool IsCalculatingShipping
        {
            get => _isCalculatingShipping;
            set => SetProperty(ref _isCalculatingShipping, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterOrders();
                }
            }
        }

        public decimal CalculatedWeight
        {
            get => _calculatedWeight;
            set => SetProperty(ref _calculatedWeight, value);
        }

        public int CalculatedWidth
        {
            get => _calculatedWidth;
            set => SetProperty(ref _calculatedWidth, value);
        }

        public int CalculatedHeight
        {
            get => _calculatedHeight;
            set => SetProperty(ref _calculatedHeight, value);
        }

        public int CalculatedLength
        {
            get => _calculatedLength;
            set => SetProperty(ref _calculatedLength, value);
        }

        public string ShippingRuleApplied
        {
            get => _shippingRuleApplied;
            set => SetProperty(ref _shippingRuleApplied, value);
        }

        public bool IsManualDimensionsMode
        {
            get => _isManualDimensionsMode;
            set
            {
                if (SetProperty(ref _isManualDimensionsMode, value))
                {
                    OnPropertyChanged(nameof(IsAutoDimensionsMode));
                    if (!value) CalculateShippingDimensions();
                }
            }
        }

        public bool IsAutoDimensionsMode => !IsManualDimensionsMode;

        public string OriginPostalCode
        {
            get => _originPostalCode;
            set => SetProperty(ref _originPostalCode, value);
        }

        public string DestinationPostalCode
        {
            get => SelectedCustomer?.ZipCode ?? "Não selecionado";
            set
            {
                if (SelectedCustomer != null && value != SelectedCustomer.ZipCode)
                {
                    SelectedCustomer.ZipCode = value;
                    OnPropertyChanged(nameof(DestinationPostalCode));
                }
            }
        }

        private bool _isEditingDestinationCep;
        public bool IsEditingDestinationCep
        {
            get => _isEditingDestinationCep;
            set => SetProperty(ref _isEditingDestinationCep, value);
        }

        public bool IsNotEditingDestinationCep => !IsEditingDestinationCep;

        public string OrderTotalFormatted => string.Format("R$ {0:N2}", OrderItems.Sum(i => i.Subtotal));
        public string GrandTotalFormatted => string.Format("R$ {0:N2}", OrderItems.Sum(i => i.Subtotal) + (SelectedShipping != null ? SelectedShipping.Price : 0));

        // Propriedades para UI/UX melhorada do frete
        public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
        public bool HasShippingQuotes => ShippingQuotes.Count > 0;
        public bool HasCheapestOption => CheapestOption != null;
        public bool HasMultipleOptions => OtherShippingOptions.Count > 0;

        public ShippingQuoteViewModel? CheapestOption => ShippingQuotes.OrderBy(q => q.Price).FirstOrDefault();
        public ObservableCollection<ShippingQuoteViewModel> OtherShippingOptions { get; } = new ObservableCollection<ShippingQuoteViewModel>();

        public ObservableCollection<OrderItemViewModel> FilteredOrders { get; } = new ObservableCollection<OrderItemViewModel>();
        public ObservableCollection<OrderItemViewModel> AllOrders { get; } = new ObservableCollection<OrderItemViewModel>();
        public ObservableCollection<CustomerForOrderViewModel> Customers { get; } = new ObservableCollection<CustomerForOrderViewModel>();
        public ObservableCollection<ProductForOrderViewModel> AvailableProducts { get; } = new ObservableCollection<ProductForOrderViewModel>();
        public ObservableCollection<OrderLineItemViewModel> OrderItems { get; } = new ObservableCollection<OrderLineItemViewModel>();
        public ObservableCollection<ShippingQuoteViewModel> ShippingQuotes { get; } = new ObservableCollection<ShippingQuoteViewModel>();


        public ICommand SyncWithSuperFreteCommand { get; }
        public ICommand DeleteOrderCommand { get; }
        public ICommand NewOrderCommand { get; }
        public ICommand SelectOrderCommand { get; }
        public ICommand SelectCustomerCommand { get; }
        public ICommand AddProductToOrderCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand GoToStepCommand { get; }
        public ICommand CalculateShippingCommand { get; }
        public ICommand SelectShippingCommand { get; }
        public ICommand SelectCheapestCommand { get; }
        public ICommand RecalculateDimensionsCommand { get; }
        public ICommand ToggleDimensionsModeCommand { get; }
        public ICommand EditDestinationCepCommand { get; }
        public ICommand SaveDestinationCepCommand { get; }
        public ICommand CancelEditDestinationCepCommand { get; }
        public ICommand FinalizeOrderCommand { get; }
        public ICommand ShowConfirmationCommand { get; }
        public ICommand ConfirmFinalizeCommand { get; }
        public ICommand CancelFinalizeCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand CloseSuccessCommand { get; }
        public ICommand CopyTrackingCodeCommand { get; }
        public ICommand OpenLabelUrlCommand { get; }
        public ICommand OpenGeneratedLabelCommand { get; }
        public ICommand CancelShipmentCommand { get; }
        public ICommand PrintLabelCommand { get; }
        public ICommand CheckoutLabelCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand RefreshOrdersCommand { get; }

        // Propriedades para visualização de pedidos processados
        public bool IsOrderProcessed => SelectedOrder != null && !SelectedOrder.IsNew && SelectedOrder.Status != OrderStatus.Pending;
        public bool IsOrderNew => SelectedOrder != null && SelectedOrder.IsNew;
        public bool IsOrderPending => SelectedOrder != null && !SelectedOrder.IsNew && SelectedOrder.Status == OrderStatus.Pending;
        public bool CanGenerateLabel => SelectedOrder != null && (SelectedOrder.IsNew || SelectedOrder.Status == OrderStatus.Pending);
        public bool HasTrackingCode => !string.IsNullOrEmpty(SelectedOrder?.TrackingCode);
        public bool CanCancelShipment => SelectedOrder != null && (SelectedOrder.Status == OrderStatus.Processing || SelectedOrder.Status == OrderStatus.Shipped) && !string.IsNullOrEmpty(SelectedOrder.SuperFreteOrderId);
        public bool CanPrintLabel => HasTrackingCode && !string.IsNullOrEmpty(SelectedOrder?.SuperFreteOrderId);
        public bool NeedsCheckout => false;

        // Propriedades para confirmação
        private bool _showConfirmationDialog;
        public bool ShowConfirmationDialog
        {
            get => _showConfirmationDialog;
            set => SetProperty(ref _showConfirmationDialog, value);
        }

        private bool _showSuccessDialog;
        public bool ShowSuccessDialog
        {
            get => _showSuccessDialog;
            set => SetProperty(ref _showSuccessDialog, value);
        }

        private bool _isGeneratingLabel;
        public bool IsGeneratingLabel
        {
            get => _isGeneratingLabel;
            set => SetProperty(ref _isGeneratingLabel, value);
        }

        private string _generatedTrackingCode = string.Empty;
        public string GeneratedTrackingCode
        {
            get => _generatedTrackingCode;
            set => SetProperty(ref _generatedTrackingCode, value);
        }

        private string _generatedLabelUrl = string.Empty;
        public string GeneratedLabelUrl
        {
            get => _generatedLabelUrl;
            set => SetProperty(ref _generatedLabelUrl, value);
        }

        public bool HasGeneratedLabel => !string.IsNullOrEmpty(GeneratedTrackingCode);

        private readonly GetOrdersUseCase _getOrdersUseCase;
        private readonly DeleteOrderUseCase _deleteOrderUseCase;
        private readonly UpdateOrderStatusUseCase _updateOrderStatusUseCase;
        private readonly ISmartSearchService _smartSearchService;

        // Search and Recommendations
        public ObservableCollection<OrderItemViewModel> RecommendedOrders { get; } = new ObservableCollection<OrderItemViewModel>();
        private bool _showRecommendations;
        public bool ShowRecommendations
        {
            get => _showRecommendations;
            set => SetProperty(ref _showRecommendations, value);
        }

        public OrdersViewModel(
            IOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            IProductRepository productRepository,
            IShipmentRepository shipmentRepository,
            ISuperFreteService superFreteService,
            SuperFreteSettings superFreteSettings,
            GetOrdersUseCase getOrdersUseCase,
            DeleteOrderUseCase deleteOrderUseCase,
            UpdateOrderStatusUseCase updateOrderStatusUseCase,
            ISmartSearchService smartSearchService,
            IUnitOfWork unitOfWork)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== OrdersViewModel: Iniciando construtor ===");

                System.Diagnostics.Debug.WriteLine("OrdersViewModel: Validando dependências...");
                _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
                _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
                _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
                _shipmentRepository = shipmentRepository ?? throw new ArgumentNullException(nameof(shipmentRepository));
                _superFreteService = superFreteService ?? throw new ArgumentNullException(nameof(superFreteService));
                _superFreteSettings = superFreteSettings ?? throw new ArgumentNullException(nameof(superFreteSettings));
                _getOrdersUseCase = getOrdersUseCase ?? throw new ArgumentNullException(nameof(getOrdersUseCase));
                _deleteOrderUseCase = deleteOrderUseCase ?? throw new ArgumentNullException(nameof(deleteOrderUseCase));
                _updateOrderStatusUseCase = updateOrderStatusUseCase ?? throw new ArgumentNullException(nameof(updateOrderStatusUseCase));
                _smartSearchService = smartSearchService ?? throw new ArgumentNullException(nameof(smartSearchService));
                _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

                System.Diagnostics.Debug.WriteLine("  ✓ Dependências OK");

                System.Diagnostics.Debug.WriteLine("OrdersViewModel: Configurando OriginPostalCode...");
                OriginPostalCode = _superFreteSettings.DefaultOriginPostalCode;

                System.Diagnostics.Debug.WriteLine("OrdersViewModel: Inicializando comandos...");
                NewOrderCommand = new RelayCommand(NewOrder);
                SelectOrderCommand = new RelayCommand<OrderItemViewModel>(SelectOrder);
                SelectCustomerCommand = new RelayCommand<CustomerForOrderViewModel>(SelectCustomer);
                AddProductToOrderCommand = new RelayCommand<ProductForOrderViewModel>(AddProductToOrder);
                RemoveItemCommand = new RelayCommand<OrderLineItemViewModel>(RemoveItem);
                NextStepCommand = new RelayCommand(NextStep);
                PreviousStepCommand = new RelayCommand(PreviousStep);
                GoToStepCommand = new RelayCommand<string>(GoToStep);
                CalculateShippingCommand = new AsyncRelayCommand(CalculateShippingAsync);
                SelectShippingCommand = new RelayCommand<ShippingQuoteViewModel>(SelectShipping);
                SelectCheapestCommand = new RelayCommand(SelectCheapest);
                RecalculateDimensionsCommand = new RelayCommand(RecalculateDimensions);
                ToggleDimensionsModeCommand = new RelayCommand(ToggleDimensionsMode);
                EditDestinationCepCommand = new RelayCommand(EditDestinationCep);
                SaveDestinationCepCommand = new AsyncRelayCommand(SaveDestinationCepAsync);
                CancelEditDestinationCepCommand = new RelayCommand(CancelEditDestinationCep);
                FinalizeOrderCommand = new RelayCommand(ShowConfirmation);
                ShowConfirmationCommand = new RelayCommand(ShowConfirmation);
                ConfirmFinalizeCommand = new AsyncRelayCommand(ConfirmFinalizeAsync);
                CancelFinalizeCommand = new RelayCommand(CancelFinalize);
                CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync);
                EditOrderCommand = new RelayCommand(EditOrder);
                CloseSuccessCommand = new RelayCommand(CloseSuccessAndReset);
                CopyTrackingCodeCommand = new RelayCommand(CopyTrackingCode);
                OpenLabelUrlCommand = new AsyncRelayCommand(OpenLabelUrlAsync);
                OpenGeneratedLabelCommand = new AsyncRelayCommand(OpenLabelUrlAsync);
                CancelShipmentCommand = new AsyncRelayCommand(CancelShipmentAsync, () => CanCancelShipment);
                PrintLabelCommand = new AsyncRelayCommand(OpenLabelUrlAsync, () => CanPrintLabel);
                CheckoutLabelCommand = new AsyncRelayCommand(CheckoutLabelAsync, () => NeedsCheckout);
                ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
                RefreshOrdersCommand = new AsyncRelayCommand(RefreshOrdersAsync);
                SyncWithSuperFreteCommand = new AsyncRelayCommand<OrderItemViewModel>(SyncWithSuperFreteAsync, (o) => o != null && !string.IsNullOrEmpty(o.TrackingCode));
                DeleteOrderCommand = new AsyncRelayCommand<OrderItemViewModel>(DeleteOrderAsync);
                System.Diagnostics.Debug.WriteLine("  ✓ Comandos inicializados");

                System.Diagnostics.Debug.WriteLine("OrdersViewModel: Iniciando LoadDataAsync...");
                _ = LoadDataAsync();
                System.Diagnostics.Debug.WriteLine("=== OrdersViewModel: Construtor finalizado ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! ERRO CRÍTICO no OrdersViewModel construtor: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"!!! Mensagem: {ex.Message}");
                throw;
            }
        }

        private async Task OpenLabelUrlAsync()
        {
            // Primeiro tenta usar URL já carregada
            var url = SelectedOrder?.LabelUrl ?? GeneratedLabelUrl;

            // Se não tiver URL mas tiver SuperFreteOrderId, busca na API
            var superFreteOrderId = SelectedOrder?.SuperFreteOrderId;
            if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(superFreteOrderId))
            {
                try
                {
                    App.SnackbarMessageQueue.Enqueue("Buscando etiqueta...");

                    url = await _superFreteService.GetLabelUrlAsync(superFreteOrderId);
                }
                catch (Exception ex)
                {
                    App.SnackbarMessageQueue.Enqueue($"Erro ao buscar etiqueta: {ex.Message}");
                    return;
                }
            }

            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    App.SnackbarMessageQueue.Enqueue($"Erro ao abrir etiqueta: {ex.Message}");
                }
            }
            else
            {
                App.SnackbarMessageQueue.Enqueue("⚠️ URL da etiqueta não disponível. Acesse o painel do SuperFrete.");
            }
        }

        private async Task CancelShipmentAsync()
        {
            if (SelectedOrder == null || string.IsNullOrEmpty(SelectedOrder.SuperFreteOrderId)) return;

            try
            {
                App.SnackbarMessageQueue.Enqueue("Cancelando envio no SuperFrete...");

                // Cancelar no SuperFrete primeiro
                try
                {
                    await _superFreteService.CancelOrderAsync(SelectedOrder.SuperFreteOrderId);
                }
                catch (Exception ex)
                {
                    App.SnackbarMessageQueue.Enqueue($"⚠️ Erro ao cancelar no SuperFrete: {ex.Message}. Cancelando localmente...");
                }

                // Cancelar o pedido no banco
                await _updateOrderStatusUseCase.Execute(SelectedOrder.Id, (int)OrderStatus.Cancelled);

                // Cancelar o shipment no banco
                var shipment = await _shipmentRepository.GetByOrderIdAsync(SelectedOrder.Id);
                if (shipment != null)
                {
                    shipment.Cancel();
                    await _shipmentRepository.UpdateAsync(shipment);
                }

                App.SnackbarMessageQueue.Enqueue("✅ Envio cancelado com sucesso!");
                await LoadOrdersAsync();

                // Atualizar o pedido selecionado
                var updated = AllOrders.FirstOrDefault(o => o.Id == SelectedOrder.Id);
                if (updated != null)
                {
                    SelectedOrder = updated;
                    NotifyOrderStateChanged();
                }
            }
            catch (Exception ex)
            {
                App.SnackbarMessageQueue.Enqueue($"Erro ao cancelar envio: {ex.Message}");
            }
        }

        private async Task CheckoutLabelAsync()
        {
            if (SelectedOrder == null || string.IsNullOrEmpty(SelectedOrder.TrackingCode)) return;

            try
            {
                IsGeneratingLabel = true;
                App.SnackbarMessageQueue.Enqueue("Processando pagamento da etiqueta...");

                var result = await _superFreteService.CheckoutAsync(SelectedOrder.TrackingCode);

                if (!string.IsNullOrEmpty(result.LabelUrl))
                {
                    GeneratedLabelUrl = result.LabelUrl;
                    App.SnackbarMessageQueue.Enqueue("✅ Etiqueta liberada! Clique em 'Imprimir' para baixar.");
                    await LoadOrdersAsync();
                }
                else
                {
                    App.SnackbarMessageQueue.Enqueue("⚠️ Etiqueta ainda não disponível. Tente novamente em alguns segundos.");
                }
            }
            catch (Exception ex)
            {
                App.SnackbarMessageQueue.Enqueue($"Erro no checkout: {ex.Message}");
            }
            finally
            {
                IsGeneratingLabel = false;
            }
        }

        private void CopyTrackingCode()
        {
            var code = SelectedOrder?.TrackingCode ?? GeneratedTrackingCode;
            if (!string.IsNullOrEmpty(code))
            {
                System.Windows.Clipboard.SetText(code);
                App.SnackbarMessageQueue.Enqueue("📋 Código copiado para a área de transferência!");
            }
        }

        private async Task RefreshOrdersAsync()
        {
            App.SnackbarMessageQueue.Enqueue("🔄 Atualizando pedidos...");

            await LoadOrdersAsync();

            // Re-selecionar o pedido atual se existir
            if (SelectedOrder != null && !SelectedOrder.IsNew)
            {
                var updated = AllOrders.FirstOrDefault(o => o.Id == SelectedOrder.Id);
                if (updated != null)
                {
                    SelectedOrder = updated;
                    NotifyOrderStateChanged();
                }
            }

            App.SnackbarMessageQueue.Enqueue("✅ Pedidos atualizados!");
        }

        private void CloseSuccessAndReset()
        {
            ShowSuccessDialog = false;
            GeneratedTrackingCode = string.Empty;
            OnPropertyChanged(nameof(HasGeneratedLabel));

            // Limpar e resetar
            SelectedOrder = null;
            SelectedCustomer = null;
            SelectedShipping = null;
            OrderItems.Clear();
            ShippingQuotes.Clear();
            OtherShippingOptions.Clear();
            CurrentStep = 1;
            OnPropertyChanged(nameof(HasSelectedOrder));
        }

        private void ShowConfirmation()
        {
            if (SelectedShipping == null)
            {
                App.SnackbarMessageQueue.Enqueue("Selecione uma opção de frete antes de finalizar.");
                return;
            }
            ShowConfirmationDialog = true;
        }

        private void CancelFinalize()
        {
            ShowConfirmationDialog = false;
        }

        private async Task ConfirmFinalizeAsync()
        {
            if (SelectedCustomer == null || OrderItems.Count == 0 || SelectedShipping == null) return;

            try
            {
                IsGeneratingLabel = true;
                App.SnackbarMessageQueue.Enqueue("Gerando etiqueta no SuperFrete...");

                // Preparar lista de produtos
                var products = OrderItems.Select(item => new ShipmentProduct
                {
                    Name = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList();

                // 1. Gerar etiqueta no SuperFrete (adiciona ao carrinho)
                var labelRequest = new ShipmentLabelRequest
                {
                    Weight = CalculatedWeight,
                    Width = CalculatedWidth,
                    Height = CalculatedHeight,
                    Length = CalculatedLength,
                    ServiceId = SelectedShipping.ServiceId,
                    ServiceName = SelectedShipping.ServiceName,
                    ShippingPrice = SelectedShipping.Price,
                    ReceiverName = SelectedCustomer.Name,
                    ReceiverDocument = SelectedCustomer.Document,
                    ReceiverPhone = SelectedCustomer.Phone,
                    ReceiverEmail = SelectedCustomer.Email,
                    ReceiverAddress = SelectedCustomer.Address,
                    ReceiverNumber = SelectedCustomer.Number,
                    ReceiverComplement = SelectedCustomer.Complement,
                    ReceiverDistrict = SelectedCustomer.District,
                    ReceiverCity = SelectedCustomer.City,
                    ReceiverState = SelectedCustomer.State,
                    ReceiverZipCode = SelectedCustomer.ZipCode,
                    Products = products
                };

                var result = await _superFreteService.GenerateLabelAsync(labelRequest);
                var superFreteOrderId = result.OrderId;

                // 2. Fazer checkout (pagar e emitir a etiqueta)
                App.SnackbarMessageQueue.Enqueue("Pagando e emitindo etiqueta...");

                var checkoutResult = await _superFreteService.CheckoutAsync(superFreteOrderId);

                // Usar dados do checkout se disponíveis
                GeneratedTrackingCode = checkoutResult.TrackingCode ?? result.TrackingCode ?? superFreteOrderId;
                GeneratedLabelUrl = checkoutResult.LabelUrl ?? result.LabelUrl ?? "";
                OnPropertyChanged(nameof(HasGeneratedLabel));
                ShowConfirmationDialog = false;
                ShowSuccessDialog = true;

                // 3. Salvar pedido no banco
                Guid orderId;
                if (SelectedOrder != null && SelectedOrder.IsNew)
                {
                    var order = new Order(SelectedCustomer.Id, OrderSource.Direct);
                    foreach (var item in OrderItems)
                    {
                        order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
                    }
                    await _orderRepository.SaveAsync(order);
                    await _unitOfWork.SaveChangesAsync();
                    await _updateOrderStatusUseCase.Execute(order.Id, (int)OrderStatus.Shipped);
                    orderId = order.Id;
                }
                else if (SelectedOrder != null)
                {
                    await _updateOrderStatusUseCase.Execute(SelectedOrder.Id, (int)OrderStatus.Shipped);
                    orderId = SelectedOrder.Id;
                }
                else
                {
                    orderId = Guid.NewGuid();
                }

                // 4. Criar e salvar o Shipment com SuperFreteOrderId
                var shipment = new Shipment(orderId, ShipmentProvider.SuperFrete);
                foreach (var item in OrderItems)
                {
                    shipment.AddItem(item.ProductId, item.Quantity);
                }
                
                // Salvar o que temos no momento (ID e Tracking temporário)
                shipment.GenerateLabel(
                    GeneratedTrackingCode, 
                    SelectedShipping.Price, 
                    superFreteOrderId);
                
                if (!string.IsNullOrEmpty(GeneratedLabelUrl))
                {
                    // Se a URL veio no checkout, já salva
                    // Poderíamos adicionar uma propriedade LabelUrl na entidade Shipment futuramente
                }

                shipment.MarkAsShipped();
                
                System.Diagnostics.Debug.WriteLine($"Salvando Shipment para Pedido {orderId}...");
                await _shipmentRepository.SaveAsync(shipment);
                System.Diagnostics.Debug.WriteLine("✓ Shipment salvo com sucesso!");

                App.SnackbarMessageQueue.Enqueue("✅ Pedido e Envio registrados com sucesso!");

                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                ShowConfirmationDialog = false;
                App.SnackbarMessageQueue.Enqueue($"Erro ao emitir etiqueta: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"!!! ERRO em ConfirmFinalizeAsync: {ex.Message}");
            }
            finally
            {
                IsGeneratingLabel = false;
            }
        }

        private async Task CancelOrderAsync()
        {
            if (SelectedOrder == null || SelectedOrder.IsNew) return;

            try
            {
                await _updateOrderStatusUseCase.Execute(SelectedOrder.Id, (int)OrderStatus.Cancelled);
                App.SnackbarMessageQueue.Enqueue("Pedido cancelado com sucesso.");
                await LoadOrdersAsync();

                // Limpar seleção
                SelectedOrder = null;
                SelectedCustomer = null;
                SelectedShipping = null;
                OrderItems.Clear();
                ShippingQuotes.Clear();
                CurrentStep = 1;
                OnPropertyChanged(nameof(HasSelectedOrder));
            }
            catch (Exception ex)
            {
                App.SnackbarMessageQueue.Enqueue($"Erro ao cancelar: {ex.Message}");
            }
        }

        private void EditOrder()
        {
            // Volta para etapa 1 para editar
            CurrentStep = 1;
        }

        private string _originalDestinationCep = string.Empty;

        private void EditDestinationCep()
        {
            if (SelectedCustomer != null)
            {
                _originalDestinationCep = SelectedCustomer.ZipCode;
                IsEditingDestinationCep = true;
                OnPropertyChanged(nameof(IsNotEditingDestinationCep));
            }
        }

        private async Task SaveDestinationCepAsync()
        {
            if (SelectedCustomer != null && !string.IsNullOrWhiteSpace(SelectedCustomer.ZipCode))
            {
                try
                {
                    // Atualizar no banco de dados
                    var customer = await _customerRepository.GetByIdAsync(SelectedCustomer.Id);
                    if (customer != null)
                    {
                        customer.UpdateAddress(
                            customer.Address,
                            customer.City,
                            customer.State,
                            SelectedCustomer.ZipCode
                        );
                        await _customerRepository.UpdateAsync(customer);
                        App.SnackbarMessageQueue.Enqueue("CEP do cliente atualizado com sucesso!");
                    }
                }
                catch (Exception ex)
                {
                    App.SnackbarMessageQueue.Enqueue($"Erro ao salvar CEP: {ex.Message}");
                }
            }

            IsEditingDestinationCep = false;
            OnPropertyChanged(nameof(IsNotEditingDestinationCep));
            OnPropertyChanged(nameof(DestinationPostalCode));
        }

        private void CancelEditDestinationCep()
        {
            if (SelectedCustomer != null)
            {
                SelectedCustomer.ZipCode = _originalDestinationCep;
                OnPropertyChanged(nameof(DestinationPostalCode));
            }
            IsEditingDestinationCep = false;
            OnPropertyChanged(nameof(IsNotEditingDestinationCep));
        }

        private void RecalculateDimensions()
        {
            IsManualDimensionsMode = false;
            CalculateShippingDimensions();
        }

        private void ToggleDimensionsMode()
        {
            IsManualDimensionsMode = !IsManualDimensionsMode;
            if (!IsManualDimensionsMode)
            {
                CalculateShippingDimensions();
            }
            else
            {
                ShippingRuleApplied = "Modo manual: edite as dimensoes acima";
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(">>> LoadDataAsync: INICIANDO...");

                System.Diagnostics.Debug.WriteLine(">>> LoadDataAsync: Chamando LoadOrdersAsync...");
                await LoadOrdersAsync();
                System.Diagnostics.Debug.WriteLine(">>> LoadDataAsync: LoadOrdersAsync concluído");

                System.Diagnostics.Debug.WriteLine(">>> LoadDataAsync: Chamando LoadCustomersAsync...");
                await LoadCustomersAsync();
                System.Diagnostics.Debug.WriteLine(">>> LoadDataAsync: LoadCustomersAsync concluído");

                System.Diagnostics.Debug.WriteLine(">>> LoadDataAsync: Chamando LoadProductsAsync...");
                await LoadProductsAsync();
                System.Diagnostics.Debug.WriteLine(">>> LoadDataAsync: LoadProductsAsync concluído");

                System.Diagnostics.Debug.WriteLine(">>> LoadDataAsync: CONCLUÍDO COM SUCESSO");
            }
            catch (Exception ex)
            {
                var errorMsg = $"❌ Erro ao carregar dados: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"!!! LoadDataAsync ERRO: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"!!! Mensagem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"!!! StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"!!! InnerException: {ex.InnerException.Message}");
                }

                App.SnackbarMessageQueue.Enqueue(errorMsg);
            }
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("  >> LoadOrdersAsync: Iniciando...");
                var orders = await _getOrdersUseCase.Execute();
                System.Diagnostics.Debug.WriteLine($"  >> LoadOrdersAsync: Retornados {orders.Count()} pedidos");

                AllOrders.Clear();

                var orderedOrders = orders.OrderByDescending(o => o.CreatedAt).ToList();
                foreach (var order in orderedOrders)
                {
                    var viewModel = await MapToViewModel(order);
                    AllOrders.Add(viewModel);
                }

                FilterOrders();
                System.Diagnostics.Debug.WriteLine("  >> LoadOrdersAsync: Concluído com sucesso");
            }
            catch (Exception ex)
            {
                App.SnackbarMessageQueue.Enqueue($"❌ Erro ao carregar pedidos: {ex.Message}");
            }
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("  >> LoadCustomersAsync: Iniciando...");
                var customers = await _customerRepository.GetAllAsync();
                System.Diagnostics.Debug.WriteLine($"  >> LoadCustomersAsync: Retornados {customers.Count()} clientes");

                Customers.Clear();

                var activeCustomers = customers.Where(c => c.IsActive).ToList();
                foreach (var customer in activeCustomers)
                {
                    Customers.Add(new CustomerForOrderViewModel
                    {
                        Id = customer.Id,
                        Name = customer.Name,
                        Document = customer.Document,
                        Email = customer.Email,
                        Phone = customer.Phone,
                        ZipCode = customer.ZipCode,
                        Address = customer.Address,
                        Number = customer.Number,
                        Complement = customer.Complement,
                        District = customer.District,
                        City = customer.City,
                        State = customer.State
                    });
                }
            }
            catch (Exception ex)
            {
                App.SnackbarMessageQueue.Enqueue($"❌ Erro ao carregar clientes: {ex.Message}");
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("  >> LoadProductsAsync: Iniciando...");
                var products = await _productRepository.GetAllAsync();
                System.Diagnostics.Debug.WriteLine($"  >> LoadProductsAsync: Retornados {products.Count()} produtos");

                AvailableProducts.Clear();

                var activeProducts = products.Where(p => p.IsActive).ToList();
                foreach (var product in activeProducts)
                {
                    AvailableProducts.Add(new ProductForOrderViewModel
                    {
                        Id = product.Id,
                        Name = product.Name,
                        SKU = product.SKU,
                        Price = product.Price,
                        PriceFormatted = string.Format("R$ {0:N2}", product.Price),
                        Stock = product.Stock,
                        Weight = product.Weight,
                        Width = product.Width,
                        Height = product.Height,
                        Length = product.Length,
                        QuantityToAdd = 1
                    });
                }
            }
            catch (Exception ex)
            {
                App.SnackbarMessageQueue.Enqueue($"❌ Erro ao carregar produtos: {ex.Message}");
            }
        }

        private void NewOrder()
        {
            SelectedOrder = new OrderItemViewModel { Id = Guid.NewGuid(), IsNew = true };
            SelectedCustomer = null;
            SelectedShipping = null;
            OrderItems.Clear();
            ShippingQuotes.Clear();
            CurrentStep = 1;
            NotifyOrderStateChanged();
        }

        private void FilterOrders()
        {
            FilteredOrders.Clear();
            RecommendedOrders.Clear();

            var query = AllOrders.AsEnumerable();

            // 1. Filter by Category
            if (!string.IsNullOrEmpty(CategoryFilter) && CategoryFilter != "Todas Categorias")
            {
                query = query.Where(o => o.Category == CategoryFilter);
            }

            // 2. Filter by Search Text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                // Use Smart Search for text filtering on the already category-filtered list
                var searchResults = _smartSearchService.Search(
                    query,
                    SearchText,
                    o => $"{o.CustomerName} {o.TrackingCode} {o.StatusText}",
                    threshold: 0.25);

                foreach (var o in searchResults)
                {
                    FilteredOrders.Add(o);
                }

                // Recommendations
                var recommendations = searchResults.Take(5).ToList();
                if (recommendations.Any() && SearchText.Length > 2)
                {
                    foreach (var o in recommendations) RecommendedOrders.Add(o);
                    ShowRecommendations = true;
                }
                else
                {
                    ShowRecommendations = false;
                }
            }
            else
            {
                // Just category filter
                foreach (var o in query) FilteredOrders.Add(o);
                ShowRecommendations = false;
            }
        }

        private async Task<OrderItemViewModel> MapToViewModel(Order order)
        {
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
            var shipment = await _shipmentRepository.GetByOrderIdAsync(order.Id);

            string category = "Geral";
            if (order.Items.Any())
            {
                var firstProduct = await _productRepository.GetByIdAsync(order.Items.First().ProductId);
                if (firstProduct != null && !string.IsNullOrEmpty(firstProduct.Category))
                {
                    category = firstProduct.Category;
                }
            }

            return new OrderItemViewModel
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = customer?.Name ?? "Sem cliente",
                CustomerEmail = customer?.Email ?? "",
                CustomerPhone = customer?.Phone ?? "",
                CustomerAddress = customer != null ? $"{customer.Address}, {customer.Number} - {customer.City}/{customer.State}" : "",
                Status = order.Status,
                StatusText = GetStatusText(order.Status),
                TotalAmount = order.TotalAmount,
                TotalFormatted = string.Format("R$ {0:N2}", order.TotalAmount),
                ItemsCount = order.Items.Count,
                CreatedAt = order.CreatedAt,
                CreatedAtFormatted = order.CreatedAt.ToString("dd/MM HH:mm"),
                Category = category,
                // Informações de envio
                TrackingCode = shipment?.TrackingNumber,
                SuperFreteOrderId = shipment?.SuperFreteOrderId ?? "",
                ShippingCost = shipment?.ShippingCost ?? 0,
                ShippedAt = shipment?.ShippedAt
            };
        }

        private void SelectOrder(OrderItemViewModel? order)
        {
            if (order != null)
            {
                SelectedOrder = order;
                // Se o pedido já foi processado, não vai para o wizard
                if (order.IsNew || order.Status == OrderStatus.Pending)
                {
                    CurrentStep = 1;
                }
                NotifyOrderStateChanged();
            }
        }

        private void NotifyOrderStateChanged()
        {
            OnPropertyChanged(nameof(HasSelectedOrder));
            OnPropertyChanged(nameof(OrderTotalFormatted));
            OnPropertyChanged(nameof(IsOrderProcessed));
            OnPropertyChanged(nameof(IsOrderNew));
            OnPropertyChanged(nameof(IsOrderPending));
            OnPropertyChanged(nameof(CanGenerateLabel));
            OnPropertyChanged(nameof(HasTrackingCode));
            OnPropertyChanged(nameof(CanCancelShipment));
            OnPropertyChanged(nameof(CanPrintLabel));
            OnPropertyChanged(nameof(NeedsCheckout));
        }

        private async void LoadOrderDetails(OrderItemViewModel order)
        {
            if (order.IsNew) return;

            var fullOrder = await _orderRepository.GetByIdAsync(order.Id);
            if (fullOrder == null) return;

            var customer = Customers.FirstOrDefault(c => c.Id == fullOrder.CustomerId);
            SelectedCustomer = customer;

            OrderItems.Clear();
            foreach (var item in fullOrder.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                OrderItems.Add(new OrderLineItemViewModel
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = product != null ? product.Name : "Produto",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    UnitPriceFormatted = string.Format("R$ {0:N2}", item.UnitPrice),
                    Subtotal = item.Subtotal,
                    SubtotalFormatted = string.Format("R$ {0:N2}", item.Subtotal),
                    Weight = product != null ? product.Weight : 0,
                    Width = product != null ? product.Width : 0,
                    Height = product != null ? product.Height : 0,
                    Length = product != null ? product.Length : 0
                });
            }

            OnPropertyChanged(nameof(HasOrderItems));
            OnPropertyChanged(nameof(OrderTotalFormatted));
            OnPropertyChanged(nameof(Step1Completed));
            OnPropertyChanged(nameof(Step2Completed));
        }

        private void SelectCustomer(CustomerForOrderViewModel? customer)
        {
            SelectedCustomer = customer;
            OnPropertyChanged(nameof(DestinationPostalCode));
        }

        private void AddProductToOrder(ProductForOrderViewModel? product)
        {
            if (product == null || product.QuantityToAdd <= 0) return;

            var existing = OrderItems.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity += product.QuantityToAdd;
                existing.Subtotal = existing.Quantity * existing.UnitPrice;
                existing.SubtotalFormatted = string.Format("R$ {0:N2}", existing.Subtotal);
            }
            else
            {
                OrderItems.Add(new OrderLineItemViewModel
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = product.QuantityToAdd,
                    UnitPrice = product.Price,
                    UnitPriceFormatted = string.Format("R$ {0:N2}", product.Price),
                    Subtotal = product.Price * product.QuantityToAdd,
                    SubtotalFormatted = string.Format("R$ {0:N2}", product.Price * product.QuantityToAdd),
                    Weight = product.Weight,
                    Width = product.Width,
                    Height = product.Height,
                    Length = product.Length
                });
            }

            product.QuantityToAdd = 1;
            OnPropertyChanged(nameof(HasOrderItems));
            OnPropertyChanged(nameof(OrderTotalFormatted));
            OnPropertyChanged(nameof(Step2Completed));
        }

        private void RemoveItem(OrderLineItemViewModel? item)
        {
            if (item != null)
            {
                OrderItems.Remove(item);
                OnPropertyChanged(nameof(HasOrderItems));
                OnPropertyChanged(nameof(OrderTotalFormatted));
                OnPropertyChanged(nameof(Step2Completed));
            }
        }

        private void NextStep()
        {
            if (CurrentStep < 3)
            {
                CurrentStep++;
                if (CurrentStep == 3) CalculateShippingDimensions();
            }
        }

        private void PreviousStep()
        {
            if (CurrentStep > 1) CurrentStep--;
        }

        private void GoToStep(string? step)
        {
            if (int.TryParse(step, out int stepNum) && stepNum >= 1 && stepNum <= 3)
            {
                CurrentStep = stepNum;
                if (stepNum == 3) CalculateShippingDimensions();
            }
        }

        private void CalculateShippingDimensions()
        {
            if (OrderItems.Count == 0) return;

            var totalQuantity = OrderItems.Sum(i => i.Quantity);
            var totalWeight = OrderItems.Sum(i => i.Weight * i.Quantity);

            if (totalQuantity == 1)
            {
                var item = OrderItems.First();
                CalculatedWeight = item.Weight;
                CalculatedWidth = item.Width;
                CalculatedHeight = item.Height;
                CalculatedLength = item.Length;
                ShippingRuleApplied = "Pedido unico: usando dimensoes do produto";
            }
            else if (totalQuantity >= 2)
            {
                CalculatedWeight = totalWeight;
                CalculatedWidth = 40;
                CalculatedHeight = 27;
                CalculatedLength = 27;
                ShippingRuleApplied = string.Format("{0} itens: caixa grande (40x27x27) + peso total ({1:N2}kg)", totalQuantity, totalWeight);
            }

            if (CalculatedWeight < 0.01m) CalculatedWeight = 0.3m;
            if (CalculatedWidth < 11) CalculatedWidth = 11;
            if (CalculatedHeight < 2) CalculatedHeight = 2;
            if (CalculatedLength < 16) CalculatedLength = 16;
        }

        private async Task CalculateShippingAsync()
        {
            if (SelectedCustomer == null || string.IsNullOrEmpty(SelectedCustomer.ZipCode)) return;

            try
            {
                IsCalculatingShipping = true;
                ShippingQuotes.Clear();
                SelectedShipping = null;

                var request = new FreightQuoteRequest
                {
                    DestinationPostalCode = SelectedCustomer.ZipCode.Replace("-", ""),
                    Weight = CalculatedWeight,
                    Width = CalculatedWidth,
                    Height = CalculatedHeight,
                    Length = CalculatedLength,
                    Quantity = 1
                };

                var quotes = await _superFreteService.GetAllQuotesAsync(request);

                foreach (var quote in quotes.Where(q => string.IsNullOrEmpty(q.Error)).OrderBy(q => q.Price))
                {
                    // O ServiceId deve ser o ID da cotação, não o ID da transportadora
                    int serviceId = 1; // Default: SEDEX
                    if (!string.IsNullOrEmpty(quote.Id) && int.TryParse(quote.Id, out int parsedId))
                    {
                        serviceId = parsedId;
                    }

                    ShippingQuotes.Add(new ShippingQuoteViewModel
                    {
                        ServiceId = serviceId,
                        ServiceName = quote.Name ?? "Servico",
                        CompanyName = quote.Company?.Name ?? "Transportadora",
                        Price = quote.Price,
                        PriceFormatted = string.Format("R$ {0:N2}", quote.Price),
                        DeliveryTime = quote.DeliveryTime,
                        DeliveryRange = string.Format("{0} a {1} dias uteis", 
                            quote.DeliveryRange?.Min ?? quote.DeliveryTime, 
                            quote.DeliveryRange?.Max ?? quote.DeliveryTime)
                    });
                }

                // Atualizar lista de outras opções (excluindo a mais barata)
                OtherShippingOptions.Clear();
                var allQuotes = ShippingQuotes.OrderBy(q => q.Price).ToList();
                for (int i = 1; i < allQuotes.Count; i++)
                {
                    OtherShippingOptions.Add(allQuotes[i]);
                }

                // Notificar mudanças nas propriedades de UI
                OnPropertyChanged(nameof(HasShippingQuotes));
                OnPropertyChanged(nameof(HasCheapestOption));
                OnPropertyChanged(nameof(HasMultipleOptions));
                OnPropertyChanged(nameof(CheapestOption));
                StatusMessage = string.Empty;
                OnPropertyChanged(nameof(HasStatusMessage));

                if (ShippingQuotes.Count == 0)
                {
                    StatusMessage = "Nenhuma opcao de frete disponivel para este CEP. Verifique o endereco do cliente.";
                    OnPropertyChanged(nameof(HasStatusMessage));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format("Erro ao calcular frete: {0}", ex.Message);
                OnPropertyChanged(nameof(HasStatusMessage));
            }
            finally
            {
                IsCalculatingShipping = false;
            }
        }

        private void SelectShipping(ShippingQuoteViewModel? shipping)
        {
            SelectedShipping = shipping;
        }

        private void SelectCheapest()
        {
            if (CheapestOption != null)
            {
                SelectedShipping = CheapestOption;
            }
        }

        private static string GetStatusText(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Pending: return "PENDENTE";
                case OrderStatus.Processing: return "PROCESSANDO";
                case OrderStatus.Shipped: return "ENVIADO";
                case OrderStatus.Completed: return "CONCLUIDO";
                case OrderStatus.Cancelled: return "CANCELADO";
                default: return status.ToString();
            }
        }

        private async Task SyncWithSuperFreteAsync(OrderItemViewModel? orderVm)
        {
            if (orderVm == null || string.IsNullOrEmpty(orderVm.TrackingCode)) return;

            try
            {
                App.SnackbarMessageQueue.Enqueue($"Sincronizando pedido {orderVm.TrackingCode}...");

                var tracking = await _superFreteService.TrackShipmentAsync(orderVm.TrackingCode);
                
                App.SnackbarMessageQueue.Enqueue($"✅ Sincronizado! Status SuperFrete: {tracking.Status}");
                
                if (tracking.Status.Contains("Entregue", StringComparison.OrdinalIgnoreCase))
                {
                    await _updateOrderStatusUseCase.Execute(orderVm.Id, (int)OrderStatus.Completed);
                    await LoadOrdersAsync();
                }
            }
            catch (Exception ex)
            {
                App.SnackbarMessageQueue.Enqueue($"❌ Erro na sincronização: {ex.Message}");
            }
        }

        private async Task DeleteOrderAsync(OrderItemViewModel? orderVm)
        {
            if (orderVm == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Deseja realmente excluir o pedido de {orderVm.CustomerName}?\nEsta ação retornará os itens ao estoque e excluirá o registro permanentemente.",
                "Confirmar Exclusão",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    await _deleteOrderUseCase.Execute(orderVm.Id);
                    App.SnackbarMessageQueue.Enqueue("✅ Pedido removido e estoque atualizado.");
                    await LoadOrdersAsync();
                    
                    if (SelectedOrder?.Id == orderVm.Id)
                    {
                        SelectedOrder = null;
                        NotifyOrderStateChanged();
                    }
                }
                catch (Exception ex)
                {
                    App.SnackbarMessageQueue.Enqueue($"❌ Erro ao remover: {ex.Message}");
                }
            }
        }
    }

    public class OrderItemViewModel : ViewModelBase
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string TotalFormatted { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted { get; set; } = string.Empty;
        public bool IsNew { get; set; }
        public string Category { get; set; } = "Geral";

        // Informações de envio
        public string? TrackingCode { get; set; }
        public string? LabelUrl { get; set; }
        public string? ShippingService { get; set; }
        public string SuperFreteOrderId { get; set; } = string.Empty;
        public decimal ShippingCost { get; set; }
        public string ShippingCostFormatted => ShippingCost > 0 ? string.Format("R$ {0:N2}", ShippingCost) : "-";
        public DateTime? ShippedAt { get; set; }
        public string ShippedAtFormatted => ShippedAt?.ToString("dd/MM/yyyy HH:mm") ?? "-";

        // Helpers para UI
        public bool HasTracking => !string.IsNullOrEmpty(TrackingCode);
        public bool HasLabel => !string.IsNullOrEmpty(LabelUrl);
        public bool IsProcessing => Status == OrderStatus.Processing;
        public bool IsShipped => Status == OrderStatus.Shipped;
        public bool IsCompleted => Status == OrderStatus.Completed;
        public bool IsCancelled => Status == OrderStatus.Cancelled;
        public bool IsPending => Status == OrderStatus.Pending;
    }

    public class CustomerForOrderViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Complement { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class ProductForOrderViewModel : ViewModelBase
    {
        private int _quantityToAdd = 1;

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PriceFormatted { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal Weight { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Length { get; set; }

        public int QuantityToAdd
        {
            get => _quantityToAdd;
            set => SetProperty(ref _quantityToAdd, value);
        }
    }

    public class OrderLineItemViewModel : ViewModelBase
    {
        private int _quantity;
        private decimal _subtotal;
        private string _subtotalFormatted = string.Empty;

        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal UnitPrice { get; set; }
        public string UnitPriceFormatted { get; set; } = string.Empty;

        public decimal Subtotal
        {
            get => _subtotal;
            set => SetProperty(ref _subtotal, value);
        }

        public string SubtotalFormatted
        {
            get => _subtotalFormatted;
            set => SetProperty(ref _subtotalFormatted, value);
        }

        public decimal Weight { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Length { get; set; }
    }

    public class ShippingQuoteViewModel
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PriceFormatted { get; set; } = string.Empty;
        public int DeliveryTime { get; set; }
        public string DeliveryRange { get; set; } = string.Empty;
    }
}
