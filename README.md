# E-Commerce Backend

An E-Commerce backend API built with **ASP.NET Core** and **C#**.

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
- Real-time notifications via SignalR (`/hubs/notifications`)
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

## Resetting the Local Database

To drop and re-seed from scratch:

```powershell
sqlcmd -S localhost -E -Q "IF DB_ID('ECommerceDb') IS NOT NULL BEGIN ALTER DATABASE ECommerceDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ECommerceDb; END"
dotnet run --project src/ECommerce.API -- --seed
```

## Notes
- Build artifacts (`bin/`, `obj/`), logs, and test results are excluded via `.gitignore`.
- `appsettings.json` contains development defaults (local SQL Server, in-memory cache, dev JWT key). Change the JWT key and use proper secrets before any production use.
