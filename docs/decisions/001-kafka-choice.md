# ADR-001 — Use Kafka for Reservation Processing

---

## Status

Accepted

---

## Context

The system must handle:

* high request throughput
* concurrent reservation attempts
* retry scenarios
* failure recovery

Direct synchronous processing creates:

* race conditions
* DB overload
* inconsistent state

---

## Decision

We will use **Apache Kafka** as the message broker.

---

## Reasons

* Supports partitioning → ordering per room
* Handles high throughput
* Enables retry + DLQ
* Decouples API from processing

---

## Consequences

### Positive

* Scalable system
* Controlled concurrency
* Reliable processing

---

### Negative

* Increased infrastructure complexity
* Requires consumer management

---

## Alternatives Considered

### Direct DB write

* simpler
* but unsafe under concurrency

---

### RabbitMQ

* simpler setup
* weaker ordering guarantees

---

## Final Decision

Kafka provides:

* scalability
* reliability
* deterministic processing
