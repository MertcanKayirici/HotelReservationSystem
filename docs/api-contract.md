# 📄 API Contract — Hotel Reservation System

---

## 1. Purpose

This document defines the **API contract** between frontend and backend.

### Goals

- Prevent breaking changes
- Ensure consistent request/response format
- Enable parallel development
- Guarantee safe and idempotent operations

---

## 2. Base Rules

### MUST

- All endpoints MUST follow REST conventions
- All responses MUST be JSON
- All endpoints MUST be versioned (`/api/v1/`)
- All write operations MUST be idempotent

---

## 3. Base URL

/api/v1

---

## 4. Reservation API

---

### 🔹 Create Reservation

POST /api/v1/reservations

---

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

## Validation Rules

- `checkOut` MUST be greater than `checkIn`  
- Dates MUST NOT be in the past  
- `idempotencyKey` MUST be unique per request  
- Same `idempotencyKey` + different payload → ERROR  

---

## Response (Async)

```json
{
  "status": "PENDING",
  "requestId": "abc-123"
}
```
HTTP Status: 202 Accepted

--- 

## 🔹 Get Reservation Status

GET /api/v1/reservations/status/{requestId}

---

## Response
```json
{
  "status": "PENDING | COMPLETED | FAILED"
}
```

---

## Completed Response (Future Extension)
```json
{
  "status": "COMPLETED",
  "reservationId": 123
}
```

---

## 5. Error Format (GLOBAL)
```json
{
  "code": "ERROR_CODE",
  "message": "Human readable message"
}
```

---

## 6. Error Codes

| Code                  | Description                          |
|----------------------|--------------------------------------|
| INVALID_DATE         | Invalid date range                   |
| CONFLICT             | Room already booked                  |
| IN_PROGRESS          | Request already processing           |
| IDEMPOTENCY_CONFLICT | Same key used with different payload |
| UNAUTHORIZED         | Auth required                        |
| SYSTEM_ERROR         | Unexpected error                     |

---

## 7. HTTP Status Codes

| Status | Usage                        |
|--------|-----------------------------|
| 200    | Success                     |
| 202    | Accepted (async processing) |
| 400    | Validation error            |
| 401    | Unauthorized                |
| 409    | Conflict                   |
| 500    | System error               |

---

## 8. Critical Rules

### MUST

- Response structure MUST NOT change  
- Endpoint names MUST NOT change  
- Idempotency MUST be enforced  
- All async operations MUST return `PENDING` first  
- Controllers MUST NOT contain business logic  

### FORBIDDEN

- Breaking response format  
- Returning raw exceptions  
- Mixing different response formats  
- Skipping idempotency validation  

---

## 9. Versioning Strategy

- `/api/v1/...` → current  
- `/api/v2/...` → breaking changes ONLY  

Old versions MUST remain available during migration  


