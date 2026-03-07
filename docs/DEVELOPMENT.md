# Guia de Desenvolvimento - PaintballManager

## Configuração do Ambiente

### Pré-requisitos

- **Windows 10/11** (para desenvolvimento WPF)
- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (recomendado) ou VS Code
- **Git** - Controle de versão

### Instalação do .NET 8.0

```bash
# Verificar versão instalada
dotnet --version

# Deve retornar 8.0.x ou superior
```

### Clonando o Projeto

```bash
git clone https://github.com/seu-usuario/paintball-manager.git
cd paintball-manager
```

### Restaurando Dependências

```bash
dotnet restore
```

### Compilando

```bash
dotnet build
```

### Executando

```bash
dotnet run --project src/Desktop/Desktop.csproj
```

---

## Estrutura do Projeto

```
PaintballManager.sln
│
├── Domain/                    # Regras de negócio
├── Application/               # Casos de uso
├── Persistence/               # Repositórios
├── Integrations/              # APIs externas
├── Infrastructure/            # Serviços de infraestrutura
├── src/Desktop/               # Aplicação WPF
└── docs/                      # Documentação
```

---

## Convenções de Código

### Nomenclatura

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Classes | PascalCase | `ProductRepository` |
| Interfaces | IPascalCase | `IProductRepository` |
| Métodos | PascalCase | `GetByIdAsync` |
| Propriedades | PascalCase | `ProductName` |
| Campos privados | _camelCase | `_productRepository` |
| Parâmetros | camelCase | `productId` |
| Constantes | UPPER_CASE | `MAX_QUANTITY` |

### Organização de Arquivos

```csharp
// 1. Usings
using Domain.Entities;
using Domain.Interfaces.Repositories;

// 2. Namespace
namespace Application.UseCases.Orders;

// 3. Classe
public class CreateOrderUseCase
{
    // 4. Campos privados
    private readonly IOrderRepository _orderRepository;
    
    // 5. Construtor
    public CreateOrderUseCase(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    // 6. Métodos públicos
    public async Task Execute(CreateOrderCommand command)
    {
        // ...
    }
    
    // 7. Métodos privados
    private void ValidateCommand(CreateOrderCommand command)
    {
        // ...
    }
}
```

### Documentação de Código

```csharp
/// <summary>
/// Cria um novo pedido no sistema.
/// </summary>
/// <param name="command">Dados do pedido a ser criado.</param>
/// <returns>ID do pedido criado.</returns>
/// <exception cref="ArgumentException">Quando os dados são inválidos.</exception>
/// <exception cref="InvalidOperationException">Quando não há estoque disponível.</exception>
public async Task<Guid> Execute(CreateOrderCommand command)
{
    // ...
}
```

---

## Criando Novas Funcionalidades

### 1. Criar uma Nova Entidade

**Passo 1:** Criar a entidade em `Domain/Entities/`

```csharp
// Domain/Entities/Promotion.cs
namespace Domain.Entities;

public class Promotion
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; }

    public Promotion(string name, decimal discountPercent, DateTime startDate, DateTime endDate)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
            
        if (discountPercent <= 0 || discountPercent > 100)
            throw new ArgumentException("Discount must be between 0 and 100", nameof(discountPercent));
            
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date", nameof(endDate));

        Id = Guid.NewGuid();
        Name = name.Trim();
        DiscountPercent = discountPercent;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = true;
    }

    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartDate && now <= EndDate;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
```

**Passo 2:** Criar a interface do repositório em `Domain/Interfaces/Repositories/`

```csharp
// Domain/Interfaces/Repositories/IPromotionRepository.cs
namespace Domain.Interfaces.Repositories;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id);
    Task<IEnumerable<Promotion>> GetAllAsync();
    Task<IEnumerable<Promotion>> GetActiveAsync();
    Task SaveAsync(Promotion promotion);
    Task DeleteAsync(Guid id);
}
```

**Passo 3:** Implementar o repositório em `Persistence/Repositories/`

