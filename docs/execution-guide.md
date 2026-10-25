# 🏨 Hotel Reservation System — Execution Guide (Developer Implementation Contract)

---

## 1. Purpose

This document defines **exactly how developers must implement the system**.

### Goals

- Eliminate implementation ambiguity
- Standardize coding behavior
- Prevent logical errors
- Make incorrect implementation difficult

---

## 2. Scope

Feature: **Create Reservation (Async, Idempotent, Concurrency-Safe)**

---

## 3. Endpoint Definition (Must Match Actual API Contract)

### 3.1 Create Reservation

**Endpoint**

```http
POST /api/v1/Reservations
```

**Authentication:** Required — valid authenticated user.

**Request body**

```json
{
  "userId": 1,
  "roomId": 1,
  "checkIn": "2026-05-01",
  "checkOut": "2026-05-05",
  "idempotencyKey": "abc-123"
}
```

> The request example must be verified against the actual `CreateReservationDto` before being treated as the final schema. The authenticated user's identity is taken from the `userId` claim, and the controller overwrites `dto.UserId` with that value. The client-provided `userId` must not be trusted.

**Accepted response — HTTP 202**

```json
{
  "status": "PENDING",
  "requestId": "abc-123"
}
```

The `requestId` is populated from `dto.IdempotencyKey` by the current controller implementation.

### 3.2 Reservation Status

**Endpoint**

```http
GET /api/v1/Reservations/status/{requestId}
```

**Authentication:** Required.

The endpoint retrieves the status for the specified request and authenticated user. The actual status values and `data` payload must be verified against the application service implementation.

### 3.3 Contract Rules

- The controller must not trust a user identity supplied by the client.
- The initial response represents acceptance for processing, not proof that the reservation has been completed.
- Error status codes and response bodies must match the actual exception middleware.
- Any change to the request DTO, response DTO, route, or error contract must be reflected in `docs/api-contract.md` and covered by appropriate tests.

---

## 4. Layer Responsibilities (Strict)

This section defines the intended responsibilities of each layer. A responsibility must not be considered implemented until it has been verified against the actual source code.

### 4.1 API Layer

**Primary file:**

```text
src/HotelReservationSystem.API/Controllers/ReservationsController.cs
```

**Responsibilities**
- Receive HTTP requests.
- Apply authentication and request-model validation.
- Obtain the authenticated user's ID from the `userId` claim.
- Set the correlation ID on the request DTO when applicable.
- Call the application service.
- Return the HTTP response defined by the API contract.

**Forbidden**
- Direct database operations.
- Direct Kafka producer or Redis lock operations.
- Implementing reservation business rules inside the controller.
- Trusting a client-supplied user ID.

### 4.2 Application Layer

**Primary responsibility:** Coordinate the reservation use case.

**Responsibilities**
- Validate reservation business rules through the appropriate application/domain components.
- Coordinate idempotency handling.
- Initiate asynchronous processing according to the implemented messaging design.
- Return results consistent with the API contract.
- Propagate failures through the established exception-handling mechanism.

**Forbidden**
- Duplicating business rules across multiple layers.
- Bypassing the established reservation-processing flow.
- Claiming that a reservation is completed merely because a request was accepted.

**Verification required:** Confirm the actual service class, method signatures, and division of responsibilities before assigning these requirements to a specific file.

### 4.3 Infrastructure Layer

**Relevant components may include:**

```text
Infrastructure/
├── Kafka/
├── Redis/
├── Persistence/
├── Idempotency/
└── Outbox/
```

The paths above describe logical component categories, not a guarantee that every directory or class exists in the repository.

**Responsibilities**
- Implement database access and persistence.
- Integrate with Kafka and Redis.
- Provide idempotency persistence where assigned by the architecture.
- Implement Outbox persistence and event delivery components.
- Handle infrastructure-specific failures and configuration.

**Forbidden**
- Owning reservation business decisions that belong to the domain/application logic.
- Bypassing application-level rules.
- Exposing secrets or connection credentials in source control.

### 4.4 Reservation Coordinator (GRC)

**Purpose:** Coordinate the critical reservation-processing operation.

The coordinator's exact implementation and location must be confirmed in the repository before its file path is treated as authoritative.

**Target responsibilities**
- Coordinate the room-lock lifecycle.
- Re-check idempotency and reservation eligibility before writing.
- Execute reservation persistence and Outbox persistence atomically when they share the same database transaction.
- Update the idempotency state consistently with the committed operation.
- Release acquired locks safely, including failure paths.

**Transaction and locking rules**
- Do not hold a database transaction open while making network calls to Kafka or other external systems.
- Do not assume a Redis lock alone guarantees database-level consistency.
- Ensure lock ownership and expiry are handled safely.
- Use database constraints or an equivalent database-level concurrency strategy where required to prevent overlapping reservations.
- Never report successful completion before the database transaction commits.

### 4.5 Kafka Consumer and Outbox Worker

**Kafka Consumer target responsibilities**
- Consume reservation requests.
- Handle duplicate delivery safely.
- Invoke the reservation-processing coordinator.
- Apply bounded retry and dead-letter handling according to the configured messaging design.

**Outbox Worker target responsibilities**
- Read eligible unpublished Outbox records.
- Publish events to the configured broker.
- Mark events as published only according to the implemented delivery

---

## 5. End-to-End Flow (Mandatory)

This section defines the intended asynchronous reservation-processing flow. Any behavior not verified in the source code must be treated as a target requirement.

### 5.1 Request Acceptance

1. The client sends an authenticated reservation request.
2. The API validates the request model.
3. The controller obtains the authenticated user's ID from the `userId` claim.
4. The application service coordinates initial validation and idempotency handling.
5. The system records or identifies the request according to the implemented idempotency strategy.
6. The request is accepted for asynchronous processing.
7. The API returns `202 Accepted` with `status: PENDING` and a `requestId`.

**Important:** HTTP `202 Accepted` means that the request has been accepted for processing. It does not guarantee that the reservation will succeed.

### 5.2 Idempotency Handling

The system must distinguish between a new request and a repeated request.

| Existing state | Required behavior |
|---|---|
| No existing key | Register the request for processing |
| `PENDING` | Do not create a duplicate operation; report that processing is in progress |
| `COMPLETED` | Return or expose the previously recorded result |
| `FAILED` | Apply the explicitly defined retry policy |
| Same key, different payload | Reject the request with an idempotency conflict |

The idempotency record must be scoped appropriately to the authenticated user and request identity.

The exact HTTP status and response body for duplicate requests must match the implemented API contract. A duplicate request must not be assumed to receive `202` in every case.

### 5.3 Kafka Consumer

