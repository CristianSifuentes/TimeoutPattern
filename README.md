# TimeoutPattern — Granular timeouts in a .NET 8 Web API

A timeout is not a number: it is a **time budget** that gets divided up.
The client has a total amount of time, the request gets a share of it, each dependency gets a
share of what is left, and the downstream service knows how much time its caller has left.
This project implements that idea with `CancellationTokenSource`, `TimeProvider` and a bit of statistics.

```
Client ──X-Request-Timeout-Ms: 2000──▶ products.read (2500 ms by policy → 2000 ms because the client asked for it)
                                         ├─ catalog.read        min(500, remaining)
                                         ├─ pricing.quote       min(1200, remaining − 50 reserve)
                                         │    └─ pricing.tax    min(300, remaining of pricing.quote) · Pessimistic
                                         └─ inventory.lookup    min(800, remaining − 100 reserve)  · optional → degrades
                                              └─ HTTP ──X-Request-Timeout-Ms: 785──▶ warehouse.stock
```

## Key ideas

| Concept | What it solves | Where |
|---|---|---|
| **Hierarchical policies by key** | `pricing.tax` inherits from `pricing` **field by field** → you configure only what changes | `TimeoutPolicyResolver` |
| **Nested budgets** | `effective = min(policy, parent remaining − reserve)`; a child never outlives its parent | `TimeoutPlanner`, `TimeoutBudget` |
| **Reserve** | Saves time for what comes next (e.g. serializing the response) | `Operations:*:Reserve` |
| **Minimum viable budget** | Does not start work it cannot finish (`BudgetExhaustedException`) | `MinimumViableBudget` |
| **Cooperative vs Pessimistic** | Pessimistic stops waiting for an SDK that ignores the token (`Task.WaitAsync`) | `TimeoutExecutor.Supervise` |
| **Deadline propagation** | gRPC style: the downstream service gives up before its caller does | `DeadlinePropagationHandler` |
| **Inbound deadline** | The client can **shorten** the deadline, never extend it | `ResolveRequest` |
| **Timeout ≠ cancellation** | `DeadlineException : TimeoutException` → 504; caller cancellation → `OperationCanceledException`; client disconnect → 499 | `TimeoutExecutor`, `DeadlineMiddleware` |
| **Forecasting loop** | An EWMA (mean + 2σ) predicts whether another iteration fits and returns a partial result **on time** | `SalesReportService`, `EwmaDurationEstimator` |
| **Observability** | `Server-Timing` (visible in DevTools), explanatory ProblemDetails, `timeout_pattern.*` metrics | `TimeoutTelemetry` |
| **Hot reload** | `IOptionsMonitor`: change `appsettings.json` and it applies without a restart; configuration is validated at startup | `TimeoutOptionsValidator` |

## Structure

```
src/
  TimeoutPattern.Core            Timeout engine (no ASP.NET dependency)
  TimeoutPattern.Application     Use cases: product page, sales report
  TimeoutPattern.Infrastructure  Simulated dependencies, HTTP gateway, deadline propagation
  TimeoutPattern.Api             Deadline middleware, [TimeoutPolicy] attribute, 504 ProblemDetails
tests/
  TimeoutPattern.Tests           Unit tests with FakeTimeProvider (deterministic) + HTTP integration tests
```

## Configuration

```jsonc
"Timeouts": {
  "DefaultRequestTimeout": "00:00:10",
  "DefaultOperationTimeout": "00:00:03",
  "MinimumViableBudget": "00:00:00.020",
  "Propagation": { "HeaderName": "X-Request-Timeout-Ms", "HonorInbound": true, "PropagateOutbound": true, "NetworkAllowance": "00:00:00.015" },
  "Requests": {
    "products":      { "Timeout": "00:00:02.500" },
    "reports":       { "Timeout": "00:00:08", "HonorInboundDeadline": false },
    "reports.sales": { "Timeout": "00:00:03" }          // inherits HonorInboundDeadline=false from "reports"
  },
  "Operations": {
    "pricing":     { "Timeout": "00:00:01.200", "Reserve": "00:00:00.050" },
    "pricing.tax": { "Timeout": "00:00:00.300", "Strategy": "Pessimistic" }   // inherits Reserve from "pricing"
  }
}
```

## Usage

```csharp
[TimeoutPolicy("products.read")]                 // request deadline
public sealed class ProductsController(ProductPageService pages) : ControllerBase { ... }

var product = await executor.ExecuteAsync(       // operation timeout, nested inside the request
    Operations.CatalogRead,
    token => catalog.FindAsync(sku, token),
    cancellationToken);
```

`HttpContext.RequestAborted` is replaced by a token that fires on **disconnect or deadline**, so
any code that already passes the request's `CancellationToken` honors the deadline without changes.

## Try it

```bash
dotnet test                                     # 29 tests
dotnet run --project src/TimeoutPattern.Api     # http://localhost:5080/swagger
```

Open `src/TimeoutPattern.Api/TimeoutPattern.Api.http` and run the scenarios. The header
`X-Simulate-Latency: inventory=3000, tax=2000` makes any dependency slow for that request only.

Example 504:

```json
{
  "title": "Dependency timed out",
  "status": 504,
  "detail": "Operation 'pricing.tax' gave up after 302 ms; it was allowed 300 ms by its own policy.",
  "timeout": {
    "scope": "operation", "name": "pricing.tax",
    "path": "products.read › pricing.quote › pricing.tax",
    "boundBy": "operation-policy", "configuredMs": 300, "effectiveMs": 300, "elapsedMs": 302.2
  }
}
```

## Tuning timeouts with data

The `timeout_pattern.budget.utilization` histogram measures `elapsed ÷ allowed` per operation:

- p99 close to **1.0** → the timeout is cutting off real work: raise it or fix the dependency.
- p99 well below 1.0 → there is headroom: lower it and fail faster.

## Design decisions

- **Why not ASP.NET Core 8's `[RequestTimeout]`**: it covers the request, but it does not split the budget
  across nested operations or propagate the deadline to other services.
- **`HttpClient.Timeout = Infinite`**: the executor decides the call's timeout; the default 100 s would never be reached first.
- **Timeouts are not logged as errors**: the middleware answers them directly with a 504.
  If they were rethrown, .NET 8's `ExceptionHandlerMiddleware` would log each one as an error with its stack trace.
- **`TimeProvider` everywhere**: unit tests advance the clock instead of sleeping.
