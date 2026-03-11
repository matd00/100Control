using Desktop.Infrastructure.MVVM;
using System.Diagnostics;

namespace Desktop.Features.Orders;

public class OrdersLayoutViewModel : ViewModelBase
{
    public OrdersViewModel NormalOrders { get; }
    public FactoryOrdersViewModel FactoryOrders { get; }

    public OrdersLayoutViewModel(OrdersViewModel normalOrders, FactoryOrdersViewModel factoryOrders)
    {
        try
        {
            Debug.WriteLine("=== OrdersLayoutViewModel: Iniciando construtor ===");

            Debug.WriteLine("OrdersLayoutViewModel: Recebendo NormalOrders...");
            NormalOrders = normalOrders ?? throw new ArgumentNullException(nameof(normalOrders));
            Debug.WriteLine("OrdersLayoutViewModel: NormalOrders recebido com sucesso");

            Debug.WriteLine("OrdersLayoutViewModel: Recebendo FactoryOrders...");
            FactoryOrders = factoryOrders ?? throw new ArgumentNullException(nameof(factoryOrders));
            Debug.WriteLine("OrdersLayoutViewModel: FactoryOrders recebido com sucesso");

            Debug.WriteLine("=== OrdersLayoutViewModel: Construtor finalizado com sucesso ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"!!! ERRO no OrdersLayoutViewModel construtor: {ex.GetType().Name}");
            Debug.WriteLine($"!!! Mensagem: {ex.Message}");
            Debug.WriteLine($"!!! StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}
