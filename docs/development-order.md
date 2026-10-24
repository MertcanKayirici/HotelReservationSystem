# Development Order — Mandatory

---

## 1. Purpose

This document defines the mandatory development, verification, and integration sequence for the Hotel Reservation System.

The objectives are to:

- Prevent incorrect dependency usage
- Protect the existing reservation core
- Avoid conflicting changes
- Enable controlled parallel development
- Ensure reliable integration and data consistency

The project is a backend-only ASP.NET Core API. Frontend and MVC UI development are out of scope.

---

## 2. Development Principles

- The existing `main` branch is the integration baseline.
- Existing core behavior must be reviewed before modification.
- Shared contracts must be agreed upon before dependent implementation begins.
- Independent modules may be developed in parallel once their contracts are defined.
- Core-system changes require Tech Lead approval.
- No feature is considered complete until its behavior is tested.

Development must follow dependency requirements, not merely a fixed calendar.

---

## 3. Phase 1 — Baseline and Database Verification (Tech Lead)

Before new development, the Tech Lead must:

- Verify the current solution structure
- Confirm the existing database provider and connection configuration
- Review current entities and EF Core migrations
- Identify existing constraints and indexes
- Review reservation persistence and transaction boundaries
- Inspect the existing idempotency implementation
- Record known build failures and test failures

Do not recreate existing tables or migrations without first reviewing the current implementation.

Any additional schema changes must be justified by an approved feature requirement.

---

## 4. Phase 2 — Infrastructure and Configuration (Tech Lead)

Verify and document:

- Database connectivity and initialization
- Dependency injection and service registration
- Redis configuration and locking behavior
- Kafka producer and consumer configuration
- Outbox persistence and processing
- Environment variables and secret handling
- Optional infrastructure behavior

Redis and Kafka must support the explicitly documented local development configuration.

When either integration is disabled or unavailable, the application must not claim guarantees it cannot provide.

Required infrastructure behavior must be verified before dependent features are integrated.

---

## 5. Phase 3 — Domain and Application Core (Tech Lead)

The Tech Lead owns the reservation core and its consistency guarantees.

Review and implement as required:

- Reservation domain entities and rules
- Availability and date-overlap validation
- Reservation application service
- Idempotency handling
- Concurrency control
- Transaction boundaries
- Reservation failure and rollback behavior