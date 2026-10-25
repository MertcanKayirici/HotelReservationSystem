# Hotel Reservation System — Technology Stack

---

## 1. Purpose

This document defines the approved technology stack and architectural constraints for the backend-only Hotel Reservation System.

### Goals

- Standardize technologies across the team
- Preserve compatibility with the existing solution
- Maintain clear architectural boundaries
- Support reliable reservation processing
- Enable optional infrastructure for local development
- Prevent unapproved technology changes

The current deliverable is a backend API. A browser-facing frontend is outside the scope of Phase 1.

---

## 2. Backend Technology

### Framework

- ASP.NET Core Web API
- Existing target framework: retain the version defined by the solution
- .NET 8 is an option only if an approved migration is required and compatible with the current codebase

### Language

- C#

### Principles

- Strong typing
- Dependency injection
- Asynchronous I/O where appropriate
- Clear separation of concerns
- Consistent API contracts
- Maintainable and testable business logic

Do not upgrade the target framework or rewrite the application architecture without Tech Lead approval.

---

## 3. Architecture

The solution follows the existing layered architecture:

```text
API → Application → Domain
Infrastructure → Application + Domain
```

### Responsibilities

- **API:** HTTP endpoints, middleware, request handling and dependency injection composition
- **Application:** use cases, application services, DTOs and abstractions
- **Domain:** entities, invariants and core business rules
- **Infrastructure:** persistence implementations, repositories and external integrations

### Mandatory rules

- API must not directly access Infrastructure implementations.
- Application must not depend on Infrastructure implementations.
- Domain must remain independent of the other application layers.
- Controllers must not own business workflows.
- Shared contracts must be coordinated before dependent changes are implemented.

---

## 4. Database and Persistence

### Primary database

- Microsoft SQL Server (MSSQL)

### ORM

- Entity Framework Core

### Requirements

- Use EF Core for application persistence.
- Track schema changes through reviewed migrations.
- Keep entity configuration and the model snapshot consistent.
- Use database constraints and indexes where appropriate.
- Use explicit transaction boundaries for operations requiring atomicity.
- Coordinate changes to `AppDbContext` and migrations with the Tech Lead.
- Never commit production credentials or real user data.

Repository and service responsibilities must follow the existing architecture. Business rules must not be hidden inside repositories.

---

## 5. Messaging and Asynchronous Processing

### Message broker

- Apache Kafka

### .NET client

- Confluent.Kafka

### Intended usage

- Reservation-related domain events
- Asynchronous event processing
- Retry and recovery workflows
- Integration with downstream consumers

Kafka is an optional infrastructure dependency for local development.

### Requirements

- Kafka must be configured explicitly.
- The application must not report successful delivery when delivery has not been confirmed.
- Retry and failure behavior must be documented.
- Consumers must handle duplicate delivery safely where required.
- Pending events must not be silently discarded when Kafka is unavailable.

The exact event contracts and processing guarantees are defined by the implementation and must be verified through tests.

---

## 6. Distributed Locking

### Technology

- Redis
- StackExchange.Redis

### Intended usage

- Distributed coordination for operations that require it
- Room-level or reservation-level locking where justified by the concurrency design

### Requirements

- Lock ownership and expiration must be handled safely.
- Lock acquisition failures must not be treated as successful acquisition.
- Lock release must not remove another operation's lock.
- Locking must be combined with appropriate database constraints and transaction handling.
- An in-memory lock must not be presented as an equivalent distributed lock.

Redis is optional for local development. If a required consistency guarantee cannot be provided without Redis, the operation must follow a documented safe policy.

---

## 7. Outbox Pattern

### Storage

- MSSQL
- EF Core-managed Outbox table

### Processing

- ASP.NET Core `BackgroundService` or the existing approved background-processing implementation

### Purpose

- Persist domain events alongside the relevant database transaction
- Prevent event loss between database commits and message publishing
- Support retry and recovery when Kafka is unavailable

### Requirements

- Outbox persistence must follow the transaction boundary of the operation it represents.
- A pending event must not be deleted before the configured delivery policy permits it.
- Retries must account for duplicate delivery.
- Processing failures must be observable.
- Recovery behavior must be tested.

Outbox processing must not be reported as successful merely because a record was created.

---

## 8. API Design and Validation

### API pattern

- ASP.NET Core Web API controllers
- RESTful resource-oriented endpoints
- Swagger / OpenAPI documentation

### Validation

- Data Annotations where appropriate
- Application-level validation for business rules
- Domain invariants for core consistency requirements

### Requirements

- Return appropriate HTTP status codes.
- Use DTOs instead of exposing persistence entities directly.
- Keep controllers thin.
- Return consistent and safe error responses.
- Protect endpoints with explicit authentication and authorization policies.
- Document API contracts and security requirements.

---

## 9. Authentication and Security

### Technologies

- JWT bearer authentication
- Password hashing using an appropriate password-hashing implementation
- Persisted refresh-token support where implemented

### Requirements

- Signing secrets must be stored outside source control.
- Validate token signature, issuer, audience and lifetime as applicable.
- Hash passwords; never store plaintext passwords.
- Apply authorization policies and role checks on protected endpoints.
- Handle refresh-token rotation and revocation securely.
- Do not trust client-supplied roles or user identifiers as authorization evidence.
- Do not expose stack traces, credentials or internal exception details to clients.

ASP.NET Core Identity must not be introduced as a replacement for the existing authentication implementation without an approved design decision.

---

## 10. Configuration Management

### Configuration sources

- `appsettings.json`
- `appsettings.Development.json` where needed
- `appsettings.Production.json` where needed
- Environment variables
- `.env.example` for documented local configuration

### Requirements

