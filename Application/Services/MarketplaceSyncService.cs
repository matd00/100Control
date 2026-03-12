using Domain.Entities;
using Domain.Interfaces.Repositories;
using Integrations.MercadoLivre.Interfaces;
using Integrations.Shopee.Interfaces;

namespace Application.Services;

public interface IMarketplaceSyncService
{
    Task SyncMercadoLivreOrdersAsync();
    Task SyncShopeeOrdersAsync();
    Task UpdateAllStockAsync();
}

public class MarketplaceSyncService : IMarketplaceSyncService
{
    private readonly IMercadoLivreService _mercadoLivreService;
    private readonly IShopeeService _shopeeService;
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryMovementRepository _inventoryMovementRepository;

    public MarketplaceSyncService(
        IMercadoLivreService mercadoLivreService,
        IShopeeService shopeeService,
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryMovementRepository inventoryMovementRepository)
    {
        _mercadoLivreService = mercadoLivreService;
        _shopeeService = shopeeService;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryMovementRepository = inventoryMovementRepository;
    }

    public async Task SyncMercadoLivreOrdersAsync()
    {
        var orders = await _mercadoLivreService.GetOrdersAsync();

        foreach (var meliOrder in orders)
        {
            // Check if order already exists
            var existingOrders = await _orderRepository.GetAllAsync();
            if (existingOrders.Any(o => o.Id.ToString() == meliOrder.OrderId))
                continue;

            // Create or get customer
            var customer = new Customer(
                meliOrder.CustomerName,
                meliOrder.CustomerEmail,
                string.Empty,
                string.Empty
            );
            await _customerRepository.SaveAsync(customer);

            // Create order
            var order = new Order(customer.Id, OrderSource.MercadoLivre);
            foreach (var item in meliOrder.Items)
            {
                // Find product by external ID
                // For now, create a placeholder
                var product = new Product(item.Title, "Geral", string.Empty, item.Price, item.Price);
                await _productRepository.SaveAsync(product);

                order.AddItem(product.Id, item.Quantity, item.Price);
            }

            await _orderRepository.SaveAsync(order);
        }
    }

    public async Task SyncShopeeOrdersAsync()
    {
        var orders = await _shopeeService.GetOrdersAsync();

        foreach (var shopeeOrder in orders)
        {
            // Similar logic to MercadoLivre
            var existingOrders = await _orderRepository.GetAllAsync();
            if (existingOrders.Any(o => o.Id.ToString() == shopeeOrder.OrderId))
                continue;

            var customer = new Customer(
                shopeeOrder.BuyerUsername,
                shopeeOrder.BuyerEmail,
                string.Empty,
                string.Empty
            );
            await _customerRepository.SaveAsync(customer);

            var order = new Order(customer.Id, OrderSource.Shopee);
            foreach (var item in shopeeOrder.Items)
            {
                var product = new Product(item.ProductName, "Geral", string.Empty, item.Price, item.Price);
                await _productRepository.SaveAsync(product);
                order.AddItem(product.Id, item.Quantity, item.Price);
            }

            await _orderRepository.SaveAsync(order);
        }
    }

    public async Task UpdateAllStockAsync()
    {
        var meliProducts = await _mercadoLivreService.GetProductsAsync();
        var shopeeProducts = await _shopeeService.GetProductsAsync();

        var allProducts = await _productRepository.GetAllAsync();

        foreach (var product in allProducts)
        {
            // TODO: Map external product IDs to internal products
            // Update stock based on marketplace data
        }
    }
}