1. The consumer receives a reservation request message.
2. It validates the message sufficiently to process it safely.
3. It checks whether the operation has already completed.
4. If already completed, it skips duplicate reservation creation.
5. Otherwise, it invokes the reservation coordinator.
6. On success, it acknowledges the message according to the consumer's delivery configuration.
7. On failure, it applies the configured bounded retry and dead-letter policy.

The consumer must tolerate at-least-once delivery. Receiving the same message more than once must not create duplicate reservations.

### 5.4 Reservation Coordinator

The coordinator is responsible for the critical reservation operation.

1. Acquire the appropriate room-level lock.
2. Confirm that the lock was acquired successfully.
3. Begin the database transaction.
4. Re-check the idempotency state and reservation eligibility.
5. Verify that the room is active.
6. Check for overlapping reservations using the database as the authoritative source.
7. Insert the reservation.
8. Insert the corresponding Outbox event in the same database transaction.
9. Update the idempotency record consistently with the reservation result.
10. Commit the transaction.
11. Release the lock safely.

If any database operation fails before commit, the transaction must roll back. The lock must still be released through a reliable cleanup path.

**Concurrency requirement:** The lock must not be treated as the only protection against conflicting reservations. Database-level consistency must also be enforced where necessary.

### 5.5 Outbox Worker

1. Read eligible unpublished Outbox records.
2. Publish the corresponding events to Kafka.
3. Mark records as published according to the implementation's delivery protocol.
4. Retry transient failures within configured limits.
5. Recover unpublished records after a process restart.

Publishing an event and updating its Outbox record are not generally one atomic operation across the database and Kafka. Duplicate publication must therefore be handled safely by consumers.

### 5.6 Failure Handling

| Failure point | Required behavior |
|---|---|
| Request validation | Reject the invalid request |
| Idempotency conflict | Reject the conflicting reuse of a key |
| Lock acquisition | Do not proceed as though the lock was acquired |
| Reservation conflict | Do not create an overlapping reservation |
| Database transaction | Roll back uncommitted changes |
| Kafka processing | Apply bounded retry and dead-letter handling |
| Outbox publishing | Preserve the unpublished event for recovery |
| Process interruption | Resume safely without duplicating the reservation |

The actual error codes and HTTP responses must be defined by `docs/api-contract.md` and verified against the exception middleware.

### 5.7 Status Tracking

The client can query:

```http
GET /api/v1/Reservations/status/{requestId}
```

The endpoint must return the status associated with the authenticated user and the requested operation.

The client must distinguish between:
- Request accepted for processing.
- Processing still in progress.
- Reservation completed successfully.
- Reservation failed.

The exact status values, response payload, and authorization checks must be verified against the application service implementation.

### 5.8 Flow Completion Criteria

The flow is considered successful only when:

- The reservation has been committed to the database.
- The idempotency record is consistent with the committed result.
- The required Outbox event has been committed with the reservation.
- Duplicate message delivery cannot create a second reservation.
- The client can retrieve the resulting operation status.

An accepted HTTP request alone does not satisfy these criteria.

---

## 6. Validation Rules (Mandatory)

All reservation requests must be validated before a reservation is committed. Validation responsibilities must be clearly separated between the API, application/domain logic, and database.

### 6.1 Request Validation

The API layer must validate the request model.

Required checks:
- `roomId` must be a valid identifier.
- `checkIn` must be provided in a supported date format.
- `checkOut` must be provided in a supported date format.
- `idempotencyKey` must be present and non-empty.
- Required fields must not contain invalid values.

Invalid request models must be rejected before reservation processing begins.

### 6.2 Date Validation

The application/domain layer must enforce the reservation date rules.

**Rule 1 — Valid date range**

```text
checkOut > checkIn
```

A reservation whose checkout date is equal to or earlier than its check-in date must be rejected.

**Rule 2 — Past dates**

The check-in date must not be earlier than the current business date, according to the system's configured time zone and date policy.

The date policy must be consistent across validation, processing, and tests.

**Rule 3 — Date-only semantics**

Check-in and checkout represent calendar dates, not arbitrary timestamps, unless the API contract explicitly specifies otherwise.

The implementation must use consistent date comparison semantics to avoid time-zone-related inconsistencies.

### 6.3 Room Validation

Before a reservation is committed, the system must verify that:

- The room exists.
- The room is in an active, bookable state.
- The room has not been made unavailable by another applicable business rule.

A room's active status must be checked against authoritative persisted state. A room that does not exist or is not bookable must not receive a reservation.

### 6.4 Reservation Conflict Validation

The database is the authoritative source for persisted reservation conflicts.

Two reservations overlap when both conditions are true:

```text
newCheckIn < existingCheckOut
AND
newCheckOut > existingCheckIn
```

Equivalent SQL predicate:

```sql
NOT (
    newCheckOut <= existingCheckIn
    OR
    newCheckIn >= existingCheckOut
)
```

This policy treats checkout and the next check-in as non-overlapping boundaries.

**Example**

| Existing reservation | New reservation | Result |
|---|---|---|
| May 1–May 5 | May 3–May 7 | Conflict |
| May 1–May 5 | May 5–May 8 | No date overlap |
| May 1–May 5 | April 28–May 1 | No date overlap |
| May 1–May 5 | May 1–May 5 | Conflict |

The conflict check must be performed again during critical reservation processing. An earlier application-level check alone is insufficient because another request may reserve the same room concurrently.

### 6.5 Database Consistency

The system must prevent conflicting reservations from being committed under concurrent requests.

Required safeguards:
- Perform the authoritative conflict check within the critical processing flow.
- Use the established transaction and locking strategy.
- Apply suitable database-level constraints or another proven concurrency-control mechanism where required.
- Ensure reservation and Outbox changes are atomic when they use the same database transaction.

A Redis lock must not be assumed to guarantee consistency by itself.

### 6.6 Validation Error Handling

Validation failures must use the error contract defined in `docs/api-contract.md`.

At minimum, the contract must distinguish:
- Invalid request data.
- Invalid date range.
- Past check-in date.
- Missing room.
- Inactive or unavailable room.
- Reservation conflict.
- Missing idempotency key.

Do not introduce new error codes or HTTP status mappings without updating the API contract and relevant tests.

### 6.7 Required Tests

The implementation must include tests for:

- Checkout equal to check-in.
- Checkout earlier than check-in.
- Check-in earlier than the permitted business date.
- Missing or invalid room identifier.
- Nonexistent room.
- Inactive room.
- Overlapping reservations.
- Reservations sharing only a checkout/check-in boundary.
- Concurrent requests attempting to reserve the same room.
- Repeated requests using the same idempotency key.

Tests must verify both the expected response and the persisted database state where applicable.

### 6.8 Verification Rule

