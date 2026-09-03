# E-Commerce Backend

A production-ready E-Commerce backend API built with **ASP.NET Core 8** and **C#**, following **Clean Architecture**.

This is a backend-only project: REST API, business logic, data access, security, background jobs, real-time notifications, and tests. There is no frontend. Use **Swagger** as the interactive UI to exercise the API.

Repository: https://github.com/Sunaila-Amin/E-Commerce-Backend

## Features

- JWT authentication with `User` and `Admin` roles (register, login, profile)
- Product and category catalog with search, inventory tracking, and caching
- Shopping cart (add, update, remove, clear) with available-stock validation
- Orders (place, cancel) with inventory reservation and release
- Payments processing
- User addresses
- Admin inventory management (update, stock adjust, low-stock listing)
- Distributed caching (`IDistributedCache`) with configurable provider: InMemory or Redis
- Real-time notifications via SignalR (`/hubs/notifications`)
- Background jobs via Hangfire (order processing, low-stock alerts, email notifications) at `/hangfire`
- EF Core + SQL Server with code-first migrations and seed data
- xUnit + Moq + FluentAssertions test suite (unit + integration)

## Tech Stack

- .NET 8 (ASP.NET Core Web API), C#
- Entity Framework Core + SQL Server
- AutoMapper
- JWT Bearer authentication
- Redis / in-memory distributed cache
- SignalR
- Hangfire with SQL Server storage
- Serilog (console + rolling file)
- Swagger / Swashbuckle
- xUnit, Moq, FluentAssertions

## Architecture

Clean Architecture with 5 projects:

```text
ECommerce.slnx
src/
  ECommerce.Models/     # Entities, enums, base/auditable types
  ECommerce.Data/       # DbContext, migrations, repositories, UnitOfWork,
                        # Redis cache, SignalR hub, Hangfire jobs, seeder
  ECommerce.Business/   # Services, DTOs, validators, AutoMapper profiles
  ECommerce.API/        # Program.cs, controllers, middleware, JWT, Swagger
tests/
  ECommerce.Tests/      # Unit + integration tests
```

| Project | Responsibility |
|---|---|
| `ECommerce.Models` | Domain entities (Product, Order, Cart, User, etc.) and enums |
| `ECommerce.Data` | EF Core persistence, repositories, caching, real-time, jobs, seeding |
| `ECommerce.Business` | Application services, DTOs, validation, mapping |
| `ECommerce.API` | HTTP layer: controllers, auth, Swagger, middleware |
| `ECommerce.Tests` | xUnit unit and integration tests |

Key domain entities: `User`, `Role`, `Product`, `Category`, `Inventory`, `Cart`, `CartItem`, `Order`, `OrderItem`, `Payment`, `Address`.

Key services: `AuthService`, `JwtTokenService`, `ProductService`, `CategoryService`, `CartService`, `OrderService`, `PaymentService`, `InventoryService`, `AddressService`.

## Prerequisites

- .NET 8 SDK or later (verified with .NET 10 SDK `10.0.400`; projects target `net8.0`)
- SQL Server (local instance with Windows Authentication, or SQL Server via Docker)
- Optional: Redis (only needed when `Cache:Provider` is `Redis`)

## Getting Started

### 1. Configure the database

Default configuration in `src/ECommerce.API/appsettings.json` uses local SQL Server with Windows Authentication:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ECommerceDb;Integrated Security=True;TrustServerCertificate=True;",
  "Redis": "localhost:6379"
},
"Cache": {
  "Provider": "InMemory"
}
```

Set `Cache:Provider` to `Redis` if you have Redis running (the `Redis` connection string is used in that case).

### 2. Create and seed the database

On first run, apply migrations and seed roles, categories, admin user, products, and inventory:

```powershell
dotnet run --project src/ECommerce.API -- --seed
```

Seeded admin account:

- Email: `admin@ecommerce.com`
- Password: `Admin@123`

Seeded catalog: Electronics / Apparel categories (plus Smartphones, Laptops, T-Shirts), Smartphone X, Laptop Pro, Cotton T-Shirt with inventory.

On later runs the database already exists, so just run:

```powershell
dotnet run --project src/ECommerce.API
```

### 3. Open Swagger

- HTTP: `http://localhost:5281/swagger`
- HTTPS: `https://localhost:7029/swagger`

