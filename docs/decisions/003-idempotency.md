# ADR-003 — Use Idempotency for Safe Request Handling

---

## Status

Accepted

---

## Context

The system must handle:

* duplicate API requests
* network retries
* Kafka reprocessing (at-least-once delivery)

Without protection:

* duplicate reservations occur

---

## Decision

We will implement **idempotency using a database table**.

---

## Strategy

Table: `idempotency`

Fields:

* key
* status (PENDING | COMPLETED | FAILED)
* response

---

## Rules

* First request → insert PENDING
* Duplicate request:

  * PENDING → return IN_PROGRESS
  * COMPLETED → return cached response
  * FAILED → allow retry

---

## Consequences

### Positive

* Prevents duplicate execution
* Safe under retries
* Enables effectively-once behavior

---

### Negative

* Additional DB storage
* Slight complexity

---

## Alternatives Considered

### No idempotency

* unsafe

---

### In-memory tracking

* not reliable in distributed systems

---

## Final Decision

Idempotency is required for:

* correctness
* reliability
* distributed safety
