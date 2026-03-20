# ADR-001 - Result Pattern

**Status:** Accepted
**Date:** 2026-02

## Context

Business rule violations are expected outcomes, not exceptional conditions.

## Decision

All domain and application operations return Result or Result<T> from Miniclip.Core. ExceptionBase subclasses are only carried inside a Result.Failure, never thrown for business logic.

## Consequences

- Every handler must check result.IsFailure before accessing result.Value.
- The API layer uses ResultExtensions.ToActionResult() to translate failure types into HTTP status codes.
- Unit tests assert on Result.IsSuccess / Result.IsFailure without catching exceptions.
