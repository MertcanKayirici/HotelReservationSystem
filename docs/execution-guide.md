# 🏨 Hotel Reservation System — Execution Guide (Developer Implementation Contract)

---

## 1. Purpose

This document defines **exactly how developers must implement the system**.

### Goals

- Eliminate implementation ambiguity
- Standardize coding behavior
- Prevent logical errors
- Make incorrect implementation difficult

---

## 2. Scope

Feature: **Create Reservation (Async, Idempotent, Concurrency-Safe)**

---

## 3. Endpoint Definition (MUST MATCH API CONTRACT)

### Request

POST /api/v1/reservations

---

### Request Body

```json
{
  "userId": 1,
  "roomId": 1,
  "checkIn": "2026-05-01",
  "checkOut": "2026-05-05",
  "idempotencyKey": "abc-123"
}
```

HTTP Status: 202 Accepted

## 4. Layer Responsibilities (STRICT)

### 🔹 API Layer

**File:**
```
API/Controllers/ReservationController.cs
```

**MUST**
- Validate request model (basic)
- Call Application Service
- Return `202 + PENDING`

**FORBIDDEN**
- Business logic
- DB access
- Kafka/Redis access

---

### 🔹 Application Layer

**File:**
```
Application/Services/ReservationService.cs
```

**MUST**
- Validate business rules (date, room status)
- Idempotency INSERT (PENDING)
- Publish Kafka message
- Return PENDING response

**FORBIDDEN**
- DB transaction
- Lock handling

---

### 🔹 Infrastructure Layer

**Files:**
```
Infrastructure/Kafka/KafkaProducer.cs
Infrastructure/Idempotency/IdempotencyService.cs
Infrastructure/Redis/RedisLockService.cs
Infrastructure/Outbox/*
Infrastructure/Persistence/*
```


**MUST**
- Only external systems

**FORBIDDEN**
- No business logic

---

### 🔴 GRC (CORE SYSTEM)

**File:**
```
Application/GRC/ReservationCoordinator.cs
```

**MUST**
- Acquire Redis lock (roomId)
- Begin DB transaction
- Re-check idempotency
- Insert reservation
- Insert outbox event
- Update idempotency → COMPLETED
- Commit transaction
- Release lock

**FORBIDDEN**
- Skipping lock
- Partial commit
- External calls inside transaction

---

## 5. End-to-End Flow (MANDATORY)

- Controller receives request  
- Validate model  
- Call ReservationService  

### ReservationService
- Validate business rules  
- Idempotency INSERT (PENDING)  

**If duplicate:**
- COMPLETED → return cached  
- PENDING → return IN_PROGRESS  
- FAILED → continue  

- Publish Kafka message  
- Return 202 PENDING  

---

### Kafka Consumer
- Re-check idempotency  
- Skip if COMPLETED  
- Call GRC  

---

### GRC
- Acquire lock (roomId)  
- BEGIN TRANSACTION  
- Insert reservation  
- Insert outbox event  
- Update idempotency  
- COMMIT  
- Release lock  

---

### Outbox Worker
- Publish events  
- Mark SENT  

---

## 6. Validation Rules

**MUST**
- checkOut > checkIn  
- No past dates  
- Room must be ACTIVE  

---

## 7. Conflict Logic (DB IS SOURCE OF TRUTH)

```sql
NOT (
  newCheckOut <= existingCheckIn
  OR
  newCheckIn >= existingCheckOut
)
```
**If conflict:**
- THROW error  
- DO NOT insert  

---

## 8. Idempotency Rules

### STATES

| State     | Behavior            |
|----------|-------------------|
| PENDING  | return IN_PROGRESS |
| COMPLETED| return cached      |
| FAILED   | allow retry        |

### CRITICAL
- Same key + different payload → ERROR  

---

## 9. Kafka Rules

### Topics
- reservation-requests  
- reservation-requests-retry  
- reservation-requests-dlq  

### Key
- roomId  

### Rules
- At-least-once delivery  
- Retry max 3  
- Then DLQ  

---

## 10. SLA & Async Behavior

- Max processing: **3s**  
- Always return **PENDING first**  

### Status endpoint
```
GET /api/v1/reservations/status/{requestId}
```

---

## 11. Error Handling

| Code                  | HTTP |
|----------------------|------|
| INVALID_DATE         | 400  |
| CONFLICT             | 409  |
| IN_PROGRESS          | 202  |
| IDEMPOTENCY_CONFLICT | 409  |
| SYSTEM_ERROR         | 500  |

---

## 12. Forbidden Actions (STRICT)

- Direct DB insert from Controller  
- Skipping idempotency  
- Skipping lock  
- Writing reservation outside GRC  
- Publishing Kafka inside transaction  
- Infinite retry  

---

## 13. Observability (MANDATORY)

### Logging
```json
{
  "event": "reservation_attempt",
  "requestId": "abc",
  "roomId": 1,
  "step": "GRC_START"
}
```

### Metrics

- request_count  
- error_count  
- latency_ms  
- retry_count  
- dlq_count  

---

## 14. Required Methods

```csharp
Task<CreateReservationResult> CreateReservationAsync(CreateReservationRequest request);
Task ProcessReservationAsync(ReservationMessage message);
Task ExecuteReservationAsync(ReservationCommand command);
```

---

## 15. Definition of Done

A feature is complete **ONLY IF:**

- Code implemented  
- Validation added  
- Idempotency implemented  
- Kafka working  
- DB transaction safe  
- Outbox implemented  
- Logs added  
- Tests passing  

---

## 16. Final Principle

> Do not improvise. Follow the flow.

---

## 17. Final Result

- Deterministic behavior  
- No duplicate reservation  
- Safe under concurrency  
- Observable system  

---

## 18. Enforcement

This document is a **contract**.

### Violations:

- cause bugs  
- break consistency  
- PR will be rejected  