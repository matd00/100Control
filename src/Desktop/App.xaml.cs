using Desktop.Infrastructure;
using System.Diagnostics;

namespace Desktop;

public partial class App : System.Windows.Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Debug.WriteLine("=== APP STARTUP: Iniciando aplicação ===");

            // Configure services and database
            Debug.WriteLine("APP STARTUP: Configurando serviços...");
            ServiceProvider = ServiceProviderConfiguration.ConfigureServices();
            Debug.WriteLine("APP STARTUP: Serviços configurados com sucesso");

            // Initialize database (create if not exists)
            Debug.WriteLine("APP STARTUP: Inicializando banco de dados...");
            ServiceProviderConfiguration.InitializeDatabase(ServiceProvider);
            Debug.WriteLine("APP STARTUP: Banco de dados inicializado");

            Debug.WriteLine("APP STARTUP: Criando MainWindow...");
            var mainWindow = new MainWindow();
            Debug.WriteLine("APP STARTUP: MainWindow criada, exibindo...");
            mainWindow.Show();
            Debug.WriteLine("=== APP STARTUP: Aplicação iniciada com sucesso ===");
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"!!! APP STARTUP ERRO CRÍTICO: {ex.GetType().Name}");
            Debug.WriteLine($"!!! Mensagem: {ex.Message}");
            Debug.WriteLine($"!!! StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"!!! InnerException: {ex.InnerException.Message}");
                Debug.WriteLine($"!!! InnerException StackTrace: {ex.InnerException.StackTrace}");
            }

            System.Windows.MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}", "Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
