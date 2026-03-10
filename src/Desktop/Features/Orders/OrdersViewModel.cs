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
        private readonly ISuperFreteService _superFreteService;
        private readonly SuperFreteSettings _superFreteSettings;

        private int _currentStep = 1;
        private OrderItemViewModel _selectedOrder;
        private CustomerForOrderViewModel _selectedCustomer;
        private ShippingQuoteViewModel _selectedShipping;
        private bool _isCalculatingShipping;
        private string _statusMessage = string.Empty;
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

        public OrderItemViewModel SelectedOrder
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

        public CustomerForOrderViewModel SelectedCustomer
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

        public ShippingQuoteViewModel SelectedShipping
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

        public ShippingQuoteViewModel CheapestOption => ShippingQuotes.OrderBy(q => q.Price).FirstOrDefault();
        public ObservableCollection<ShippingQuoteViewModel> OtherShippingOptions { get; } = new ObservableCollection<ShippingQuoteViewModel>();

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
            ISuperFreteService superFreteService,
            SuperFreteSettings superFreteSettings)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _productRepository = productRepository;
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

            _ = LoadDataAsync();
        }

        private void CopyTrackingCode()
        {
            if (!string.IsNullOrEmpty(GeneratedTrackingCode))
            {
                System.Windows.Clipboard.SetText(GeneratedTrackingCode);
                StatusMessage = "📋 Código copiado para a área de transferência!";
                OnPropertyChanged(nameof(HasStatusMessage));
            }
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

                // Gerar etiqueta no SuperFrete
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
                    ReceiverAddress = SelectedCustomer.Address,
                    ReceiverCity = SelectedCustomer.City,
                    ReceiverState = SelectedCustomer.State,
                    ReceiverZipCode = SelectedCustomer.ZipCode
                };

                var trackingCode = await _superFreteService.GenerateLabelAsync(labelRequest);
                GeneratedTrackingCode = trackingCode;
                OnPropertyChanged(nameof(HasGeneratedLabel));
                ShowConfirmationDialog = false;
                ShowSuccessDialog = true;

                // Salvar pedido
                if (SelectedOrder != null && SelectedOrder.IsNew)
                {
                    var order = new Order(SelectedCustomer.Id, OrderSource.Direct);
                    foreach (var item in OrderItems)
                    {
                        order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
                    }
                    order.MarkAsProcessing();
                    await _orderRepository.SaveAsync(order);
                }
                else if (SelectedOrder != null)
                {
                    var order = await _orderRepository.GetByIdAsync(SelectedOrder.Id);
                    if (order != null)
                    {
                        order.MarkAsProcessing();
                        await _orderRepository.UpdateAsync(order);
                    }
                }

                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                ShowConfirmationDialog = false;
                StatusMessage = $"Erro ao gerar etiqueta: {ex.Message}";
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
            Orders.Clear();
            foreach (var order in orders.OrderByDescending(o => o.CreatedAt))
            {
                var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
                Orders.Add(new OrderItemViewModel
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    CustomerName = customer != null ? customer.Name : "Sem cliente",
                    Status = order.Status,
                    StatusText = GetStatusText(order.Status),
                    TotalAmount = order.TotalAmount,
                    TotalFormatted = string.Format("R$ {0:N2}", order.TotalAmount),
                    ItemsCount = order.Items.Count,
                    CreatedAt = order.CreatedAt,
                    CreatedAtFormatted = order.CreatedAt.ToString("dd/MM HH:mm")
                });
            }
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
                    Email = customer.Email,
                    Phone = customer.Phone,
                    ZipCode = customer.ZipCode,
                    Address = customer.Address,
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
            OnPropertyChanged(nameof(HasSelectedOrder));
            OnPropertyChanged(nameof(OrderTotalFormatted));
        }

        private void SelectOrder(OrderItemViewModel order)
        {
            if (order != null)
            {
                SelectedOrder = order;
                CurrentStep = 1;
            }
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

        private void SelectCustomer(CustomerForOrderViewModel customer)
        {
            SelectedCustomer = customer;
            OnPropertyChanged(nameof(DestinationPostalCode));
        }

        private void AddProductToOrder(ProductForOrderViewModel product)
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

        private void RemoveItem(OrderLineItemViewModel item)
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

        private void GoToStep(string step)
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
                    ShippingQuotes.Add(new ShippingQuoteViewModel
                    {
                        ServiceId = quote.Company?.Id ?? 1,
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

        private void SelectShipping(ShippingQuoteViewModel shipping)
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
        public OrderStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string TotalFormatted { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtFormatted { get; set; } = string.Empty;
        public bool IsNew { get; set; }
    }

    public class CustomerForOrderViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
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
