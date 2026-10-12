# TimeoutPattern — Timeouts granulares en .NET 8 Web API

Un timeout no es un número: es un **presupuesto de tiempo** que se reparte.
El cliente dispone de un tiempo total, la petición recibe una parte, cada dependencia recibe una
parte de lo que queda y el servicio de abajo sabe cuánto le queda a quien lo llamó.
Este proyecto implementa esa idea con `CancellationTokenSource`, `TimeProvider` y un poco de estadística.

```
Cliente ──X-Request-Timeout-Ms: 2000──▶ products.read (2500 ms por política → 2000 ms porque el cliente lo pidió)
                                         ├─ catalog.read        min(500, restante)
                                         ├─ pricing.quote       min(1200, restante − 50 reserva)
                                         │    └─ pricing.tax    min(300, restante de pricing.quote) · Pessimistic
                                         └─ inventory.lookup    min(800, restante − 100 reserva)  · opcional → degrada
                                              └─ HTTP ──X-Request-Timeout-Ms: 785──▶ warehouse.stock
```

## Ideas clave

| Concepto | Qué resuelve | Dónde |
|---|---|---|
| **Políticas jerárquicas por clave** | `pricing.tax` hereda **campo por campo** de `pricing` → configuras solo lo que cambia | `TimeoutPolicyResolver` |
| **Presupuestos anidados** | `efectivo = min(política, restante del padre − reserva)`; un hijo nunca vive más que su padre | `TimeoutPlanner`, `TimeoutBudget` |
| **Reserva** | Guarda tiempo para lo que viene después (p. ej. serializar la respuesta) | `Operations:*:Reserve` |
| **Mínimo viable** | No arranca trabajo que no puede terminar (`BudgetExhaustedException`) | `MinimumViableBudget` |
| **Cooperative vs Pessimistic** | Pessimistic deja de esperar a un SDK que ignora el token (`Task.WaitAsync`) | `TimeoutExecutor.Supervise` |
| **Propagación de deadline** | Al estilo gRPC: el servicio de abajo se rinde antes que quien lo llama | `DeadlinePropagationHandler` |
| **Deadline entrante** | El cliente puede **acortar** el deadline, nunca alargarlo | `ResolveRequest` |
| **Timeout ≠ cancelación** | `DeadlineException : TimeoutException` → 504; la cancelación del llamador → `OperationCanceledException`; si el cliente se desconecta → 499 | `TimeoutExecutor`, `DeadlineMiddleware` |
| **Bucle con pronóstico** | Una EWMA (media + 2σ) predice si cabe otra iteración y devuelve un resultado parcial **a tiempo** | `SalesReportService`, `EwmaDurationEstimator` |
| **Observabilidad** | `Server-Timing` (visible en DevTools), ProblemDetails explicativos, métricas `timeout_pattern.*` | `TimeoutTelemetry` |
| **Hot reload** | `IOptionsMonitor`: cambias `appsettings.json` y se aplica sin reiniciar; la configuración se valida al arrancar | `TimeoutOptionsValidator` |

## Estructura

```
src/
  TimeoutPattern.Core            Motor de timeouts (sin dependencia de ASP.NET)
  TimeoutPattern.Application     Casos de uso: página de producto, reporte de ventas
  TimeoutPattern.Infrastructure  Dependencias simuladas, gateway HTTP, propagación de deadline
  TimeoutPattern.Api             Middleware de deadline, atributo [TimeoutPolicy], 504 ProblemDetails
tests/
  TimeoutPattern.Tests           Unitarios con FakeTimeProvider (deterministas) + integración por HTTP
```

## Configuración

```jsonc
"Timeouts": {
  "DefaultRequestTimeout": "00:00:10",
  "DefaultOperationTimeout": "00:00:03",
  "MinimumViableBudget": "00:00:00.020",
  "Propagation": { "HeaderName": "X-Request-Timeout-Ms", "HonorInbound": true, "PropagateOutbound": true, "NetworkAllowance": "00:00:00.015" },
  "Requests": {
    "products":      { "Timeout": "00:00:02.500" },
    "reports":       { "Timeout": "00:00:08", "HonorInboundDeadline": false },
    "reports.sales": { "Timeout": "00:00:03" }          // hereda HonorInboundDeadline=false de "reports"
  },
  "Operations": {
    "pricing":     { "Timeout": "00:00:01.200", "Reserve": "00:00:00.050" },
    "pricing.tax": { "Timeout": "00:00:00.300", "Strategy": "Pessimistic" }   // hereda Reserve de "pricing"
  }
}
```

## Uso

```csharp
[TimeoutPolicy("products.read")]                 // deadline de la petición
public sealed class ProductsController(ProductPageService pages) : ControllerBase { ... }

var product = await executor.ExecuteAsync(       // timeout de la operación, anidado en la petición
    Operations.CatalogRead,
    token => catalog.FindAsync(sku, token),
    cancellationToken);
```

`HttpContext.RequestAborted` se reemplaza por un token que se dispara por **desconexión o deadline**, así que
cualquier código que ya pase el `CancellationToken` del request respeta el deadline sin cambios.

## Probarlo

```bash
dotnet test                                     # 29 tests
dotnet run --project src/TimeoutPattern.Api     # http://localhost:5080/swagger
```

Abre `src/TimeoutPattern.Api/TimeoutPattern.Api.http` y ejecuta los escenarios. El header
`X-Simulate-Latency: inventory=3000, tax=2000` hace lenta cualquier dependencia solo para esa petición.

Ejemplo de 504:

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

## Ajustar los timeouts con datos

El histograma `timeout_pattern.budget.utilization` mide `transcurrido ÷ permitido` por operación:

- p99 cercano a **1.0** → el timeout está cortando trabajo real: súbelo o arregla la dependencia.
- p99 muy por debajo de 1.0 → hay margen: bájalo y falla más rápido.

## Decisiones de diseño

- **Por qué no `[RequestTimeout]` de ASP.NET Core 8**: cubre la petición, pero no reparte el presupuesto
  entre operaciones anidadas ni propaga el deadline hacia otros servicios.
- **`HttpClient.Timeout = Infinite`**: el executor es quien decide el timeout de la llamada; los 100 s por defecto nunca se alcanzarían primero.
- **Los timeouts no se registran como Error**: el middleware los responde directamente con un 504.
  Si se relanzaran, el `ExceptionHandlerMiddleware` de .NET 8 escribiría cada uno como error con su stack trace.
- **`TimeProvider` en todas partes**: los tests unitarios avanzan el reloj en lugar de dormir.
