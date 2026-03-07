# Arquitetura do Sistema - PaintballManager

## Visão Geral

O PaintballManager segue os princípios da **Clean Architecture** (Arquitetura Limpa) proposta por Robert C. Martin, combinada com conceitos de **Domain-Driven Design (DDD) leve**.

## Camadas da Arquitetura

### 1. Domain Layer (Camada de Domínio)

**Responsabilidade:** Contém as regras de negócio puras do sistema.

**Componentes:**
- **Entities (Entidades):** Objetos de negócio com identidade única
- **Value Objects:** Objetos imutáveis sem identidade (ex: Address, Money)
- **Domain Events:** Eventos que ocorrem no domínio
- **Repository Interfaces:** Contratos para persistência de dados

**Características:**
- ✅ Não depende de nenhuma outra camada
- ✅ Contém apenas lógica de negócio
- ✅ Não conhece frameworks ou bibliotecas externas
- ✅ Entidades são auto-validadas

```csharp
// Exemplo de entidade auto-validada
public class Product
{
    public Product(string name, decimal cost, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty");
            
        if (price < cost)
            throw new ArgumentException("Price cannot be less than cost");
            
        // ... inicialização
    }
}
```

### 2. Application Layer (Camada de Aplicação)

**Responsabilidade:** Orquestra o fluxo de dados e coordena a execução das regras de negócio.

**Componentes:**
- **Use Cases:** Casos de uso específicos da aplicação
- **Application Services:** Serviços que coordenam múltiplos use cases
- **DTOs/Commands:** Objetos de transferência de dados
- **Interfaces:** Contratos para serviços externos

**Características:**
- ✅ Depende apenas da camada de Domínio
- ✅ Define a interface da aplicação
- ✅ Não contém lógica de negócio (delega ao domínio)
- ✅ Coordena transações e fluxos

```csharp
// Exemplo de Use Case
public class CreateOrderUseCase
{
    public async Task Execute(CreateOrderCommand command)
    {
        // 1. Validação de entrada
        // 2. Buscar entidades necessárias
        // 3. Executar lógica de domínio
        // 4. Persistir resultados
        // 5. Disparar eventos
    }
}
```

### 3. Infrastructure Layer (Camada de Infraestrutura)

**Responsabilidade:** Implementa as interfaces definidas nas camadas superiores.

**Subprojetos:**

#### 3.1 Persistence
- Implementação dos repositórios
- Acesso a banco de dados
- Mapeamento objeto-relacional

#### 3.2 Integrations
- Comunicação com APIs externas
- Mercado Livre, Shopee, SuperFrete
- Parsing e transformação de dados

#### 3.3 Infrastructure
- Serviços de suporte
- Logging, caching, etc.

**Características:**
- ✅ Implementa interfaces do domínio e aplicação
- ✅ Conhece detalhes técnicos (banco, APIs, etc.)
- ✅ Facilmente substituível (ex: trocar banco de dados)

### 4. Presentation Layer (Camada de Apresentação)

**Responsabilidade:** Interface com o usuário.

**Projeto:** Desktop (WPF)

**Padrão:** MVVM (Model-View-ViewModel)

**Componentes:**
- **Views (XAML):** Interface visual
- **ViewModels:** Lógica de apresentação
- **Commands:** Ações do usuário
- **Converters:** Transformação de dados para exibição

---

## Diagrama de Dependências

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│                          PRESENTATION                                   │
│                           (Desktop)                                     │
│                                                                         │
│    ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐        │
│    │    Views     │◄───│  ViewModels  │◄───│    Commands      │        │
│    │   (XAML)     │    │              │    │                  │        │
│    └──────────────┘    └──────────────┘    └──────────────────┘        │
│                              │                                          │
└──────────────────────────────┼──────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│                          APPLICATION                                    │
│                                                                         │
│    ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐        │
│    │  Use Cases   │    │   Services   │    │    Commands      │        │
│    │              │    │              │    │    (DTOs)        │        │
│    └──────────────┘    └──────────────┘    └──────────────────┘        │
│           │                   │                                         │
└───────────┼───────────────────┼─────────────────────────────────────────┘
            │                   │
            ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│                            DOMAIN                                       │
│                                                                         │
│    ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐        │
│    │   Entities   │    │  Interfaces  │    │  Value Objects   │        │
│    │              │    │ (Repository) │    │                  │        │
│    └──────────────┘    └──────────────┘    └──────────────────┘        │
│                              ▲                                          │
└──────────────────────────────┼──────────────────────────────────────────┘
                               │
                               │ (implements)
                               │