```csharp
// Persistence/Repositories/InMemoryPromotionRepository.cs
namespace Persistence.Repositories;

public class InMemoryPromotionRepository : IPromotionRepository
{
    private readonly Dictionary<Guid, Promotion> _promotions = new();

    public Task<Promotion?> GetByIdAsync(Guid id)
    {
        _promotions.TryGetValue(id, out var promotion);
        return Task.FromResult(promotion);
    }

    public Task<IEnumerable<Promotion>> GetAllAsync()
    {
        return Task.FromResult(_promotions.Values.AsEnumerable());
    }

    public Task<IEnumerable<Promotion>> GetActiveAsync()
    {
        var active = _promotions.Values.Where(p => p.IsValid());
        return Task.FromResult(active);
    }

    public Task SaveAsync(Promotion promotion)
    {
        _promotions[promotion.Id] = promotion;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _promotions.Remove(id);
        return Task.CompletedTask;
    }
}
```

### 2. Criar um Novo Use Case

```csharp
// Application/UseCases/Promotions/CreatePromotionUseCase.cs
namespace Application.UseCases.Promotions;

public class CreatePromotionCommand
{
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class CreatePromotionUseCase
{
    private readonly IPromotionRepository _repository;

    public CreatePromotionUseCase(IPromotionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Execute(CreatePromotionCommand command)
    {
        // Validação
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        // Criar entidade (validação no construtor)
        var promotion = new Promotion(
            command.Name,
            command.DiscountPercent,
            command.StartDate,
            command.EndDate
        );

        // Persistir
        await _repository.SaveAsync(promotion);

        return promotion.Id;
    }
}
```

### 3. Criar uma Nova View

**Passo 1:** Adicionar o conteúdo no `MainWindow.xaml`

```xml
<!-- PROMOTIONS -->
<Grid x:Name="PromotionsContent" Visibility="Collapsed">
    <Border Style="{StaticResource CardStyle}">
        <StackPanel>
            <Grid Margin="0,0,0,24">
                <TextBlock Text="Promocoes" FontSize="18" FontWeight="SemiBold" Foreground="#1E293B"/>
                <Button Content="+ Nova Promocao" Style="{StaticResource PrimaryButton}" HorizontalAlignment="Right"/>
            </Grid>
            
            <!-- Lista de promoções -->
            <ItemsControl ItemsSource="{Binding Promotions}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Background="#F0F9FF" CornerRadius="8" Padding="16" Margin="0,0,0,8">
                            <Grid>
                                <StackPanel>
                                    <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                                    <TextBlock Text="{Binding DiscountPercent, StringFormat='{}{0}% OFF'}" Foreground="#22C55E"/>
                                </StackPanel>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </StackPanel>
    </Border>
</Grid>
```

**Passo 2:** Adicionar o botão no menu

```xml
<Button x:Name="BtnPromotions" Content="Promocoes" Style="{StaticResource MenuButton}" Click="Promotions_Click"/>
```

**Passo 3:** Adicionar o handler no code-behind

```csharp
private void Promotions_Click(object sender, RoutedEventArgs e)
{
    SetActiveButton(BtnPromotions);
    HideAllContent();
    PageTitle.Text = "Promocoes";
    PageSubtitle.Text = "Gerenciar promocoes e descontos";
    PromotionsContent.Visibility = Visibility.Visible;
}
```

### 4. Registrar no Container de DI

```csharp
// src/Desktop/Infrastructure/ServiceProviderConfiguration.cs
public static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    // Repositórios
    services.AddSingleton<IPromotionRepository, InMemoryPromotionRepository>();

    // Use Cases
    services.AddTransient<CreatePromotionUseCase>();

    return services.BuildServiceProvider();
}
```

---

## Criando Integrações

### Estrutura de uma Integração

```
Integrations/
└── NovaIntegracao/
    ├── Interfaces/
    │   └── INovaIntegracaoService.cs
    ├── Services/
    │   └── NovaIntegracaoService.cs
    └── DTOs/
        └── NovaIntegracaoDto.cs
```

### Exemplo: Integração com API de Pagamento

```csharp
// Integrations/Pagamento/Interfaces/IPagamentoService.cs
namespace Integrations.Pagamento.Interfaces;

public interface IPagamentoService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentStatus> GetStatusAsync(string transactionId);
    Task<bool> RefundAsync(string transactionId);
}

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string CustomerEmail { get; set; }
    public string Description { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; }
    public string ErrorMessage { get; set; }
}
```

```csharp
// Integrations/Pagamento/Services/PagamentoService.cs
namespace Integrations.Pagamento.Services;

public class PagamentoService : IPagamentoService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public PagamentoService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Pagamento:ApiKey"];
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var content = JsonContent.Create(request);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        
        var response = await _httpClient.PostAsync("/payments", content);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<PaymentResult>();
            return result!;
        }
        
        return new PaymentResult 
        { 
            Success = false, 
            ErrorMessage = "Payment failed" 
        };
    }
}
```

