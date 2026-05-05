# Observability

Miniclip Simulator uses **OpenTelemetry** for distributed tracing and metrics, and **Serilog** for structured logging. Both the API and the ReadModels WebJob are fully instrumented and export all signals to an OTLP endpoint (e.g. the .NET Aspire Dashboard, Jaeger, or an OTEL Collector).

---

## Logging — Serilog

Both hosts call `builder.AddStructuredLogging()` from `Miniclip.Core.ServiceDefaults`:

```csharp
// Program.cs (API and WebJob)
builder.AddStructuredLogging();
```

This configures Serilog to:
- Read additional configuration from `appsettings.json` (log levels, sinks, etc.).
- Enrich log events with machine name and thread ID.
- Write structured JSON to the console.
- Optionally write to an OTLP log sink when the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable is set.

The API additionally uses Serilog request logging middleware:
```csharp
app.UseSerilogRequestLogging();
```

---

## Tracing — OpenTelemetry

### Activity source

`Miniclip.Core.OpenTelemetry` defines a shared `ActivitySource` named `Miniclip.Simulator`:

```csharp
public static OpenTelemetryActivity StartActivity(string name, ...)
    => new(ActivitySourceInstance.StartActivity(name, ActivityKind.Server, ...));
```

`TracingMiddleware` (in `Miniclip.Core.Messaging.Pipeline`) starts one span per consumed message, named `"message process {event-type}"`:

```csharp
using var activity = OpenTelemetryActivity.StartActivity(
    $"{OpenTelemetryConstants.Tags.MessageProcess} {envelope.MessageType}");
```

### `ActivityExtensions.NoticeError`

Tags the current span with an error status on domain conflicts:

```csharp
// LoggingBehavior
Activity.Current.NoticeError(failed.Error.Code);
```

### Instrumented sources

| Source | Added by |
|---|---|
| `Miniclip.Simulator` (custom) | `AddSimulator()` |
| ASP.NET Core | `AddAspNetCoreInstrumentation()` (API only) |
| HTTP client | `AddHttpClientInstrumentation()` (API only) |
| KurrentDB client | `AddKurrentDBClientInstrumentation()` (API only) |
| Kafka producer | `AddKafkaProducerInstrumentation<string, string>()` (API only) |
| Kafka consumer | `AddKafkaConsumerInstrumentation<string, string>()` (WebJob only) |
| MySQL (MySqlData) | `AddMySqlData()` |
| MySQL (MySqlConnector) | `AddMySqlConnector()` |

All traces are exported via OTLP (`AddOtlpExporter()`).

---

## Metrics — OpenTelemetry

### Custom meter

`Miniclip.Core.OpenTelemetry.OpenTelemetryMetrics` exposes counters on the `Miniclip.Simulator.Kafka` meter:

| Metric | Type | Description |
|---|---|---|
| `kafka.retry.attempts` | Counter | Total message processing retry attempts |
| `kafka.messages.failed` | Counter | Messages that permanently failed after all retries |

> The `Miniclip.Simulator.Kafka` meter name is defined in `OpenTelemetryConstants.Metrics.SimulatorMetricName` and registered via `AddSimulator()` in both the API and WebJob.

### Instrumented meters

| Meter | Added by |
|---|---|
| `Miniclip.Simulator.Kafka` (custom) | `AddSimulator()` |
| ASP.NET Core | `AddAspNetCoreInstrumentation()` (API only) |
| HTTP client | `AddHttpClientInstrumentation()` (API only) |
| Kafka producer | `AddKafkaProducerInstrumentation<string, string>()` (API only) |
| Kafka consumer | `AddKafkaConsumerInstrumentation<string, string>()` (WebJob only) |

All metrics are exported via OTLP (`AddOtlpExporter()`).

---

## OpenTelemetry Configuration

### API (`Miniclip.Simulator.Api`)

Registered in `OpenTelemetryConfiguration.AddOpenTelemetryDependencies()`:

```csharp
services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter()
        .AddKafkaProducerInstrumentation<string, string>()
        .AddSimulator())
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter()
        .AddKafkaProducerInstrumentation<string, string>()
        .AddMySqlData()
        .AddMySqlConnector()
        .AddKurrentDBClientInstrumentation()
        .AddSimulator());
```

### ReadModels WebJob (`Miniclip.Simulator.ReadModels.WebJob`)

```csharp
services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddOtlpExporter()
        .AddSimulator())
    .WithTracing(t => t
        .AddOtlpExporter()
        .AddMySqlData()
        .AddMySqlConnector()
        .AddSimulator());

// Kafka instrumentation is wired dynamically per registered ConsumerSubscription:
// services.ConfigureOpenTelemetryMeterProvider((sp, m) => ...)
// services.ConfigureOpenTelemetryTracerProvider((sp, t) => ...)
```

---

## Aspire Dashboard

When running locally with `dotnet run` in `Miniclip.Simulator.AppHost`, the .NET Aspire Dashboard is available at `https://localhost:15888`. It shows:
- **Structured logs** from all services (with Serilog fields preserved).
- **Distributed traces** with span details and parent-child relationships.
- **Metrics** dashboards for ASP.NET Core, Kafka, and custom simulator counters.
- **Resource health** for MySQL, KurrentDB, Kafka, API, and WebJob.
