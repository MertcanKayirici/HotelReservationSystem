# 📁 Folder Structure Contract — Hotel Reservation System

---

## 1. Purpose

Define **exact placement rules** for all code to enforce Clean Architecture.

### Goals

* Prevent wrong layer usage
* Enforce strict separation of concerns
* Standardize code organization
* Make incorrect implementation difficult

---

## 2. Project Structure (FINAL)

```text
HotelReservationSystem/
│
├── src/
│   │
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
│   │   ├── Commands/        (optional)
│   │   ├── Queries/         (optional)
│   │   ├── Orchestration/   (formerly GRC)
│   │   ├── Validators/
│   │   ├── Behaviors/       (optional)
│   │   └── Exceptions/
│   │
│   ├── HotelReservationSystem.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── ValueObjects/
│   │
│   ├── HotelReservationSystem.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   │
│   │   ├── Repositories/
│   │   ├── Kafka/
│   │   ├── Redis/
│   │   ├── Idempotency/
│   │   ├── Outbox/
│   │   ├── Logging/
│   │   └── DependencyInjection/
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

---

## 3. Layer Responsibilities (STRICT)

### 🔹 API Layer

**Contains**

* Controllers
* Request / Response models
* Middleware / Filters

**MUST**

* Handle HTTP only

**FORBIDDEN**

* Business logic
* Database access
* Kafka / Redis usage

---

### 🔹 Application Layer

**Contains**

* Services (business logic)
* Interfaces
* DTOs
* Orchestration (transaction + coordination)
* Validators

**MUST**

* All business rules live here

**FORBIDDEN**

* Direct infrastructure usage

---

### 🔹 Domain Layer

**Contains**

* Entities
* Value Objects
* Enums

**MUST**

* Pure domain logic
* No dependencies

---

### 🔹 Infrastructure Layer

**Contains**

* Database (EF Core)
* Kafka
* Redis
* Idempotency
* Outbox
* Logging

**MUST**

* External system integrations only

**FORBIDDEN**

* Business logic

---

## 4. Dependency Rules (NON-NEGOTIABLE)

### Allowed

* API → Application → Domain
* Infrastructure → Application + Domain

### Forbidden

* API → Infrastructure
* Application → Infrastructure
* Domain → ANY layer

---

## 5. File Placement Rules

| Type          | Location                    |
| ------------- | --------------------------- |
| Controllers   | API/Controllers             |
| DTOs          | Application/DTOs            |
| Services      | Application/Services        |
| Orchestration | Application/Orchestration   |
| Entities      | Domain/Entities             |
| Repositories  | Infrastructure/Repositories |
| Kafka         | Infrastructure/Kafka        |
| Redis         | Infrastructure/Redis        |
| Idempotency   | Infrastructure/Idempotency  |
| Outbox        | Infrastructure/Outbox       |

---

## 6. Ownership Mapping

| Layer          | Owner       |
| -------------- | ----------- |
| API            | Dev1 / Dev2 |
| Application    | Tech Lead   |
| Domain         | Tech Lead   |
| Infrastructure | Tech Lead   |

---

## 7. Common Violations (AUTO REJECT)

* Business logic inside Controller
* DB access outside Repository
* Kafka usage outside Infrastructure
* Logic inside Infrastructure
* Wrong folder placement

---

## 8. Enforcement

Violations:

* Break architecture
* Create bugs
* Reduce maintainability

**Result**

* PR is rejected

---

## 9. Final Principle

> File location defines responsibility
> Wrong placement = wrong behavior = broken system

---

## 10. Final Result

* Clean architecture enforced
* Predictable structure
* Easy onboarding
* Minimal mistakes
