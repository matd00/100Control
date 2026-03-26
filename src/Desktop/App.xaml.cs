using Desktop.Infrastructure;
using Serilog;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace Desktop;

public partial class App : System.Windows.Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    public static ISnackbarMessageQueue SnackbarMessageQueue { get; } = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup global exception handling
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            // Configure services and database
            ServiceProvider = ServiceProviderConfiguration.ConfigureServices();
            Log.Information("APP STARTUP: Serviços configurados com sucesso");

            // Initialize database (create if not exists)
            Log.Information("APP STARTUP: Inicializando banco de dados...");
            ServiceProviderConfiguration.InitializeDatabase(ServiceProvider);
            Log.Information("APP STARTUP: Banco de dados inicializado");

            Log.Information("APP STARTUP: Criando MainWindow...");
            var mainWindow = new MainWindow();
            Log.Information("APP STARTUP: MainWindow criada, exibindo...");
            mainWindow.Show();
            Log.Information("=== APP STARTUP: Aplicação iniciada com sucesso ===");
        }
        catch (System.Exception ex)
        {
            Log.Fatal(ex, "!!! APP STARTUP ERRO CRÍTICO");
            System.Windows.MessageBox.Show($"Erro ao iniciar a aplicação: {ex.Message}\n\nConsulte os logs para mais detalhes.", "Erro Fatal", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Dispatcher Unhandled Exception");
        System.Windows.MessageBox.Show($"Ocorreu um erro inesperado: {e.Exception.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Fatal(ex, "Unhandled Domain Exception");
        }
        else
        {
            Log.Fatal("Unhandled Domain Exception (non-exception object): {ExceptionObject}", e.ExceptionObject);
        }
    }
}
