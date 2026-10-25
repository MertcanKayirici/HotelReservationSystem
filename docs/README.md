# Documentation — Hotel Reservation System

## 1. System Overview

The Hotel Reservation System is a backend application designed to manage hotel reservations while protecting data consistency and preventing conflicting bookings.

### Core Principles

- **Idempotency** helps prevent duplicate effects when requests are retried.
- **Reliable messaging** supports asynchronous processing and recovery.
- **The database** is the authoritative source for persisted reservation state.
- **Concurrency control** protects reservation availability under concurrent requests.
- **Clean Architecture** separates responsibilities and controls dependency direction.

These guarantees must be verified through implementation and automated tests.

---

## 2. Purpose

This folder contains the technical and organizational documentation for the Hotel Reservation System.

### Goals

- Reduce implementation ambiguity.
- Standardize development practices.
- Enable faster onboarding.
- Prevent architectural violations.
- Define API and runtime expectations.
- Support reliable integration and testing.

---

## 3. Document Structure

### Architecture

**File:** `architecture.md`

Defines system boundaries, architectural layers, dependency direction, components, and high-level processing flows.

### Implementation

**File:** `execution-guide.md`

Defines the expected implementation workflow, reservation processing rules, idempotency behavior, and error handling.

### API

**File:** `api-contract.md`

Defines API endpoints, request and response formats, authentication requirements, and HTTP status codes.

API changes require an explicit review and approval process. The contract must be updated whenever an approved change affects externally visible behavior.

### Runtime

**File:** `production-kit.md`

Documents infrastructure setup, configuration, database migrations, startup, and test execution.

### Team

**File:** `team-rules.md`

Defines responsibilities, branch strategy, pull request rules, and team coordination.

### Technology Stack

**File:** `tech-stack.md`

Defines approved technologies, architectural constraints, configuration expectations, and technology change policies.

---

## 4. How to Read the Documentation

### New Developers

Read the documents in this order:

1. `architecture.md` — understand the system structure.
2. `tech-stack.md` — understand the approved technologies.
3. `execution-guide.md` — understand the reservation workflow.
4. `api-contract.md` — understand API behavior.
5. `team-rules.md` — understand team responsibilities and collaboration.
6. `production-kit.md` — configure, run, and verify the application.

### Existing Developers

Refer to the relevant document for the task:

- Implementation and reservation workflow: `execution-guide.md`
- API behavior: `api-contract.md`
- Architectural boundaries: `architecture.md`
- Technology decisions: `tech-stack.md`
- Team workflow and pull requests: `team-rules.md`
- Environment setup and verification: `production-kit.md`

---

## 5. High-Level System Flow

The expected reservation workflow has two connected phases.

### Phase A — Request Acceptance

```text
Client
  → API Controller
  → Application Service
  → Idempotency Handling
  → Persist Processing State and Outbox Intent
  → Return 202 Accepted
```

The initial processing state and Outbox intent must be persisted consistently according to the approved transaction design.

### Phase B — Asynchronous Processing

```text
Outbox Worker
  → Publish Event to Kafka
  → Kafka Consumer
  → Reservation Coordinator
  → Concurrency Protection
  → Reservation Validation
  → Database Transaction
  → Persist Reservation Outcome
  → Update Processing State
```

The exact transaction boundaries, status transitions, retry behavior, and acknowledgment strategy must match the implementation.

---

## 6. Sources of Truth

| Area | Primary Document |
|---|---|
| API contract | `api-contract.md` |
| Implementation workflow | `execution-guide.md` |
| Architecture and dependencies | `architecture.md` |
| Team behavior and reviews | `team-rules.md` |
| Runtime and verification | `production-kit.md` |
| Approved technologies | `tech-stack.md` |

The source code, automated tests, and actual runtime behavior must also be inspected when verifying whether a documented requirement has been implemented.

---

## 7. Enforcement Rules

### Required

- Read the relevant documentation before implementing a change.
- Respect architectural boundaries.
- Follow the approved reservation-processing workflow.
- Apply idempotency to operations designated as requiring it.
- Preserve data consistency and safe failure behavior.
- Update documentation when an approved change affects an existing contract.
- Verify behavior through appropriate tests.

### Forbidden

- Guessing undocumented implementation details.
- Making unapproved API contract changes.
- Placing business logic in the wrong architectural layer.
- Bypassing required idempotency or concurrency protection.
- Accessing databases or external systems outside approved boundaries.
- Treating documented requirements as proof that implementation is complete.

---

## 8. Conflict Resolution

When documents appear to conflict, do not silently choose one rule and ignore the other.

Use the following process:

1. Identify the conflicting requirements.
2. Determine which architectural, API, security, or data-consistency guarantees are affected.
3. Refer the conflict to the Tech Lead for an explicit decision.
4. Update the affected documents together.
5. Ensure the code and automated tests reflect the approved decision.

**Important:** `api-contract.md` governs externally visible API behavior; `architecture.md` governs architectural boundaries; `execution-guide.md` governs the approved implementation workflow within those boundaries. None of these documents may silently override the others.

---

## 9. Developer Responsibilities

Every developer must:

- Read the documentation relevant to their task.
- Follow the agreed implementation and review process.
- Avoid introducing unapproved technologies or architectural patterns.
- Coordinate changes to shared contracts and database schema.
- Add or update relevant tests.
- Report discrepancies between documentation and implementation.
- Document approved deviations where necessary.

---

## 10. Final Principle

> Documentation is part of the system's engineering contract. It must remain consistent with the architecture, implementation, API behavior, and operational procedures.

---

## 11. Expected Result

- Faster developer onboarding.
- More predictable implementation.
- Clear architectural responsibilities.
- Consistent API behavior.
- Better failure handling and data integrity.
- More reliable integration and release verification.
