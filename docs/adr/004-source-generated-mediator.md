# ADR-004 - Source-generated Mediator instead of MediatR

Status: Accepted | Date: 2026-02

## Context

MediatR uses reflection at runtime to resolve handlers. For a .NET 10 project targeting minimal overhead, a source-generated alternative is preferred.

## Decision

Use the Mediator NuGet package (Mediator.Abstractions + Mediator.SourceGenerator). All command/query handlers and notification handlers are discovered and wired at compile time.

## Consequences

- IRequest<TResponse>, IRequestHandler<TRequest, TResponse>, and INotificationHandler<TNotification> come from Mediator.Abstractions, not MediatR.
- IMediator is injected via DI; usage in controllers is identical to MediatR.
- Adding a new handler requires no registration boilerplate; the source generator handles it.
- Do not mix MediatR types with Mediator types.
