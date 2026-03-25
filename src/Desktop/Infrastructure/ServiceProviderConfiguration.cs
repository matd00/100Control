using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Domain.Interfaces.Repositories;
using Domain.Services;
using Application.UseCases.Orders;
using Application.UseCases.Products;
using Application.UseCases.Customers;
using Application.UseCases.Purchases;
using Application.UseCases.Shipments;
using Application.UseCases.FactoryOrders;
using Application.Services;
using Persistence.Context;
using Persistence.Repositories;
using Desktop.Features.Dashboard;
using Desktop.Features.Products;
using Desktop.Features.Orders;
using Desktop.Features.Customers;
using Desktop.Features.Suppliers;
using Desktop.Features.Purchases;
using Desktop.Features.Kits;
using Desktop.Features.Inventory;
using Desktop.Features.Shipments;
using Integrations.SuperFrete.Extensions;
using Integrations.SuperFrete.Interfaces;
using Integrations.SuperFrete.Configuration;
using Integrations.MercadoLivre.Interfaces;
using Integrations.MercadoLivre.Services;
using Integrations.Shopee.Interfaces;
using Integrations.Shopee.Services;

namespace Desktop.Infrastructure;

public static class ServiceProviderConfiguration
{
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PaintballManager",
        "paintballmanager.db");

    public static IServiceProvider ConfigureServices()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== ServiceProviderConfiguration: Iniciando ===");
            var services = new ServiceCollection();

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Configurando Configuration...");
            // Configuration with User Secrets
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
                .Build();
            System.Diagnostics.Debug.WriteLine($"  Base Path: {AppContext.BaseDirectory}");

            services.AddSingleton<IConfiguration>(configuration);

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Configurando banco de dados...");
            // Ensure directory exists
            var dbDirectory = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
            {
                System.Diagnostics.Debug.WriteLine($"  Criando diretório: {dbDirectory}");
                Directory.CreateDirectory(dbDirectory);
            }
            System.Diagnostics.Debug.WriteLine($"  DB Path: {DbPath}");

            // Database Context (SQLite)
            services.AddDbContext<PaintballManagerDbContext>(options =>
                options.UseSqlite($"Data Source={DbPath}"));

            // Unit of Work
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PaintballManagerDbContext>());

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Registrando repositórios...");
            // Repositories (Entity Framework)
            services.AddScoped<IProductRepository, EfProductRepository>();
            services.AddScoped<ICustomerRepository, EfCustomerRepository>();
            services.AddScoped<IOrderRepository, EfOrderRepository>();
            services.AddScoped<ISupplierRepository, EfSupplierRepository>();
            services.AddScoped<IPurchaseRepository, EfPurchaseRepository>();
            services.AddScoped<IKitRepository, EfKitRepository>();
            services.AddScoped<IShipmentRepository, EfShipmentRepository>();
            services.AddScoped<IPartRepository, EfPartRepository>();
            services.AddScoped<IInventoryMovementRepository, EfInventoryMovementRepository>();
            services.AddScoped<IFactoryOrderRepository, EfFactoryOrderRepository>();

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Registrando Use Cases...");
            // Use Cases
            services.AddTransient<CreateOrderUseCase>();
            services.AddTransient<UpdateOrderUseCase>();
            services.AddTransient<GetOrdersUseCase>();
            services.AddTransient<DeleteOrderUseCase>();
            services.AddTransient<CreateProductUseCase>();
            services.AddTransient<AdjustStockUseCase>();
            services.AddTransient<RegisterCustomerUseCase>();
            services.AddTransient<RegisterPurchaseUseCase>();
            services.AddTransient<GenerateShipmentUseCase>();
            services.AddTransient<UpdateOrderStatusUseCase>();

            // Factory Order Use Cases
            services.AddTransient<CreateFactoryOrderUseCase>();
            services.AddTransient<UpdateFactoryOrderStatusUseCase>();
            services.AddTransient<AddTrackingCodeUseCase>();

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Registrando Services...");
            // Services
            services.AddSingleton<ISmartSearchService, SmartSearchService>();
            services.AddScoped<IMarketplaceSyncService, MarketplaceSyncService>();
            services.AddScoped<IAutomationService, AutomationService>();

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Registrando Integration Services...");
            // Integration Services (placeholder tokens - configure with real values)
            services.AddScoped<IMercadoLivreService>(sp =>
                new MercadoLivreService(new HttpClient { BaseAddress = new Uri("https://api.mercadolibre.com") }, ""));
            services.AddScoped<IShopeeService>(sp =>
                new ShopeeService(new HttpClient { BaseAddress = new Uri("https://partner.shopeemobile.com") }, "", ""));

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Registrando SuperFrete...");
            // SuperFrete Integration
            try
            {
                services.AddSuperFrete(configuration);
                System.Diagnostics.Debug.WriteLine("  SuperFrete registrado com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  !!! ERRO ao registrar SuperFrete: {ex.Message}");
                throw;
            }

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Registrando ViewModels...");
            // ViewModels - All pages with CRUD
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<OrdersViewModel>();
            services.AddTransient<FactoryOrdersViewModel>();
            services.AddTransient<OrdersLayoutViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<SuppliersViewModel>();
            services.AddTransient<PurchasesViewModel>();
            services.AddTransient<KitsViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<ShipmentsViewModel>();

            System.Diagnostics.Debug.WriteLine("ServiceProviderConfiguration: Construindo ServiceProvider...");
            var serviceProvider = services.BuildServiceProvider();
            System.Diagnostics.Debug.WriteLine("=== ServiceProviderConfiguration: Concluído com sucesso ===");

            return serviceProvider;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! ERRO CRÍTICO em ServiceProviderConfiguration: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"!!! Mensagem: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"!!! StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public static void InitializeDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaintballManagerDbContext>();
        context.Database.Migrate();
    }
}