The rules in this section define the required behavior. Before marking them complete, verify that the actual implementation enforces them and that automated tests demonstrate the expected results.

---

## 7. Conflict Logic (Database Is the Source of Truth)

### 7.1 Core Rule

The database is the authoritative source for persisted reservation data.

An application-level availability check is useful for early validation, but it does not guarantee that a room remains available until the reservation is committed.

The system must prevent overlapping reservations from being committed, including when multiple requests are processed concurrently.

### 7.2 Overlap Predicate

Two reservations overlap when:

```sql
NOT (
    newCheckOut <= existingCheckIn
    OR
    newCheckIn >= existingCheckOut
)
```

Equivalent predicate:

```sql
newCheckIn < existingCheckOut
AND
newCheckOut > existingCheckIn
```

The predicate must be evaluated for the same room and the reservation states that count as occupying inventory.

The implementation must explicitly define which statuses participate in conflict detection. Cancelled or otherwise non-blocking reservations must not be treated as active conflicts unless the business rules specify otherwise.

### 7.3 Critical Processing Requirements

During reservation processing, the system must:

1. Identify the room being requested.
2. Acquire the configured concurrency-control mechanism.
3. Verify that the room exists and is bookable.
4. Re-check existing reservations using the authoritative database state.
5. Reject the operation if an overlapping reservation exists.
6. Insert the reservation only when the conflict check succeeds.
7. Commit the reservation within the established transaction boundary.

The conflict check and reservation insert must be protected against concurrent operations. A check followed by an unprotected insert is insufficient.

### 7.4 Concurrency Control

The implementation must use a documented and tested concurrency-control strategy.

Possible mechanisms include:
- A distributed lock with safe ownership, expiry, and release behavior.
- Database transactions with an appropriate isolation level.
- Database-level constraints or another database-enforced strategy where suitable.

The selected mechanism must be compatible with the actual database provider and schema.

**Important:** A Redis lock does not automatically guarantee database consistency. Lock expiry, process crashes, multiple application instances, and requests that bypass the lock must be considered.

### 7.5 Transaction Requirements

Where reservation persistence and Outbox persistence use the same database, they must participate in the same transaction when atomicity is required.

If the operation fails before commit:
- The reservation insert must roll back.
- The Outbox insert must roll back.
- The system must not report the reservation as successfully completed.

External network calls, including Kafka publishing, must not be performed while holding the database transaction open.

### 7.6 Conflict Response

When a reservation conflicts with an existing reservation:
- Do not create the new reservation.
- Do not create an Outbox event representing a successful reservation.
- Preserve idempotency consistency according to the defined failure policy.
- Return the error response specified by `docs/api-contract.md`.

The current exception middleware maps `ROOM_CONFLICT` to HTTP `409 Conflict`. Keep the documentation and tests aligned with that mapping unless the API contract is intentionally changed.

### 7.7 Required Tests

The implementation must verify:

- Two reservations with overlapping dates cannot both succeed.
- Reservations sharing only a checkout/check-in boundary can both succeed.
- Concurrent requests for the same room do not produce overlapping committed reservations.
- A failed reservation does not leave partial reservation or Outbox data.
- Lock acquisition failure does not permit unprotected processing.
- Duplicate message delivery does not create a second reservation.
- Reservations in non-blocking states follow the documented status policy.

Concurrency tests must exercise the actual database and relevant locking strategy. Unit tests or mocked repository tests alone are not sufficient evidence of database-level concurrency safety.

### 7.8 Completion Criteria

This section is complete only when the conflict predicate, participating reservation statuses, transaction boundary, concurrency-control mechanism, and expected error response are documented and verified against the implementation.

---


## 8. Idempotency Rules (Mandatory)

### 8.1 Purpose

Idempotency ensures that retrying the same logical reservation request does not create duplicate reservations.

The idempotency mechanism must remain safe under:
- Repeated HTTP requests.
- Client retries after a timeout.
- Duplicate Kafka message delivery.
- Consumer restarts and message redelivery.
- Concurrent requests using the same idempotency key.

### 8.2 Key Requirements

Every reservation request must provide a non-empty idempotency key.

The system must:
- Identify repeated requests using the defined idempotency scope.
- Persist enough information to determine whether a request is a duplicate.
- Associate the key with a canonical representation or fingerprint of the request payload.
- Reject reuse of the same key with a different logical payload.
- Prevent concurrent processing of the same logical request from creating duplicate reservations.

The idempotency scope must be explicitly defined. It should include the authenticated user identity so that unrelated users cannot unintentionally share the same request identity.

### 8.3 State Model

The following table defines the target state behavior.

| State | Required behavior |
|---|---|
| No record | Register the request for processing |
| `PENDING` | Do not start duplicate processing; report that processing is in progress |
| `COMPLETED` | Return or expose the previously recorded result |
| `FAILED` | Apply the explicitly defined retry policy |

State transitions must be atomic or otherwise protected against concurrent updates.

### 8.4 Same Key, Different Payload

If a request uses an existing idempotency key with a different logical payload, the system must reject it.

The payload comparison must include all fields that affect reservation semantics, including:
- Authenticated user identity.
- Room identifier.
- Check-in date.
- Check-out date.

The comparison must use normalized values so that equivalent representations do not produce inconsistent results.

The error must follow the existing API contract. The current exception middleware maps `IDEMPOTENCY_KEY_CONFLICT` to `IDEMPOTENCY_CONFLICT` with HTTP `409 Conflict`.

### 8.5 Retry After Failure

A `FAILED` record must not automatically be reset to `PENDING` without a defined recovery policy.

Before retrying, the system must determine:
- Whether the previous attempt produced any committed reservation.
- Whether an Outbox event was committed.
- Whether the failure is retryable.
- Whether the original request payload matches the stored payload.
- Whether retrying could duplicate an external side effect.

A retry must preserve the original logical request identity.

### 8.6 Atomicity and Concurrency

The idempotency mechanism must prevent two concurrent requests from both claiming the same new key and independently creating reservations.

Use an appropriate database uniqueness constraint, atomic insert/update operation, transaction, or equivalent concurrency-safe mechanism.

The system must also handle the case where the database commit succeeds but the caller or Kafka consumer does not receive confirmation.

A repeated attempt must discover the committed result instead of creating a second reservation.

### 8.7 Cached Results

When a request is already `COMPLETED`, the system may return its previously recorded result according to the API contract.

The cached result must:
- Belong to the same idempotency scope.
- Correspond to the same logical payload.
- Not expose another user's reservation data.
- Remain consistent with the committed reservation.

### 8.8 Required Tests

