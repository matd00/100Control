using System.Windows;
using System.Windows.Controls;

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
        {
            _activeMenuButton.Style = (Style)FindResource("MenuButton");
        }
        button.Style = (Style)FindResource("MenuButtonActive");
        _activeMenuButton = button;
    }

    private void HideAllContent()
    {
        DashboardContent.Visibility = Visibility.Collapsed;
        ProductsContent.Visibility = Visibility.Collapsed;
        OrdersContent.Visibility = Visibility.Collapsed;
        CustomersContent.Visibility = Visibility.Collapsed;
        SuppliersContent.Visibility = Visibility.Collapsed;
        PurchasesContent.Visibility = Visibility.Collapsed;
        KitsContent.Visibility = Visibility.Collapsed;
        ShipmentsContent.Visibility = Visibility.Collapsed;
        MercadoLivreContent.Visibility = Visibility.Collapsed;
        ShopeeContent.Visibility = Visibility.Collapsed;
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnDashboard);
        ShowDashboard();
    }

    private void Products_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnProducts);
        HideAllContent();
        PageTitle.Text = "Produtos";
        PageSubtitle.Text = "Gerenciar produtos e estoque";
        ProductsContent.Visibility = Visibility.Visible;
    }

    private void Orders_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnOrders);
        HideAllContent();
        PageTitle.Text = "Pedidos";
        PageSubtitle.Text = "Visualizar e processar pedidos";
        OrdersContent.Visibility = Visibility.Visible;
    }

    private void Customers_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnCustomers);
        HideAllContent();
        PageTitle.Text = "Clientes";
        PageSubtitle.Text = "Cadastro e gestao de clientes";
        CustomersContent.Visibility = Visibility.Visible;
    }

    private void Suppliers_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnSuppliers);
        HideAllContent();
        PageTitle.Text = "Fornecedores";
        PageSubtitle.Text = "Cadastro de fornecedores";
        SuppliersContent.Visibility = Visibility.Visible;
    }

    private void Purchases_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnPurchases);
        HideAllContent();
        PageTitle.Text = "Compras";
        PageSubtitle.Text = "Registrar compras de fornecedores e usados";
        PurchasesContent.Visibility = Visibility.Visible;
    }

    private void Kits_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnKits);
        HideAllContent();
        PageTitle.Text = "Kits";
        PageSubtitle.Text = "Gerenciar kits de paintball";
        KitsContent.Visibility = Visibility.Visible;
    }

    private void Shipments_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnShipments);
        HideAllContent();
        PageTitle.Text = "Envios";
        PageSubtitle.Text = "Gerar e rastrear envios";
        ShipmentsContent.Visibility = Visibility.Visible;
    }

    private void MercadoLivre_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnMercadoLivre);
        HideAllContent();
        PageTitle.Text = "Mercado Livre";
        PageSubtitle.Text = "Integracao com Mercado Livre";
        MercadoLivreContent.Visibility = Visibility.Visible;
    }

    private void Shopee_Click(object sender, RoutedEventArgs e)
    {
        SetActiveButton(BtnShopee);
        HideAllContent();
        PageTitle.Text = "Shopee";
        PageSubtitle.Text = "Integracao com Shopee";
        ShopeeContent.Visibility = Visibility.Visible;
    }

    private void ShowDashboard()
    {
        HideAllContent();
        PageTitle.Text = "Dashboard";
        PageSubtitle.Text = "Visao geral do sistema";
        DashboardContent.Visibility = Visibility.Visible;
    }
}
