# API Contract — Hotel Reservation System

## 1. Purpose and Source of Truth

This document defines the API contract shared by the backend, frontend, QA, and AI coding assistants.

The current implementation, request/response models, middleware, and tests are the source of truth. Do not document planned behavior as implemented.

If this document conflicts with the code, report the mismatch and resolve it through a reviewed change.

## 2. Global Rules

- API routes use the `/api/v1/` prefix.
- Request and response bodies use JSON where applicable.
- Reservation endpoints require authentication.
- The authenticated user's ID must be obtained from the JWT claim named `userId`.
- Never trust a client-supplied user ID for authorization.
- Controllers must not contain business logic.
- Preserve existing idempotency and concurrency guarantees.
- Do not change routes, request models, response shapes, or HTTP statuses without reviewing affected consumers and tests.
- Never expose raw exception details, stack traces, or secrets to API clients.

## 3. Authentication API

Base route: `/api/v1/auth`

### 3.1 Register

`POST /api/v1/auth/register`

Request fields: `email`, `password`.

Success: `200 OK`

```json
{
  "success": true,
  "data": {
    "accessToken": "<access-token>",
    "refreshToken": "<refresh-token>"
  }
}
```

### 3.2 Login

`POST /api/v1/auth/login`

Request fields: `email`, `password`.

Success: `200 OK`, using the same response structure as registration.

### 3.3 Refresh Token

`POST /api/v1/auth/refresh`

Request:

```json
{
  "refreshToken": "<refresh-token>"
}
```

Success: `200 OK`, returning a new authentication response with access and refresh tokens.

### 3.4 Logout

`POST /api/v1/auth/logout`

Request:

```json
{
  "refreshToken": "<refresh-token>"
}
```

Success: `200 OK`

```json
{
  "success": true
}
```

## 4. Reservation API

Base route: `/api/v1/Reservations`

All endpoints in this section require authentication.

### 4.1 Create Reservation

`POST /api/v1/Reservations`

The request body uses `CreateReservationDto`.

Known fields include:

- `roomId`
- `userId`
- `checkIn`
- `checkOut`
- `idempotencyKey`
- `correlationId`

The server replaces `dto.UserId` with the authenticated user's ID. A client-supplied `userId` must not determine ownership.

The server assigns `CorrelationId` from the request's correlation middleware context.

#### Success Response

HTTP status: `202 Accepted`

```json
{
  "status": "PENDING",
  "requestId": "unique-request-key"
}
```

`requestId` is currently set from `dto.IdempotencyKey`.

The `202 Accepted` response confirms that the request was accepted for processing; it does not by itself confirm that the reservation has completed.

#### Validation and Idempotency

The current exception middleware defines these reservation errors:

- `INVALID_DATE_RANGE` — check-out must be later than check-in.
- `IDEMPOTENCY_KEY_REQUIRED` — an idempotency key is required.
- `ROOM_CONFLICT` — room already booked.
- `ROOM_LOCKED` — room is currently locked.
- `IN_PROGRESS` — request is already being processed.
- `IDEMPOTENCY_CONFLICT` — the same key was used with a different request.
- `RESERVATION_FAILED` — a previous request failed.

Use the same idempotency key when retrying the same logical request. Do not silently reuse that key for a different payload.

### 4.2 Get Reservation Status

`GET /api/v1/Reservations/status/{requestId}`

The server:
1. Reads the `userId` claim from the authenticated principal.
2. Returns `401 Unauthorized` if the claim cannot be parsed as an integer.
3. Calls the status service with both `requestId` and the authenticated user's ID.
4. Returns the service result with `200 OK` when successful.

#### Response Model

The response uses `ReservationStatusResponse`:

```json
{
  "status": "IN_PROGRESS",
  "data": null
}
```

`data` is optional and may contain additional result information. Its exact structure depends on the service result and must be verified against the current implementation before frontend code assumes specific fields.

Known status concepts include `IN_PROGRESS`, `COMPLETED`, and `FAILED`. Do not assume every state occurs in every execution path.

