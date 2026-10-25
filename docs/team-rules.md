# Team Rules — Hotel Reservation System

---

## 1. Purpose

This document defines the mandatory team rules for the Hotel Reservation System to ensure:

- Consistent development
- Clear module ownership
- Predictable system behavior
- Safe integration
- Maintainable architecture
- Reliable data consistency
- Controlled delivery

These rules apply to all developers and all pull requests.

---

## 2. Project Scope

The project is a backend-only hotel reservation system built with ASP.NET Core Web API.

**In scope:**
- RESTful API development
- Authentication and authorization
- Room management
- Reservation management
- Database persistence and migrations
- Idempotency and concurrency control
- Kafka and Redis integrations
- Outbox processing
- Automated testing and Swagger documentation

**Out of scope:**
- Razor Views and MVC user interfaces
- Bootstrap layouts and frontend styling
- Browser-based reservation forms
- Browser-based room management pages

Swagger is used for API documentation and testing. It is not considered a frontend application.

---

## 3. Roles and Responsibilities

| Role | Responsibility |
|---|---|
| Tech Lead (Dev1 in the initial phase) | Core system, architecture, integration, shared infrastructure and final verification |
| Dev1 — Auth & Security | Authentication, authorization and security |
| Dev2 — Room Management API | Room entity, business rules and room endpoints |

The Tech Lead coordinates integration and makes final decisions on shared architecture and core-system changes.

### Dev1 — Auth & Security

Responsible for:
- Registration and login
- Password hashing and verification
- JWT generation and validation
- Refresh-token persistence, rotation and revocation
- Role-based authorization
- Authentication request validation
- Authentication and authorization tests
- Secure authentication error handling

### Dev2 — Room Management API

Responsible for:
- Room entity and persistence configuration
- Room DTOs and validation
- Room creation, listing, detail and update endpoints
- Room activation and deactivation
- Room service and business rules
- Room API tests
- Swagger documentation for room endpoints

Razor Views, MVC page controllers, Bootstrap and frontend assets are not part of Dev2's task.

### Tech Lead — Core & Integration

Responsible for:
- Reservation orchestration and business rules
- Availability and date-overlap validation
- Idempotency
- Concurrency safety
- Redis integration and locking behavior
- Kafka integration
- Outbox and background processing
- Shared database and architecture decisions
- Integration reviews and final testing
- Release preparation and documentation

---

## 4. Ownership Rules — Strict

### Mandatory

- Each developer owns their assigned module.
- Developers must work on task-specific branches.
- Changes outside a developer's module require prior coordination.
- Shared contracts and shared files must be reviewed before modification.
- The Tech Lead coordinates changes to the reservation core, Kafka, Redis locking, Outbox processing, idempotency and shared database schema.
- Existing working behavior must be preserved unless a change is explicitly approved.

### Forbidden

- Editing another developer's module without approval
- Making uncoordinated cross-module changes
- Rewriting the architecture without approval
- Replacing existing implementations without reviewing their behavior
- Including unrelated changes in a pull request
- Assuming that a larger diff means a more complete implementation

---

## 5. Architecture Rules — Non-Negotiable

The intended dependency direction is:

```text
API → Application → Domain
Infrastructure → Application + Domain
```

### Forbidden dependencies

- API must not directly access Infrastructure implementations.
- Application must not depend on Infrastructure implementations.
- Domain must not reference API, Application or Infrastructure.
- Business logic must not be duplicated across layers.

### Layer responsibilities

**API**
- Receive HTTP requests
- Validate request structure
- Invoke application services
- Return appropriate HTTP responses

**Application**
- Coordinate use cases
- Enforce application-level business rules
- Define service and repository interfaces
- Coordinate operations through abstractions

**Domain**
- Represent core entities and domain rules
- Protect domain invariants
- Remain independent of infrastructure concerns

**Infrastructure**
- Implement persistence and repository interfaces
- Integrate with databases and external systems
- Implement technical concerns such as Redis, Kafka and security services
- Avoid owning application business decisions

---

## 6. Controller, Service and Repository Rules

### Controllers

**Must:**
- Receive requests
- Apply appropriate request validation
- Call the relevant application service
- Return consistent HTTP responses

**Must not:**
- Contain business workflows
- Access the database directly
- Call Kafka or Redis directly
- Implement reservation locking or transaction orchestration

Simple conditional checks for HTTP handling are allowed; business decisions belong in the appropriate application or domain layer.

### Services

**Must:**
- Implement or coordinate application use cases
- Enforce applicable business rules
- Coordinate validation and persistence
- Handle failure paths explicitly
- Use transactions and idempotency where required by the use case

### Repositories

**Must:**
- Implement persistence operations
- Translate application requests into appropriate database operations
- Respect transaction boundaries and persistence constraints

**Must not:**
- Own application workflows
- Make unrelated business decisions
- Replace service-level validation with hidden business behavior

### Infrastructure

**Must:**
- Encapsulate external-system implementations
- Implement defined interfaces
- Handle technical failures and integration concerns
- Keep configuration and secrets out of source code

---

## 7. Reservation Core — Critical

The reservation core is owned and coordinated by the Tech Lead.

It must preserve:
- Reservation consistency
- Availability validation
- Date-overlap rules
- Safe handling of concurrent requests
- Correct transaction boundaries
- Defined failure and rollback behavior

**Forbidden:**
- Bypassing required concurrency protection
- Partially committing a reservation workflow
- Reporting success before the required persistence operation succeeds
- Silently weakening consistency guarantees when Redis is unavailable

The exact locking and transaction strategy must match the guarantees supported by the selected database and infrastructure.

---

## 8. Idempotency Rules — Critical

