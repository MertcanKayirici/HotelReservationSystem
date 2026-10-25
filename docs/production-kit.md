# Hotel Reservation System — Production Kit (Run & Verify Guide)

## 1. Purpose

Define a reproducible and verifiable process for configuring, running, and testing the Hotel Reservation System across development environments.

### Goals

- Reduce environment inconsistencies.
- Standardize infrastructure setup across developers.
- Make application startup repeatable.
- Verify configuration, database connectivity, and application behavior.
- Prevent undocumented local-only dependencies.

**Scope:** This guide covers infrastructure, configuration, migrations, seed data, startup, and verification. It does not authorize changes to business logic.

---

## 2. System Boundaries

This guide covers:

- Infrastructure setup using Docker Compose.
- Environment configuration.
- Database migrations and seed data.
- Application startup.
- Unit, integration, and load testing.
- API smoke testing.

It does not define reservation business rules. Those remain governed by the architecture and execution contracts.

> A reproducible local environment supports production readiness, but does not independently prove that the system is production-ready.

---

## 3. Docker Compose (Required)

**File:** `docker-compose.yml`

### Required services

| Service | Purpose |
|---|---|
| MSSQL | Primary relational database |
| Redis | Distributed locking and other configured Redis operations |
| Zookeeper | Kafka coordination, if required by the selected Kafka version |
| Kafka | Event streaming |

### Configuration requirements

The Compose configuration must:

- Use explicit image versions or controlled image tags.
- Load credentials from environment variables or an untracked local environment file.
- Avoid committing real credentials.
- Define service health checks where supported.
- Configure startup dependencies and readiness checks.
- Persist database data when required for local development.
- Keep internal container addresses separate from host-accessible addresses.
- Configure Kafka listeners correctly for both container-to-container and host-to-container communication.

The following is a **configuration checklist, not a replacement for the repository's existing Compose file**. Preserve the repository's actual Kafka version and listener configuration when applying these rules.

### Rules

- Start local infrastructure through Docker Compose.
- Do not require undocumented manual database or broker configuration.
- Do not assume a running container is ready to accept connections.
- Do not use floating `latest` image tags as the only version-control mechanism for reproducible environments.
- Document any intentional exception to containerized local infrastructure.

---

## 4. Environment Configuration

**Files:** `.env.example`, `docker-compose.yml`, and the API configuration files.

### Example `.env.example`

```env
MSSQL_SA_PASSWORD=ReplaceWithLocalDevelopmentSecret
DB_CONNECTION=Server=localhost,1433;Database=HotelDb;User Id=sa;Password=ReplaceWithLocalDevelopmentSecret;TrustServerCertificate=True
REDIS_CONNECTION=localhost:6379
KAFKA_BROKER=localhost:9092
ASPNETCORE_ENVIRONMENT=Development
```

This example is a template. The actual variable names must match the application's configuration bindings and the Compose file.

### Rules

- Never commit production secrets.
- Keep `.env.example` free of real credentials.
- Ensure local environment files are excluded from version control.
- Do not assume ASP.NET Core automatically maps arbitrary environment variable names to custom configuration keys.
- Use the application's established configuration mechanism consistently.
- Ensure containerized services use their internal service names and ports when communicating with one another.
- Keep host-side connection strings separate where host and container networking differ.

**Verification:** Confirm every required configuration key is actually read by the application and that missing required settings produce a clear startup failure.

---

## 5. Database Migrations (EF Core)

EF Core migrations are the standard mechanism for versioning database schema changes.

### Migration commands

Run commands from the repository root, adjusting the project and context names to match the actual solution:

```bash
dotnet ef migrations add InitialCreate \
  --project src/HotelReservationSystem.Infrastructure \
  --startup-project src/HotelReservationSystem.API
```

```bash
dotnet ef database update \
  --project src/HotelReservationSystem.Infrastructure \
  --startup-project src/HotelReservationSystem.API
```

If the `DbContext` is located in a different project, specify its actual location using `--project` and, when necessary, `--context`.

### Rules

- Commit migration files with the corresponding schema changes.
- Do not rely on undocumented manual schema modifications.
- Verify migration behavior against a clean development database.
- Do not automatically apply development migrations to production without an approved deployment process.
- Ensure the startup project can load the required configuration and construct the `DbContext`.

---

## 6. Seed Data

**Suggested location:** `Infrastructure/Persistence/SeedData.cs`

Use this location only if it matches the existing project structure.

### Purpose

Provide predictable development or test data, such as default rooms, where required by the test scenarios.

### Requirements

- Seed operations must be idempotent.
- Repeated execution must not create duplicate records.
- Seed data must not contain real personal information or production secrets.
- Seeded records must satisfy database constraints.
- Tests must not depend on uncontrolled shared database state.
- Production seeding must be explicitly controlled.

Seed data should be applied through a documented initialization mechanism. Do not assume the file exists or is automatically invoked.

