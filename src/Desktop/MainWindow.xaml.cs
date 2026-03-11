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
        HideAllContent();
        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;

        var viewModel = App.ServiceProvider.GetRequiredService<TViewModel>();
        var view = new TView { DataContext = viewModel };
        DynamicContent.Children.Add(view);
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
        SetActiveButton(BtnOrders);
        LoadView<OrdersLayoutView, OrdersLayoutViewModel>("Pedidos", "Criar e gerenciar pedidos");
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