The implementation must test:
- Missing or empty idempotency keys.
- Repeated requests with the same key and payload.
- Reuse of a key with a different room.
- Reuse of a key with different dates.
- Reuse of a key by a different authenticated user.
- Concurrent requests using the same key.
- Duplicate Kafka delivery.
- Retry after a failed attempt.
- Retry after a successful database commit with lost confirmation.
- Consistency between reservation, idempotency, and Outbox records.

Tests involving uniqueness, transactions, or concurrency must use the relevant real persistence infrastructure rather than relying exclusively on mocks.

### 8.9 Completion Criteria

Idempotency is complete only when the key scope, payload comparison, uniqueness enforcement, state transitions, retry policy, cached-result behavior, and failure recovery have been verified against the implementation.

Do not claim exactly-once processing merely because an idempotency key exists. The required outcome is that repeated delivery cannot create duplicate logical reservations.

---

## 9. Kafka Rules (Mandatory)

### 9.1 Purpose

Kafka transports reservation-processing requests and asynchronous events between system components.

Message production and consumption must be designed to tolerate failures, duplicate delivery, and consumer restarts.

Kafka message delivery does not replace database transactions, idempotency, or reservation conflict protection.

### 9.2 Topic Definitions

The target reservation-processing design uses the following topics:

| Topic | Purpose |
|---|---|
| `reservation-requests` | Primary reservation-processing requests |
| `reservation-requests-retry` | Requests awaiting retry, if a separate retry topic is implemented |
| `reservation-requests-dlq` | Requests that have exhausted the permitted retry policy |

These names are architectural requirements until verified against the actual Kafka configuration. Do not assume that all three topics have already been created or configured.

### 9.3 Message Key

The reservation request message should use `roomId` as its Kafka key when room-based partition ordering is required.

This can help route messages for the same room to the same partition. However, partition ordering alone does not guarantee reservation consistency across consumers, database transactions, or other processing paths.

Database-level concurrency protection remains mandatory.

### 9.4 Message Contract

Reservation messages must contain the information required to process the original logical request safely.

The contract should define:
- A stable request or idempotency identifier.
- The authenticated user's identity.
- The room identifier.
- Check-in and check-out dates.
- A correlation identifier where applicable.
- A schema or message version when versioned contracts are required.

The consumer must validate incoming messages before processing them.

Message contracts must be versioned and updated consistently across producers, consumers, and relevant tests.

### 9.5 Delivery Semantics

The system must tolerate at-least-once delivery.

Consequently:
- A message may be delivered more than once.
- A consumer may fail after committing database changes but before acknowledging a message.
- A message may be redelivered after a consumer restart.
- Duplicate delivery must not create duplicate logical reservations.

Consumers must use the idempotency mechanism and persisted reservation state to determine whether processing is still required.

Do not claim exactly-once business processing solely because Kafka producer or consumer settings provide exactly-once features.

### 9.6 Retry Policy

The target policy permits a maximum of three retry attempts after the initial processing attempt.

The implementation must explicitly define whether the configured value counts retries or total attempts.

Required behavior:
1. Identify whether the failure is retryable.
2. Apply a bounded retry policy.
3. Use an appropriate delay or backoff strategy where configured.
4. Avoid infinite retry loops.
5. Route exhausted or non-retryable messages to the DLQ according

---

## 10. SLA and Asynchronous Behavior

### 10.1 Asynchronous Processing

The reservation endpoint accepts requests for asynchronous processing.

The API must distinguish between:
- **Request acceptance:** The API has accepted the request for processing.
- **Reservation completion:** The reservation has been successfully committed.
- **Reservation failure:** Processing has failed and the failure has been recorded according to the defined policy.

An HTTP `202 Accepted` response indicates acceptance only. It must not be interpreted as confirmation of a completed reservation.

### 10.2 API Response Time

The target API response time must be defined separately from the background-processing duration.

The API should return the acceptance response promptly after the required request-acceptance operations have succeeded.

The initial response must not wait for the entire reservation workflow to finish.

**Target:** An initial response time of no more than three seconds may be used as a performance objective, subject to measurement under the documented test environment.

This target is not a guaranteed production SLA until it has been measured, validated, and formally adopted.

### 10.3 Background Processing Time

Background processing includes the operations required to validate and persist the reservation, update idempotency state, and commit the corresponding Outbox record.

The completion time may vary because of:
- Database load and transaction contention.
- Redis lock contention.
- Kafka availability and consumer lag.
- Retry delays.
- Infrastructure failures.

The project must define and measure a separate background-processing objective. The three-second API response target must not automatically be treated as a three-second reservation-completion guarantee.

### 10.4 Status Endpoint

Clients can retrieve the operation status through:

```http
GET /api/v1/Reservations/status/{requestId}
```

The endpoint must:
- Verify the authenticated user's access to the requested operation.
- Return the recorded status according to the API contract.
- Avoid reporting completion before the reservation transaction commits.
- Expose failure information only according to the defined public error contract.

Clients should use a bounded polling strategy rather than repeatedly querying the endpoint without delay.

Polling intervals and client-side timeouts should be configurable where appropriate.

### 10.5 Timeouts and Retries

The implementation must define appropriate timeouts for:
- Database operations.
- Redis lock acquisition.
- Kafka production and consumption.
- Background-processing attempts.
- Client status polling.

Retries must be bounded and must not bypass idempotency or concurrency protections.

A timeout does not prove that an operation failed. Before retrying, the system must account for the possibility that the original operation committed successfully but its confirmation was lost.

### 10.6 Performance Measurements

At minimum, measure the following separately:

| Measurement | Meaning |
|---|---|
| API response latency | Time from request receipt to HTTP response |
| Reservation processing latency | Time from accepted request to terminal reservation state |
| Kafka consumer lag | Delay between message availability and consumption |
| Database transaction duration | Time spent executing the relevant transaction |
| Retry count | Number of retry attempts |
| Failure rate | Proportion of requests that fail |
| Timeout rate | Proportion of operations that exceed configured time limits |

Measurements must use consistent timestamps and correlation identifiers where applicable.

### 10.7 SLA Verification

Before claiming that the SLA is met:
- Define the measurement environment and workload.
- Run representative load tests.
- Measure latency percentiles, including p95 and p99 where appropriate.
- Test under concurrent reservation requests.
- Include failure and retry scenarios.
- Record the results and accepted thresholds.

An unmeasured target must be documented as a target, not as a verified guarantee.

### 10.8 Completion Criteria

This section is complete when API latency and background-processing objectives are separately defined, timeouts and retries are documented, status polling behavior is specified, and the relevant performance measurements can be collected.

---

## 11. Error Handling (Must Match Actual Middleware)

### 11.1 General Rules

All API errors must follow the established exception-handling mechanism.

