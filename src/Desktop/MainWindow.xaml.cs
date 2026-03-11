using System.Windows;
using System.Windows.Controls;
using Desktop.Features.Products;
using Desktop.Features.Orders;
using Desktop.Features.Customers;
using Desktop.Features.Suppliers;
using Desktop.Features.Purchases;
using Desktop.Features.Kits;
using Desktop.Features.Shipments;
using Desktop.Features.Dashboard;
using Desktop.Features.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace Desktop;

public partial class MainWindow : Window
{
    private Button? _activeMenuButton;

    public MainWindow()
    {
        InitializeComponent();
        _activeMenuButton = BtnDashboard;
        TxtDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
        ShowDashboard();
    }

    private void SetActiveButton(Button button)
    {
        if (_activeMenuButton != null)
            _activeMenuButton.Style = (Style)FindResource("MenuButton");
        button.Style = (Style)FindResource("MenuButtonActive");
        _activeMenuButton = button;
    }

    private void HideAllContent()
    {
        DashboardContent.Visibility = Visibility.Collapsed;
        MercadoLivreContent.Visibility = Visibility.Collapsed;
        ShopeeContent.Visibility = Visibility.Collapsed;
        DynamicContent.Children.Clear();
    }

    private void LoadView<TView, TViewModel>(string title, string subtitle) 
        where TView : UserControl, new()
        where TViewModel : class
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== LOADVIEW: Carregando {typeof(TView).Name} com {typeof(TViewModel).Name} ===");

            System.Diagnostics.Debug.WriteLine("LOADVIEW: Escondendo conteúdo anterior...");
            HideAllContent();

            System.Diagnostics.Debug.WriteLine($"LOADVIEW: Configurando título: {title}");
            PageTitle.Text = title;
            PageSubtitle.Text = subtitle;

            System.Diagnostics.Debug.WriteLine($"LOADVIEW: Resolvendo ViewModel {typeof(TViewModel).Name}...");
            var viewModel = App.ServiceProvider.GetRequiredService<TViewModel>();
            System.Diagnostics.Debug.WriteLine($"LOADVIEW: ViewModel {typeof(TViewModel).Name} resolvido com sucesso");

            System.Diagnostics.Debug.WriteLine($"LOADVIEW: Criando View {typeof(TView).Name}...");
            var view = new TView { DataContext = viewModel };
            System.Diagnostics.Debug.WriteLine($"LOADVIEW: View {typeof(TView).Name} criada");

            System.Diagnostics.Debug.WriteLine("LOADVIEW: Adicionando ao DynamicContent...");
            DynamicContent.Children.Add(view);
            System.Diagnostics.Debug.WriteLine($"=== LOADVIEW: {typeof(TView).Name} carregado com sucesso ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! LOADVIEW ERRO ao carregar {typeof(TView).Name}: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"!!! Mensagem: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"!!! StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"!!! InnerException: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"!!! InnerException StackTrace: {ex.InnerException.StackTrace}");
            }

            var errorMessage = $"Erro ao carregar {title}:\n\n{ex.Message}";

            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception:\n{ex.InnerException.Message}";

                if (ex.InnerException.InnerException != null)
                {
                    errorMessage += $"\n\nInner Inner Exception:\n{ex.InnerException.InnerException.Message}";
                }
            }

            errorMessage += $"\n\nStackTrace:\n{ex.StackTrace}";

            MessageBox.Show(errorMessage, "Erro Detalhado", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnDashboard);
        ShowDashboard();
    }

    private void Products_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnProducts);
        LoadView<ProductsView, ProductsViewModel>("Produtos", "Gerenciar produtos, dimensões e calcular frete");
    }

    private void Orders_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  CLIQUE NO MENU PEDIDOS - INICIANDO CARREGAMENTO           ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine("");

            SetActiveButton(BtnOrders);
            LoadView<OrdersLayoutView, OrdersLayoutViewModel>("Pedidos", "Criar e gerenciar pedidos");

            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  PEDIDOS CARREGADO COM SUCESSO                             ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine("");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  ERRO CRÍTICO AO CARREGAR PEDIDOS                          ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"Tipo: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Mensagem: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine("");
            throw;
        }
    }

    private void Customers_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnCustomers);
        LoadView<CustomersView, CustomersViewModel>("Clientes", "Cadastro e gestão de clientes");
    }

    private void Suppliers_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnSuppliers);
        LoadView<SuppliersView, SuppliersViewModel>("Fornecedores", "Cadastro de fornecedores");
    }

    private void Inventory_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnInventory);
        LoadView<InventoryView, InventoryViewModel>("Histórico de Estoque", "Movimentações de entrada e saída de produtos");
    }

    private void Purchases_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnPurchases);
        LoadView<PurchasesView, PurchasesViewModel>("Compras", "Registrar compras");
    }

    private void Kits_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnKits);
        LoadView<KitsView, KitsViewModel>("Kits", "Gerenciar kits de paintball");
    }

    private void Shipments_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnShipments);
        LoadView<ShipmentsView, ShipmentsViewModel>("Envios", "Gerar etiquetas e rastrear envios");
    }

    private void MercadoLivre_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnMercadoLivre);
        HideAllContent();
        PageTitle.Text = "Mercado Livre";
        PageSubtitle.Text = "Integração com Mercado Livre";
        MercadoLivreContent.Visibility = Visibility.Visible;
    }

    private void Shopee_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnShopee);
        HideAllContent();
        PageTitle.Text = "Shopee";
        PageSubtitle.Text = "Integração com Shopee";
        ShopeeContent.Visibility = Visibility.Visible;
    }

    private void ShowDashboard()
    {
        HideAllContent();
        PageTitle.Text = "Dashboard";
        PageSubtitle.Text = "Visão geral do sistema";

        var viewModel = App.ServiceProvider.GetRequiredService<DashboardViewModel>();
        DashboardContent.DataContext = viewModel;
        DashboardContent.Visibility = Visibility.Visible;
    }
}