---

## 7. Run Procedure

Execute the following steps in order. Wait for infrastructure readiness before applying migrations or starting the application.

### Step 1 — Start infrastructure

```bash
docker compose up -d
```

Check service status:

```bash
docker compose ps
```

Inspect logs if a service is unhealthy:

```bash
docker compose logs --tail=100
```

### Step 2 — Apply migrations

After SQL Server is ready:

```bash
dotnet ef database update \
  --project src/HotelReservationSystem.Infrastructure \
  --startup-project src/HotelReservationSystem.API
```

Use the actual context and project paths if they differ.

### Step 3 — Run the API

```bash
dotnet run --project src/HotelReservationSystem.API
```

Use the configured launch profile and record the actual HTTP/HTTPS URLs from application startup output.

### Rules

- Do not proceed when a required service is unhealthy.
- Do not assume the API listens on port `5000`; verify its actual launch configuration.
- Do not skip required migrations in a fresh local environment.
- If a project provides an automated initialization or migration mechanism, document how it interacts with this procedure.

---

## 8. Test Suite

### 8.1 Unit tests

```bash
dotnet test
```

This command runs tests discovered by the selected solution or project. Verify that the intended test projects are included.

### 8.2 Integration tests

Run the relevant integration test project using its actual path:

```bash
dotnet test tests/Integration
```

Integration tests must document their external dependencies, required configuration, and cleanup behavior.

### 8.3 Load tests (k6)

**Expected script location:** `tests/load/k6-test.js`

```bash
k6 run tests/load/k6-test.js
```

Run load tests only against a designated test environment. Do not target production without explicit authorization.

### Rules

- Required tests must pass before merge.
- Skipped or excluded tests must be reported rather than silently treated as passing.
- Test results must identify the executed test projects and environment.
- Load testing is separate from ordinary unit-test execution.
- Tests must not rely on uncontrolled execution order or shared mutable state.

---

## 9. API Smoke Test (Required)

The API smoke test verifies that authentication, request handling, and the reservation acceptance endpoint work together.

### Preconditions

- Docker infrastructure is healthy.
- Database migrations have been applied.
- The API is running.
- A valid JWT access token has been obtained through the supported authentication flow.
- The test room and dates satisfy the application's actual reservation rules.

### Request

The endpoint and response below follow the observed controller contract. Replace the base URL with the actual URL shown by the application.

```bash
curl -i -X POST "http://localhost:5000/api/v1/Reservations" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: smoke-unique-001" \
  -d '{
    "userId": 1,
    "roomId": 1,
    "checkIn": "REPLACE_WITH_VALID_CHECK_IN",
    "checkOut": "REPLACE_WITH_VALID_CHECK_OUT",
    "idempotencyKey": "smoke-unique-001"
  }'
```

**Important:** This is a request template. Replace the dates with valid dates in the format accepted by the DTO and confirm the actual request model before executing it. The `Idempotency-Key` header is illustrative unless the application explicitly supports it; the observed controller contract reads `idempotencyKey` from the request DTO.

The controller derives the user identity from the JWT `userId` claim and overwrites the request DTO's `userId`. Therefore, the body value does not determine the authenticated user's identity.

### Expected acceptance response

For an accepted reservation request, the observed controller returns HTTP `202 Accepted` with a response shaped like:

```json
{
  "status": "PENDING",
  "requestId": "smoke-unique-001"
}
```

The `requestId` value is based on the request's idempotency key in the observed controller implementation.

### Verification

- Confirm the actual HTTP status code.
- Confirm the response contains `status` and `requestId`.
- Use the returned request ID to query the status endpoint.
- Verify the resulting status and payload against the actual service implementation.
- Check application logs using the correlation ID where available.
- Repeat the request with the same idempotency key to test the documented retry behavior.

A successful `202 Accepted` response confirms request acceptance, not necessarily completion of the reservation.

---

## 10. Global Rules

### Required

- Use the documented infrastructure startup process.
- Version database schema changes through EF Core migrations.
- Run the required test suite before merge.
- Keep environment configuration documented and reproducible.
- Verify service readiness before running dependent steps.
- Use the API contract for smoke testing.
- Record failures and relevant test output.

### Forbidden

- Committing real credentials.
- Depending on undocumented manual database setup.
- Silently skipping required tests.
- Treating local-only configuration as a shared configuration contract.
- Claiming end-to-end verification when only unit tests have passed.
- Applying unreviewed schema or production configuration changes.

---

## 11. Final Principle

> If the system cannot be started and verified through a documented, repeatable process, its operational readiness has not been demonstrated.

---

## 12. Expected Outcome

- Reproducible development environment.
- Predictable infrastructure startup.
- Versioned database schema.
- Repeatable test execution.
- Verifiable API acceptance behavior.
- Clear separation between request acceptance and background processing.
- Fewer undocumented environment assumptions.