- Keep secrets out of committed configuration files.
- Use placeholder values in examples.
- Validate required configuration at startup.
- Document optional and required settings.
- Keep local development configuration reproducible.
- Ensure disabled optional integrations do not appear healthy or operational when they are not.

Configuration names must remain consistent across code, Docker Compose and documentation.

---

## 11. Logging and Observability

### Logging

- Microsoft.Extensions.Logging
- Serilog only if approved and needed

### Requirements

Logs should provide useful operational context, such as:

- Request or correlation ID
- Operation or processing step
- Relevant entity identifier where appropriate
- Outcome and failure category
- Retry information for background processing

User identifiers must be logged only when necessary and appropriate.

Never log passwords, access tokens, refresh tokens, signing secrets or sensitive personal data.

Health checks must accurately represent the status of required and optional dependencies.

---

## 12. Testing

### Unit testing

- xUnit, if compatible with the existing test solution

### API integration testing

- Microsoft.AspNetCore.Mvc.Testing, where appropriate

### Load testing

- k6, as a later-stage or explicitly approved testing tool

### Required coverage

- Authentication and authorization
- Room management and validation
- Reservation availability and date-overlap rules
- Idempotency and repeated requests
- Concurrent reservation attempts
- Database transaction and rollback behavior
- Redis enabled, disabled and failure scenarios
- Kafka unavailable and recovery scenarios
- Outbox retry and duplicate-delivery behavior
- API error responses and configuration failures

Tests must verify actual system guarantees rather than assume that optional infrastructure is available.

---

## 13. Containerization

### Technologies

- Docker
- Docker Compose

### Potential services

- ASP.NET Core API
- MSSQL
- Redis
- Kafka and its required supporting services

### Requirements

- Document the minimum local development setup.
- Make optional services configurable.
- Do not require every developer to install Kafka and Redis just to work on independent API features.
- Keep secrets out of committed Compose configuration.
- Provide health checks and startup dependencies where appropriate.
- Document database initialization and migration procedures.

The supported configuration must be tested rather than inferred from the presence of a Compose file.

---

## 14. Code Quality

### Tools

- .NET built-in analyzers
- `dotnet format`
- StyleCop only if explicitly adopted by the team

### Requirements

- Consistent naming and formatting
- Clear abstractions
- No unnecessary dependencies
- No duplicate business logic
- Reviewable and focused changes
- No unrelated refactoring in feature pull requests

New tooling must have a clear benefit and be approved before becoming mandatory.

---

## 15. Version Control and Collaboration

### Tools

- Git
- GitHub

### Branch strategy

- `main`: protected integration and release baseline
- `feature/*`: feature development
- `fix/*`: bug fixes
- `chore/*`: maintenance and documentation

### Requirements

- No direct pushes to `main`.
- Every code change must use a pull request.
- Shared files require coordination.
- Existing tags and `main` history must not be rewritten.
- Pull requests must include scope, impact and testing evidence.

---

## 16. Development Tools

- Visual Studio or Visual Studio Code
- .NET SDK compatible with the solution
- NuGet
- SQL Server tooling appropriate to the environment
- Git and GitHub

All developers must be able to follow the documented setup without requiring undocumented local configuration.

---

## 17. Out of Scope for Phase 1

The following are not part of the current backend deliverable:

- HTML and CSS application pages
- Bootstrap layouts
- Razor Views
- MVC page controllers
- Browser-based reservation forms
- SignalR-based UI updates
- Frontend frameworks

Swagger remains in scope for API documentation and testing.

Additional monitoring, dashboards or background-processing tools may be considered later but are not mandatory without approval.

---

## 18. Technology Summary

| Layer | Approved technology |
|---|---|
| Backend | ASP.NET Core Web API |
| Language | C# |
| Database | Microsoft SQL Server |
| ORM | Entity Framework Core |
| Messaging | Apache Kafka / Confluent.Kafka |
| Distributed coordination | Redis / StackExchange.Redis |
| Reliable event persistence | MSSQL Outbox |
| Background processing | ASP.NET Core BackgroundService or existing approved implementation |
| Authentication | JWT bearer authentication |
| API documentation | Swagger / OpenAPI |
| Logging | Microsoft.Extensions.Logging |
| Testing | Existing test framework; xUnit where approved |
| HTTP integration testing | Microsoft.AspNetCore.Mvc.Testing where appropriate |
| Load testing | k6 when required |
| Containerization | Docker / Docker Compose |
| Version control | Git / GitHub |

---

## 19. Critical Rules

### Mandatory

- Preserve the approved layered architecture.
- Use MSSQL and EF Core for application persistence.
- Protect reservation consistency and idempotency guarantees.
- Use Outbox processing for the approved reliable event-delivery workflow.
- Treat Kafka and Redis failures explicitly.
- Keep secrets outside source control.
- Test relevant failure paths.
- Obtain Tech Lead approval for changes to shared architecture and core behavior.

### Forbidden

- Direct database access from controllers
- Business workflows inside controllers
- Uncoordinated schema or migration changes
- Silent loss of pending events
- Unsafe fallback locking
- Reporting unconfirmed external operations as successful
- Skipping required idempotency
- Mixing frontend work into backend deliverables
- Unapproved replacement of the existing framework or authentication design

---

## 20. Technology Change Policy

The technology stack is locked for Phase 1.

Any change must:

1. Be proposed by the responsible developer.
2. Explain the problem being solved.
3. Identify compatibility and migration risks.
4. Demonstrate a measurable benefit.
5. Be approved by the Tech Lead before implementation.

No technology change may silently break API contracts, data consistency or the existing integration baseline.

---

## 21. Final Principle

> One backend system, one agreed technology stack, explicit contracts and controlled integration.

The objective is a maintainable, testable and reliable backend. Production readiness must be demonstrated through implementation, tests, configuration verification and documented operational behavior.
