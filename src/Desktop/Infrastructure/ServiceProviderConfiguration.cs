using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Domain.Interfaces;
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

using Serilog;
using Application;

namespace Desktop.Infrastructure;

public static class ServiceProviderConfiguration
{
    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PaintballManager",
        "paintballmanager.db");

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PaintballManager",
        "logs",
        "log-.txt");

    public static IServiceProvider ConfigureServices()
    {
        try
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(LogPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("=== ServiceProviderConfiguration: Iniciando ===");
            var services = new ServiceCollection();

            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            Log.Information("ServiceProviderConfiguration: Configurando Configuration...");
            // Configuration with User Secrets
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
                .Build();
            Log.Information("  Base Path: {BasePath}", AppContext.BaseDirectory);

            services.AddSingleton<IConfiguration>(configuration);

            Log.Information("ServiceProviderConfiguration: Configurando banco de dados...");
            // Ensure directory exists
            var dbDirectory = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
            {
                Log.Information("  Criando diretório: {DbDirectory}", dbDirectory);
                Directory.CreateDirectory(dbDirectory);
            }
            Log.Information("  DB Path: {DbPath}", DbPath);

            // Database Context (SQLite)
            services.AddDbContext<PaintballManagerDbContext>(options =>
                options.UseSqlite($"Data Source={DbPath}"));

            // Unit of Work
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PaintballManagerDbContext>());

            // Domain Events
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            Log.Information("ServiceProviderConfiguration: Registrando repositórios...");
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

            Log.Information("ServiceProviderConfiguration: Registrando Use Cases e MediatR...");
            // Application Services (MediatR, Validators, etc.)
            services.AddApplication();

            // Legacy Use Cases (Refactoring to MediatR in progress)
            services.AddTransient<UpdateOrderUseCase>();

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

            Log.Information("ServiceProviderConfiguration: Registrando Services...");
            // Services
            services.AddSingleton<ISmartSearchService, SmartSearchService>();
            services.AddScoped<IMarketplaceSyncService, MarketplaceSyncService>();
            services.AddScoped<IAutomationService, AutomationService>();

            Log.Information("ServiceProviderConfiguration: Registrando Integration Services...");
            // Integration Services (placeholder tokens - configure with real values)
            services.AddScoped<IMercadoLivreService>(sp =>
                new MercadoLivreService(new HttpClient { BaseAddress = new Uri("https://api.mercadolibre.com") }, ""));
            services.AddScoped<IShopeeService>(sp =>
                new ShopeeService(new HttpClient { BaseAddress = new Uri("https://partner.shopeemobile.com") }, "", ""));

            Log.Information("ServiceProviderConfiguration: Registrando SuperFrete...");
            // SuperFrete Integration
            try
            {
                services.AddSuperFrete(configuration);
                Log.Information("  SuperFrete registrado com sucesso");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "  !!! ERRO ao registrar SuperFrete: {Message}", ex.Message);
                throw;
            }

            Log.Information("ServiceProviderConfiguration: Registrando ViewModels...");
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

            Log.Information("ServiceProviderConfiguration: Construindo ServiceProvider...");
            var serviceProvider = services.BuildServiceProvider();
            Log.Information("=== ServiceProviderConfiguration: Concluído com sucesso ===");

            return serviceProvider;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "!!! ERRO CRÍTICO em ServiceProviderConfiguration: {Type}", ex.GetType().Name);
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
