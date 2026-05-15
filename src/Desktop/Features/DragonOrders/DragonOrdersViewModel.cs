using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MaterialDesignThemes.Wpf;

namespace Desktop.Features.DragonOrders;

public class DragonOrdersViewModel : INotifyPropertyChanged
{
    private readonly IDragonOrderRepository _repository;
    private readonly ICustomerRepository _customerRepository;

    public ISnackbarMessageQueue NotificationsQueue { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

    private void ShowNotification(string message, bool isError = false)
    {
        NotificationsQueue.Enqueue(isError ? $"❌ {message}" : $"✅ {message}");
    }

    public class CustomerSummary : INotifyPropertyChanged
    {
        public Guid? CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public ObservableCollection<DragonOrder> Orders { get; set; } = new();

        public decimal TotalAmount => Orders.Where(o => o.Status != DragonOrderStatus.Cancelado).Sum(o => o.TotalAmount);
        public decimal TotalPaid => Orders.Where(o => o.Status != DragonOrderStatus.Cancelado).Sum(o => o.TotalPaid);
        public decimal TotalRemaining => Orders.Where(o => o.Status != DragonOrderStatus.Cancelado).Sum(o => o.RemainingBalance);
        public decimal TotalCashback => Orders.Where(o => o.Status != DragonOrderStatus.Cancelado).Sum(o => o.CashbackAmount);

        public bool HasDebt => TotalRemaining > 0;

        public void Refresh()
        {
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(TotalPaid));
            OnPropertyChanged(nameof(TotalRemaining));
            OnPropertyChanged(nameof(TotalCashback));
            OnPropertyChanged(nameof(HasDebt));
            OnPropertyChanged(nameof(Orders));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Navigation callback — set from MainWindow
    public Action? NavigateToCustomers { get; set; }

    public DragonOrdersViewModel(IDragonOrderRepository repository, ICustomerRepository customerRepository)
    {
        _repository = repository;
        _customerRepository = customerRepository;

        CreateOrderCommand = new RelayCommand(async _ => await CreateOrder(), _ => CanCreateOrder());
        AddItemCommand = new RelayCommand(async _ => await AddItem(), _ => CanAddItem());
        RemoveItemCommand = new RelayCommand<DragonOrderItem>(async item => await RemoveItem(item));
        AddPaymentCommand = new RelayCommand(async _ => await AddPayment(), _ => CanAddPayment());
        RemovePaymentCommand = new RelayCommand<DragonPayment>(async p => await RemovePayment(p));
        SelectOrderCommand = new RelayCommand<DragonOrder>(async o => await SelectOrderAsync(o));
        SelectCustomerSummaryCommand = new RelayCommand<CustomerSummary>(async s => await SelectCustomerSummaryAsync(s));
        ConfirmOrderCommand = new RelayCommand(async _ => await ConfirmOrder());
        MarkSentCommand = new RelayCommand(async _ => await MarkSent());
        MarkDeliveredCommand = new RelayCommand(async _ => await MarkDelivered());
        MarkFactoryPaidCommand = new RelayCommand(async _ => await MarkFactoryPaid());
        CancelOrderCommand = new RelayCommand(async _ => await CancelOrder());
        ClearFormCommand = new RelayCommand(_ => ClearForm());
        RefreshCommand = new RelayCommand(async _ => await LoadOrders());
        DeleteOrderCommand = new RelayCommand(async _ => await DeleteOrder());
        GoToCustomersCommand = new RelayCommand(_ => NavigateToCustomers?.Invoke());
        SelectCustomerCommand = new RelayCommand<Customer>(async c => await SelectCustomerAsync(c));
        ToggleViewModeCommand = new RelayCommand(_ => { ViewMode = ViewMode == "Orders" ? "Customers" : "Orders"; });

        NextPageCommand = new RelayCommand(async _ => { CurrentPage++; await LoadOrders(); }, _ => CurrentPage < TotalPages);
        PrevPageCommand = new RelayCommand(async _ => { CurrentPage--; await LoadOrders(); }, _ => CurrentPage > 1);

        _ = Initialize();
    }

    // Pagination
    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; OnPropertyChanged(); }
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set { _pageSize = value; OnPropertyChanged(); }
    }

