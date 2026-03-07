namespace Application.Services;

public interface IAutomationService : IDisposable
{
    Task StartAsync();
    Task StopAsync();
}

public class AutomationService : IAutomationService
{
    private readonly IMarketplaceSyncService _marketplaceSyncService;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _automationTask;
    private bool _disposed = false;

    public AutomationService(IMarketplaceSyncService marketplaceSyncService)
    {
        _marketplaceSyncService = marketplaceSyncService ?? throw new ArgumentNullException(nameof(marketplaceSyncService));
    }

    public async Task StartAsync()
    {
        if (_automationTask != null && !_automationTask.IsCompleted)
        {
            return;  // Already running
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        // Sync marketplaces every 5 minutes
        _automationTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _marketplaceSyncService.SyncMercadoLivreOrdersAsync();
                    await _marketplaceSyncService.SyncShopeeOrdersAsync();
                    await _marketplaceSyncService.UpdateAllStockAsync();

                    // Wait 5 minutes or until cancellation is requested
                    await Task.Delay(TimeSpan.FromMinutes(5), token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    System.Diagnostics.Debug.WriteLine($"Automation error: {ex.Message}");

                    // Wait before retrying
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }, token);

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();

            if (_automationTask != null)
            {
                try
                {
                    await _automationTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
            _automationTask?.Dispose();
        }

        _disposed = true;
    }

    ~AutomationService()
    {
        Dispose(false);
    }
}
