# Folder Structure Contract — Hotel Reservation System

## 1. Purpose

This document defines the file placement rules and architectural boundaries for the Hotel Reservation System.

### Goals

- Prevent incorrect layer usage.
- Enforce separation of concerns.
- Standardize code organization.
- Make architectural violations easier to detect during code review.
- Keep project dependencies aligned with Clean Architecture principles.

---

## 2. Project Structure (Final)

```text
HotelReservationSystem/
│
├── src/
│   ├── HotelReservationSystem.API/
│   │   ├── Controllers/
│   │   ├── Models/
│   │   │   ├── Requests/
│   │   │   └── Responses/
│   │   ├── Middlewares/
│   │   ├── Filters/
│   │   ├── Extensions/
│   │   ├── Settings/
│   │   │   └── JwtSettings.cs
│   │   ├── appsettings.json
│   │   └── Program.cs
│   │
│   ├── HotelReservationSystem.Application/
│   │   ├── Services/
│   │   ├── Interfaces/
│   │   │   └── Repositories/
│   │   ├── DTOs/
│   │   ├── Commands/          (optional)
│   │   ├── Queries/           (optional)
│   │   ├── Orchestration/
│   │   ├── Validators/
│   │   ├── Behaviors/         (optional)
│   │   └── Exceptions/
│   │
│   ├── HotelReservationSystem.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── ValueObjects/
│   │   └── Services/          (when required)
│   │
│   └── HotelReservationSystem.Infrastructure/
│       ├── Persistence/
│       │   ├── Configurations/
│       │   └── Migrations/
│       ├── Repositories/
│       ├── Kafka/
│       ├── Redis/
│       ├── Idempotency/
│       ├── Outbox/
│       ├── Logging/
│       └── DependencyInjection/
│
├── tests/
│   ├── Unit/
│   ├── Integration/
│   └── Load/
│
├── docs/
│   ├── README.md
│   ├── architecture.md
│   ├── api-contract.md
│   ├── team-rules.md
│   ├── tech-stack.md
│   └── decisions/
│
├── scripts/
│   ├── seed.sql
│   └── setup-db.sql
│
├── docker-compose.yml
├── .env.example
├── .gitignore
├── HotelReservationSystem.sln
└── README.md
```

> This structure defines the intended organization. Optional folders should be created only when the corresponding implementation requires them. The actual repository structure and project references must be checked before enforcing these rules.

---

## 3. Layer Responsibilities (Strict)

### 3.1 API Layer

**Location:** `HotelReservationSystem.API`

**Contains:**
- HTTP controllers and endpoint definitions.
- HTTP request and response models.
- Authentication and authorization configuration.
- Middleware, filters, and API-specific extensions.
- API configuration and application startup.

**Must:**
- Handle HTTP concerns.
- Authenticate and authorize requests.
- Validate request models and invoke application use cases.
- Map application results to the API response contract.
- Return appropriate HTTP status codes.

**Must not:**
- Implement core reservation business rules.
- Access the database directly.
- Use Kafka or Redis clients directly.
- Bypass Application services or approved workflows.

### 3.2 Application Layer

**Location:** `HotelReservationSystem.Application`

**Contains:**
- Application services and use cases.
- Interfaces and abstractions required by application workflows.
- DTOs, commands, and queries.
- Application orchestration.
- Application-level validators and exceptions.
- Behaviors when the project uses a behavior pipeline.

**Must:**
- Coordinate application use cases.
- Enforce application workflow and sequencing rules.
- Invoke Domain behavior.
- Depend on abstractions for persistence and external capabilities.
- Define application-facing contracts for Infrastructure implementations.

**Must not:**
- Depend on Infrastructure implementations.
- Directly instantiate or use Infrastructure-specific Kafka, Redis, or database clients.
- Duplicate Domain invariants unnecessarily.
- Bypass required authorization, idempotency, or reservation-processing workflows.

### 3.3 Domain Layer

**Location:** `HotelReservationSystem.Domain`

**Contains:**
- Entities.
- Value objects.
- Domain enums.
- Domain services and business rules when required.

**Must:**
- Represent core business concepts and invariants.
- Enforce domain rules independently of HTTP requests and external systems.
- Remain independent of the API, Application, and Infrastructure layers.

**Must not:**
- Depend on API, Application, or Infrastructure.
- Perform HTTP, database, Kafka, or Redis operations.
- Depend on infrastructure frameworks or external-system clients.

### 3.4 Infrastructure Layer

**Location:** `HotelReservationSystem.Infrastructure`