Hangfire dashboard (background jobs): `http://localhost:5281/hangfire`

## Authentication

1. Call `POST /api/auth/login` with:
   ```json
   {
     "email": "admin@ecommerce.com",
     "password": "Admin@123"
   }
   ```
2. Copy the `token` value from the response (the long string starting with `eyJ...`). Copy only the token itself, without quotes or the `expiresAt` field.
3. Click **Authorize** in Swagger and enter:
   ```text
   Bearer <paste-your-token-here>
   ```
4. Verify with `GET /api/auth/profile`.

Tokens expire after 120 minutes (`Jwt:ExpiryMinutes` in `appsettings.json`). If you get `401` with `invalid_token` / token-expired, log in again for a fresh token.

## API Overview

| Area | Endpoints |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/profile` |
| Products | `GET /api/products`, `GET /api/products/{id}`, `POST /api/products`, `PUT /api/products/{id}`, `DELETE` |
| Categories | `GET /api/categories`, CRUD for admins |
| Cart | `GET /api/cart`, `POST /api/cart/items`, `PUT /api/cart/items/{cartItemId}`, `DELETE /api/cart/items/{cartItemId}`, `DELETE /api/cart` |
| Orders | `POST /api/orders`, `GET /api/orders`, `GET /api/orders/{id}`, cancel |
| Payments | Payment creation and processing endpoints |
| Addresses | User address CRUD |
| Inventory (admin) | Get, update, `POST /api/inventory/adjust` (body uses `Delta`), low-stock |

Add-to-cart request body:

```json
{
  "productId": 1,
  "quantity": 2
}
```

Seeded product IDs: `1` = Smartphone X, `2` = Laptop Pro, `3` = Cotton T-Shirt.

## Business Rules

- Cart is a wish list, but adding items validates available stock: the resulting cart quantity must not exceed `Inventory.Available` (`Quantity - Reserved`). Otherwise the API returns `Only X unit(s) of {product} available in stock.`
- Placing an order independently re-checks stock per line and rejects with `Insufficient stock for {product}.` if unavailable (defense in depth, e.g. stock dropped between cart-add and checkout).
- Placing an order reserves stock (`Reserved += quantity`); cancelling releases it (`Reserved -= quantity`).
- Admins adjust inventory via the inventory adjust endpoint with a positive or negative `Delta`.

## Caching

Services cache read-heavy data (e.g. product catalog) through `ICacheService.GetOrSetAsync(key, factory, expiration)`:

1. Cache hit returns deserialized DTOs without hitting the database.
2. Cache miss runs the factory (repository query), stores the JSON-serialized result with a TTL, and returns it.
3. Writes invalidate the relevant keys via `RemoveAsync`.

The provider is configured with `Cache:Provider` (`InMemory` uses `AddDistributedMemoryCache`; `Redis` uses `AddStackExchangeRedisCache`). Both implement `IDistributedCache`, so application code is unchanged.

## Real-Time Notifications

- Hub: `NotificationHub`, mapped at `/hubs/notifications` in `Program.cs`.
- `NotificationService` (via `IHubContext<NotificationHub>`) broadcasts `OrderStatusChanged` to the `user-{userId}` group and `StockChanged` to the `stock-monitors` group.
- JWT for SignalR is read from the `access_token` query string during the WebSocket handshake.
- Hangfire jobs handle order processing, low-stock alerts, and email notifications.

## Testing

Run the full suite (unit + integration):

```powershell
dotnet test tests/ECommerce.Tests/ECommerce.Tests.csproj
```

Integration tests build the real DI graph with EF Core InMemory, in-memory distributed cache, and mocked background/notification services, so no SQL Server or Redis is required to run tests.

## Resetting the Local Database

To drop and re-seed from scratch:

```powershell
sqlcmd -S localhost -E -Q "IF DB_ID('ECommerceDb') IS NOT NULL BEGIN ALTER DATABASE ECommerceDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ECommerceDb; END"
dotnet run --project src/ECommerce.API -- --seed
```

## Notes

- Build artifacts (`bin/`, `obj/`), logs, and test results are excluded via `.gitignore`.
- `appsettings.json` contains development defaults (local SQL Server, in-memory cache, dev JWT key). Change the JWT key and use proper secrets before any production use.
