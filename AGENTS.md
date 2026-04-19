# Miniclip Simulator — Agent Instructions

For full project context, architecture, domain model, and coding conventions, see:

**[AI.md](./AI.md)**

---

## Quick Reference

- **Stack:** .NET 10, ASP.NET Core, KurrentDB (formerly EventStoreDB), Kafka, MySQL (read), .NET Aspire
- **Pattern:** Clean Architecture + CQRS + DDD + Event Sourcing + event-driven projections
- **Mediator:** source-generated `Mediator` package — not MediatR
- **Error handling:** `Result<T>` pattern — never throw for business rule violations
- **Write store:** KurrentDB (aggregates as event streams; client: `KurrentDB.Client`)
- **Read store:** MySQL, populated by `KafkaConsumerHost` + `ProjectionMessageHandler<TEvent>` (Kafka consumers in the ReadModels WebJob)
- **Observability:** OpenTelemetry (traces + metrics via OTLP) + Serilog structured logging
- **Solution root:** `src/`
- **Entry points:** `src/Miniclip.Simulator.AppHost` (Aspire), `src/Miniclip.Simulator.Api` (API), `src/Miniclip.Simulator.ReadModels.WebJob` (projections)
