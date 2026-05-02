# 🏨 Hotel Reservation System — Execution Guide (Developer Contract)

---

## 1. Purpose

This document defines **HOW the system must be implemented**.

### Goals

* Remove ambiguity
* Guide junior developers step-by-step
* Prevent incorrect implementation
* Enforce clean architecture

---

## 2. Scope

Feature: **Create Reservation (Async + Idempotent + Safe)**

---

## 3. Endpoint Definition (MUST MATCH)

```http
POST /api/v1/reservations
```

### Request

```json
{
  "userId": 1,
  "roomId": 1,
  "checkIn": "2026-05-01",
  "checkOut": "2026-05-05",
  "idempotencyKey": "abc-123"
}
```

### Response

```json
{
  "status": "PENDING",
  "requestId": "abc-123"
}
```

👉 Always return **202 Accepted**

---

## 4. Layer Responsibilities (VERY IMPORTANT)

---

### 🔹 API Layer

📁 `API/Controllers/ReservationsController.cs`

**MUST**

* Validate request (basic)
* Call Application Service
* Return 202 + PENDING

**FORBIDDEN**

* Business logic
* DB access
* Kafka / Redis

---

### 🔹 Application Layer

📁 `Application/Services/ReservationService.cs`

**MUST**

* Validate business rules
* Handle idempotency
* Send event to Kafka (via Outbox)
* Return PENDING

**FORBIDDEN**

* DB transaction
* Lock logic

---

### 🔴 Orchestration (GRC)

📁 `Application/Orchestration/ReservationCoordinator.cs`

👉 **THIS IS THE HEART OF THE SYSTEM**

**MUST**

1. Acquire Redis lock (roomId)
2. Start DB transaction
3. Check conflict
4. Insert reservation
5. Insert outbox event
6. Update idempotency → COMPLETED
7. Commit transaction
8. Release lock

**FORBIDDEN**

* Skipping lock
* Partial execution
* External calls inside transaction

---

### 🔹 Infrastructure Layer

📁 `Infrastructure/*`

**Contains**

* DB (EF Core)
* Kafka
* Redis
* Idempotency
* Outbox

**MUST**

* Only external operations

**FORBIDDEN**

* Business logic

---

## 5. End-to-End Flow (MANDATORY)

```
Controller
 → Service
 → Idempotency (PENDING)
 → Outbox (event stored)
 → Worker publishes Kafka
 → Consumer receives
 → Coordinator (GRC)
 → DB write
 → Idempotency COMPLETED
```

---

## 6. Idempotency Rules (CRITICAL)

| State     | Behavior           |
| --------- | ------------------ |
| PENDING   | return IN_PROGRESS |
| COMPLETED | return cached      |
| FAILED    | allow retry        |

### MUST

* Same key + different payload → ERROR

---

## 7. Conflict Rule (IMPORTANT)

```sql
NOT (
  newCheckOut <= existingCheckIn
  OR
  newCheckIn >= existingCheckOut
)
```

👉 If conflict → THROW error

---

## 8. Kafka Rules

Topics:

* reservation-events
* reservation-events-dlq

Rules:

* At-least-once delivery
* Retry → then DLQ

---

## 9. Validation Rules

* checkOut > checkIn
* No past dates
* Room must be active

---

## 10. Error Handling

| Code        | HTTP |
| ----------- | ---- |
| INVALID     | 400  |
| CONFLICT    | 409  |
| IN_PROGRESS | 409  |
| NOT_FOUND   | 404  |
| SYSTEM      | 500  |

---

## 11. Logging (MANDATORY)

Every step MUST log:

```json
{
  "step": "GRC_START",
  "roomId": 1,
  "requestId": "abc-123"
}
```

---

## 12. Forbidden Actions (AUTO FAIL)

* DB access in Controller
* Kafka usage outside Infrastructure
* Skipping idempotency
* Skipping lock
* Writing reservation outside Coordinator

---

## 13. Definition of Done

Feature is complete ONLY IF:

* Works end-to-end
* No duplicate reservation
* Idempotency working
* Kafka working
* Logs added
* No runtime errors

---

## 14. For Junior Developers

👉 If unsure:

1. Follow the flow
2. Do NOT invent logic
3. Ask before changing architecture

---

## 15. Final Principle

> Follow the flow. Do not improvise.

---

## 16. Result

* No double booking
* Safe under concurrency
* Predictable system
