# Referência da API de Domínio - PaintballManager

Este documento descreve todas as entidades, interfaces e contratos do domínio do sistema.

---

## Índice

- [Entidades](#entidades)
  - [Product](#product)
  - [Order](#order)
  - [Customer](#customer)
  - [Supplier](#supplier)
  - [Purchase](#purchase)
  - [Kit](#kit)
  - [Part](#part)
  - [Shipment](#shipment)
  - [InventoryMovement](#inventorymovement)
- [Enumerações](#enumerações)
- [Interfaces de Repositórios](#interfaces-de-repositórios)
- [DTOs de Integração](#dtos-de-integração)

---

## Entidades

### Product

Representa um produto no estoque.

```csharp
namespace Domain.Entities;

public class Product
{
    // Propriedades
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Stock { get; private set; }
    public decimal Cost { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Construtor
    public Product(string name, string description, decimal cost, decimal price)
    
    // Métodos
    public void DecreaseStock(int quantity)
    public void IncreaseStock(int quantity)
    public void UpdatePrice(decimal newPrice)
    public void Deactivate()
}
```

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nome do produto (max 200 caracteres) |
| `Description` | `string` | Descrição do produto |
| `Stock` | `int` | Quantidade em estoque |
| `Cost` | `decimal` | Custo de aquisição |
| `Price` | `decimal` | Preço de venda |
| `IsActive` | `bool` | Se o produto está ativo |
| `CreatedAt` | `DateTime` | Data de criação |
| `UpdatedAt` | `DateTime?` | Data da última atualização |

**Validações:**
- Nome não pode ser vazio ou exceder 200 caracteres
- Custo deve ser maior que 0
- Preço deve ser maior que 0
- Preço não pode ser menor que o custo

**Métodos:**

| Método | Parâmetros | Descrição |
|--------|------------|-----------|
| `DecreaseStock` | `int quantity` | Diminui o estoque (valida disponibilidade) |
| `IncreaseStock` | `int quantity` | Aumenta o estoque |
| `UpdatePrice` | `decimal newPrice` | Atualiza o preço de venda |
| `Deactivate` | - | Desativa o produto |

---

### Order

Representa um pedido de venda.

```csharp
namespace Domain.Entities;

public class Order
{
    // Propriedades
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderSource Source { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Construtor
    public Order(Guid customerId, OrderSource source = OrderSource.Direct)
    
    // Métodos
    public void AddItem(Guid productId, int quantity, decimal price)
    public void RemoveItem(Guid productId)
    public void MarkAsProcessing()
    public void MarkAsCompleted()
    public void Cancel()
}
```

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `CustomerId` | `Guid` | ID do cliente |
| `Source` | `OrderSource` | Origem do pedido (ML, Shopee, etc.) |
| `Items` | `List<OrderItem>` | Itens do pedido |
| `TotalAmount` | `decimal` | Valor total (calculado) |
| `Status` | `OrderStatus` | Status atual |
| `CreatedAt` | `DateTime` | Data de criação |
| `UpdatedAt` | `DateTime?` | Data da última atualização |

**Métodos:**

| Método | Parâmetros | Descrição |
|--------|------------|-----------|
| `AddItem` | `Guid productId, int quantity, decimal price` | Adiciona item ao pedido |
| `RemoveItem` | `Guid productId` | Remove item do pedido |
| `MarkAsProcessing` | - | Muda status para "Processando" |
| `MarkAsCompleted` | - | Muda status para "Concluído" |
| `Cancel` | - | Cancela o pedido |

---

### OrderItem

Representa um item de um pedido.

```csharp
public class OrderItem
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Subtotal => Quantity * UnitPrice;

    public OrderItem(Guid productId, int quantity, decimal unitPrice)
}
```

---

### Customer

Representa um cliente.

```csharp
namespace Domain.Entities;

public class Customer
{
    // Propriedades
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }
    public string Address { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public string Document { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Construtor
    public Customer(string name, string email, string phone, string document)
    
    // Métodos
    public void UpdateAddress(string address, string city, string state, string zipCode)
    public void Deactivate()
}
```

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nome completo (max 200 caracteres) |
| `Email` | `string` | Email válido (max 200 caracteres) |
| `Phone` | `string` | Telefone (max 20 caracteres) |
| `Address` | `string` | Endereço |
| `City` | `string` | Cidade |
| `State` | `string` | Estado (UF) |
| `ZipCode` | `string` | CEP |
| `Document` | `string` | CPF ou CNPJ (max 20 caracteres) |
| `IsActive` | `bool` | Se o cliente está ativo |
| `CreatedAt` | `DateTime` | Data de cadastro |

**Validações:**
- Nome, email, telefone e documento são obrigatórios
- Email deve ter formato válido

---

### Supplier

Representa um fornecedor.

```csharp
namespace Domain.Entities;

public class Supplier
{
    // Propriedades
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Document { get; private set; }
    public string ContactName { get; private set; }
    public string Phone { get; private set; }
    public string Email { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }
    public bool IsInternational { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Construtor
    public Supplier(string name, string document, string contactName, string phone, string email)
}
```

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Razão social |
| `Document` | `string` | CNPJ (nacional) |
| `ContactName` | `string` | Nome do contato |
| `Phone` | `string` | Telefone |
| `Email` | `string` | Email |
| `City` | `string` | Cidade |
| `Country` | `string` | País |
| `IsInternational` | `bool` | Se é fornecedor internacional |
| `IsActive` | `bool` | Se está ativo |

---

### Purchase

Representa uma compra de fornecedor.

```csharp
namespace Domain.Entities;

public class Purchase
{
    // Propriedades
    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public PurchaseType Type { get; private set; }
    public List<PurchaseItem> Items { get; private set; }
    public decimal TotalAmount { get; private set; }
    public PurchaseStatus Status { get; private set; }
    public DateTime PurchaseDate { get; private set; }
    public DateTime? ReceivedDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Construtor
    public Purchase(Guid supplierId, PurchaseType type)
    
    // Métodos
    public void AddItem(Guid productId, int quantity, decimal unitCost)
    public void MarkAsReceived()
}
```

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `SupplierId` | `Guid` | ID do fornecedor |
| `Type` | `PurchaseType` | Tipo (Fábrica ou Usado) |
| `Items` | `List<PurchaseItem>` | Itens da compra |
| `TotalAmount` | `decimal` | Valor total |
| `Status` | `PurchaseStatus` | Status (Pendente, Recebido) |
| `PurchaseDate` | `DateTime` | Data da compra |
| `ReceivedDate` | `DateTime?` | Data de recebimento |

---

### Kit

Representa um kit de paintball.

```csharp
namespace Domain.Entities;

public class Kit
{
    // Propriedades
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public List<KitItem> Items { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Construtor
    public Kit(string name, string description, decimal price)
    
    // Métodos
    public void AddItem(Guid productId, int quantity)
    public void RemoveItem(Guid productId)
    public decimal CalculateCost()
}
```

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `Id` | `Guid` | Identificador único |
| `Name` | `string` | Nome do kit (max 200 caracteres) |
| `Description` | `string` | Descrição dos componentes |
| `Price` | `decimal` | Preço de venda do kit |
| `Items` | `List<KitItem>` | Produtos do kit |
| `IsActive` | `bool` | Se está disponível |

**Validações:**
- Máximo de 100 componentes por kit
- Não permite produtos duplicados

---

### Shipment

Representa um envio/remessa.

```csharp
namespace Domain.Entities;

public class Shipment
{
    // Propriedades
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string TrackingCode { get; private set; }
    public ShipmentProvider Provider { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public decimal ShippingCost { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    // Construtor
    public Shipment(Guid orderId, ShipmentProvider provider, decimal shippingCost)
    
    // Métodos
    public void SetTrackingCode(string trackingCode)
    public void MarkAsShipped()
    public void MarkAsInTransit()
    public void MarkAsDelivered()
}
```

| Status | Descrição |
|--------|-----------|
| `Pending` | Aguardando geração de etiqueta |
| `LabelGenerated` | Etiqueta gerada |
| `Shipped` | Postado |
| `InTransit` | Em trânsito |
| `Delivered` | Entregue |

---

### InventoryMovement

Representa uma movimentação de estoque.

```csharp
namespace Domain.Entities;

public class InventoryMovement
{
    // Propriedades
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public MovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string Reference { get; private set; }
    public string Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Construtor
    public InventoryMovement(Guid productId, MovementType type, int quantity, string reference)
}
```

| Tipo | Quantidade | Descrição |
|------|------------|-----------|
| `Purchase` | Positiva | Entrada por compra |
| `Sale` | Negativa | Saída por venda |
| `Adjustment` | +/- | Ajuste manual |
| `KitAssembly` | Negativa | Montagem de kit |

---

## Enumerações

### OrderSource
```csharp
public enum OrderSource
{
    MercadoLivre = 1,
    Shopee = 2,
    Instagram = 3,
    WhatsApp = 4,
    Direct = 5
}
```

### OrderStatus
```csharp
public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Completed = 4,
    Cancelled = 5
}
```

### PurchaseType
```csharp
public enum PurchaseType
{
    Factory = 1,   // Compra de fábrica
    Used = 2       // Compra de produto usado
}
```

### PurchaseStatus
```csharp
public enum PurchaseStatus
{
    Pending = 1,
    Received = 2
}
```

### ShipmentProvider
```csharp
public enum ShipmentProvider
{
    SuperFrete = 1,
    Correios = 2,
    Transportadora = 3,
    DropShipping = 4
}
```

### ShipmentStatus
```csharp
public enum ShipmentStatus
{
    Pending = 1,
    LabelGenerated = 2,
    Shipped = 3,
    InTransit = 4,
    Delivered = 5
}
```

### MovementType
```csharp
public enum MovementType
{
    Purchase = 1,
    Sale = 2,
    Adjustment = 3,
    KitAssembly = 4
}
```

---

## Interfaces de Repositórios

### IProductRepository
```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<Product>> GetActiveAsync();
    Task<IEnumerable<Product>> GetLowStockAsync(int threshold = 5);
    Task SaveAsync(Product product);
    Task DeleteAsync(Guid id);
}
```

### IOrderRepository
```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetAllAsync();
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<IEnumerable<Order>> GetByCustomerAsync(Guid customerId);
    Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task SaveAsync(Order order);
}
```

### ICustomerRepository
```csharp
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByEmailAsync(string email);
    Task<IEnumerable<Customer>> GetAllAsync();
    Task SaveAsync(Customer customer);
    Task DeleteAsync(Guid id);
}
```

### ISupplierRepository
```csharp
public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task SaveAsync(Supplier supplier);
    Task DeleteAsync(Guid id);
}
```

### IPurchaseRepository
```csharp
public interface IPurchaseRepository
{
    Task<Purchase?> GetByIdAsync(Guid id);
    Task<IEnumerable<Purchase>> GetAllAsync();
    Task<IEnumerable<Purchase>> GetBySupplierAsync(Guid supplierId);
    Task SaveAsync(Purchase purchase);
}
```

### IKitRepository
```csharp
public interface IKitRepository
{
    Task<Kit?> GetByIdAsync(Guid id);
    Task<IEnumerable<Kit>> GetAllAsync();
    Task<IEnumerable<Kit>> GetActiveAsync();
    Task SaveAsync(Kit kit);
    Task DeleteAsync(Guid id);
}
```

### IShipmentRepository
```csharp
public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId);
    Task<IEnumerable<Shipment>> GetAllAsync();
    Task<IEnumerable<Shipment>> GetByStatusAsync(ShipmentStatus status);
    Task SaveAsync(Shipment shipment);
}
```

### IInventoryMovementRepository
```csharp
public interface IInventoryMovementRepository
{
    Task<IEnumerable<InventoryMovement>> GetByProductAsync(Guid productId);
    Task<IEnumerable<InventoryMovement>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task SaveAsync(InventoryMovement movement);
}
```

---

## DTOs de Integração

### Mercado Livre

```csharp
public class MeliOrderDto
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public DateTime CreatedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<MeliOrderItemDto> Items { get; set; }
}

public class MeliOrderItemDto
{
    public string ProductId { get; set; }
    public string Title { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class MeliProductDto
{
    public string ProductId { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
}
```

### Shopee

```csharp
public class ShopeeOrderDto
{
    public string OrderSn { get; set; }
    public string BuyerUsername { get; set; }
    public string BuyerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ShopeeOrderItemDto> Items { get; set; }
}

public class ShopeeOrderItemDto
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

### SuperFrete

```csharp
public class FreightQuoteDto
{
    public decimal Price { get; set; }
    public int DeliveryDays { get; set; }
    public string ServiceType { get; set; }
}

public class ShippingLabelDto
{
    public string TrackingCode { get; set; }
    public string LabelUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class TrackingDto
{
    public string TrackingCode { get; set; }
    public string Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public List<TrackingEventDto> Events { get; set; }
}
```
