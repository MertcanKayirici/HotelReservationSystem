# QA Test Plan — Hotel Reservation System

## 1. Purpose

This document defines the quality assurance strategy for the Hotel Reservation System.

It covers API behavior, authentication, authorization, reservation workflows, idempotency, database integrity, integration behavior, and regression testing.

The QA owner is responsible for maintaining this plan. The Tech Lead decides whether unresolved issues block integration.

## 2. Testing Principles

- Test observable behavior, not only internal implementation.
- Test both successful and unsuccessful scenarios.
- Use isolated test data and databases for integration tests.
- Never use production credentials or production data for testing.
- Do not mark a test as passed unless it has actually been executed.
- Keep tests repeatable and independent where practical.
- A passing unit test does not prove that a complete distributed workflow works.
- Record environment limitations and skipped verification explicitly.

## 3. Test Levels

### 3.1 Unit Tests

Verify individual services, validation rules, error handling, and business logic using controlled dependencies.

Examples:
- Invalid reservation dates are rejected.
- Missing idempotency keys are rejected.
- Existing idempotency records are handled correctly.
- Room-lock failures stop the workflow.
- Locks are released after failures.

### 3.2 Integration Tests

Verify interactions between real application components and infrastructure.

Examples:
- EF Core persists data correctly.
- SQL Server unique constraints reject duplicate keys.
- Transaction failures roll back changes.
- Redis locking behaves as expected.
- Outbox messages are persisted and processed correctly.

Use an isolated test database and test infrastructure.

### 3.3 API Tests

Verify observable HTTP behavior.

Examples:
- Correct status codes.
- Correct response structure.
- Request validation.
- Authentication and authorization.
- Error handling.
- Compatibility with documented API contracts.

### 3.4 End-to-End Tests

Verify complete user workflows across multiple components.

Examples:
- Register, log in, and use an authorized endpoint.
- Browse available rooms and inspect room details.
- Create a reservation and inspect its status.

End-to-end tests should be added when the frontend and required infrastructure are available.

## 4. Authentication and Authorization Scenarios

| Scenario | Expected result |
|---|---|
| Register with valid input | Account is created according to the API contract |
| Register with invalid input | Validation error is returned |
| Register with an existing email | Duplicate account is rejected |
| Login with valid credentials | Authentication succeeds |
| Login with invalid credentials | Authentication fails without revealing sensitive details |
| Call a protected endpoint without a token | Request is rejected |
| Call a protected endpoint with an invalid or expired token | Request is rejected |
| Access another user's protected resource | Access is denied unless explicitly permitted |
| Use an insufficient role | Access is denied |
| Refresh or revoke a token | Behavior matches the implemented token policy |

Verify actual routes, roles, and token behavior against the implementation before writing API tests.

## 5. Reservation API Scenarios

| Scenario | Expected result |
|---|---|
| Valid reservation request | Accepted according to the API contract |
| Checkout is before or equal to check-in | Request is rejected |
| Required field is missing | Validation error is returned |
| Room does not exist | Appropriate not-found or validation response |
| Room is unavailable for the requested period | Reservation is rejected according to business rules |
| Request is unauthorized | Request is rejected |
| User requests another user's reservation | Access is denied unless explicitly permitted |
| Reservation status is requested | Correct status is returned |
| Unexpected server error occurs | Safe error response is returned without internal details |

Do not assume that room availability checks or every listed endpoint already exist. Confirm implementation before treating a scenario as executable.

## 6. Idempotency Scenarios

The system must handle repeated requests consistently according to the implemented idempotency contract.

| Scenario | Expected result |
|---|---|
| Idempotency key is missing | Request is rejected |
| New key and valid request | Request enters the expected initial state |
| Same key and same request while pending | Existing in-progress behavior is preserved |
| Same key and same request after completion | Existing completion behavior is preserved |
| Same key with a different request payload | Conflict is returned |
| Two concurrent requests use the same key | At most one idempotency record is created |
| Database rejects a duplicate key | The error is handled without exposing internal database details |
| A failure occurs during transactional persistence | Transaction is rolled back |
| Retry occurs after a failed attempt | Behavior follows the documented retry policy |

