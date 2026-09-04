# Architecture

This is a **modular monolith** using **Clean Architecture** with a strict dependency rule: dependencies point inward, and the outer layers only depend on abstractions defined in the inner layers.

## Layers & direction

```
API  →  Infrastructure  →  Application  →  Domain
        (EF Core,       (use cases,    (entities, VOs,
         caching, jobs)  services)      events, rules)
```

| Layer | Responsibilities | Notes |
|-------|-------------------|-------|
| `ECommerce.Domain` | Entities, Value Objects, Domain Events, Exceptions, Enums. Zero dependencies. | `Money`, `Email`, `OrderStatus`, outbox message, domain events. |
| `ECommerce.Application` | Use cases/services, DTOs, validation, and **port interfaces** (`IProductRepository`, `ICacheService`, `IEmailService`, `IPaymentProvider`, `IOrderNotifier`, …). | Depends on Domain only. |
| `ECommerce.Infrastructure` | EF Core `DbContext`, repositories, `UnitOfWork`, caching, background jobs, email, metrics implementations. | Implements Application ports. |
| `ECommerce.Api` | Controllers, middleware, Hubs, JWT security, OpenAPI, health, configuration. | Composition root; wires DI. |

## Key design decisions

- **Outbox pattern**: When `UnitOfWork.SaveChangesAsync` runs, aggregate domain events are serialized into the `OutboxMessages` table in the same transaction (see `UnitOfWork.CaptureDomainEventsToOutbox`). An `OutboxProcessingService` later dispatches them to handlers (`EventDispatcher` → `OrderPlacedEventHandler` → `IEmailService`), with retry/attempt tracking. A trigger endpoint (`POST /api/admin/outbox/process`) is provided; in production a hosted background worker can call `ProcessPendingAsync` on a timer.
- **Caching**: `ICacheService` abstracts caching. The default `InMemoryCacheService` is thread-safe with TTL expiry; swap in a Redis implementation in `Infrastructure.DependencyInjection` without touching application code. Products are cached (`products:list:{key}`, `products:detail:{id}`, TTL 10 min) and invalidated on writes.
- **Payments**: `IPaymentProvider` abstraction with `MockPaymentProvider`. `PaymentService` provides idempotent ordering (`IdempotencyKey`) and reconciles webhooks by provider reference. Webhooks are signed with HMAC-SHA256 and verified by `WebhookSignatureVerifier` before processing.
- **Real-time**: `OrderHub` (SignalR, `[Authorize]`) adds each connection to a `user-{id}` group on connect and supports per-order subscription via `SubscribeToOrder`. `OrderNotifier` (API) uses `IHubContext<OrderHub, IOrderNotificationClient>` to push status changes from `OrderService`.
- **Identity**: All order/cart scoping flows from the JWT (`ICurrentUser`/`IUserContext`), not from route `{userId}`.

## Data model (core aggregates)

- `User` (roles: Customer, Admin)
- `Category`
- `Product`
- `Inventory` (reserved/available quantities)
- `Cart` / `CartItem`
- `Order` / `OrderItem` (+ status, shipping address) — records domain events on creation/placement/status
- `Payment` (status, provider reference, idempotency key)
- `OutboxMessage`

## Error handling & logging

- `ExceptionHandlingMiddleware` maps domain exceptions to HTTP problem details (e.g., 401, 404, 409, 400).
- `RequestLoggingMiddleware` adds a correlation id and structured log lines.
- Health endpoints: `/health` (liveness), `/health/ready` (readiness).
- Metrics: `IRequestMetricsService` records per-path request counts, exposed at `GET /api/admin/metrics` (admin only).

## Observability / production readiness

- Docker (see `docker/Dockerfile`, `docker/docker-compose.yml`).
- GitHub Actions CI (`.github/workflows/ci.yml`): restore → build → test, uploads TRX results.
- Structured logging via `Microsoft.Extensions.Logging`; prepared for metrics/tracing enrichment later.