<p align="center">
  <img src="icon.png" alt="GM.DistributedLock Samples" width="140" height="140" />
</p>

# GM.DistributedLock Samples

[![CI](https://github.com/gmetskhvarishvili/GM.DistributedLock.Samples/actions/workflows/ci.yml/badge.svg)](https://github.com/gmetskhvarishvili/GM.DistributedLock.Samples/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A minimal ASP.NET Core Web API that shows
**[GM.DistributedLock](https://www.nuget.org/packages/GM.DistributedLock)** preventing a classic
race: **overselling inventory**. Concurrent reservations for the same SKU are serialized by a
per-SKU lock, so a non-atomic check-and-decrement can never sell more than the available stock.
Swap between the in-memory and
**[GM.DistributedLock.Redis](https://www.nuget.org/packages/GM.DistributedLock.Redis)** backends
with one config setting. Targets **.NET 10**.

## What it demonstrates

- An `InventoryStore` whose `TryConsume` is deliberately **non-atomic** (a real read-then-write
  race window).
- An `InventoryService` that wraps it in `IDistributedLock.AcquireAsync("stock:{sku}", …)`, so the
  check-and-decrement runs exclusively per SKU — **50 concurrent reservations against a stock of 5
  yield exactly 5 successes** and never a negative balance.
- Backend switching with **no code change** — `Lock:Provider` = `Memory` (default) or `Redis`.

## Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/inventory/{sku}/reserve` | Reserves one unit; returns `reserved` and `remaining` |
| `GET` | `/api/v1/inventory/{sku}` | Returns the remaining stock |
| `GET` | `/health/live` | Liveness probe (no downstream checks) |
| `GET` | `/health/ready` | Readiness probe |

Seeded SKUs: `widget` (5 in stock) and `gadget` (100). Hammer
`POST /api/v1/inventory/widget/reserve` concurrently and you'll see exactly five `reserved: true`
responses.

## Running

```bash
dotnet run --project GM.DistributedLock.Sample.API
```

To coordinate across multiple instances, use Redis — run one locally with
`docker run -p 6379:6379 -d redis` and set `Lock:Provider` to `Redis`:

```bash
Lock__Provider=Redis dotnet run --project GM.DistributedLock.Sample.API
```

## Testing

```bash
dotnet test
```

The tests drive `InventoryService` with the in-memory lock and assert that concurrent reservations
never oversell — no Redis required.

## License

MIT — see [LICENSE](LICENSE).
