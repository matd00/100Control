using Desktop.Infrastructure;

namespace Desktop;

public partial class App : System.Windows.Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Configure services and database
            ServiceProvider = ServiceProviderConfiguration.ConfigureServices();

            // Initialize database (create if not exists)
            ServiceProviderConfiguration.InitializeDatabase(ServiceProvider);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}", "Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
