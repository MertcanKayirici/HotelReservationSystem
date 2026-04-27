# 🏨 Hotel Reservation System — Project Folder Structure (ASP.NET Core MVC)

---

## 1. Purpose

This document defines the **updated project folder structure** for:

* ASP.NET Core MVC
* MSSQL + Entity Framework Core
* Kafka + Redis + Outbox architecture

### Goals

* Clear separation of concerns
* Scalable architecture
* Developer-friendly structure
* Prevent incorrect implementation

---

## 2. Solution Structure (ROOT)

```id="k2m1xp"
HotelReservationSystem/
│
├── src/
│   ├── HotelReservationSystem.API/
│   ├── HotelReservationSystem.Application/
│   ├── HotelReservationSystem.Domain/
│   ├── HotelReservationSystem.Infrastructure/
│
├── tests/
│   ├── HotelReservationSystem.Tests/
│
├── db/
│   └── migrations/
│
├── scripts/
│
├── docs/
│
├── docker-compose.yml
├── HotelReservationSystem.sln
├── README.md
```

---

## 3. Layered Architecture Overview

| Layer          | Responsibility           |
| -------------- | ------------------------ |
| API            | Controllers (HTTP layer) |
| Application    | Business logic           |
| Domain         | Entities & core rules    |
| Infrastructure | DB, Kafka, Redis         |

---

## 4. API Layer (Presentation)

```id="b9p8qa"
HotelReservationSystem.API/
│
├── Controllers/
│   ├── ReservationController.cs
│   ├── AuthController.cs
│   └── RoomController.cs
│
├── Models/                 # Request/Response models (DTO)
│   ├── Requests/
│   └── Responses/
│
├── Middleware/
│   ├── ExceptionMiddleware.cs
│   └── RequestLoggingMiddleware.cs
│
├── Filters/
│
├── Extensions/
│   └── ServiceCollectionExtensions.cs
│
├── Program.cs
└── appsettings.json
```

### Rules

**Controller MUST**

* Receive request
* Validate model
* Call Application layer
* Return response

**Controller MUST NOT**

* Contain business logic
* Access DB directly

---

## 5. Application Layer (CORE LOGIC)

```id="y7d2xm"
HotelReservationSystem.Application/
│
├── Services/
│   ├── ReservationService.cs
│   ├── AuthService.cs
│   └── RoomService.cs
│
├── Interfaces/
│   ├── IReservationService.cs
│   ├── IReservationRepository.cs
│   └── IUnitOfWork.cs
│
├── DTOs/
│   ├── Reservation/
│   ├── Auth/
│   └── Room/
│
├── Validators/
│
└── Behaviors/
```

### Responsibilities

* Business logic
* Validation rules
* Idempotency checks
* Kafka publish calls

---

## 6. Domain Layer (CORE MODEL)

```id="y1l0qp"
HotelReservationSystem.Domain/
│
├── Entities/
│   ├── Reservation.cs
│   ├── Room.cs
│   └── User.cs
│
├── Enums/
├── ValueObjects/
├── Exceptions/
└── Rules/
```

### Responsibilities

* Pure business entities
* No external dependencies
* Core domain rules

---

## 7. Infrastructure Layer (EXTERNAL SYSTEMS)

```id="t0m4dx"
HotelReservationSystem.Infrastructure/
│
├── Persistence/
│   ├── DbContext/
│   │   └── AppDbContext.cs
│   │
│   ├── Configurations/
│   │   └── ReservationConfig.cs
│   │
│   ├── Repositories/
│   │   └── ReservationRepository.cs
│   │
│   └── Migrations/
│
├── Kafka/
│   ├── KafkaProducer.cs
│   ├── KafkaConsumer.cs
│   └── Topics.cs
│
├── Redis/
│   └── RedisLockService.cs
│
├── Idempotency/
│   └── IdempotencyService.cs
│
├── Outbox/
│   ├── OutboxEntity.cs
│   └── OutboxWorker.cs
│
└── DependencyInjection.cs
```

### Rules

* No business logic
* Only external integrations
* Implements Application interfaces

---

## 8. GRC (Global Reservation Coordinator)

```id="g4k8zn"
HotelReservationSystem.Application/
│
└── GRC/
    └── ReservationCoordinator.cs
```

### Responsibilities

* Lock handling (Redis)
* Transaction control (EF Core + MSSQL)
* Reservation insert
* Outbox write

### Rule

👉 ONLY this component can write reservation data

---

## 9. Tests

```id="c8r1ka"
tests/
└── HotelReservationSystem.Tests/
    │
    ├── Integration/
    │   └── ReservationTests.cs
    │
    ├── Load/
    │   └── k6-tests.js
    │
    └── Unit/
```

---

## 10. Database Layer

```id="e1k9tr"
db/
└── migrations/
```

### Rule

* All schema changes via EF Core migrations
* No manual DB changes

---

## 11. Scripts

```id="p4x2wm"
scripts/
│
├── seed.cs
└── migrate.sh
```

---

## 12. Docs

```id="f2m7lp"
docs/
│
├── architecture.md
├── execution-guide.md
├── production-kit.md
├── api-contract.md
├── team-rules.md
└── decisions/
```

---

## 13. Docker

```id="q9v6ns"
docker-compose.yml
```

Services:

* MSSQL
* Redis
* Kafka

---

## 14. Global Rules (CRITICAL)

### MUST

* Use layered architecture
* Keep controllers thin
* Business logic only in Application
* Use GRC for reservation writes
* Use Infrastructure for integrations

---

### FORBIDDEN

* DB access from Controller
* Business logic in Infrastructure
* Skipping transaction control
* Direct Kafka usage outside service

---

## 15. Final Principle

> Architecture enforces correctness.

Correct structure:

* prevents bugs
* ensures consistency
* enables scaling

---

## 16. Final Result

* Clean layered system
* Clear ownership
* Zero ambiguity
* Production-ready architecture

---

## 17. Final Note

This structure is **mandatory**.

Violations:

* break system integrity
* will be rejected in code review
