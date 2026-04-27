# 📁 Folder Structure Contract — Hotel Reservation System

---

## 1. Purpose

Define exactly where each piece of code MUST be placed.

### Goals

- Prevent wrong layer usage
- Enforce clean architecture
- Standardize code organization
- Make incorrect implementation difficult

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
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── GRC/
│   │   ├── Validators/
│   │   ├── Behaviors/
│   │   └── Exceptions/
│   │
│   ├── HotelReservationSystem.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── ValueObjects/
│   │
│   ├── HotelReservationSystem.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Db/
│   │   │   ├── Configurations/
│   │   │   └── Migrations/
│   │   │
│   │   ├── Repositories/
│   │   ├── Kafka/
│   │   ├── Redis/
│   │   ├── Idempotency/
│   │   ├── Outbox/
│   │   └── Logging/
│
├── tests/
│   ├── Unit/
│   ├── Integration/
│   └── Load/
│
├── docs/
│   ├── README.md
│   ├── architecture.md
│   ├── execution-guide.md
│   ├── api-contract.md
│   ├── team-rules.md
│   ├── production-kit.md
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

**Contains:**
- Controllers  
- Request/Response models  
- Middleware / Filters  

**MUST**
- Only handle HTTP  

**FORBIDDEN**
- Business logic  
- DB access  
- Kafka / Redis calls  

---

### 🔹 Application Layer

**Contains:**
- Services (business logic)  
- Interfaces  
- DTOs  
- GRC (transaction + lock orchestration)  
- Validators  

**MUST**
- All business rules here  

---

### 🔹 Domain Layer

**Contains:**
- Entities  
- ValueObjects  
- Enums  

**MUST**
- Pure domain models  
- No dependencies  

---

### 🔹 Infrastructure Layer

**Contains:**
- DB access (EF Core)  
- Kafka  
- Redis  
- Idempotency  
- Outbox  
- Logging  

**MUST**
- External systems only  

**FORBIDDEN**
- Business logic  

---

## 4. Dependency Rules (NON-NEGOTIABLE)

- API → Application → Domain  
- Infrastructure → Application + Domain  

### FORBIDDEN
- API → Infrastructure  
- Application → Infrastructure  
- Domain → ANY layer  

---

## 5. File Placement Rules

**MUST**
- Controllers → `/API/Controllers`  
- Services → `/Application/Services`  
- Entities → `/Domain/Entities`  
- Repositories → `/Infrastructure/Repositories`  
- Kafka → `/Infrastructure/Kafka`  
- Redis → `/Infrastructure/Redis`  
- Idempotency → `/Infrastructure/Idempotency`  
- Outbox → `/Infrastructure/Outbox`  

---

## 6. Ownership Mapping

| Layer         | Owner        |
|--------------|-------------|
| API          | Dev1 / Dev2 |
| Application  | Tech Lead   |
| Domain       | Tech Lead   |
| Infrastructure | Tech Lead |

---

## 7. Common Violations (AUTO REJECT)

- Business logic in Controller  
- DB access outside Repository  
- Kafka usage outside Infrastructure  
- Logic inside Infrastructure  
- Wrong folder placement  

---

## 8. Enforcement

Violations:

- break architecture  
- create bugs  
- reduce maintainability  

**Result:**
- PR REJECTED  

---

## 9. Final Principle

> File location defines responsibility.  
> Wrong placement = wrong behavior = broken system  

---

## 10. Final Result

- Clean architecture enforced  
- Predictable structure  
- Easy onboarding  
- Minimal mistakes  