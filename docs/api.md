# API Reference

All JSON. Except where marked, endpoints require a `Authorization: Bearer <JWT>` header.

An OpenAPI document is generated and mapped in development by the OpenAPI extension (`AddOpenApi`/`MapOpenApi`). An interactive UI (e.g. Scalar) can be pointed at the generated JSON document.

## Authentication

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| POST | `/api/auth/register` | Public | Register a customer, returns `AuthResponse` (user + token) |
| POST | `/api/auth/login` | Public | Login, returns `AuthResponse` |

## Products

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| GET | `/api/products` | Public | Search/list products with paging/filter/sort |
| GET | `/api/products/{id}` | Public | Product detail |
| POST | `/api/products` | Admin | Create product |
| PUT | `/api/products/{id}` | Admin | Update product |
| DELETE | `/api/products/{id}` | Admin | Deactivate/delete product |
| PUT | `/api/products/{id}/inventory` | Admin | Update inventory |

## Cart

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| GET | `/api/cart` | Customer | View cart |
| POST | `/api/cart/items` | Customer | Add item |
| PUT | `/api/cart/items/{productId}` | Customer | Update quantity |
| DELETE | `/api/cart/items/{productId}` | Customer | Remove item |
| DELETE | `/api/cart` | Customer | Clear cart |

## Checkout & Orders

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| POST | `/api/checkout` | Customer | Place order (reserves stock, processes payment) |
| GET | `/api/orders` | Customer | List own orders |
| GET | `/api/orders/{orderId}` | Customer/Admin | Order detail (ownership checked) |
| POST | `/api/orders/{orderId}/cancel` | Customer/Admin | Cancel order (releases stock) |
| GET | `/api/admin/orders` | Admin | List all orders |
| PUT | `/api/admin/orders/{orderId}/status` | Admin | Update order status (publishes SignalR notification) |

## Payments

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| POST | `/api/payments/webhook` | Provider (HMAC signed) | Accept provider webhook; verify `X-Signature` (HMAC-SHA256 over body) |

Dev webhook secret: `dev-webhook-secret-change-me` (see `appsettings.json` → `Payments.WebhookSecret`).

## Operations

| Method | Path | Access | Description |
|--------|------|--------|-------------|
| GET | `/health` | Public | Liveness |
| GET | `/health/ready` | Public | Readiness |
| GET | `/api/admin/metrics` | Admin | In-memory request metrics |
| POST | `/api/admin/outbox/process` | Admin | Trigger background outbox processing |

## Real-time (SignalR)

- Hub endpoint: `/hubs/orders` (WebSocket transport; requires JWT).
- Outgoing to client: `ReceiveOrderStatusChanged(orderId, status)`.
- Client → hub: `SubscribeToOrder(orderId)`.