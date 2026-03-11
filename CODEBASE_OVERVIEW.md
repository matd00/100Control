# Codebase Overview - PaintballManager

This document provides a concise overview of the project structure and the role of each component within the codebase, following Clean Architecture principles.

## 🏗 Project Architecture

The system is organized into several projects following a layered architecture:

### 1. **Domain** (`/Domain`)
- **Role:** Pure business logic and domain entities. It has no dependencies on other layers.
- **Key Folders:**
  - `Entities/`: Business objects (e.g., `Product.cs`, `Order.cs`, `Customer.cs`).
  - `Interfaces/Repositories/`: Abstractions for data access (e.g., `IProductRepository.cs`).

### 2. **Application** (`/Application`)
- **Role:** Orchestrates the domain logic to fulfill specific use cases.
- **Key Folders:**
  - `UseCases/`: Specific business scenarios (e.g., `CreateOrderUseCase.cs`).
  - `Services/`: Application-wide services like `MarketplaceSyncService.cs`.

### 3. **Persistence** (`/Persistence`)
- **Role:** Concrete implementation of the data access interfaces using Entity Framework Core.
- **Key Folders:**
  - `Context/`: EF Core `PaintballManagerDbContext`.
  - `Repositories/`: EF Core implementations of the domain interfaces (e.g., `EfProductRepository.cs`).
  - `Migrations/`: Database schema history.

### 4. **Integrations** (`/Integrations`)
- **Role:** Clients and services for external API communication.
- **Key Folders:**
  - `MercadoLivre/`: Integration with Mercado Livre API.
  - `Shopee/`: Integration with Shopee API.
  - `SuperFrete/`: Integration with SuperFrete for shipping and tracking.

### 5. **Infrastructure** (`/Infrastructure`)
- **Role:** Currently a placeholder for cross-cutting infrastructure concerns (logging, configuration, etc.).

### 6. **Presentation Layers**

There are multiple presentation projects, but the primary active one is:
- **src/Desktop**: The main WPF Application.
  - `Features/`: UI logic organized by feature (Dashboard, Products, Orders, etc.).
  - `Infrastructure/`: UI-specific infrastructure like `MVVM` base classes and `ServiceProviderConfiguration`.

Other presentation projects (experimental or legacy):
- **100Control**: A WinUI 3 (Project Reunion) implementation (largely empty/scaffold).
- **src/Api**: An ASP.NET Core API for external access.
- **PaintballManager.Desktop**: A partial/legacy WPF project.

### 7. **Tests** (`/Tests`)
- **Application.Tests**: Unit tests for the Application layer.
- **Domain.Tests**: Unit tests for the Domain entities and business rules.

---

## 🚀 Getting Started for AI Agents

When working on this codebase:
- **Business Logic:** Look in `Domain` for rules and `Application` for processes.
- **Data Access:** Check `Persistence` for how things are saved.
- **External APIs:** Check `Integrations`.
- **UI Changes:** Focus on `src/Desktop` for the WPF interface.
- **Verification:** Always run or add tests in the `Tests` directory.

The main database context is `PaintballManagerDbContext` in the `Persistence` project.
The application uses Dependency Injection, configured in `src/Desktop/Infrastructure/ServiceProviderConfiguration.cs`.
