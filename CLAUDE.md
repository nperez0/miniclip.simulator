# Miniclip Simulator — Claude Instructions

For full project context, architecture, domain model, and coding conventions, see:

**[AI.md](./AI.md)**

---

## Quick Reference

- **Stack:** .NET 10, ASP.NET Core, EventStoreDB, Kafka, MySQL (read), .NET Aspire
- **Pattern:** Clean Architecture + CQRS + DDD + Event Sourcing + event-driven projections
- **Mediator:** source-generated `Mediator` package — not MediatR
- **Error handling:** `Result<T>` pattern — never throw for business rule violations
- **Write store:** EventStoreDB (aggregates as event streams)
- **Read store:** MySQL, populated by `ProjectionsConsumerService<TEvent>` (Kafka consumers)
- **Solution root:** `src/`
- **Entry point:** `src/Miniclip.Simulator.AppHost`