## 5. Error Responses

### 5.1 Standard Middleware Error Shape

For errors handled by the general exception mapping:

```json
{
  "success": false,
  "code": "ERROR_CODE",
  "message": "Human-readable message"
}
```

### 5.2 Verified Error Mappings

| HTTP status | Error code | Meaning |
|---:|---|---|
| 400 | `INVALID_DATE_RANGE` | Invalid reservation date range |
| 400 | `IDEMPOTENCY_KEY_REQUIRED` | Missing idempotency key |
| 401 | `INVALID_CREDENTIALS` | Authentication failed |
| 401 | `INVALID_REFRESH_TOKEN` | Authentication failed |
| 401 | `AUTHENTICATION_FAILED` | Authentication failed |
| 404 | `NOT_FOUND` | Resource not found |
| 409 | `USER_ALREADY_EXISTS` | An account with this email already exists |
| 409 | `ROOM_CONFLICT` | Room already booked |
| 409 | `ROOM_LOCKED` | Room is currently locked |
| 409 | `IN_PROGRESS` | Request is already being processed |
| 409 | `IDEMPOTENCY_CONFLICT` | Same key used with a different request |
| 409 | `RESERVATION_FAILED` | The previous reservation request failed |
| 500 | `INTERNAL_ERROR` | Unexpected server error |

### 5.3 Special Responses

An already-completed idempotent operation may return `200 OK`:

```json
{
  "status": "COMPLETED",
  "response": "<cached-response>"
}
```

A previously failed reservation request may return `409 Conflict` with additional `previousResponse` data:

```json
{
  "success": false,
  "code": "RESERVATION_FAILED",
  "message": "This reservation request previously failed.",
  "previousResponse": "<cached-result>"
}
```

These special cases do not use exactly the same response shape as the general error mapping. Frontend code must handle them explicitly.

## 6. Security and Reliability Rules

- Reservation ownership is determined by the authenticated identity.
- Status queries must remain scoped to the authenticated user.
- Tokens and credentials must never be logged or returned in error messages.
- Idempotency keys must be validated and handled consistently.
- Duplicate-key and concurrency failures must not leak raw database exceptions.
- Do not remove transaction, outbox, Redis-lock, or idempotency protections to make a test pass.
- A mocked unit test does not prove database-level concurrency behavior. Use appropriate integration tests for SQL Server-specific guarantees.

## 7. Frontend Integration Rules

Frontend developers must:

1. Use the exact route and HTTP method documented here.
2. Send JSON with the required fields and correct types.
3. Attach the access token to protected requests using the configured bearer-token scheme.
4. Reuse the original idempotency key when retrying the same reservation.
5. Treat `202 Accepted` as acceptance for processing, not as final reservation success.
6. Use `requestId` to query reservation status.
7. Handle errors using both HTTP status and error code.
8. Never assume every error has a `data` property.
9. Never trust a user ID supplied by the frontend as proof of ownership.
10. Report discrepancies between Swagger, code, tests, and this document rather than adding undocumented workarounds.

## 8. Contract Change Procedure

Any contract change must include:

1. A reason for the change.
2. An assessment of affected backend, frontend, QA, and integration code.
3. Updated request/response models or controller behavior where necessary.
4. Tests for the changed behavior.
5. Updates to this document and relevant examples.
6. Pull-request review and approval.

Do not introduce breaking changes into `/api/v1/` silently. Consider a new API version when backward compatibility cannot be preserved.

## 9. Verification Checklist

- [ ] Verify all fields and validation rules in `CreateReservationDto`.
- [ ] Verify the exact status-service result and possible `data` payloads.
- [ ] Verify status lookup ownership behavior and not-found cases.
- [ ] Verify every exception mapping against tests.
- [ ] Verify registration, login, refresh, and logout failure responses.
- [ ] Verify same-key retries, conflicting payloads, and concurrent requests.
- [ ] Compare the documented behavior with Swagger and integration tests.

**Final principle:** The API contract describes verified behavior, not assumptions.
