# E-Commerce Backend (.NET 10)

A production-style, modular-monolith e-commerce backend built with **.NET 10**, following **Clean Architecture** principles with a strict dependency rule (dependencies point inward).

## Highlights

- **Clean Architecture**: `Domain` → `Application` → `Infrastructure` → `API`
- **REST API** with OpenAPI/Scalar documentation
- **Authentication & Authorization**: JWT (HS256) via `Microsoft.AspNetCore.Authentication.JwtBearer`, BCrypt password hashing
- **Product catalog & inventory** with concurrency-safe reservations
- **Cart → Checkout → Order** flow with idempotent payments
- **Payment abstraction** with a mock provider and signature-verified webhooks
- **Caching** layer (in-memory; Redis-ready) with product caching and invalidation
- **Outbox pattern** + background processing worker with retry and mock email notifications
- **SignalR** `OrderHub` for real-time order-status notifications
- **Health checks**, structured logging, metrics, and Docker/CI-CD support

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│  ECommerce.Api        Controllers, Hubs, Middleware, Security,      │
│                       OpenAPI, Health, Metrics                       │
│  ECommerce.Infrastructure  EF Core, Repositories, UnitOfWork,        │
│                       Caching, BackgroundJobs, Email, Observability  │
│  ECommerce.Application    Use Cases, Services, DTOs, Ports (ifaces)  │
│  ECommerce.Domain     Entities, Value Objects, Domain Events,        │
│                       Exceptions, Enums                              │
└─────────────────────────────────────────────────────────────────────┘
      Dependency rule: API → Infrastructure ⇢ Application ⇢ Domain
```

See `docs/architecture.md` for details, and `docs/api.md` for the endpoint catalogue.

## Prerequisites

- .NET SDK 10.0+

## Run locally

```bash
dotnet run --project src/ECommerce.Api
```

- API base URL: `http://localhost:8080`
- Default profile uses an **in-memory** database (`Database:Provider = InMemory`) and seeds demo products/categories plus users.
- OpenAPI JSON document is generated and mapped in development via the OpenAPI extension; a UI (e.g. Scalar) can consume the JSON document.

## Test

```bash
dotnet build ECommerce.sln   # must be 0 warnings / 0 errors
dotnet test ECommerce.sln    # unit + integration
```

## Docker

```bash
docker compose -f docker/docker-compose.yml up --build
```

See `docs/architecture.md` and `docs/api.md` for full documentation.

## Default admin (development)

The database initializer seeds a demo admin and customer for development:

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@ecommerce.com` | `AdminPass123!` |
| Customer | `customer@ecommerce.com` | `CustomerPass123!` |

**Change all secrets in production** — see `src/ECommerce.Api/appsettings.json` (`Jwt.Secret`, `Payments.WebhookSecret`).