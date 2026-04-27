# ADR-002 — Use Redis for Distributed Locking

---

## Status

Accepted

---

## Context

Multiple users may try to reserve the same room at the same time.

This can cause:

* race conditions
* double booking
* inconsistent DB state

---

## Decision

We will use **Redis-based distributed locking**.

---

## Lock Strategy

Key format:

room:{roomId}

---

## Reasons

* Fast in-memory system
* Suitable for distributed environments
* Prevents concurrent writes

---

## Consequences

### Positive

* Eliminates race conditions
* Ensures single writer per room
* Improves consistency

---

### Negative

* Requires Redis infrastructure
* Requires lock timeout handling

---

## Alternatives Considered

### DB locking

* safe
* but reduces performance

---

### No locking

* leads to data corruption

---

## Final Decision

Redis provides optimal balance between:

* performance
* simplicity
* correctness
