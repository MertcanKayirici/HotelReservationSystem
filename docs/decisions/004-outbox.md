# ADR-004 — Use Outbox Pattern for Event Consistency

---

## Status

Accepted

---

## Context

The system must ensure:

* DB writes and event publishing are consistent
* no message loss
* no partial failures

Direct Kafka publishing inside transactions is unsafe.

---

## Decision

We will use the **Outbox Pattern**.

---

## Strategy

1. Insert reservation into DB
2. Insert event into `outbox_events`
3. Commit transaction
4. Background worker publishes event

---

## Table

outbox_events:

* id
* event_type
* payload
* status (PENDING | SENT)

---

## Consequences

### Positive

* Guarantees consistency
* Prevents lost events
* Enables retry mechanism

---

### Negative

* Requires background worker
* Slight delay in event publishing

---

## Alternatives Considered

### Direct event publish

* can lose messages

---

### Dual write (DB + Kafka)

* unsafe and inconsistent

---

## Final Decision

Outbox pattern ensures:

* reliability
* consistency
* fault tolerance
