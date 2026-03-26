using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Interfaces.Repositories;

namespace Desktop.Features.Dashboard;

public enum DashboardPeriod
{
    Today,
    Last7Days,
    Last30Days,
    Last90Days,
    AllTime
}

public class DashboardViewModel : ViewModelBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IFactoryOrderRepository _factoryOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly Integrations.SuperFrete.Interfaces.ISuperFreteService _superFreteService;

    private DashboardPeriod _selectedPeriod = DashboardPeriod.Last30Days;
    private decimal _totalRevenue;
    private decimal _totalProfit;
    private int _orderCount;
    private decimal _averageOrderValue;
    
    private int _pendingOrders;
    private int _lowStockProductsCount;
    private int _factoryPendingOrders;
    private decimal _factoryPeriodRevenue;
    private int _totalLabelsCount;

    public DashboardPeriod SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                _ = LoadDashboardDataAsync();
            }
        }
    }

    public decimal TotalRevenue
    {
        get => _totalRevenue;
        set => SetProperty(ref _totalRevenue, value);
    }

    public decimal TotalProfit
    {
        get => _totalProfit;
        set => SetProperty(ref _totalProfit, value);
    }

    public int OrderCount
    {
        get => _orderCount;
        set => SetProperty(ref _orderCount, value);
    }

    public decimal AverageOrderValue
    {
        get => _averageOrderValue;
        set => SetProperty(ref _averageOrderValue, value);
    }

    public int PendingOrders
    {
        get => _pendingOrders;
        set => SetProperty(ref _pendingOrders, value);
    }

    public int LowStockProductsCount
    {
        get => _lowStockProductsCount;
        set => SetProperty(ref _lowStockProductsCount, value);
    }

    public int FactoryPendingOrders
    {
        get => _factoryPendingOrders;
        set => SetProperty(ref _factoryPendingOrders, value);
    }

    public decimal FactoryPeriodRevenue
    {
        get => _factoryPeriodRevenue;
        set => SetProperty(ref _factoryPeriodRevenue, value);
    }

    public int TotalLabelsCount
    {
        get => _totalLabelsCount;
        set => SetProperty(ref _totalLabelsCount, value);
    }

    public ObservableCollection<RecentOrderViewModel> RecentOrders { get; } = new();
    public ObservableCollection<TopProductViewModel> TopProducts { get; } = new();
    public ObservableCollection<SalesSourceViewModel> SalesBySource { get; } = new();

    public ICommand RefreshCommand { get; }

    public DashboardViewModel(
        IOrderRepository orderRepository, 
        IProductRepository productRepository,
        IFactoryOrderRepository factoryOrderRepository,
        ICustomerRepository customerRepository,
        Integrations.SuperFrete.Interfaces.ISuperFreteService superFreteService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _factoryOrderRepository = factoryOrderRepository ?? throw new ArgumentNullException(nameof(factoryOrderRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _superFreteService = superFreteService ?? throw new ArgumentNullException(nameof(superFreteService));

        RefreshCommand = new AsyncRelayCommand(LoadDashboardDataAsync);

        // Load initial data
        _ = LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            var orders = await _orderRepository.GetAllAsync();
            var products = await _productRepository.GetAllAsync();
            var factoryOrders = await _factoryOrderRepository.GetAllAsync();
            var customers = await _customerRepository.GetAllAsync();

            // Fetch SuperFrete label count asynchronously
            _ = Task.Run(async () => {
                try {
                    var labels = await _superFreteService.ListLabelsAsync();
                    TotalLabelsCount = labels.Count;
                } catch { /* Ignore SF errors in dashboard */ }
            });
            DateTime startDate = SelectedPeriod switch
            {
                DashboardPeriod.Today => DateTime.UtcNow.Date,
                DashboardPeriod.Last7Days => DateTime.UtcNow.AddDays(-7),
                DashboardPeriod.Last30Days => DateTime.UtcNow.AddDays(-30),
                DashboardPeriod.Last90Days => DateTime.UtcNow.AddDays(-90),
                DashboardPeriod.AllTime => DateTime.MinValue,
                _ => DateTime.UtcNow.AddDays(-30)
            };

            var periodOrders = orders
                .Where(o => o.CreatedAt >= startDate && o.Status != Domain.Entities.OrderStatus.Cancelled)
                .ToList();

            TotalRevenue = periodOrders.Sum(o => o.TotalAmount);
            OrderCount = periodOrders.Count;
            AverageOrderValue = OrderCount > 0 ? TotalRevenue / OrderCount : 0;

            // Calculate profit using actual product costs
            decimal totalProfit = 0;
            var productDict = products.ToDictionary(p => p.Id);

            foreach (var order in periodOrders)
            {
                foreach (var item in order.Items)
                {
                    if (productDict.TryGetValue(item.ProductId, out var product))
                    {
                        totalProfit += item.Subtotal - (product.Cost * item.Quantity);
                    }
                    else
                    {
                        // Fallback if product not found (30% margin estimate)
                        totalProfit += item.Subtotal * 0.30m;
                    }
                }
            }
            TotalProfit = totalProfit;

            // Global Metrics (not affected by period selection)
            PendingOrders = orders.Count(o => o.Status == Domain.Entities.OrderStatus.Pending);
            LowStockProductsCount = products.Count(p => p.IsActive && p.Stock < 5);
            FactoryPendingOrders = factoryOrders.Count(o => o.Status == Domain.Entities.FactoryOrderStatus.Pendente);
            
            // Factory Period Revenue
            FactoryPeriodRevenue = factoryOrders
                .Where(o => o.CreatedAt >= startDate && o.Status != Domain.Entities.FactoryOrderStatus.Cancelado)
                .Sum(o => o.TotalSalePrice);

            // Recent orders
            RecentOrders.Clear();
            foreach (var order in orders.OrderByDescending(o => o.CreatedAt).Take(8))
            {
                customerDict.TryGetValue(order.CustomerId, out var customer);

                RecentOrders.Add(new RecentOrderViewModel
                {
                    OrderId = order.Id.ToString()[..8],
                    CustomerName = customer?.Name ?? "Cliente",
                    Total = order.TotalAmount,
                    Status = order.Status.ToString(),
                    Date = order.CreatedAt
                });
            }

            // Top Products
            var topProductsData = periodOrders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Subtotal)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToList();

            TopProducts.Clear();
            foreach (var item in topProductsData)
            {
                if (productDict.TryGetValue(item.ProductId, out var product))
                {
                    TopProducts.Add(new TopProductViewModel
                    {
                        ProductName = product.Name,
                        Quantity = item.TotalQuantity,
                        Revenue = item.TotalRevenue
                    });
                }
            }

            // Sales by Source
            var salesBySourceData = periodOrders
                .GroupBy(o => o.Source)
                .Select(g => new SalesSourceViewModel
                {
                    Source = g.Key.ToString(),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    Percentage = periodOrders.Count > 0 ? (double)g.Count() / periodOrders.Count * 100 : 0
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            SalesBySource.Clear();
            foreach (var item in salesBySourceData)
            {
                SalesBySource.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
        }
    }
}

public class RecentOrderViewModel
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class TopProductViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesSourceViewModel
{
    public string Source { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public double Percentage { get; set; }
}
