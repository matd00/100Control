using PaintballManager.Desktop.Infrastructure.MVVM;

namespace PaintballManager.Desktop.Features.Dashboard.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private decimal _todaysSales;
    public decimal TodaysSales
    {
        get => _todaysSales;
        set => SetProperty(ref _todaysSales, value);
    }

    private int _pendingOrders;
    public int PendingOrders
    {
        get => _pendingOrders;
        set => SetProperty(ref _pendingOrders, value);
    }

    private int _lowStockItems;
    public int LowStockItems
    {
        get => _lowStockItems;
        set => SetProperty(ref _lowStockItems, value);
    }

    private decimal _todaysProfit;
    public decimal TodaysProfit
    {
        get => _todaysProfit;
        set => SetProperty(ref _todaysProfit, value);
    }

    public DashboardViewModel()
    {
        LoadDashboardData();
    }

    private void LoadDashboardData()
    {
        // TODO: Load real data from repositories
        TodaysSales = 0m;
        PendingOrders = 0;
        LowStockItems = 0;
        TodaysProfit = 0m;
    }
}