The system must:
- Return the HTTP status associated with the actual exception mapping.
- Use stable machine-readable error codes.
- Provide safe, meaningful error messages.
- Avoid exposing stack traces, credentials, internal connection details, or sensitive implementation information.
- Preserve correlation information in logs where available.

The exception middleware is the source of truth for the current global exception-to-response mappings.

### 11.2 Current Exception Mapping

The following mappings reflect the current `ExceptionMiddleware` implementation verified during documentation review.

| Exception or error condition | Public error code | HTTP status |
|---|---|---:|
| Invalid date range (`INVALID_DATE_RANGE`) | `INVALID_DATE_RANGE` | 400 |
| Missing idempotency key (`IDEMPOTENCY_KEY_REQUIRED`) | `IDEMPOTENCY_KEY_REQUIRED` | 400 |
| Room reservation conflict (`ROOM_CONFLICT`) | `ROOM_CONFLICT` | 409 |
| Room lock unavailable (`ROOM_LOCKED`) | `ROOM_LOCKED` | 409 |
| Request already being processed (`IN_PROGRESS`) | `IN_PROGRESS` | 409 |
| Idempotency key reused with a different payload (`IDEMPOTENCY_KEY_CONFLICT`) | `IDEMPOTENCY_CONFLICT` | 409 |
| Resource not found (`NOT_FOUND`) | `NOT_FOUND` | 404 |
| Invalid credentials | `INVALID_CREDENTIALS` | 401 |
| Invalid refresh token | `INVALID_REFRESH_TOKEN` | 401 |
| User not found during authentication | `AUTHENTICATION_FAILED` | 401 |
| User already exists | `USER_ALREADY_EXISTS` | 409 |
| Unhandled exception | `INTERNAL_ERROR` | 500 |

These mappings must be re-verified whenever `ExceptionMiddleware` changes.

### 11.3 General Error Response

The middleware's general error response uses the following shape:

```json
{
  "success": false,
  "code": "ROOM_CONFLICT",
  "message": "..."
}
```

The message shown above is illustrative. The actual message depends on the exception mapping.

Clients must use the machine-readable `code` for application behavior rather than relying on the human-readable message.

### 11.4 Special Response Cases

The middleware also defines special responses for certain exceptions.

**Previously completed request**

HTTP `200 OK`:

```json
{
  "status": "COMPLETED",
  "response": {}
}
```

The `response` value represents the cached result. The empty object above is illustrative, not a guarantee of the actual payload shape.

**Previously failed reservation request**

HTTP `409 Conflict`:

```json
{
  "success": false,
  "code": "RESERVATION_FAILED",
  "message": "This reservation request previously failed.",
  "previousResponse": {}
}
```

The `previousResponse` value depends on the stored result.

These special responses must be tested separately from the general error response.

### 11.5 HTTP Status Consistency

The current middleware maps `IN_PROGRESS` to `409 Conflict`, not `202 Accepted`.

Do not change this behavior merely by editing documentation. If the API is intentionally changed to return `202` for repeated in-progress requests, update the middleware, API contract, client expectations, and automated tests together.

Likewise, do not introduce undocumented codes such as `INVALID_DATE`, `CONFLICT`, or `SYSTEM_ERROR` when the current contract uses different codes.

### 11.6 Authentication and Authorization

Authentication failures must not reveal whether a sensitive account or credential exists beyond the behavior explicitly required by the authentication contract.

Reservation status requests must be scoped to the authenticated user. A request must not expose another user's reservation data.

The controller's explicit `Unauthorized()` responses and the middleware's authentication exception mappings must both be covered by tests.

### 11.7 Logging and Diagnostics

For each relevant failure, record structured diagnostic information where available:
- Correlation ID.
- Request or idempotency identifier.
- Operation or processing stage.
- Error category.
- Exception details in protected server-side logs.
- Retry attempt or DLQ status where applicable.

Do not return internal exception details to clients. Avoid logging access tokens, passwords, or unnecessary personal information.

### 11.8 Required Tests

Verify:
- Each documented exception maps to the correct HTTP status and error code.
- General and special error response shapes remain stable.
- Invalid authentication is handled correctly.
- Users cannot query another user's reservation status.
- Unexpected exceptions produce a safe `500` response.
- Error handling does not leave inconsistent reservation, idempotency, or Outbox state.

### 11.9 Completion Criteria

Error handling is complete only when the documented mappings match the middleware, response bodies match the API contract, sensitive details are protected, and automated tests cover the expected mappings.

---

## 12. Forbidden Actions (Strict)

The following actions are prohibited because they can compromise reservation consistency, security, or system reliability.

### 12.1 API Layer Violations

Developers MUST NOT:
- Insert, update, or delete reservation records directly from a controller.
- Implement reservation business rules inside controllers.
- Trust a client-supplied `userId` instead of the authenticated identity.
- Access Kafka or Redis directly from the controller.
- Return a successful reservation-completion response before the operation has committed.

### 12.2 Reservation Processing Violations

Developers MUST NOT:
- Create reservations outside the established reservation-processing coordinator.
- Bypass idempotency checks.
- Ignore an existing completed request.
- Allow the same idempotency key to represent different logical payloads.
- Skip the authoritative reservation-conflict check.
- Continue processing after a required lock acquisition fails.
- Assume that a Redis lock alone guarantees database consistency.
- Mark a reservation as completed before its transaction commits.

### 12.3 Transaction and Messaging Violations

Developers MUST NOT:
- Publish Kafka messages directly inside the reservation database transaction.
- Assume a database transaction automatically includes Kafka or Redis operations.
- Commit reservation data without the required Outbox record when the design requires atomic Outbox persistence.
- Mark an Outbox event as published before the configured publishing protocol permits it.
- Acknowledge a Kafka message prematurely in a way that can lose work.
- Implement unbounded or infinite retries.
- Replay failed messages while bypassing idempotency or conflict checks.

### 12.4 Database Violations

Developers MUST NOT:
- Ignore database transaction failures.
- Swallow exceptions and report success.
- Perform a conflict check followed by an unprotected reservation insert.
- Introduce schema changes without the required migration and review process.
- Assume that unit tests alone prove database-level concurrency safety.

### 12.5 Security Violations

Developers MUST NOT:
- Commit passwords, access tokens, connection strings, or other secrets to source control.
- Return stack traces or internal exception details to API clients.
- Expose one user's reservation information to another user.
- Log credentials or unnecessary sensitive data.
- Disable authentication or authorization checks merely to simplify development or testing.

### 12.6 Contract and Documentation Violations

Developers MUST NOT:
- Change API routes, response bodies, error codes, or status mappings without reviewing the API contract.
- Claim that a feature is implemented when only its interface or documentation exists.
- Claim that tests passed without executing them and reviewing the results.
- Introduce undocumented architectural changes without review.
- Treat proposed target behavior as verified implementation behavior.