---

## Testes

### Estrutura de Testes

```
Tests/
├── Domain.Tests/
│   └── Entities/
│       └── ProductTests.cs
├── Application.Tests/
│   └── UseCases/
│       └── CreateOrderUseCaseTests.cs
└── Integration.Tests/
    └── Repositories/
        └── ProductRepositoryTests.cs
```

### Exemplo de Teste de Entidade

```csharp
// Tests/Domain.Tests/Entities/ProductTests.cs
using Domain.Entities;
using Xunit;

namespace Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateProduct()
    {
        // Arrange
        var name = "Test Product";
        var description = "Description";
        var cost = 100m;
        var price = 150m;

        // Act
        var product = new Product(name, description, cost, price);

        // Assert
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal(name, product.Name);
        Assert.Equal(cost, product.Cost);
        Assert.Equal(price, product.Price);
        Assert.True(product.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowException(string name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Product(name, "desc", 100, 150));
    }

    [Fact]
    public void Constructor_WithPriceLessThanCost_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Product("Product", "desc", 150, 100));
    }

    [Fact]
    public void DecreaseStock_WithValidQuantity_ShouldDecrease()
    {
        // Arrange
        var product = new Product("Product", "desc", 100, 150);
        product.IncreaseStock(10);

        // Act
        product.DecreaseStock(5);

        // Assert
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public void DecreaseStock_WithInsufficientStock_ShouldThrowException()
    {
        // Arrange
        var product = new Product("Product", "desc", 100, 150);
        product.IncreaseStock(5);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            product.DecreaseStock(10));
    }
}
```

### Exemplo de Teste de Use Case

```csharp
// Tests/Application.Tests/UseCases/CreateOrderUseCaseTests.cs
using Application.UseCases.Orders;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Moq;
using Xunit;

namespace Application.Tests.UseCases;

public class CreateOrderUseCaseTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly CreateOrderUseCase _useCase;

    public CreateOrderUseCaseTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        
        _useCase = new CreateOrderUseCase(
            _orderRepoMock.Object,
            _customerRepoMock.Object,
            _productRepoMock.Object
        );
    }

    [Fact]
    public async Task Execute_WithValidCommand_ShouldCreateOrder()
    {
        // Arrange
        var customer = new Customer("John", "john@email.com", "123456789", "12345678901");
        var product = new Product("Product", "desc", 100, 150);
        product.IncreaseStock(10);

        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(customer);
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(product);

        var command = new CreateOrderCommand
        {
            CustomerId = customer.Id,
            Source = OrderSource.Direct,
            Items = new List<OrderItemCommand>
            {
                new() { ProductId = product.Id, Quantity = 2 }
            }
        };

        // Act
        await _useCase.Execute(command);

        // Assert
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WithNullCommand_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _useCase.Execute(null!));
    }
}
```

### Executando Testes

```bash
# Executar todos os testes
dotnet test

# Com cobertura
dotnet test --collect:"XPlat Code Coverage"

# Testes específicos
dotnet test --filter "FullyQualifiedName~ProductTests"
```

---

## Debugging

### Visual Studio

1. Defina breakpoints (F9)
2. Inicie o debug (F5)
3. Use Step Over (F10) e Step Into (F11)
4. Examine variáveis na janela "Locals"

### Logs

```csharp
// Adicionar logging
public class CreateOrderUseCase
{
    private readonly ILogger<CreateOrderUseCase> _logger;

    public async Task Execute(CreateOrderCommand command)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", command.CustomerId);
        
        try
        {
            // ...
            _logger.LogInformation("Order {OrderId} created successfully", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for customer {CustomerId}", command.CustomerId);
            throw;
        }
    }
}
```

---

## Boas Práticas

### DO's ✅

- Validar todas as entradas
- Usar async/await corretamente
- Documentar métodos públicos
- Escrever testes unitários
- Usar injeção de dependência
- Seguir o princípio de responsabilidade única
- Tratar exceções adequadamente

### DON'Ts ❌

- Não expor detalhes internos das entidades
- Não usar `async void` (exceto event handlers)
- Não capturar exceções genéricas sem relanç
- Não criar dependências circulares
- Não colocar lógica de negócio em ViewModels
- Não acessar banco diretamente de Use Cases

---

## Recursos Úteis

- [Clean Architecture - Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
