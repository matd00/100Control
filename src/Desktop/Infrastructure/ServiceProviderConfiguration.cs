using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Domain.Interfaces.Repositories;
using Persistence.Context;
using Persistence.Repositories;
using Application.UseCases.Orders;
using Application.UseCases.Products;
using Application.UseCases.Customers;
using Application.UseCases.Purchases;
using Application.UseCases.Shipments;
using Application.UseCases.FactoryOrders;
using Application.Services;
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
        var services = new ServiceCollection();

        // Configuration with User Secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Ensure directory exists
        var dbDirectory = Path.GetDirectoryName(DbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        // Database Context (SQLite)
        services.AddDbContext<PaintballManagerDbContext>(options =>
            options.UseSqlite($"Data Source={DbPath}"));

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

        // Use Cases
        services.AddTransient<CreateOrderUseCase>();
        services.AddTransient<CreateProductUseCase>();
        services.AddTransient<RegisterCustomerUseCase>();
        services.AddTransient<RegisterPurchaseUseCase>();
        services.AddTransient<GenerateShipmentUseCase>();
        services.AddTransient<UpdateOrderStatusUseCase>();

        // Factory Order Use Cases
        services.AddTransient<CreateFactoryOrderUseCase>();
        services.AddTransient<UpdateFactoryOrderStatusUseCase>();
        services.AddTransient<AddTrackingCodeUseCase>();

        // Services
        services.AddScoped<IMarketplaceSyncService, MarketplaceSyncService>();
        services.AddScoped<IAutomationService, AutomationService>();

        // Integration Services (placeholder tokens - configure with real values)
        services.AddScoped<IMercadoLivreService>(sp =>
            new MercadoLivreService(new HttpClient { BaseAddress = new Uri("https://api.mercadolibre.com") }, ""));
        services.AddScoped<IShopeeService>(sp =>
            new ShopeeService(new HttpClient { BaseAddress = new Uri("https://partner.shopeemobile.com") }, "", ""));

        // SuperFrete Integration
        services.AddSuperFrete(configuration);

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

        return services.BuildServiceProvider();
    }

    public static void InitializeDatabase(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaintballManagerDbContext>();
        context.Database.Migrate();
    }
}
