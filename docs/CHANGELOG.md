# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

## [1.0.0] - 2026-03-07

### Adicionado

#### Estrutura do Projeto
- Arquitetura Clean Architecture com separação em 6 projetos
- Camada de Domínio com entidades e interfaces
- Camada de Aplicação com Use Cases
- Camada de Persistência com repositórios In-Memory
- Camada de Integrações para APIs externas
- Projeto Desktop WPF com MVVM

#### Entidades de Domínio
- `Product` - Gestão de produtos e estoque
- `Order` - Gestão de pedidos multi-canal
- `OrderItem` - Itens de pedido
- `Customer` - Cadastro de clientes
- `Supplier` - Cadastro de fornecedores
- `Purchase` - Registro de compras
- `PurchaseItem` - Itens de compra
- `Kit` - Sistema de kits de produtos
- `KitItem` - Componentes de kit
- `Shipment` - Gestão de envios
- `InventoryMovement` - Rastreamento de movimentações
- `Part` - Peças e componentes

#### Repositórios
- `EfProductRepository` - Repositório de produtos (Entity Framework)
- `EfOrderRepository` - Repositório de pedidos
- `EfCustomerRepository` - Repositório de clientes
- `EfSupplierRepository` - Repositório de fornecedores
- `EfPurchaseRepository` - Repositório de compras
- `EfKitRepository` - Repositório de kits
- `EfPartRepository` - Repositório de peças
- `EfShipmentRepository` - Repositório de envios
- `EfInventoryMovementRepository` - Repositório de movimentações

#### Casos de Uso
- `CreateOrderUseCase` - Criação de pedidos com validação de estoque
- `UpdateOrderStatusUseCase` - Atualização de status de pedidos
- `CreateProductUseCase` - Cadastro de produtos
- `RegisterCustomerUseCase` - Cadastro de clientes
- `RegisterPurchaseUseCase` - Registro de compras
- `GenerateShipmentUseCase` - Geração de envios

#### Serviços de Aplicação
- `MarketplaceSyncService` - Sincronização com marketplaces
- `AutomationService` - Automações do sistema

#### Integrações
- `IMercadoLivreService` / `MercadoLivreService`
  - Buscar pedidos
  - Buscar produtos
  - Atualizar estoque
  - Atualizar preços
- `IShopeeService` / `ShopeeService`
  - Buscar pedidos
  - Buscar produtos
  - Atualizar estoque
- `ISuperFreteService` / `SuperFreteService`
  - Calcular frete
  - Gerar etiquetas
  - Rastrear envios

#### Interface Desktop (WPF)
- Design system moderno com cores e estilos consistentes
- Sidebar com menu de navegação
- Dashboard com métricas e ações rápidas
- Página de Produtos com tabela e ações
- Página de Pedidos com cards por status
- Página de Clientes com tabela
- Página de Fornecedores com tabela
- Página de Compras com filtros por tipo
- Página de Kits com cards de componentes
- Página de Envios com rastreamento
- Página de Mercado Livre (integração)
- Página de Shopee (integração)

#### Infraestrutura MVVM
- `ViewModelBase` - Classe base para ViewModels
- `RelayCommand` - Implementação de ICommand
- `ServiceProviderConfiguration` - Container de DI

#### Documentação
- README.md principal
- ARCHITECTURE.md - Documentação de arquitetura
- DEVELOPMENT.md - Guia de desenvolvimento
- API_REFERENCE.md - Referência do domínio
- CHANGELOG.md - Histórico de mudanças

### Segurança
- Validação de entrada em todas as entidades
- Limite de caracteres em campos de texto
- Limite de quantidade em operações de estoque
- Validação de estoque antes de vendas
- Proteção contra valores negativos
- Sanitização de dados de entrada

---

## [Unreleased]

### Adicionado
- ✅ **Persistência com Entity Framework Core + SQLite**
  - DbContext completo com todas as entidades
  - Repositórios EF para todas as entidades
  - Banco de dados local em `%LOCALAPPDATA%/PaintballManager/paintballmanager.db`
  - Dados persistem entre execuções da aplicação

### Planejado
- Sistema de autenticação e autorização
- Relatórios em PDF
- Gráficos de vendas (dashboard avançado)
- Notificações push
- Backup automático de dados
- API REST para acesso externo
- Aplicativo mobile com .NET MAUI
- Testes unitários e de integração
- CI/CD com GitHub Actions
- Docker support

---

## Guia de Versionamento

- **MAJOR (X.0.0)**: Mudanças incompatíveis na API
- **MINOR (0.X.0)**: Novas funcionalidades compatíveis
- **PATCH (0.0.X)**: Correções de bugs compatíveis

### Tipos de Mudanças

- `Adicionado` - Novas funcionalidades
- `Alterado` - Mudanças em funcionalidades existentes
- `Obsoleto` - Funcionalidades que serão removidas
- `Removido` - Funcionalidades removidas
- `Corrigido` - Correções de bugs
- `Segurança` - Correções de vulnerabilidades