Idempotency must be applied to operations where retries could create duplicate effects, especially reservation creation and other explicitly designated critical write operations.

### Mandatory

- Define which endpoints require idempotency.
- Scope keys to the appropriate caller or operation.
- Persist or otherwise reliably coordinate idempotency state.
- Repeated requests with the same key and equivalent payload must follow the documented replay behavior.
- Reusing the same key with a different payload must return a defined conflict or validation error.
- Handle concurrent requests using the same key safely.
- Do not report success before the operation reaches its defined success state.

Not every write operation automatically requires an idempotency key. The requirement must be based on the operation's retry and duplicate-effect risks.

---

## 9. Kafka and Redis Rules

Kafka and Redis are optional infrastructure dependencies for local development, subject to the documented application configuration.

### Redis

- Redis-dependent features must have explicit enabled and disabled behavior.
- A local in-memory lock must not be presented as a distributed lock.
- Reservation consistency must not rely on an unsafe fallback.
- If a required guarantee cannot be provided without Redis, the operation must follow a documented safe policy.

### Kafka and Outbox

- Kafka delivery must not be reported as successful when it has not been confirmed.
- Pending Outbox records must not be discarded merely because Kafka is unavailable.
- Retry and recovery behavior must be documented.
- Consumers must handle duplicate delivery safely where required.
- The application must document which features remain available when Kafka is disabled.

---

## 10. Shared Files and Database Schema

The following files require coordination:

- `src/HotelReservationSystem.API/Program.cs`
- `src/HotelReservationSystem.API/appsettings.json`
- `src/HotelReservationSystem.Infrastructure/Data/AppDbContext.cs`
- EF Core migrations
- `AppDbContextModelSnapshot.cs`
- Shared project files and dependency registrations

### Rules

- Explain shared-file changes in the pull request.
- Do not overwrite another developer's changes.
- Coordinate entity and schema changes before generating migrations.
- Review migration code and test it against a development database.
- Do not independently delete or rewrite shared migrations.
- Keep the model snapshot consistent with the resulting schema.

The Tech Lead coordinates conflicting migration changes and final database integration.

---

## 11. Pull Request Rules — Strict

Every code change must be submitted through a pull request.

Each pull request must include:
- Description
- Scope
- Reason for the change
- Files or modules affected
- API or database impact
- Configuration changes
- Tests performed
- Known limitations

### Review requirements

| Change type | Required review |
|---|---|
| Documentation or isolated non-functional change | At least 1 reviewer |
| Application service or API behavior | At least 1 reviewer |
| Authentication, authorization or security | At least 1 reviewer with Tech Lead review for shared security contracts |
| Database schema or migration | Tech Lead approval |
| Reservation core, idempotency, Redis locking, Kafka or Outbox | Tech Lead approval |
| Shared architecture or API contract change | Tech Lead approval |

For this three-person project, the reviewer count must remain practical. The Tech Lead may require additional review for high-risk changes.

### Forbidden

- Direct pushes to `main`
- Self-approval of a pull request
- Unreviewed merges
- Force-pushing shared branches without coordination
- Merging with unresolved conflicts or failing required checks

---

## 12. Pull Request Rejection Criteria

A pull request must be returned for correction when it:
- Violates the dependency rules
- Adds business workflows to controllers
- Accesses persistence outside the approved abstraction
- Omits required validation
- Breaks idempotency or consistency guarantees
- Reports Kafka or Redis operations as successful when they failed
- Introduces an undocumented API contract change
- Contains secrets or credentials
- Includes unrelated frontend work
- Fails required builds or tests

Missing tests or documentation must be addressed according to the risk and scope of the change.

---

## 13. Branch Strategy

- `main` — protected integration and release baseline
- `feature/*` — feature development
- `fix/*` — bug fixes
- `chore/*` — maintenance and documentation

The team must not create or use an additional `dev` integration branch unless the Tech Lead explicitly approves that workflow.

Existing branches and release tags must not be deleted or rewritten merely to adopt this strategy.

---

## 14. Definition of Done

A task is complete only when:
- The implementation matches the agreed scope.
- Applicable validation is implemented.
- Required idempotency and consistency guarantees are preserved.
- Relevant logging is included without exposing secrets.
- Error and failure paths are handled.
- Relevant automated tests pass.
- The solution builds successfully.
- Configuration and documentation are updated.
- The pull request is approved and integrated.

A successful build alone does not prove a feature is complete.

---

## 15. Daily Workflow

Each developer must communicate:
- Work completed
- Current work plan
- Blockers or risks
- Shared files that may require coordination
- Changes to API contracts or database schema

Communication should focus on actionable progress rather than unnecessary reporting overhead.

---

## 16. Failure Ownership

| Issue type | Primary owner |
|---|---|
| Reservation workflow or consistency bug | Tech Lead |
| Authentication or authorization bug | Dev1 |
| Room management API bug | Dev2 |
| Shared database or migration conflict | Tech Lead coordinates |
| Cross-module integration bug | Relevant developer(s), coordinated by Tech Lead |
| Deployment or infrastructure configuration bug | Tech Lead coordinates with the responsible developer |

Ownership identifies the first responder; it does not prevent collaboration on a fix.

---

## 17. Enforcement

Changes that violate these rules may be rejected or returned for correction.

Critical security, data integrity, architecture and configuration issues must be resolved before integration.

The goal is not to maximize restrictions; it is to make responsibilities clear and changes safe to review.

---

## 18. Global Principle

> No one owns everything. Everyone owns a defined responsibility.

## 19. Final Principle

> Reliable systems require clear contracts, safe failure behavior and disciplined coordination — not just working code.
