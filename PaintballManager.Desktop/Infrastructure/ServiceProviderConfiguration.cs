using Microsoft.Extensions.DependencyInjection;
using PaintballManager.Application.Services;
using PaintballManager.Application.UseCases.Customers;
using PaintballManager.Application.UseCases.Orders;
using PaintballManager.Application.UseCases.Products;
using PaintballManager.Application.UseCases.Purchases;
using PaintballManager.Application.UseCases.Shipments;
using PaintballManager.Desktop.Features.Dashboard.ViewModels;
using PaintballManager.Desktop.Features.Orders.ViewModels;
using PaintballManager.Desktop.Features.Products.ViewModels;
using PaintballManager.Domain.Interfaces.Repositories;
using PaintballManager.Integrations.MercadoLivre.Interfaces;
using PaintballManager.Integrations.MercadoLivre.Services;
using PaintballManager.Integrations.Shopee.Interfaces;
using PaintballManager.Integrations.Shopee.Services;
using PaintballManager.Integrations.SuperFrete.Configuration;
using PaintballManager.Integrations.SuperFrete.Interfaces;
using PaintballManager.Integrations.SuperFrete.Services;
using PaintballManager.Persistence.Repositories;

namespace PaintballManager.Desktop.Infrastructure;

public static class ServiceProviderConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Repositories (In-Memory for now)
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<ISupplierRepository, InMemorySupplierRepository>();
        services.AddSingleton<IPurchaseRepository, InMemoryPurchaseRepository>();
        services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();
        services.AddSingleton<IKitRepository, InMemoryKitRepository>();
        services.AddSingleton<IPartRepository, InMemoryPartRepository>();
        services.AddSingleton<IInventoryMovementRepository, InMemoryInventoryMovementRepository>();

        // SuperFrete Settings
        var superFreteSettings = new SuperFreteSettings
        {
            ApiToken = "YOUR_API_KEY",
            BaseUrl = "https://api.superfrete.com",
            DefaultOriginPostalCode = "00000000"
        };
        services.AddSingleton(superFreteSettings);

        // Use Cases
        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<CreateOrderUseCase>();
        services.AddScoped<UpdateOrderStatusUseCase>();
        services.AddScoped<RegisterCustomerUseCase>();
        services.AddScoped<RegisterPurchaseUseCase>();
        services.AddScoped<GenerateShipmentUseCase>();

        // Services
        services.AddScoped<IMarketplaceSyncService, MarketplaceSyncService>();
        services.AddScoped<IAutomationService, AutomationService>();

        // Integration Services
        services.AddHttpClient<IMercadoLivreService>(client =>
        {
            client.BaseAddress = new Uri("https://api.mercadolibre.com");
        }).ConfigureHttpClient((sp, client) =>
        {
            // Configure with your API token
        }).ConfigureMessageHandler(_ => new HttpClientHandler());

        services.AddHttpClient<IShopeeService>(client =>
        {
            client.BaseAddress = new Uri("https://partner.shopeemobile.com");
        });

        services.AddHttpClient<ISuperFreteService, SuperFreteService>(client =>
        {
            client.BaseAddress = new Uri(superFreteSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(superFreteSettings.TimeoutSeconds);
        });

        // Register implementations
        services.AddScoped<IMercadoLivreService>(sp =>
            new MercadoLivreService(sp.GetRequiredService<HttpClient>(), "YOUR_ACCESS_TOKEN"));

        services.AddScoped<IShopeeService>(sp =>
            new ShopeeService(sp.GetRequiredService<HttpClient>(), "YOUR_PARTNER_ID", "YOUR_PARTNER_KEY"));

        // ViewModels
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<ProductsViewModel>();
        services.AddScoped<OrdersViewModel>();

        return services.BuildServiceProvider();
    }
}