┌──────────────────────────────┼──────────────────────────────────────────┐
│                              │                                          │
│                        INFRASTRUCTURE                                   │
│                                                                         │
│    ┌──────────────┐    ┌──────────────┐    ┌──────────────────┐        │
│    │ Persistence  │    │ Integrations │    │  Infrastructure  │        │
│    │ (Repos impl) │    │ (APIs)       │    │  (Services)      │        │
│    └──────────────┘    └──────────────┘    └──────────────────┘        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Fluxo de uma Requisição

### Exemplo: Criar um Pedido

```
┌─────────┐    ┌───────────────┐    ┌──────────────────┐    ┌────────────┐
│  VIEW   │───►│  VIEWMODEL    │───►│  CreateOrderUse  │───►│  DOMAIN    │
│ (XAML)  │    │ (Command)     │    │  Case            │    │  (Order)   │
└─────────┘    └───────────────┘    └──────────────────┘    └────────────┘
                                            │                      │
                                            ▼                      ▼
                                    ┌──────────────────┐    ┌────────────┐
                                    │  IOrderRepository│───►│ Repository │
                                    │  (interface)     │    │ (Persist.) │
                                    └──────────────────┘    └────────────┘
```

**Passo a passo:**

1. **View (XAML):** Usuário clica em "Criar Pedido"
2. **ViewModel:** Executa `CreateOrderCommand`
3. **Use Case:** Recebe o comando e valida
4. **Domain:** Entidade `Order` é criada com suas regras
5. **Repository Interface:** Chama `SaveAsync(order)`
6. **Repository Implementation:** Persiste no banco
7. **Response:** Retorna para a View atualizar UI

---

## Padrões Utilizados

### Repository Pattern
Abstrai o acesso a dados, permitindo trocar a implementação sem afetar o domínio.

```csharp
// Interface (Domain)
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task SaveAsync(Product product);
    Task DeleteAsync(Guid id);
}

// Implementação (Persistence)
public class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = new();
    
    public async Task SaveAsync(Product product)
    {
        _products[product.Id] = product;
    }
}
```

### Use Case Pattern
Encapsula uma operação de negócio específica.

```csharp
public class CreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    
    public async Task Execute(CreateOrderCommand command)
    {
        // Lógica específica para criar pedido
    }
}
```

### MVVM Pattern (Model-View-ViewModel)
Separa a lógica de UI dos dados.

```csharp
public class ProductsViewModel : ViewModelBase
{
    private ObservableCollection<Product> _products;
    
    public ICommand LoadProductsCommand { get; }
    public ICommand CreateProductCommand { get; }
}
```

### Dependency Injection
Inversão de controle para facilitar testes e manutenção.

```csharp
services.AddSingleton<IProductRepository, InMemoryProductRepository>();
services.AddTransient<CreateOrderUseCase>();
```

---

## Benefícios da Arquitetura

| Benefício | Descrição |
|-----------|-----------|
| **Testabilidade** | Cada camada pode ser testada isoladamente |
| **Manutenibilidade** | Mudanças em uma camada não afetam outras |
| **Escalabilidade** | Fácil adicionar novos casos de uso |
| **Flexibilidade** | Pode trocar banco, framework UI, etc. |
| **Independência** | Domain não conhece detalhes técnicos |

---

## Evolução Futura

### Migração para Banco de Dados
```csharp
// Atual (In-Memory)
services.AddSingleton<IProductRepository, InMemoryProductRepository>();

// Futuro (Entity Framework)
services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(connectionString));
services.AddScoped<IProductRepository, EfProductRepository>();
```

### Migração para API REST
```csharp
// Adicionar projeto WebApi
// Controllers chamam os mesmos Use Cases
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrderUseCase;
    
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderCommand command)
    {
        await _createOrderUseCase.Execute(command);
        return Ok();
    }
}
```

### Adicionar Aplicativo Mobile
```csharp
// MAUI pode usar os mesmos ViewModels
// Apenas criar novas Views para mobile
```

---

## Conclusão

A arquitetura do PaintballManager foi projetada para ser:

- 🎯 **Focada no negócio** - O domínio é o centro
- 🔄 **Flexível** - Fácil de mudar tecnologias
- 🧪 **Testável** - Cada parte pode ser testada
- 📈 **Escalável** - Cresce de forma organizada
- 🔒 **Segura** - Validações em múltiplas camadas