    private int _totalOrdersCount;
    public int TotalOrdersCount
    {
        get => _totalOrdersCount;
        set { _totalOrdersCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPages)); }
    }

    public int TotalPages => (int)Math.Ceiling((double)TotalOrdersCount / PageSize);

    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }

    // View State
    private string _viewMode = "Orders"; // "Orders" or "Customers"
    public string ViewMode
    {
        get => _viewMode;
        set { _viewMode = value; OnPropertyChanged(); }
    }

    // Collections
    private ObservableCollection<DragonOrder> _orders = new();
    public ObservableCollection<DragonOrder> Orders
    {
        get => _orders;
        set { _orders = value; OnPropertyChanged(); UpdateCustomerSummaries(); }
    }

    private ObservableCollection<CustomerSummary> _customerSummaries = new();
    public ObservableCollection<CustomerSummary> CustomerSummaries
    {
        get => _customerSummaries;
        set { _customerSummaries = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredCustomerSummaries)); }
    }

    private void UpdateCustomerSummaries()
    {
        var summaries = Orders
            .GroupBy(o => o.CustomerId ?? Guid.Empty)
            .Select(g => new CustomerSummary
            {
                CustomerId = g.Key == Guid.Empty ? null : g.Key,
                Name = g.First().CustomerName,
                Contact = g.First().CustomerContact,
                Orders = new ObservableCollection<DragonOrder>(g.OrderByDescending(o => o.CreatedAt))
            })
            .OrderBy(s => s.Name)
            .ToList();

        CustomerSummaries = new ObservableCollection<CustomerSummary>(summaries);
    }

    private ObservableCollection<Customer> _customers = new();
    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set { _customers = value; OnPropertyChanged(); }
    }

    // Selected order
    private DragonOrder? _selectedOrder;
    public DragonOrder? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            _selectedOrder = value;
            if (value != null) SelectedCustomerSummary = null; // Clear customer selection if order is picked
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedOrder));
            OnPropertyChanged(nameof(SelectedOrderItems));
            OnPropertyChanged(nameof(SelectedOrderPayments));
        }
    }

    private CustomerSummary? _selectedCustomerSummary;
    public CustomerSummary? SelectedCustomerSummary
    {
        get => _selectedCustomerSummary;
        set
        {
            _selectedCustomerSummary = value;
            if (value != null) SelectedOrder = null; // Clear order selection if customer summary is picked
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedCustomerSummary));
        }
    }

    public bool HasSelectedOrder => SelectedOrder != null;
    public bool HasSelectedCustomerSummary => SelectedCustomerSummary != null;
    public IReadOnlyList<DragonOrderItem>? SelectedOrderItems => SelectedOrder?.Items;
    public IReadOnlyList<DragonPayment>? SelectedOrderPayments => SelectedOrder?.Payments?.OrderByDescending(p => p.PaidAt).ToList();

    // Smart status-based visibility
    public bool CanConfirm => SelectedOrder != null && SelectedOrder.Status == DragonOrderStatus.Pendente;
    public bool CanMarkSent => SelectedOrder != null && SelectedOrder.Status == DragonOrderStatus.Confirmado;
    public bool CanMarkDelivered => SelectedOrder != null && SelectedOrder.Status == DragonOrderStatus.EnviadoPelaFabrica;
    public bool CanMarkFactoryPaid => SelectedOrder != null && SelectedOrder.Status != DragonOrderStatus.Cancelado;
    public bool CanCancelOrder => SelectedOrder != null && SelectedOrder.Status != DragonOrderStatus.Cancelado && SelectedOrder.Status != DragonOrderStatus.Entregue;
    public bool CanDeleteOrder => SelectedOrder != null;

    // Customer search
    private string _customerSearchText = string.Empty;
    public string CustomerSearchText
    {
        get => _customerSearchText;
        set { _customerSearchText = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredCustomers)); }
    }

    public ObservableCollection<Customer> FilteredCustomers
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CustomerSearchText) || CustomerSearchText.Length < 2)
                return new ObservableCollection<Customer>(Customers.Take(10));

            var search = CustomerSearchText.ToLowerInvariant();
            return new ObservableCollection<Customer>(Customers.Where(c =>
                c.Name.ToLowerInvariant().Contains(search) ||
                c.Phone.Contains(search) ||
                c.Email.ToLowerInvariant().Contains(search) ||
                c.Document.Contains(search)
            ).Take(15));
        }
    }

    private Customer? _selectedCustomer;
    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set { _selectedCustomer = value; OnPropertyChanged(); }
    }

    // Metrics
    private decimal _totalReceivable;
    public decimal TotalReceivable { get => _totalReceivable; set { _totalReceivable = value; OnPropertyChanged(); } }

    private decimal _totalFactoryDebt;
    public decimal TotalFactoryDebt { get => _totalFactoryDebt; set { _totalFactoryDebt = value; OnPropertyChanged(); } }

    private decimal _totalReceivedMonth;
    public decimal TotalReceivedMonth { get => _totalReceivedMonth; set { _totalReceivedMonth = value; OnPropertyChanged(); } }

    private decimal _totalCashback;
    public decimal TotalCashback { get => _totalCashback; set { _totalCashback = value; OnPropertyChanged(); } }

    // Form: New Order
    private string _customerName = string.Empty;
    public string CustomerName { get => _customerName; set { _customerName = value; OnPropertyChanged(); } }

    private string _customerContact = string.Empty;
    public string CustomerContact { get => _customerContact; set { _customerContact = value; OnPropertyChanged(); } }

    private bool _isOwnOrder;
    public bool IsOwnOrder { get => _isOwnOrder; set { _isOwnOrder = value; OnPropertyChanged(); if (value) CustomerName = "MEU PEDIDO"; } }

    private bool _isClubeDragon;
    public bool IsClubeDragon { get => _isClubeDragon; set { _isClubeDragon = value; OnPropertyChanged(); } }

    private BoxType _selectedBoxType = BoxType.DragonFire;
    public BoxType SelectedBoxType { get => _selectedBoxType; set { _selectedBoxType = value; OnPropertyChanged(); } }

    // Item-level box type
    private BoxType _itemBoxType = BoxType.DragonFire;
    public BoxType ItemBoxType { get => _itemBoxType; set { _itemBoxType = value; OnPropertyChanged(); } }

    public Array BoxTypes => Enum.GetValues(typeof(BoxType));

    private string _shippingCost = "0";
    public string ShippingCost { get => _shippingCost; set { _shippingCost = value; OnPropertyChanged(); } }

    private string _orderNotes = string.Empty;
    public string OrderNotes { get => _orderNotes; set { _orderNotes = value; OnPropertyChanged(); } }

    // Form: Add Item — description is auto-generated from BoxType
    private string _itemDescription = string.Empty;
    public string ItemDescription 
    { 
        get => _itemDescription; 
        set 
        { 
            _itemDescription = value; 
            OnPropertyChanged(); 
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        } 
    }

    private string _itemQuantity = "1";
    public string ItemQuantity 
    { 
        get => _itemQuantity; 
        set 
        { 
            _itemQuantity = value; 
            OnPropertyChanged(); 
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        } 
    }

    private string _itemUnitCost = string.Empty;
    public string ItemUnitCost { get => _itemUnitCost; set { _itemUnitCost = value; OnPropertyChanged(); } }

    private string _itemUnitPrice = string.Empty;
    public string ItemUnitPrice { get => _itemUnitPrice; set { _itemUnitPrice = value; OnPropertyChanged(); } }

    // Form: Add Payment
    private string _paymentAmount = string.Empty;
    public string PaymentAmount { get => _paymentAmount; set { _paymentAmount = value; OnPropertyChanged(); } }

    private DragonPaymentMethod _paymentMethod = DragonPaymentMethod.Pix;
    public DragonPaymentMethod PaymentMethod { get => _paymentMethod; set { _paymentMethod = value; OnPropertyChanged(); } }

    public Array PaymentMethods => Enum.GetValues(typeof(DragonPaymentMethod));

    private string _paymentNotes = string.Empty;
    public string PaymentNotes { get => _paymentNotes; set { _paymentNotes = value; OnPropertyChanged(); } }

    // Filter
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set 
        { 
            _searchText = value; 
            OnPropertyChanged(); 
            CurrentPage = 1; 
            _ = LoadOrders(); 
        }
    }

    private string _statusFilter = "Todos";
    public string StatusFilter
    {
        get => _statusFilter;
        set 
        { 
            _statusFilter = value; 
            OnPropertyChanged(); 
            CurrentPage = 1; 
            _ = LoadOrders(); 
        }
    }

    public ObservableCollection<DragonOrder> FilteredOrders
    {
        get
        {
            var filtered = Orders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(o =>
                    o.CustomerName.ToLowerInvariant().Contains(search) ||
                    (o.Notes?.ToLowerInvariant().Contains(search) ?? false));
            }

            filtered = StatusFilter switch
            {
                "Pendentes" => filtered.Where(o => !o.IsFullyPaid && o.Status != DragonOrderStatus.Cancelado && !o.IsOwnOrder),
                "Pagos" => filtered.Where(o => o.IsFullyPaid),
                "Fábrica Pendente" => filtered.Where(o => !o.IsFactoryPaid && o.Status != DragonOrderStatus.Cancelado),
                "Meus Pedidos" => filtered.Where(o => o.IsOwnOrder),
                _ => filtered
            };

            return new ObservableCollection<DragonOrder>(filtered);
        }
    }

    // Status
    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

    // Commands
    public ICommand CreateOrderCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand AddPaymentCommand { get; }
    public ICommand RemovePaymentCommand { get; }
    public ICommand SelectOrderCommand { get; }
    public ICommand SelectCustomerSummaryCommand { get; }
    public ICommand ConfirmOrderCommand { get; }
    public ICommand MarkSentCommand { get; }
    public ICommand MarkDeliveredCommand { get; }
    public ICommand MarkFactoryPaidCommand { get; }
    public ICommand CancelOrderCommand { get; }
    public ICommand ClearFormCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand DeleteOrderCommand { get; }
    public ICommand GoToCustomersCommand { get; }
    public ICommand SelectCustomerCommand { get; }
    public ICommand ToggleViewModeCommand { get; }

    public ObservableCollection<CustomerSummary> FilteredCustomerSummaries
    {
        get
        {
            var filtered = CustomerSummaries.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(s =>
                    s.Name.ToLowerInvariant().Contains(search) ||
                    s.Contact.ToLowerInvariant().Contains(search));
            }

            if (StatusFilter == "Pendentes")
            {
                filtered = filtered.Where(s => s.HasDebt);
            }

            return new ObservableCollection<CustomerSummary>(filtered);
        }
    }

    private async Task Initialize()
    {
        await LoadCustomers();
        await LoadOrders();
    }

    private async Task LoadCustomers()
    {
        try
        {
            var customers = await _customerRepository.GetAllAsync();
            Customers = new ObservableCollection<Customer>(customers.Where(c => c.IsActive));
            OnPropertyChanged(nameof(FilteredCustomers));
        }
        catch { /* silent */ }
    }

    private Task SelectCustomerAsync(Customer? customer)
    {
        if (customer == null) return Task.CompletedTask;
        SelectedCustomer = customer;
        CustomerName = customer.Name;
        CustomerContact = $"{customer.Phone} / {customer.Email}";
        return Task.CompletedTask;
    }

    private async Task LoadOrders()
    {
        try
        {
            StatusMessage = "Carregando...";
            
            // Map string filter to enum
            DragonOrderStatus? statusFilter = StatusFilter switch
            {
                "Pendentes" => DragonOrderStatus.Pendente,
                "Cancelado" => DragonOrderStatus.Cancelado,
                _ => null
            };

            var pagedResult = await _repository.GetPagedAsync(CurrentPage, PageSize, SearchText, statusFilter);
            
            Orders = new ObservableCollection<DragonOrder>(pagedResult.Items);
            TotalOrdersCount = pagedResult.TotalCount;

            // Metrics still need a full or partial calculation, but for now we update based on visible or recent
            // For true metrics, we might need a separate repository call or keep a "TotalSummary"
            
            OnPropertyChanged(nameof(FilteredOrders));
            OnPropertyChanged(nameof(FilteredCustomerSummaries));
            StatusMessage = $"{TotalOrdersCount} pedidos (Pág {CurrentPage}/{TotalPages})";
            
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            StatusMessage = "Erro ao carregar pedidos";
            ShowNotification($"Erro ao carregar dados: {ex.Message}", true);
        }
    }

    private bool CanCreateOrder() => 
        (IsOwnOrder && !string.IsNullOrWhiteSpace(CustomerName)) || 
        (!IsOwnOrder && SelectedCustomer != null);

    private async Task CreateOrder()
    {
        try
        {
            if (!CanCreateOrder())
            {
                ShowNotification("Dados do cliente incompletos", true);
                return;
            }

            decimal.TryParse(ShippingCost.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var shipping);

            var name = IsOwnOrder ? "MEU PEDIDO" : SelectedCustomer!.Name;
            var contact = IsOwnOrder ? string.Empty : $"{SelectedCustomer!.Phone} / {SelectedCustomer!.Email}";

            var order = new DragonOrder(
                customerName: name,
                customerContact: contact,
                isClubeDragon: IsClubeDragon,
                isOwnOrder: IsOwnOrder,
                shippingCost: shipping,
                customerId: IsOwnOrder ? null : SelectedCustomer?.Id,
                notes: string.IsNullOrWhiteSpace(OrderNotes) ? null : OrderNotes);

            await _repository.AddAsync(order);
            
            ShowNotification("Pedido criado com sucesso!");
            ClearForm();
            await LoadOrders();

            // Auto-select the newly created order
            SelectedOrder = Orders.FirstOrDefault(o => o.Id == order.Id);
            ViewMode = "Orders";
        }
        catch (Exception ex)
        {
            ShowNotification($"Erro ao criar pedido: {ex.Message}", true);
        }
    }

    private bool _isAddingItem;
    public bool IsAddingItem
    {
        get => _isAddingItem;
        set { _isAddingItem = value; OnPropertyChanged(); }
    }

    private bool CanAddItem() =>
        SelectedOrder != null &&
        int.TryParse(ItemQuantity, out var q) && q > 0;

    private async Task AddItem()
    {
        if (SelectedOrder == null) return;
        IsAddingItem = true;
        try
        {
            if (!int.TryParse(ItemQuantity, out var qty) || qty <= 0)
            {
                ShowNotification("Quantidade inválida", true);
                return;
            }

            decimal.TryParse(ItemUnitCost.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var cost);
            decimal.TryParse(ItemUnitPrice.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var price);

            var desc = string.IsNullOrWhiteSpace(ItemDescription)
                ? $"Caixa {ItemBoxType}"
                : ItemDescription;

            SelectedOrder.AddItem(desc, qty, cost, price, ItemBoxType);
            await _repository.UpdateAsync(SelectedOrder);

            ItemDescription = string.Empty;
            ItemQuantity = "1";
            ItemUnitCost = string.Empty;
            ItemUnitPrice = string.Empty;

            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification("Item adicionado ao pedido!");
        }
        catch (Exception ex)
        {
            ShowNotification($"Erro ao adicionar item: {ex.Message}", true);
        }
        finally
        {
            IsAddingItem = false;
        }
    }

    private async Task RemoveItem(DragonOrderItem? item)
    {
        if (SelectedOrder == null || item == null) return;

        try
        {
            SelectedOrder.RemoveItem(item.Id);
            await _repository.UpdateAsync(SelectedOrder);
            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification("Item removido");
        }
        catch (Exception ex)
        {
            ShowNotification($"Erro ao remover item: {ex.Message}", true);
        }
    }

    private bool CanAddPayment() =>
        SelectedOrder != null &&
        decimal.TryParse(PaymentAmount?.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var amt) && amt > 0;

    private async Task AddPayment()
    {
        if (SelectedOrder == null) return;

        try
        {
            if (!decimal.TryParse(PaymentAmount.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                ShowNotification("Valor de pagamento inválido", true);
                return;
            }

            SelectedOrder.AddPayment(amount, PaymentMethod,
                string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes);
            await _repository.UpdateAsync(SelectedOrder);

            PaymentAmount = string.Empty;
            PaymentNotes = string.Empty;

            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification($"Pagamento de R$ {amount:N2} registrado!");
        }
        catch (Exception ex)
        {
            ShowNotification($"Erro ao registrar pagamento: {ex.Message}", true);
        }
    }

    private async Task RemovePayment(DragonPayment? payment)
    {
        if (SelectedOrder == null || payment == null) return;

        try
        {
            SelectedOrder.RemovePayment(payment.Id);
            await _repository.UpdateAsync(SelectedOrder);
            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification("Pagamento removido");
        }
        catch (Exception ex)
        {
            ShowNotification($"Erro ao remover pagamento: {ex.Message}", true);
        }
    }

    private Task SelectOrderAsync(DragonOrder? order)
    {
        SelectedOrder = order;
        SelectedCustomerSummary = null;
        return Task.CompletedTask;
    }

    private Task SelectCustomerSummaryAsync(CustomerSummary? summary)
    {
        SelectedCustomerSummary = summary;
        SelectedOrder = null;
        return Task.CompletedTask;
    }

    private async Task ConfirmOrder()
    {
        if (SelectedOrder == null) return;
        try
        {
            SelectedOrder.Confirm();
            await _repository.UpdateAsync(SelectedOrder);
            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification("Pedido confirmado!");
        }
        catch (Exception ex) { ShowNotification($"Erro: {ex.Message}", true); }
    }

    private async Task MarkSent()
    {
        if (SelectedOrder == null) return;
        try
        {
            SelectedOrder.MarkAsSentByFactory();
            await _repository.UpdateAsync(SelectedOrder);
            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification("Pedido enviado!");
        }
        catch (Exception ex) { ShowNotification($"Erro: {ex.Message}", true); }
    }

    private async Task MarkDelivered()
    {
        if (SelectedOrder == null) return;
        try
        {
            SelectedOrder.MarkAsDelivered();
            await _repository.UpdateAsync(SelectedOrder);
            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification("Pedido entregue!");
        }
        catch (Exception ex) { ShowNotification($"Erro: {ex.Message}", true); }
    }

    private async Task MarkFactoryPaid()
    {
        if (SelectedOrder == null) return;
        try
        {
            if (SelectedOrder.IsFactoryPaid)
                SelectedOrder.UnmarkFactoryAsPaid();
            else
                SelectedOrder.MarkFactoryAsPaid();

            await _repository.UpdateAsync(SelectedOrder);
            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification(SelectedOrder.IsFactoryPaid ? "Fábrica paga!" : "Pagamento fábrica desmarcado");
        }
        catch (Exception ex) { ShowNotification($"Erro: {ex.Message}", true); }
    }

    private async Task CancelOrder()
    {
        if (SelectedOrder == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Deseja realmente cancelar o pedido de {SelectedOrder.CustomerName}?\nEsta ação não pode ser desfeita.",
            "Confirmar Cancelamento",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            SelectedOrder.Cancel();
            await _repository.UpdateAsync(SelectedOrder);
            RefreshSelectedOrder();
            await LoadOrders();
            ShowNotification("Pedido cancelado");
        }
        catch (Exception ex) { ShowNotification($"Erro: {ex.Message}", true); }
    }

    private async Task DeleteOrder()
    {
        if (SelectedOrder == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Deseja excluir permanentemente o pedido de {SelectedOrder.CustomerName}?\nTodos os itens e pagamentos serão removidos.",
            "Confirmar Exclusão",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            await _repository.DeleteAsync(SelectedOrder.Id);
            SelectedOrder = null;
            await LoadOrders();
            ShowNotification("Pedido excluído permanentemente");
        }
        catch (Exception ex) { ShowNotification($"Erro ao excluir: {ex.Message}", true); }
    }

    private void ClearForm()
    {
        CustomerName = string.Empty;
        CustomerContact = string.Empty;
        CustomerSearchText = string.Empty;
        SelectedCustomer = null;
        IsOwnOrder = false;
        IsClubeDragon = false;
        SelectedBoxType = BoxType.DragonFire;
        ItemBoxType = BoxType.DragonFire;
        ShippingCost = "0";
        OrderNotes = string.Empty;
        SelectedOrder = null;
        SelectedCustomerSummary = null;
    }

    private void RefreshSelectedOrder()
    {
        OnPropertyChanged(nameof(SelectedOrder));
        OnPropertyChanged(nameof(SelectedOrderItems));
        OnPropertyChanged(nameof(SelectedOrderPayments));
        OnPropertyChanged(nameof(HasSelectedOrder));
        OnPropertyChanged(nameof(HasSelectedCustomerSummary));
        OnPropertyChanged(nameof(CanConfirm));
        OnPropertyChanged(nameof(CanMarkSent));
        OnPropertyChanged(nameof(CanMarkDelivered));
        OnPropertyChanged(nameof(CanMarkFactoryPaid));
        OnPropertyChanged(nameof(CanCancelOrder));
        OnPropertyChanged(nameof(CanDeleteOrder));

        if (SelectedCustomerSummary != null)
        {
            SelectedCustomerSummary.Refresh();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Func<object?, Task>? _executeAsync;
    private readonly Action<object?>? _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public async void Execute(object? parameter)
    {
        if (_executeAsync != null) await _executeAsync(parameter);
        else _execute?.Invoke(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _executeAsync;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Func<T?, Task> executeAsync, Func<T?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public async void Execute(object? parameter) => await _executeAsync((T?)parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