Concurrency scenarios should use real SQL Server integration tests where database uniqueness and transaction behavior are being verified.

A mocked unit test alone does not prove that the real SQL Server race condition is handled correctly.

## 7. Outbox and Background Processing

When testing Outbox behavior, verify:

- [ ] The Outbox message is persisted in the expected transaction.
- [ ] A failed transaction does not leave an unintended Outbox record.
- [ ] Processed messages are not processed again incorrectly.
- [ ] Retry behavior matches the configured policy.
- [ ] Cancellation is handled without corrupting message state.
- [ ] Dead-letter or terminal failure behavior is correct where implemented.
- [ ] Duplicate event handling is safe where required.
- [ ] Logs provide useful diagnostic context without leaking secrets.

Confirm actual worker and retry behavior before defining exact assertions.

## 8. Database and Transaction Tests

- [ ] Unique constraints reject duplicate values.
- [ ] Required relationships and foreign keys are enforced.
- [ ] Failed transactions do not leave partial persisted changes.
- [ ] Migrations apply to an isolated clean database.
- [ ] Database configuration matches the environment.
- [ ] Tests do not modify the main development database accidentally.
- [ ] Persistence failures are handled safely.
- [ ] Existing data is not deleted by test cleanup unless it belongs to the isolated test.

## 9. Security and Error Handling

- [ ] Invalid input cannot bypass server-side validation.
- [ ] Unauthorized requests cannot access protected data.
- [ ] Resource ownership is enforced.
- [ ] Responses do not expose stack traces or SQL details.
- [ ] Logs do not contain passwords or tokens.
- [ ] Secrets are not committed to the repository.
- [ ] CORS and authentication configuration are not weakened to bypass tests.
- [ ] Unexpected exceptions produce safe responses.
- [ ] Error codes and response structures match the API contract.

## 10. Regression Testing

After changes to shared code, run the relevant existing tests.

Pay particular attention to:

- Authentication and token handling.
- Reservation creation and status handling.
- Idempotency state transitions.
- SQL Server unique constraints.
- Transaction rollback.
- Redis lock acquisition and release.
- Outbox persistence and processing.
- API response compatibility.

Run the full test suite before merging when practical.

## 11. Test Evidence

For each verification run, record:

- Date and environment.
- Command or test procedure.
- Tests executed.
- Passed, failed, and skipped counts.
- Relevant failure details.
- Dependencies that were unavailable.
- Follow-up actions.

Never report tests as successful based only on compilation or a previous test run.

## 12. Defect Severity

### Critical

Examples: unauthorized access to sensitive data, secret exposure, destructive data loss, or duplicate processing that causes severe integrity violations.

**Action:** Block merging and notify the Tech Lead immediately.

### High

Examples: core reservation flow is broken, authorization can be bypassed, or transactions leave inconsistent data.

**Action:** Block merging until fixed or explicitly assessed by the Tech Lead.

### Medium

Examples: a documented workflow fails in a limited scenario or error handling is inconsistent.

**Action:** Fix before release unless the Tech Lead approves a documented exception.

### Low

Examples: minor documentation errors or non-blocking usability issues.

**Action:** Record and prioritize appropriately.

## 13. Release Readiness Checklist

- [ ] Relevant unit tests pass.
- [ ] Relevant integration tests pass.
- [ ] API and authorization tests pass.
- [ ] Critical reservation and idempotency scenarios are verified.
- [ ] No unresolved critical or high-severity defect remains.
- [ ] Configuration and secrets have been reviewed.
- [ ] Deployment and recovery procedures have been checked where applicable.
- [ ] Documentation reflects the implemented behavior.
- [ ] CI checks pass or unavailable checks are explicitly documented.
- [ ] The Tech Lead approves release readiness.

## 14. Ownership

- **QA owner / Tech Lead:** Maintains the test plan, reviews evidence, prioritizes defects, and decides whether quality issues block integration.
- **Feature developer:** Adds and runs tests for their changes and reports limitations.
- **Infrastructure developer:** Verifies deployment configuration, service health, and infrastructure-dependent tests.
- **Frontend developer:** Verifies user-visible workflows, validation, API integration, and error states.

Passing this checklist provides evidence of verification; it does not guarantee the absence of all defects.
