# PaintballManager - Sistema de Gestão

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white" />
  <img src="https://img.shields.io/badge/Architecture-Clean-green?style=for-the-badge" />
  <img src="https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge" />
</p>

## 📋 Índice

- [Sobre o Projeto](#-sobre-o-projeto)
- [Arquitetura](#-arquitetura)
- [Tecnologias](#-tecnologias)
- [Estrutura de Pastas](#-estrutura-de-pastas)
- [Módulos do Sistema](#-módulos-do-sistema)
- [Entidades de Domínio](#-entidades-de-domínio)
- [Casos de Uso](#-casos-de-uso)
- [Integrações](#-integrações)
- [Interface do Usuário](#-interface-do-usuário)
- [Como Executar](#-como-executar)
- [Configuração](#-configuração)
- [Contribuição](#-contribuição)

---

## 🎯 Sobre o Projeto

O **PaintballManager** é um sistema completo de gestão para lojas de paintball, desenvolvido em C# com WPF. O sistema permite gerenciar produtos, pedidos, clientes, fornecedores, compras, kits, envios e integração com marketplaces (Mercado Livre e Shopee).

### Principais Funcionalidades

- ✅ **Dashboard** - Visão geral com métricas de vendas, pedidos e estoque
- ✅ **Gestão de Produtos** - Cadastro, controle de estoque e precificação
- ✅ **Gestão de Pedidos** - Multi-canal (ML, Shopee, Instagram, WhatsApp, Direto)
- ✅ **Gestão de Clientes** - Cadastro e histórico de compras
- ✅ **Gestão de Fornecedores** - Cadastro de fornecedores nacionais e internacionais
- ✅ **Gestão de Compras** - Compras de fábrica e produtos usados
- ✅ **Sistema de Kits** - Montagem de kits com múltiplos produtos
- ✅ **Sistema de Envios** - Integração com SuperFrete e rastreamento
- ✅ **Integrações** - Mercado Livre, Shopee, SuperFrete
- ✅ **Movimentação de Estoque** - Rastreamento completo de entradas e saídas

---

## 🏗 Arquitetura

O projeto segue os princípios de **Clean Architecture** com **DDD (Domain-Driven Design) leve**, garantindo separação de responsabilidades, testabilidade e manutenibilidade.

```
┌─────────────────────────────────────────────────────────────────┐
│                        DESKTOP APP (WPF)                        │
│                     Apresentação / Interface                     │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                          │
│                    Use Cases / Serviços                         │
│   ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐    │
│   │ CreateOrder │  │ SyncOrders  │  │ RegisterPurchase    │    │
│   │  UseCase    │  │   Service   │  │     UseCase         │    │
│   └─────────────┘  └─────────────┘  └─────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                        DOMAIN LAYER                             │
│                   Entidades / Regras de Negócio                 │
│   ┌─────────┐ ┌───────┐ ┌──────────┐ ┌─────┐ ┌──────────┐      │
│   │ Product │ │ Order │ │ Customer │ │ Kit │ │ Shipment │      │
│   └─────────┘ └───────┘ └──────────┘ └─────┘ └──────────┘      │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                         │
│  ┌────────────────┐  ┌───────────────┐  ┌──────────────────┐   │
│  │   Persistence  │  │  Integrations │  │  Infrastructure  │   │
│  │  (Repositórios)│  │ (APIs Externas)│  │    (Serviços)    │   │
│  └────────────────┘  └───────────────┘  └──────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Princípios Aplicados

- **SOLID** - Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- **DRY** - Don't Repeat Yourself
- **Clean Code** - Código legível e manutenível
- **Repository Pattern** - Abstração de acesso a dados
- **Dependency Injection** - Inversão de controle

---

## 🛠 Tecnologias

| Tecnologia | Versão | Descrição |
|------------|--------|-----------|
| .NET | 8.0 | Framework principal |
| C# | 12 | Linguagem de programação |
| WPF | - | Interface desktop (Windows Presentation Foundation) |
| XAML | - | Markup para UI |
| MVVM | - | Padrão de arquitetura de UI |

### Bibliotecas

- `Microsoft.Extensions.DependencyInjection` - Injeção de dependência
- `System.Text.Json` - Serialização JSON (integrações)

---

## 📁 Estrutura de Pastas

```
PaintballManager/
│
├── Domain/                          # Camada de Domínio
│   ├── Entities/                    # Entidades de negócio
│   │   ├── Product.cs
│   │   ├── Order.cs
│   │   ├── Customer.cs
│   │   ├── Supplier.cs
│   │   ├── Purchase.cs
│   │   ├── Kit.cs
│   │   ├── Part.cs
│   │   ├── Shipment.cs
│   │   └── InventoryMovement.cs
│   │
│   └── Interfaces/                  # Interfaces dos repositórios
│       └── Repositories/
│           ├── IProductRepository.cs
│           ├── IOrderRepository.cs
│           ├── ICustomerRepository.cs
│           ├── ISupplierRepository.cs
│           ├── IPurchaseRepository.cs
│           ├── IKitRepository.cs
│           ├── IPartRepository.cs
│           ├── IShipmentRepository.cs
│           └── IInventoryMovementRepository.cs
│
├── Application/                     # Camada de Aplicação
│   ├── UseCases/                    # Casos de uso
│   │   ├── Orders/
│   │   │   ├── CreateOrderUseCase.cs
│   │   │   └── UpdateOrderStatusUseCase.cs
│   │   ├── Products/
│   │   │   └── CreateProductUseCase.cs
│   │   ├── Customers/
│   │   │   └── RegisterCustomerUseCase.cs
│   │   ├── Purchases/
│   │   │   └── RegisterPurchaseUseCase.cs
│   │   └── Shipments/
│   │       └── GenerateShipmentUseCase.cs
│   │
│   └── Services/                    # Serviços de aplicação
│       ├── MarketplaceSyncService.cs
│       └── AutomationService.cs
│
├── Persistence/                     # Camada de Persistência
│   ├── Context/
│   │   └── PaintballManagerDbContext.cs
│   │
│   └── Repositories/                # Implementações dos repositórios
│       ├── InMemoryProductRepository.cs
│       ├── InMemoryOrderRepository.cs
│       ├── InMemoryCustomerRepository.cs
│       ├── InMemorySupplierRepository.cs
│       ├── InMemoryPurchaseRepository.cs
│       ├── InMemoryKitRepository.cs
│       ├── InMemoryPartRepository.cs
│       ├── InMemoryShipmentRepository.cs
│       └── InMemoryInventoryMovementRepository.cs
│
├── Integrations/                    # Integrações Externas
│   ├── MercadoLivre/
│   │   ├── Interfaces/
│   │   │   └── IMercadoLivreService.cs
│   │   └── Services/
│   │       └── MercadoLivreService.cs
│   │
│   ├── Shopee/
│   │   ├── Interfaces/
│   │   │   └── IShopeeService.cs
│   │   └── Services/
│   │       └── ShopeeService.cs
│   │
│   └── SuperFrete/
│       ├── Interfaces/
│       │   └── ISuperFreteService.cs
│       └── Services/
│           └── SuperFreteService.cs
│
├── Infrastructure/                  # Infraestrutura
│   └── (Serviços de infraestrutura)
│
└── src/Desktop/                     # Aplicação Desktop (WPF)
    ├── App.xaml                     # Configuração da aplicação
    ├── App.xaml.cs
    ├── MainWindow.xaml              # Janela principal
    ├── MainWindow.xaml.cs
    │
    ├── Infrastructure/
    │   ├── ServiceProviderConfiguration.cs  # DI Container
    │   └── MVVM/
    │       ├── ViewModelBase.cs
    │       └── RelayCommand.cs
    │
    └── Features/                    # Funcionalidades por módulo
        ├── Dashboard/
        │   └── DashboardViewModel.cs
        ├── Products/
        │   └── ProductsViewModel.cs
        └── Orders/
            └── OrdersViewModel.cs
```

---

## 📦 Módulos do Sistema

### 1. Dashboard
Visão geral do sistema com métricas em tempo real:
- Vendas do dia/mês
- Pedidos pendentes
- Produtos com estoque baixo
- Lucro e margem
- Pedidos recentes
- Ações rápidas

### 2. Produtos
Gerenciamento completo de produtos:
- Cadastro de produtos
- Controle de estoque
- Custo e preço de venda
- Status (ativo/inativo)
- SKU único

### 3. Pedidos
Sistema multi-canal de pedidos:
- Criação manual de pedidos
- Importação de marketplaces
- Status: Pendente → Processando → Enviado → Entregue/Cancelado
- Origem: Mercado Livre, Shopee, Instagram, WhatsApp, Direto

### 4. Clientes
Cadastro de clientes:
- Dados pessoais
- Endereço completo
- Histórico de compras
- Total de compras

### 5. Fornecedores
Cadastro de fornecedores:
- Fornecedores nacionais
- Importadores internacionais
- Contato e CNPJ
- Histórico de compras

### 6. Compras
Registro de compras:
- Compras de fábrica
- Compras de produtos usados (pessoa física)
- Status: Pendente → Recebido
- Entrada automática no estoque

### 7. Kits
Sistema de kits de paintball:
- Montagem de kits com múltiplos produtos
- Componentes: Marker, Máscara, Cilindro, Hopper, etc.
- Preço do kit vs soma dos componentes
- Baixa automática do estoque de cada componente

### 8. Envios
Gestão de envios:
- Integração com SuperFrete
- Geração de etiquetas
- Rastreamento
- Status: Aguardando → Enviado → Em Trânsito → Entregue

### 9. Integrações
Conexão com marketplaces:
- **Mercado Livre**: Sincronização de pedidos, atualização de estoque e preços
- **Shopee**: Sincronização de pedidos, atualização de estoque e preços
- **SuperFrete**: Cálculo de frete, geração de etiquetas, rastreamento

---

## 🏛 Entidades de Domínio

### Product (Produto)
```csharp
public class Product
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int Stock { get; }
    public decimal Cost { get; }
    public decimal Price { get; }
    public bool IsActive { get; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; }
    
    // Métodos
    void DecreaseStock(int quantity);
    void IncreaseStock(int quantity);
    void UpdatePrice(decimal newPrice);
    void Deactivate();
}
```

### Order (Pedido)
```csharp
public class Order
{
    public Guid Id { get; }
    public Guid CustomerId { get; }
    public OrderSource Source { get; }  // MercadoLivre, Shopee, Instagram, WhatsApp, Direct
    public List<OrderItem> Items { get; }
    public decimal TotalAmount { get; }
    public OrderStatus Status { get; }  // Pending, Processing, Shipped, Completed, Cancelled
    public DateTime CreatedAt { get; }
    
    // Métodos
    void AddItem(Guid productId, int quantity, decimal price);
    void RemoveItem(Guid productId);
    void MarkAsProcessing();
    void MarkAsCompleted();
    void Cancel();
}
```

### Customer (Cliente)
```csharp
public class Customer
{
    public Guid Id { get; }
    public string Name { get; }
    public string Email { get; }
    public string Phone { get; }
    public string Address { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Document { get; }  // CPF/CNPJ
    public bool IsActive { get; }
    public DateTime CreatedAt { get; }
}
```

### Kit
```csharp
public class Kit
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public decimal Price { get; }
    public List<KitItem> Items { get; }
    public bool IsActive { get; }
    public DateTime CreatedAt { get; }
    
    // Métodos
    void AddItem(Guid productId, int quantity);
    void RemoveItem(Guid productId);
    decimal CalculateCost();  // Soma dos custos dos componentes
}
```

### Shipment (Envio)
```csharp
public class Shipment
{
    public Guid Id { get; }
    public Guid OrderId { get; }
    public string TrackingCode { get; }
    public ShipmentProvider Provider { get; }  // SuperFrete, Transportadora, DropShipping
    public ShipmentStatus Status { get; }
    public decimal ShippingCost { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ShippedAt { get; }
    public DateTime? DeliveredAt { get; }
}
```

### InventoryMovement (Movimentação de Estoque)
```csharp
public class InventoryMovement
{
    public Guid Id { get; }
    public Guid ProductId { get; }
    public MovementType Type { get; }  // Purchase, Sale, Adjustment, KitAssembly
    public int Quantity { get; }  // Positivo = entrada, Negativo = saída
    public string Reference { get; }  // ID do pedido/compra relacionado
    public DateTime CreatedAt { get; }
}
```

---

## 🔄 Casos de Uso

### CreateOrderUseCase
Cria um novo pedido no sistema.

```csharp
// Entrada
CreateOrderCommand {
    CustomerId: Guid,
    Source: OrderSource,
    Items: List<OrderItemCommand>
}

// Fluxo
1. Valida dados de entrada
2. Verifica se cliente existe e está ativo
3. Para cada item:
   - Verifica se produto existe e está ativo
   - Verifica disponibilidade de estoque
   - Adiciona item ao pedido
4. Salva o pedido
5. Baixa o estoque dos produtos
6. Registra movimentação de estoque
```

### RegisterPurchaseUseCase
Registra uma compra de fornecedor.

```csharp
// Entrada
RegisterPurchaseCommand {
    SupplierId: Guid,
    Type: PurchaseType,  // Factory, Used
    Items: List<PurchaseItemCommand>
}

// Fluxo
1. Valida dados de entrada
2. Verifica se fornecedor existe
3. Cria a compra
4. Adiciona itens
5. Aumenta estoque dos produtos
6. Registra movimentação de estoque
```

### GenerateShipmentUseCase
Gera um envio para um pedido.

```csharp
// Entrada
GenerateShipmentCommand {
    OrderId: Guid,
    Provider: ShipmentProvider
}

// Fluxo
1. Valida dados de entrada
2. Verifica se pedido existe e está em status válido
3. Calcula frete via integração (SuperFrete)
4. Gera etiqueta
5. Cria o envio
6. Atualiza status do pedido
```

---

## 🔌 Integrações

### Mercado Livre
```csharp
public interface IMercadoLivreService
{
    Task<List<MeliOrderDto>> GetOrdersAsync();
    Task<List<MeliProductDto>> GetProductsAsync();
    Task UpdateStockAsync(string productId, int quantity);
    Task UpdatePriceAsync(string productId, decimal price);
}
```

**Fluxo de Sincronização:**
```
Mercado Livre API
       │
       ▼
 SyncService
       │
       ├──► Importa novos pedidos
       ├──► Cria/atualiza clientes
       ├──► Baixa estoque
       └──► Registra movimentações
```

### Shopee
```csharp
public interface IShopeeService
{
    Task<List<ShopeeOrderDto>> GetOrdersAsync();
    Task<List<ShopeeProductDto>> GetProductsAsync();
    Task UpdateStockAsync(string productId, int quantity);
}
```

### SuperFrete
```csharp
public interface ISuperFreteService
{
    Task<FreightQuoteDto> CalculateFreightAsync(string zipCodeFrom, string zipCodeTo, decimal weight);
    Task<ShippingLabelDto> GenerateLabelAsync(ShipmentRequest request);
    Task<TrackingDto> TrackShipmentAsync(string trackingCode);
}
```

---

## 🖥 Interface do Usuário

A interface foi desenvolvida com WPF usando design moderno e responsivo.

### Design System

**Cores:**
- Primary: `#3B82F6` (Azul)
- Success: `#22C55E` (Verde)
- Warning: `#F59E0B` (Amarelo)
- Danger: `#EF4444` (Vermelho)
- Background: `#F0F2F5`
- Sidebar: `#1E293B`

**Componentes:**
- Cards com sombras suaves
- Botões com hover effects
- Tabelas com linhas alternadas
- Badges de status coloridos
- Menu lateral com seções

### Páginas

| Página | Descrição |
|--------|-----------|
| Dashboard | Métricas, pedidos recentes, ações rápidas |
| Produtos | Lista de produtos com estoque, preços e status |
| Pedidos | Cards de pedidos por status com ações |
| Clientes | Tabela de clientes com histórico |
| Fornecedores | Tabela de fornecedores |
| Compras | Lista de compras (fábrica/usados) |
| Kits | Cards de kits com componentes |
| Envios | Lista de envios com rastreamento |
| Mercado Livre | Dashboard de integração |
| Shopee | Dashboard de integração |

---

## 🚀 Como Executar

### Pré-requisitos
- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (recomendado) ou VS Code

### Passos

1. **Clone o repositório**
```bash
git clone https://github.com/seu-usuario/paintball-manager.git
cd paintball-manager
```

2. **Restaure as dependências**
```bash
dotnet restore
```

3. **Compile o projeto**
```bash
dotnet build
```

4. **Execute a aplicação**
```bash
dotnet run --project src/Desktop/Desktop.csproj
```

Ou no Visual Studio:
- Abra o arquivo `.sln`
- Defina `Desktop` como projeto de inicialização
- Pressione `F5`

---

## ⚙️ Configuração

### Injeção de Dependência

O arquivo `ServiceProviderConfiguration.cs` configura todos os serviços:

```csharp
public static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    // Repositórios (In-Memory)
    services.AddSingleton<IProductRepository, InMemoryProductRepository>();
    services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
    services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
    // ...

    // Use Cases
    services.AddTransient<CreateOrderUseCase>();
    services.AddTransient<CreateProductUseCase>();
    // ...

    // Services
    services.AddSingleton<IMarketplaceSyncService, MarketplaceSyncService>();
    services.AddSingleton<IAutomationService, AutomationService>();

    return services.BuildServiceProvider();
}
```

### Configuração de Banco de Dados

Atualmente o sistema usa repositórios **In-Memory**. Para produção, substitua por:

```csharp
// Entity Framework Core
services.AddDbContext<PaintballManagerDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IProductRepository, EfProductRepository>();
```

### Configuração de Integrações

Crie um arquivo `appsettings.json`:

```json
{
  "MercadoLivre": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "AccessToken": "YOUR_ACCESS_TOKEN"
  },
  "Shopee": {
    "PartnerId": "YOUR_PARTNER_ID",
    "PartnerKey": "YOUR_PARTNER_KEY",
    "ShopId": "YOUR_SHOP_ID"
  },
  "SuperFrete": {
    "ApiKey": "YOUR_API_KEY"
  }
}
```

---

## 🧪 Testes

### Estrutura de Testes

```
Tests/
├── Domain.Tests/
│   └── Entities/
│       ├── ProductTests.cs
│       ├── OrderTests.cs
│       └── KitTests.cs
│
├── Application.Tests/
│   └── UseCases/
│       ├── CreateOrderUseCaseTests.cs
│       └── RegisterPurchaseUseCaseTests.cs
│
└── Integration.Tests/
    └── MercadoLivre/
        └── MercadoLivreServiceTests.cs
```

### Executar Testes

```bash
dotnet test
```

---

## 📈 Roadmap

- [ ] Persistência com Entity Framework Core + PostgreSQL
- [ ] Autenticação e autorização de usuários
- [ ] Relatórios em PDF
- [ ] Gráficos de vendas
- [ ] Notificações push
- [ ] Backup automático
- [ ] API REST para acesso externo
- [ ] Aplicativo mobile (MAUI)

---

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`)
3. Commit suas mudanças (`git commit -m 'Adiciona nova feature'`)
4. Push para a branch (`git push origin feature/nova-feature`)
5. Abra um Pull Request

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 👥 Autores

- **Desenvolvedor** - Desenvolvido com C# e WPF

---

## 📞 Suporte

Para suporte, abra uma issue no repositório ou entre em contato.

---

<p align="center">
  Desenvolvido com ❤️ para a comunidade de Paintball
</p>