**Contains:**
- EF Core persistence and repository implementations.
- Kafka producers, consumers, and related integration code.
- Redis integrations.
- Idempotency persistence and supporting mechanisms.
- Outbox persistence and background processing.
- Infrastructure-specific logging and dependency-injection registrations.

**Must:**
- Implement interfaces defined by Application where appropriate.
- Handle database access and external-system integration.
- Respect transaction boundaries and persistence consistency.
- Support the workflows defined by Application.

**Must not:**
- Become the owner of core reservation business rules.
- Bypass Domain invariants or required Application workflows.
- Allow external-system failures to silently violate persistence consistency.

---

## 4. Dependency Rules

### 4.1 Allowed Compile-Time Dependencies

| Source Layer | Allowed Dependency |
|---|---|
| API | Application |
| Application | Domain |
| Infrastructure | Application |
| Infrastructure | Domain |
| Domain | No dependency on other solution layers |

The API may reference Infrastructure **only in the composition root**, such as `Program.cs`, to register concrete implementations for dependency injection, if this is the architecture selected by the team.

This exception must not allow controllers or other API components to use Infrastructure implementations directly.

### 4.2 Forbidden Dependencies

- Domain → Application
- Domain → Infrastructure
- Domain → API
- Application → Infrastructure implementations
- Domain → external-system clients
- Controllers → Infrastructure repositories or Kafka/Redis clients directly

Dependencies must point toward the core business model. Infrastructure implements abstractions rather than becoming a dependency of Application.

---

## 5. File Placement Rules

| Component | Required Location |
|---|---|
| Controllers | `API/Controllers` |
| HTTP request/response models | `API/Models/Requests` and `API/Models/Responses` |
| DTOs | `Application/DTOs` |
| Application services | `Application/Services` |
| Application interfaces | `Application/Interfaces` |
| Repository interfaces | `Application/Interfaces/Repositories` |
| Orchestration | `Application/Orchestration` |
| Entities | `Domain/Entities` |
| Value objects | `Domain/ValueObjects` |
| Domain services | `Domain/Services` |
| Repository implementations | `Infrastructure/Repositories` |
| EF Core persistence | `Infrastructure/Persistence` |
| Kafka integration | `Infrastructure/Kafka` |
| Redis integration | `Infrastructure/Redis` |
| Idempotency infrastructure | `Infrastructure/Idempotency` |
| Outbox infrastructure | `Infrastructure/Outbox` |
| Infrastructure-specific logging | `Infrastructure/Logging` |

A file's responsibility determines its location. If a component has mixed responsibilities, separate those responsibilities rather than placing the entire component in an inappropriate layer.

---

## 6. Ownership Mapping

| Layer | Primary Owner |
|---|---|
| API | Dev1 / Dev2 |
| Application | Tech Lead |
| Domain | Tech Lead |
| Infrastructure | Tech Lead |

Ownership defines primary responsibility, not exclusive access. All contributors must follow the same architectural rules, and cross-layer changes require appropriate review.

---

## 7. Common Violations (PR Rejection Criteria)

The following violations must be corrected before a pull request can be approved:

- Business rules implemented directly in controllers.
- Direct database access from controllers.
- Application code depending on Infrastructure implementations.
- Domain code depending on external frameworks or services.
- Kafka or Redis clients used directly from API controllers.
- Reservation persistence bypassing the approved processing workflow.
- Files placed in folders that contradict their architectural responsibility.
- Infrastructure independently deciding core reservation business rules.
- Changes that introduce forbidden project references.
- Changes that bypass required transaction, idempotency, or consistency mechanisms.

Reviewers must verify the actual code, project references, and runtime behavior. Folder names alone do not prove that Clean Architecture is being followed.

---

## 8. Enforcement

Architectural violations must be identified during development and pull request review.

Required actions:

1. Inspect changed files and their responsibilities.
2. Check project references for forbidden dependencies.
3. Verify that business rules remain in the appropriate layer.
4. Verify that persistence and external integrations follow approved workflows.
5. Request changes when a violation is found.
6. Approve only after required corrections are completed.

**Result:** Pull requests that violate these rules are not approved until the violations are resolved.

---

## 9. Final Principle

> File location should reflect responsibility. Correct folder placement supports Clean Architecture, but actual dependency direction and implementation behavior determine whether the architecture is respected.

---

## 10. Expected Outcome

- Clear separation of responsibilities.
- Consistent project organization.
- Easier onboarding and code review.
- Reduced risk of architectural drift.
- More predictable maintenance and testing.
- Better enforcement of Clean Architecture through project references and review rules.
```
