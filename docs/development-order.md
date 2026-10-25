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

### Exit Criteria

- [ ] Reservation rules are documented and tested.
- [ ] Duplicate requests cannot silently create duplicate reservations.
- [ ] Conflicting reservations are handled consistently.
- [ ] Transaction and rollback behavior is verified.
- [ ] Existing behavior is preserved unless an approved change requires otherwise.
- [ ] Relevant unit and integration tests pass.

No dependent feature may change core reservation behavior without Tech Lead approval.

---

## 6. Phase 4 — API Contract and Authentication

**Owner:** Tech Lead, with frontend-independent API verification.

Before dependent implementation begins:

- Verify endpoint routes and HTTP methods.
- Confirm request and response models.
- Verify authentication and authorization requirements.
- Confirm error codes and HTTP status mappings.
- Document idempotency requirements.
- Verify correlation ID propagation.
- Align Swagger, API documentation, and integration tests.

### Exit Criteria

- [ ] `docs/api-contract.md` matches the verified implementation.
- [ ] Protected endpoints enforce authentication.
- [ ] Reservation status lookup enforces user ownership.
- [ ] Error responses are documented accurately.
- [ ] Contract changes have tests and review approval.

The API contract must not be changed silently to accommodate a client implementation.

---

## 7. Phase 5 — Independent Infrastructure Work

**Owner:** Melike (Dev1)

Infrastructure work may proceed in parallel after the required configuration and interfaces are agreed upon.

Scope:

- Docker Compose configuration
- Environment variable documentation
- Secret management
- Database initialization and recovery procedures
- CI/CD pipeline
- Automated build and test execution
- Health checks and operational diagnostics
- Deployment documentation

### Constraints

- Do not modify reservation business rules as part of infrastructure work.
- Do not commit credentials or production secrets.
- Do not remove required infrastructure dependencies to make local execution appear successful.
- Document optional infrastructure behavior explicitly.
- Coordinate changes to shared configuration files before merging.

### Exit Criteria

- [ ] A clean environment can follow the documented setup procedure.
- [ ] Required services start and their health can be checked.
- [ ] Configuration failures are visible and actionable.
- [ ] CI executes the agreed build and test steps.
- [ ] Recovery and migration procedures are documented.

---

## 8. Phase 6 — API Consumer and Integration Work

**Owner:** Kader (Dev2), when API consumers or client integration are required.

The repository's current scope is a backend-only ASP.NET Core API. Frontend and MVC UI development are out of scope unless explicitly approved as a separate project scope.

For API consumer or integration tasks:

- Use the verified API contract.
- Follow the documented authentication flow.
- Handle asynchronous reservation processing and status polling.
- Handle validation errors, authorization failures, and conflicts.
- Preserve idempotency keys when retrying the same logical request.
- Avoid assumptions about undocumented response fields.
- Report contract mismatches rather than introducing silent workarounds.

### Exit Criteria

- [ ] Integration behavior matches the API contract.
- [ ] Success and failure responses are handled.
- [ ] Authentication and authorization behavior is respected.
- [ ] Retry behavior does not create duplicate logical operations.
- [ ] Integration tests or reproducible verification evidence are provided.

---

## 9. Phase 7 — QA, Security, and Reliability Verification

**Owner:** Tech Lead / QA.

Verification must cover the integrated system, not only isolated modules.

Required areas:

- Authentication and authorization
- Request validation
- Reservation date and availability rules
- Idempotency and duplicate requests
- Concurrent reservation attempts
- Transaction and rollback behavior
- Redis locking behavior
- Kafka publishing and consumption
- Outbox processing and recovery
- Error handling and correlation IDs
- Database migrations and persistence
- Configuration and secret handling

### Evidence Requirements

Every completed test must identify:

- The scenario and expected behavior
- The actual result
- The test type (unit, integration, or end-to-end)
- Any required infrastructure
- The test command or reproducible procedure
- Any remaining limitation

A mocked unit test must not be presented as proof of real database concurrency. A passing build must not be presented as proof that all runtime behavior is correct.

### Exit Criteria

- [ ] Required tests pass.
- [ ] Critical regressions are resolved.
- [ ] Security-sensitive behavior is verified.
- [ ] Known limitations are documented.
- [ ] No unresolved critical defect is accepted without explicit approval.

---

## 10. Phase 8 — Integration and Pull Requests

**Owner:** Tech Lead.

All work must be integrated through the repository's approved branch and pull-request workflow.

Before merging:

1. Confirm the change has a clear scope and owner.
2. Review the diff for unrelated or generated changes.
3. Verify architecture and API contract compliance.
4. Check migrations and configuration changes.
5. Run the required build and tests.
6. Review security and failure-handling implications.
7. Update affected documentation.
8. Confirm required reviewers have approved the PR.

### Merge Rules

- Never merge failing required checks without explicit authorization.
- Do not bypass review requirements.
- Do not combine unrelated changes into one PR merely for convenience.
- Do not force-push shared branches without coordination.
- Do not overwrite another developer's uncommitted work.
- Resolve conflicts deliberately and rerun affected tests after resolution.

---

## 11. Phase 9 — Release Readiness

A release candidate may be considered ready only after the following are verified:

- [ ] Application builds successfully.
- [ ] Required automated tests pass.
- [ ] Database migrations are reviewed.
- [ ] Required configuration is documented.
- [ ] Authentication and authorization are verified.
- [ ] Idempotency and reservation conflict behavior are tested.
- [ ] Outbox and messaging behavior are verified for the supported configuration.
- [ ] Logging and correlation IDs are functional.
- [ ] Operational recovery steps are documented.
- [ ] Known limitations and outstanding risks are recorded.
- [ ] Tech Lead approves the release candidate.

A checklist item must not be marked complete without evidence.

---

## 12. Parallel Development Rules

Parallel work is permitted only when module boundaries and shared contracts are clear.

Before work begins, define:

- Scope and ownership
- Files and modules permitted to change
- Interfaces and contracts
- Dependencies on other work
- Required tests
- Acceptance criteria

If two workstreams need to modify the same core file, coordinate the changes before implementation.

A developer or AI coding assistant must stop and request review when the task requires an unapproved change to shared contracts, database schema, reservation consistency, or security boundaries.

---

## 13. AI-Assisted Development Rules

AI coding assistants must:

1. Read the relevant architecture, folder-structure, team, and API contract documents before editing.
2. Inspect the current implementation before proposing replacements.
3. Work only within the approved task scope.
4. Preserve unrelated changes and existing user work.
5. Never invent successful test results or claim commands were executed when they were not.
6. Explain any assumptions and unresolved questions.
7. Provide a concise list of changed files and the reason for each change.
8. Report tests run, tests not run, and any failures.
9. Request approval before changing core reservation behavior, shared contracts, or database migrations.
10. Never commit secrets or destructive database operations without explicit authorization.

If a requirement conflicts with an existing contract or architecture decision, stop and report the conflict rather than silently choosing a new design.

---

## 14. Definition of Done

A task is complete only when:

- Its agreed acceptance criteria are met.
- The implementation follows the approved architecture.
- Relevant tests have been added or updated.
- Required verification has been performed.
- API and database compatibility have been considered.
- Documentation has been updated where necessary.
- The pull request has been reviewed and approved.
- Known limitations are disclosed.

**Final principle: Development order follows dependency and verification requirements. No phase is complete merely because its code has been written.**
