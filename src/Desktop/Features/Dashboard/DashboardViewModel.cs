using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Interfaces.Repositories;

namespace Desktop.Features.Dashboard;

public class DashboardViewModel : ViewModelBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    private decimal _todaysSales;
    private int _pendingOrders;
    private int _lowStockProducts;
    private decimal _todaysProfit;

    public decimal TodaysSales
    {
        get => _todaysSales;
        set => SetProperty(ref _todaysSales, value);
    }

    public int PendingOrders
    {
        get => _pendingOrders;
        set => SetProperty(ref _pendingOrders, value);
    }

    public int LowStockProducts
    {
        get => _lowStockProducts;
        set => SetProperty(ref _lowStockProducts, value);
    }

    public decimal TodaysProfit
    {
        get => _todaysProfit;
        set => SetProperty(ref _todaysProfit, value);
    }

    public ObservableCollection<RecentOrderViewModel> RecentOrders { get; } = new();

    public ICommand RefreshCommand { get; }

    public DashboardViewModel(IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));

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

            var todaysOrders = orders
                .Where(o => o.CreatedAt.Date == DateTime.UtcNow.Date)
                .ToList();

            TodaysSales = todaysOrders
                .Where(o => o.Status != Domain.Entities.OrderStatus.Cancelled)
                .Sum(o => o.TotalAmount);

            PendingOrders = orders.Count(o => o.Status == Domain.Entities.OrderStatus.Pending);

            LowStockProducts = products.Count(p => p.IsActive && p.Stock < 5);

            // Calculate profit (simplified: Price - Cost)
            TodaysProfit = TodaysSales * 0.30m; // Placeholder

            // Recent orders
            RecentOrders.Clear();
            foreach (var order in orders.OrderByDescending(o => o.CreatedAt).Take(10))
            {
                RecentOrders.Add(new RecentOrderViewModel
                {
                    OrderId = order.Id.ToString()[..8],
                    CustomerName = "Cliente",
                    Total = order.TotalAmount,
                    Status = order.Status.ToString(),
                    Date = order.CreatedAt
                });
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