### 12.7 Pull Request Enforcement

A pull request MUST be rejected or returned for correction if it:
- Violates a mandatory rule in this document.
- Introduces a duplicate-reservation or idempotency regression.
- Breaks the documented API contract without an approved change.
- Leaves a transaction or resource lock unsafe on failure paths.
- Introduces an unbounded retry loop.
- Exposes secrets or weakens required access controls.
- Lacks required tests for its changes.

Exceptions require explicit review and documented approval. A documentation change alone does not authorize a change in runtime behavior.

### 12.8 Verification

Reviewers must verify these rules against the changed code, relevant tests, configuration, and database behavior.

A rule is not considered satisfied merely because the pull request description states that it is satisfied.

---

## 13. Observability (Mandatory)

### 13.1 Purpose

The system must provide sufficient observability to investigate reservation failures, processing delays, duplicate requests, concurrency conflicts, and infrastructure problems.

Observability consists of structured logs, measurable metrics, and correlation across the API and background-processing components.

Requirements in this section must be verified against the actual logging and metrics configuration before being marked as implemented.

### 13.2 Structured Logging

Reservation-related logs should use structured fields rather than relying exclusively on free-form text.

Example:

```json
{
  "event": "reservation_attempt",
  "requestId": "abc-123",
  "roomId": 1,
  "step": "GRC_START",
  "correlationId": "correlation-example"
}
```

This is an illustrative log event, not a guarantee of the current logging output.

Where applicable, log:
- Event name.
- Request or idempotency identifier.
- Correlation ID.
- Room identifier.
- Processing component.
- Processing stage.
- Outcome.
- Error category.
- Retry attempt.
- Elapsed time.

Use consistent field names across the API, Kafka consumer, coordinator, and Outbox worker.

### 13.3 Correlation ID

A correlation ID should connect the relevant operations belonging to the same logical request.

The system must:
- Propagate the correlation ID through the applicable processing stages.
- Include it in relevant structured logs.
- Preserve it in Kafka messages where the message contract supports it.
- Avoid generating unrelated identifiers for each stage when the original correlation ID is available.

The existing controller reads the correlation ID from `HttpContext.Items["CorrelationId"]`. The middleware that creates and propagates this value must be verified separately.

Do not use a correlation ID as a replacement for the idempotency key or as an authorization mechanism.

### 13.4 Required Metrics

The target metrics include:

| Metric | Purpose |
|---|---|
| `request_count` | Count incoming reservation requests |
| `error_count` | Count failed operations |
| `latency_ms` | Measure relevant operation latency |
| `retry_count` | Count retry attempts |
| `dlq_count` | Count messages routed to the dead-letter queue |

Metric names are illustrative and may be adapted to the project's monitoring convention.

The implementation should also measure:
- API response latency.
- Reservation-processing duration.
- Reservation conflict count.
- Idempotency conflict count.
- Redis lock acquisition failures.
- Kafka consumer lag.
- Outbox publishing failures.
- Database transaction duration.

Where appropriate, metrics should distinguish success, failure, and retry outcomes.

### 13.5 Measurement Rules

Metrics must have clear definitions.

For example:
- `request_count` should specify whether it counts HTTP requests or unique logical reservation operations.
- `error_count` should define which failures are included.
- `latency_ms` should identify the measured operation and its start/end boundaries.
- `retry_count` should distinguish retry attempts from initial attempts.
- `dlq_count` should count messages actually routed to the DLQ, not merely messages that failed once.

Avoid unbounded metric labels such as raw request IDs, correlation IDs, or arbitrary exception messages. Use logs for per-request investigation and metrics for aggregated measurements.

### 13.6 Logging and Security

Logs must not contain:
- Passwords or authentication tokens.
- Database connection strings or secrets.
- Unnecessary personal information.
- Sensitive request payloads that are not required for diagnosis.

Client-facing error messages must not expose internal exception details. Detailed diagnostic information should remain in appropriately protected server-side logs.

### 13.7 Failure Investigation

For a failed reservation, authorized developers should be able to trace:
1. The initial API request.
2. The acceptance response.
3. The corresponding Kafka processing attempt, where applicable.
4. The coordinator's outcome.
5. The database transaction result.
6. The Outbox publication result, where applicable.
7. Any retry or dead-letter handling.

The investigation must distinguish between a failed request, a failed processing attempt, and a reservation that was committed successfully but whose confirmation was lost.

### 13.8 Required Verification

Verify that:
- Structured logs are emitted at important processing stages.
- Correlation IDs propagate correctly.
- Exceptions are logged without leaking secrets.
- Required metrics are registered and updated.
- Metrics distinguish API latency from background-processing latency.
- Retry and DLQ events are observable.
- Tests or integration checks validate critical instrumentation behavior.

### 13.9 Completion Criteria

Observability is complete only when the required log fields and metric definitions are documented, the relevant instrumentation is implemented, and representative success and failure scenarios can be investigated using the available telemetry.

---

## 14. Required Methods and Responsibilities

### 14.1 Purpose

This section defines the required operations for the asynchronous reservation workflow.

Method names and signatures must match the actual implementation. The signatures below are logical contracts, not a claim that identically named methods already exist.

### 14.2 Reservation Request Acceptance

**Logical operation:**

```csharp
Task<CreateReservationResult> CreateReservationAsync(
    CreateReservationRequest request);
```

**Responsibilities**
- Coordinate initial request validation.
- Apply the idempotency policy.
- Initiate asynchronous processing according to the configured architecture.
- Return the result required by the API contract.

The operation must not report reservation completion merely because the request was accepted.

### 14.3 Background Request Processing

**Logical operation:**

```csharp
Task ProcessReservationAsync(
    ReservationMessage message);
```

**Responsibilities**
- Process an incoming reservation message.
- Handle duplicate delivery safely.
- Check whether further processing is required.
- Invoke the critical reservation-processing operation.
- Propagate failures to the configured retry and dead-letter mechanism.

### 14.4 Critical Reservation Execution

**Logical operation:**

```csharp
Task ExecuteReservationAsync(
    ReservationCommand command);
```

**Responsibilities**
- Coordinate concurrency protection.
- Re-check reservation eligibility and idempotency.
- Detect reservation conflicts.
- Persist the reservation and required Outbox record atomically where they share the same database transaction.
- Maintain consistent idempotency state.
- Release acquired resources safely on success and failure.

### 14.5 Supporting Operations

The implementation may also require dedicated operations for:
- Acquiring and releasing room locks.
- Creating and retrieving idempotency records.
- Publishing and consuming Kafka messages.
- Persisting and publishing Outbox events.
- Retrieving reservation processing status.
- Recording retry and failure information.

