# 🏨 Hotel Reservation System — Production Kit (Run & Verify Guide)

---

## 1. Purpose

Ensure the system is **runnable, reproducible, and testable** across all environments.

### Goals

- Eliminate environment inconsistencies
- Enable identical setup for all developers
- Prevent “works on my machine” issues

---

## 2. System Boundaries

This layer provides:

- Infrastructure setup (DB, Kafka, Redis)
- Environment configuration
- Migration and seeding
- Test validation

This layer **does NOT modify business logic**.

---

## 3. Docker Compose (MANDATORY)

**File:** `docker-compose.yml`

### Services

- MSSQL → primary database
- Redis → distributed locking
- Kafka + Zookeeper → event streaming

---

### Example

```yaml
version: '3.8'

services:
  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: mssql
    environment:
      SA_PASSWORD: "YourStrong!Passw0rd"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  zookeeper:
    image: bitnami/zookeeper:latest
    environment:
      ALLOW_ANONYMOUS_LOGIN: yes

  kafka:
    image: bitnami/kafka:latest
    ports:
      - "9092:9092"
    environment:
      KAFKA_CFG_ZOOKEEPER_CONNECT: zookeeper:2181
      ALLOW_PLAINTEXT_LISTENER: yes
```

---

## Rules

- Infrastructure MUST be started via Docker  
- Manual setup is FORBIDDEN  

---

## 4. Environment Configuration

**File:** `.env.example`

```env
DB_CONNECTION=Server=localhost,1433;Database=HotelDb;User Id=sa;Password=YourStrong!Passw0rd;
REDIS_CONNECTION=localhost:6379
KAFKA_BROKER=localhost:9092
ASPNETCORE_ENVIRONMENT=Development
```

---

## Rules

- No hardcoded values  
- All configs MUST come from environment variables  

---

## 5. Database Migrations (EF Core)

### Commands

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Rules

- Use EF Core migrations ONLY  
- Manual DB changes are FORBIDDEN  

---

## 6. Seed Data

**File:** `Infrastructure/Persistence/SeedData.cs`

### Purpose
- Insert default rooms  

### Rule
- Must be idempotent  

---

## 7. Run Procedure (STRICT ORDER)

```bash
# 1. Start infrastructure
docker-compose up -d

# 2. Apply migrations
dotnet ef database update

# 3. Run application
dotnet run --project src/HotelReservationSystem.API
```

---

## Rules

- Order MUST NOT change  
- Skipping steps is FORBIDDEN  

---

## 8. Test Suite

### Unit Tests

```bash
dotnet test
```

---

## Load Test (k6)

```bash
k6 run tests/load/k6-test.js
```

---

## Rules

- Tests MUST pass before merge  
- Untested code is FORBIDDEN  

---

## 9. Smoke Test (MANDATORY)

```bash
curl -X POST http://localhost:5000/api/v1/reservations \
-H "Content-Type: application/json" \
-d '{
  "userId":1,
  "roomId":1,
  "checkIn":"2026-05-01",
  "checkOut":"2026-05-05",
  "idempotencyKey":"smoke-1"
}'
```

---

### Expected Response

```json
{
  "status": "PENDING"
}
```

---

## 10. Global Rules

### MUST

- Use Docker for infrastructure  
- Use EF migrations  
- Run tests before commit  
- Use shared environment config  

### FORBIDDEN

- Manual DB setup  
- Skipping tests  
- Local-only configs  

---

## 11. Final Principle

> If it cannot be run the same way, it is not production-ready.

---

## 12. Final Result

- Reproducible environment  
- Deterministic startup  
- Verified system  
- Zero ambiguity  