# Miniclip Simulator — Agent Instructions

For full project context, architecture, domain model, and coding conventions, see:

**[AI.md](./AI.md)**

---

## Quick Reference

- **Stack:** .NET 10, ASP.NET Core, EF Core, MySQL, .NET Aspire
- **Pattern:** Clean Architecture + CQRS + DDD + event-driven projections
- **Mediator:** source-generated `Mediator` package — not MediatR
- **Error handling:** `Result<T>` pattern — never throw for business rule violations
- **Solution root:** `src/`
- **Entry point:** `src/Miniclip.Simulator.AppHost`