These responsibilities may be implemented through different classes or method signatures. Avoid introducing duplicate abstractions when an existing component already provides the required behavior.

### 14.6 Method Design Rules

Developers MUST:
- Use asynchronous APIs for I/O-bound operations.
- Propagate cancellation where appropriate.
- Avoid swallowing exceptions that affect processing correctness.
- Keep responsibilities clear between API, application, domain, and infrastructure components.
- Use explicit result types where they improve error handling and readability.
- Avoid returning success before the relevant operation has reached its documented success boundary.

Developers MUST NOT:
- Add methods solely to match this document when an existing design already satisfies the requirement.
- Introduce a second reservation-processing path that bypasses idempotency or concurrency protection.
- Treat method existence as proof that its required behavior is correctly implemented.

### 14.7 Verification Checklist

Before marking this section complete, verify:

- [ ] The actual application service entry point.
- [ ] The actual Kafka consumer processing method.
- [ ] The actual reservation coordinator entry point.
- [ ] The status retrieval operation.
- [ ] The idempotency persistence operations.
- [ ] The Outbox persistence and publishing operations.
- [ ] Error propagation and cancellation behavior.
- [ ] Tests for the responsibilities of each critical operation.

Record actual class names and signatures in the relevant architecture documentation after verifying the source code.

---

## 15. Definition of Done (Mandatory)

A reservation-related feature is considered complete only when all applicable requirements below have been satisfied.

### 15.1 Implementation
- [ ] Implementation follows the documented architecture.
- [ ] API behavior matches `docs/api-contract.md`.
- [ ] Request and business-rule validation are implemented.
- [ ] Idempotency behavior is implemented and verified.
- [ ] Reservation conflict protection is implemented.
- [ ] Database transaction boundaries are correct.
- [ ] Required Kafka and Outbox behavior is implemented.
- [ ] Errors are handled according to the established contract.
- [ ] Configuration and secrets are handled securely.

### 15.2 Testing
- [ ] Relevant unit tests pass.
- [ ] Relevant integration tests pass.
- [ ] Database-dependent behavior is tested against the appropriate database provider.
- [ ] Idempotency and duplicate-message scenarios are tested.
- [ ] Reservation conflict and concurrency scenarios are tested where applicable.
- [ ] Failure and recovery paths are tested.
- [ ] Existing tests continue to pass.

### 15.3 Observability and Documentation
- [ ] Relevant structured logging is implemented.
- [ ] Required metrics are available or explicitly tracked as outstanding work.
- [ ] API and architecture documentation reflect the actual behavior.
- [ ] Configuration changes are documented.
- [ ] Known limitations are recorded.

### 15.4 Review and Integration
- [ ] The pull request has a clear scope and description.
- [ ] Code review requirements are satisfied.
- [ ] No unintended files or secrets are included.
- [ ] Required CI checks pass.
- [ ] The change is integrated according to the team's branch and merge policy.

A checklist item must not be marked complete without supporting implementation or test evidence.

---

## 16. Final Engineering Principles

The following principles govern reservation-related implementation decisions.

### 16.1 Correctness Before Convenience

Do not simplify the implementation by bypassing validation, idempotency, authorization, transactions, or concurrency protection.

### 16.2 Database Consistency

The database must remain the authoritative source for committed reservation state. Application-level checks and distributed locks must not replace the necessary database consistency guarantees.

### 16.3 Explicit Failure Handling

Every critical operation must define its success boundary and failure behavior. Exceptions must not be silently ignored when they affect reservation correctness.

### 16.4 Safe Retries

Retries must be bounded and safe to repeat. A timeout or lost acknowledgment must not automatically be interpreted as proof that the original operation failed.

### 16.5 Contract Stability

API routes, DTOs, response bodies, status codes, and error codes must remain consistent with the documented contract. Breaking changes require explicit review and corresponding test and documentation updates.

### 16.6 Evidence-Based Completion

A design decision, method signature, or documentation statement does not prove that a feature is implemented. Completion requires verification against the source code, configuration, and appropriate tests.

---

## 17. Expected System Properties

The reservation workflow is designed to achieve the following properties.

### 17.1 Duplicate Protection

Repeated requests and duplicate message delivery must not create duplicate logical reservations.

### 17.2 Concurrency Safety

Concurrent requests must not create overlapping committed reservations for the same room when the business rules prohibit that overlap.

### 17.3 Transactional Consistency

Reservation persistence, idempotency state, and the required Outbox record must remain consistent across transaction success and failure.

### 17.4 Recoverable Asynchronous Processing

Transient failures, process restarts, and message redelivery must be handled without losing accepted work or creating duplicate reservations.

### 17.5 Observable Execution

Developers must be able to investigate processing outcomes and relevant failures using structured logs, correlation identifiers, and available metrics.

### 17.6 Secure Access

Users must only access reservation information they are authorized to access. Client-supplied identity values must not override the authenticated identity.

### 17.7 Verified Performance

API response latency and background-processing latency must be measured separately. Performance targets must not be described as guarantees until validated.

**Important:** These are required system properties, not claims that every property has already been achieved. Verification evidence must be recorded before declaring the system production-ready.

---

## 18. Enforcement and Change Control

This document is the implementation contract for the reservation workflow.

### 18.1 Pull Request Enforcement

A pull request may be rejected or returned for changes when it:
- Violates a mandatory requirement.
- Introduces a duplicate-reservation or data-consistency risk.
- Bypasses required authentication or authorization.
- Breaks the API contract without an approved change.
- Introduces unsafe transaction, locking, retry, or message-acknowledgment behavior.
- Lacks required tests or supporting evidence.
- Claims functionality that is not implemented or verified.

### 18.2 Contract Changes

Changes to this document must be reviewed when they affect:
- Reservation business rules.
- API request or response contracts.
- Idempotency semantics.
- Transaction boundaries.
- Concurrency-control strategy.
- Kafka message contracts or retry behavior.
- Outbox delivery semantics.
- Security requirements.
- Performance targets or operational guarantees.

Related documentation and automated tests must be updated as part of the same change where applicable.

### 18.3 Handling Conflicts

If this document conflicts with the current source code, API contract, or architecture decisions:

1. Identify the conflicting requirements.
2. Determine whether the code or documentation is outdated.
3. Propose the intended behavior.
4. Obtain the required review and approval.
5. Update the implementation, tests, and documentation consistently.

Do not silently change runtime behavior to match an unreviewed documentation edit.

### 18.4 Final Rule

**Do not improvise around critical correctness requirements.**

Follow the approved contract, verify the implementation, test failure scenarios, and document any intentional deviation before considering the feature complete.

---

## 19. Implementation Verification Matrix

