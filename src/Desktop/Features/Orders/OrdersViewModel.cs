using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Integrations.SuperFrete.Configuration;
using Integrations.SuperFrete.Interfaces;
using Integrations.SuperFrete.Models;

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

        public ObservableCollection<OrderItemViewModel> AllOrders { get; } = new ObservableCollection<OrderItemViewModel>();
        public ObservableCollection<OrderItemViewModel> Orders { get; } = new ObservableCollection<OrderItemViewModel>();
        public ObservableCollection<CustomerForOrderViewModel> Customers { get; } = new ObservableCollection<CustomerForOrderViewModel>();
        public ObservableCollection<ProductForOrderViewModel> AvailableProducts { get; } = new ObservableCollection<ProductForOrderViewModel>();
        public ObservableCollection<OrderLineItemViewModel> OrderItems { get; } = new ObservableCollection<OrderLineItemViewModel>();
        public ObservableCollection<ShippingQuoteViewModel> ShippingQuotes { get; } = new ObservableCollection<ShippingQuoteViewModel>();


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

        public OrdersViewModel(
            IOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            IProductRepository productRepository,
            IShipmentRepository shipmentRepository,
            ISuperFreteService superFreteService,
            SuperFreteSettings superFreteSettings)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _shipmentRepository = shipmentRepository;
            _superFreteService = superFreteService;
            _superFreteSettings = superFreteSettings;

            // Carregar CEP de origem das configurações
            OriginPostalCode = superFreteSettings.DefaultOriginPostalCode;

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
            CancelShipmentCommand = new AsyncRelayCommand(CancelShipmentAsync, () => CanCancelShipment);
            PrintLabelCommand = new AsyncRelayCommand(OpenLabelUrlAsync, () => CanPrintLabel);
            CheckoutLabelCommand = new AsyncRelayCommand(CheckoutLabelAsync, () => NeedsCheckout);
            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
            RefreshOrdersCommand = new AsyncRelayCommand(RefreshOrdersAsync);

            _ = LoadDataAsync();
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
                    StatusMessage = "Buscando etiqueta...";
                    OnPropertyChanged(nameof(HasStatusMessage));

                    url = await _superFreteService.GetLabelUrlAsync(superFreteOrderId);

                    StatusMessage = "";
                    OnPropertyChanged(nameof(HasStatusMessage));
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Erro ao buscar etiqueta: {ex.Message}";
                    OnPropertyChanged(nameof(HasStatusMessage));
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
                    StatusMessage = $"Erro ao abrir etiqueta: {ex.Message}";
                    OnPropertyChanged(nameof(HasStatusMessage));
                }
            }
            else
            {
                StatusMessage = "⚠️ URL da etiqueta não disponível. Acesse o painel do SuperFrete.";
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        private async Task CancelShipmentAsync()
        {
            if (SelectedOrder == null || string.IsNullOrEmpty(SelectedOrder.SuperFreteOrderId)) return;

            try
            {
                StatusMessage = "Cancelando envio no SuperFrete...";
                OnPropertyChanged(nameof(HasStatusMessage));

                // Cancelar no SuperFrete primeiro
                try
                {
                    await _superFreteService.CancelOrderAsync(SelectedOrder.SuperFreteOrderId);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"⚠️ Erro ao cancelar no SuperFrete: {ex.Message}. Cancelando localmente...";
                    OnPropertyChanged(nameof(HasStatusMessage));
                }

                // Cancelar o pedido no banco
                var order = await _orderRepository.GetByIdAsync(SelectedOrder.Id);
                if (order != null)
                {
                    order.Cancel();
                    await _orderRepository.UpdateAsync(order);
                }

                // Cancelar o shipment no banco
                var shipment = await _shipmentRepository.GetByOrderIdAsync(SelectedOrder.Id);
                if (shipment != null)
                {
                    shipment.Cancel();
                    await _shipmentRepository.UpdateAsync(shipment);
                }

                StatusMessage = "✅ Envio cancelado com sucesso!";
                await LoadOrdersAsync();

                // Atualizar o pedido selecionado
                var updated = Orders.FirstOrDefault(o => o.Id == SelectedOrder.Id);
                if (updated != null)
                {
                    SelectedOrder = updated;
                    NotifyOrderStateChanged();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao cancelar envio: {ex.Message}";
            }
            OnPropertyChanged(nameof(HasStatusMessage));
        }

        private async Task CheckoutLabelAsync()
        {
            if (SelectedOrder == null || string.IsNullOrEmpty(SelectedOrder.TrackingCode)) return;

            try
            {
                IsGeneratingLabel = true;
                StatusMessage = "Processando pagamento da etiqueta...";
                OnPropertyChanged(nameof(HasStatusMessage));

                var result = await _superFreteService.CheckoutAsync(SelectedOrder.TrackingCode);

                if (!string.IsNullOrEmpty(result.LabelUrl))
                {
                    GeneratedLabelUrl = result.LabelUrl;
                    StatusMessage = "✅ Etiqueta liberada! Clique em 'Imprimir' para baixar.";
                    await LoadOrdersAsync();
                }
                else
                {
                    StatusMessage = "⚠️ Etiqueta ainda não disponível. Tente novamente em alguns segundos.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro no checkout: {ex.Message}";
            }
            finally
            {
                IsGeneratingLabel = false;
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        private void CopyTrackingCode()
        {
            var code = SelectedOrder?.TrackingCode ?? GeneratedTrackingCode;
            if (!string.IsNullOrEmpty(code))
            {
                System.Windows.Clipboard.SetText(code);
                StatusMessage = "📋 Código copiado para a área de transferência!";
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }

        private async Task RefreshOrdersAsync()
        {
            StatusMessage = "🔄 Atualizando pedidos...";
            OnPropertyChanged(nameof(HasStatusMessage));

            await LoadOrdersAsync();

            // Re-selecionar o pedido atual se existir
            if (SelectedOrder != null && !SelectedOrder.IsNew)
            {
                var updated = Orders.FirstOrDefault(o => o.Id == SelectedOrder.Id);
                if (updated != null)
                {
                    SelectedOrder = updated;
                    NotifyOrderStateChanged();
                }
            }

            StatusMessage = "✅ Pedidos atualizados!";
            OnPropertyChanged(nameof(HasStatusMessage));

            // Limpar mensagem após 2 segundos
            await Task.Delay(2000);
            StatusMessage = "";
            OnPropertyChanged(nameof(HasStatusMessage));
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
                StatusMessage = "Selecione uma opção de frete antes de finalizar.";
                OnPropertyChanged(nameof(HasStatusMessage));
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
                StatusMessage = "Gerando etiqueta no SuperFrete...";
                OnPropertyChanged(nameof(HasStatusMessage));

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
                StatusMessage = "Pagando e emitindo etiqueta...";
                OnPropertyChanged(nameof(HasStatusMessage));

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
                    order.MarkAsProcessing();
                    order.MarkAsShipped();
                    await _orderRepository.SaveAsync(order);
                    orderId = order.Id;
                }
                else if (SelectedOrder != null)
                {
                    var order = await _orderRepository.GetByIdAsync(SelectedOrder.Id);
                    if (order != null)
                    {
                        order.MarkAsProcessing();
                        order.MarkAsShipped();
                        await _orderRepository.UpdateAsync(order);
                    }
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
                shipment.GenerateLabel(
                    GeneratedTrackingCode, 
                    SelectedShipping.Price, 
                    superFreteOrderId);
                shipment.MarkAsShipped();
                await _shipmentRepository.SaveAsync(shipment);

                StatusMessage = "✅ Etiqueta emitida com sucesso!";
                OnPropertyChanged(nameof(HasStatusMessage));

                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                ShowConfirmationDialog = false;
                StatusMessage = $"Erro ao emitir etiqueta: {ex.Message}";
                OnPropertyChanged(nameof(HasStatusMessage));
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
                var order = await _orderRepository.GetByIdAsync(SelectedOrder.Id);
                if (order != null)
                {
                    order.Cancel();
                    await _orderRepository.UpdateAsync(order);
                    StatusMessage = "Pedido cancelado com sucesso.";
                    OnPropertyChanged(nameof(HasStatusMessage));
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
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao cancelar: {ex.Message}";
                OnPropertyChanged(nameof(HasStatusMessage));
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
                        StatusMessage = "CEP do cliente atualizado com sucesso!";
                        OnPropertyChanged(nameof(HasStatusMessage));
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Erro ao salvar CEP: {ex.Message}";
                    OnPropertyChanged(nameof(HasStatusMessage));
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
            await LoadOrdersAsync();
            await LoadCustomersAsync();
            await LoadProductsAsync();
        }

        private async Task LoadOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            AllOrders.Clear();
            foreach (var order in orders.OrderByDescending(o => o.CreatedAt))
            {
                AllOrders.Add(await MapToViewModel(order));
            }
            FilterOrders();
        }

        private async Task LoadCustomersAsync()
        {
            var customers = await _customerRepository.GetAllAsync();
            Customers.Clear();
            foreach (var customer in customers.Where(c => c.IsActive))
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

        private async Task LoadProductsAsync()
        {
            var products = await _productRepository.GetAllAsync();
            AvailableProducts.Clear();
            foreach (var product in products.Where(p => p.IsActive))
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
            Orders.Clear();
            var search = SearchText?.ToLowerInvariant() ?? "";

            foreach (var o in AllOrders)
            {
                if (!string.IsNullOrEmpty(search))
                {
                    if (!o.CustomerName.ToLowerInvariant().Contains(search) &&
                        !(o.TrackingCode?.ToLowerInvariant().Contains(search) ?? false))
                        continue;
                }

                Orders.Add(o);
            }
        }

        private async Task<OrderItemViewModel> MapToViewModel(Order order)
        {
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
            var shipment = await _shipmentRepository.GetByOrderIdAsync(order.Id);

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
                // Carregar detalhes do pedido
                LoadOrderDetails(order);
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