### 19.1 Purpose

Every critical requirement must be traceable to its implementation, verification method, and current status.

A requirement must not be marked as implemented solely because it appears in this document.

### 19.2 Verification Matrix

| Requirement | Implementation to verify | Required evidence |
|---|---|---|
| Authentication | Authentication configuration and reservation controller | Unauthorized-request tests |
| User identity | `ReservationsController` and authentication claims | Tests proving client-supplied identity cannot override the authenticated user |
| Request validation | Request DTOs and validation components | Invalid-request tests |
| Date validation | Reservation application/domain logic | Date-boundary tests |
| Room availability | Room persistence and reservation-processing logic | Missing-room and inactive-room tests |
| Idempotency | Idempotency service and persistence layer | Duplicate-key and payload-conflict tests |
| Reservation conflict | Coordinator and database operations | Overlap and concurrency tests |
| Redis lock | Lock service and coordinator | Acquisition-failure and release tests |
| Database transaction | Reservation persistence and coordinator | Rollback and consistency tests |
| Kafka processing | Producer, consumer, and configuration | Delivery, redelivery, and failure tests |
| Outbox | Outbox persistence and worker | Atomicity and recovery tests |
| Retry and DLQ | Consumer configuration and error handling | Retry-limit and dead-letter tests |
| Status endpoint | Reservation controller and application service | Status and authorization tests |
| Error responses | `ExceptionMiddleware` | HTTP status and response-contract tests |
| Observability | Logging, correlation middleware, and metrics | Log and instrumentation verification |
| Performance | API and background-processing pipeline | Load-test results and latency measurements |

The listed component names are starting points for inspection. Their existence and exact paths must be confirmed against the repository.

### 19.3 Verification Status

Use the following statuses when tracking requirements:

- `NOT_VERIFIED` — Evidence has not yet been collected.
- `IN_PROGRESS` — Implementation or verification is underway.
- `PASS` — Required behavior has been verified.
- `FAIL` — Verification exposed a defect.
- `NOT_APPLICABLE` — The requirement does not apply, with a documented reason.

Do not mark a requirement `PASS` based only on a code review when runtime or integration evidence is required.

### 19.4 Evidence Requirements

Verification evidence should identify:
- The requirement being verified.
- The relevant source files or configuration.
- The test or verification procedure.
- The observed result.
- Any known limitations.
- The date or CI run associated with the evidence, where appropriate.

Sensitive information must not be included in evidence.

### 19.5 Completion Criteria

The verification matrix is complete when every critical reservation requirement has a defined verification method and its current status is supported by evidence.

---

## 20. Release Readiness Checklist

### 20.1 Code and Integration

- [ ] All intended changes are committed and reviewed.
- [ ] Required pull requests are approved.
- [ ] No unintended files or local artifacts are included.
- [ ] The intended branch contains the verified changes.
- [ ] Build and CI checks pass.

### 20.2 API and Security

- [ ] Authentication and authorization checks pass.
- [ ] The API contract matches the implementation.
- [ ] User identity is obtained from the authenticated context.
- [ ] Error responses do not expose internal details.
- [ ] Secrets are supplied through approved configuration mechanisms.

### 20.3 Reservation Correctness

- [ ] Date and room validation pass.
- [ ] Idempotency behavior passes its required tests.
- [ ] Reuse of a key with a different payload is rejected.
- [ ] Reservation conflict tests pass.
- [ ] Required concurrency tests pass.
- [ ] Database rollback behavior is verified.
- [ ] Reservation and Outbox consistency is verified.

### 20.4 Messaging and Recovery

- [ ] Kafka configuration and topic names are verified.
- [ ] Duplicate message delivery is handled safely.
- [ ] Retry limits and DLQ routing are verified.
- [ ] Consumer acknowledgment behavior is reviewed.
- [ ] Outbox recovery after failure or restart is tested.
- [ ] Recovery procedures are documented.

### 20.5 Infrastructure and Operations

- [ ] Required environment variables are documented.
- [ ] Database migrations have been reviewed.
- [ ] Deployment configuration has been checked.
- [ ] Required dependencies and health checks work.
- [ ] Logging and correlation IDs are verified.
- [ ] Required metrics are available or their absence is explicitly documented.
- [ ] Backup and recovery procedures are reviewed where applicable.

### 20.6 Performance and Final Approval

- [ ] API response latency has been measured.
- [ ] Background-processing latency has been measured separately.
- [ ] Representative concurrent workload tests have been executed.
- [ ] Known limitations and outstanding risks are documented.
- [ ] Required reviewers have approved the release.

### 20.7 Release Decision

A release must not be described as production-ready when a critical correctness, security, or recovery requirement remains unverified.

Outstanding non-critical items must be documented with an owner, impact assessment, and planned resolution.

The release decision must be based on actual test results, configuration review, and operational readiness—not solely on successful compilation.

---

## 21. Document Maintenance and Ownership

### 21.1 Ownership

The Tech Lead or designated maintainer is responsible for coordinating changes to this implementation contract.

All contributors are responsible for identifying documentation that becomes outdated as a result of their changes.

### 21.2 When Updates Are Required

Update this document and related documentation when changes affect:
- Reservation processing flow.
- API request or response contracts.
- Authentication or authorization.
- Idempotency rules.
- Transaction or concurrency-control strategy.
- Kafka topics, message contracts, retry behavior, or DLQ handling.
- Outbox persistence or publishing behavior.
- Error mappings.
- Observability requirements.
- Performance targets or release criteria.

### 21.3 Review Process

Documentation changes must:
1. Identify the affected requirements.
2. Explain the reason for the change.
3. Distinguish verified behavior from proposed behavior.
4. Update related documents and tests where necessary.
5. Receive the same level of review appropriate to the risk of the associated implementation change.

### 21.4 Source of Truth

Use the following ownership boundaries:

- `docs/api-contract.md` — externally visible API behavior.
- `docs/architecture.md` — system components and their interactions.
- `docs/execution-guide.md` — implementation requirements and verification criteria.
- `docs/team-rules.md` — team workflow and collaboration rules.
- Automated tests — executable verification of expected behavior.
- Source code and runtime configuration — actual implemented behavior.

When these sources conflict, identify the discrepancy and resolve it through an explicit, reviewed change. Do not silently assume that documentation or code is automatically correct.

### 21.5 Versioning

Meaningful changes to critical requirements should be reviewed and traceable through version control.

For significant architectural or contract decisions, record the rationale in the appropriate architecture decision record (ADR).

### 21.6 Final Maintenance Rule

This document must evolve with the implementation.

Its purpose is to make expected behavior explicit, prevent unsafe assumptions, and provide verifiable criteria for development, review, testing, and release.

---