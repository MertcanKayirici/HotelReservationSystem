# 🏨 Hotel Reservation System

A distributed hotel reservation system built with **ASP.NET Core**, designed to handle concurrent booking requests, safe retries, and reliable asynchronous processing.

The project focuses on reservation consistency, idempotency, distributed locking, and reliable event-driven communication.

## 📌 Overview

The system is designed to address common challenges in reservation platforms:

- Concurrent reservation requests for the same room
- Duplicate API requests and safe retries
- Reliable event publishing and asynchronous processing
- Reservation status tracking
- Observability through structured logging and correlation IDs

## ⚙️ Tech Stack

| Technology | Purpose |
|---|---|
| ASP.NET Core Web API | HTTP endpoints and application entry point |
| C# / .NET | Backend implementation |
| Microsoft SQL Server | Persistent relational storage |
| Entity Framework Core | Data access and migrations |
| Redis | Distributed locking |
| Apache Kafka | Asynchronous event processing |
| Transactional Outbox | Reliable event publishing |
| Serilog | Structured logging |
| Docker Compose | Local infrastructure orchestration |

## 🧠 Core Concepts

### Idempotency

Prevents duplicate processing when a client retries the same logical reservation request.

### Distributed Locking

Coordinates competing requests for a room to reduce reservation conflicts.

### Event-Driven Processing

Uses Kafka to support asynchronous communication between system components.

### Transactional Outbox

Stores messages alongside database changes so they can be published reliably by a background worker.

### Eventual Consistency

Some reservation operations complete asynchronously. Clients can use the request identifier to query processing status.

### Observability

Structured logging and `CorrelationId` help trace requests across processing stages.

## 🚀 Features

- Reservation creation API
- Reservation conflict handling
- Idempotency and duplicate-request handling
- Asynchronous reservation processing
- Reservation status lookup
- Kafka-based event processing
- Transactional outbox processing
- Distributed locking with Redis
- Structured logging and correlation IDs
- Automated tests for key backend behaviors

> **Implementation note:** Retry policies, dead-letter queue (DLQ) behavior, and end-to-end concurrency guarantees should be considered verified only when their implementation and corresponding tests have been confirmed.

## 🏗️ Architecture

```text
Client
  |
  v
ASP.NET Core API
  |
  v
Authentication & Validation
  |
  v
Reservation Application Layer
  |
  +--> Idempotency
  |
  +--> Redis Distributed Lock
  |
  v
SQL Server Transaction
  |
  +--> Reservation Data
  |
  +--> Outbox Message
           |
           v
     Outbox Background Worker
           |
           v
          Kafka
           |
           v
      Kafka Consumer
           |
           v
   Reservation Processing
```

The exact processing sequence depends on the implemented coordinator, transaction boundaries, and consumer behavior. See the architecture and decision documents for implementation details.

## 📂 Project Documentation

Start with [`docs/README.md`](docs/README.md) for the documentation index.

| Document | Purpose |
|---|---|
| [`docs/architecture.md`](docs/architecture.md) | System architecture and component boundaries |
| [`docs/folder-structure.md`](docs/folder-structure.md) | Repository structure and placement rules |
| [`docs/api-contract.md`](docs/api-contract.md) | API endpoints and request/response expectations |
| [`docs/team-rules.md`](docs/team-rules.md) | Team workflow and collaboration rules |
| [`docs/tech-stack.md`](docs/tech-stack.md) | Technology choices and constraints |
| [`docs/execution-guide.md`](docs/execution-guide.md) | Development and execution workflow |
| [`docs/production-kit.md`](docs/production-kit.md) | Production-oriented configuration and operational guidance |
| [`docs/decisions/`](docs/decisions/) | Architecture decision records |

Additional team documents:

- `docs/ai-development-rules.md` — constraints for AI-assisted coding
- `docs/pr-review-checklist.md` — pull-request review checklist
- `docs/qa-test-plan.md` — QA scenarios and release verification

These three files should be added to the repository if they have only been drafted and not yet saved.

## 🧪 Testing

Run tests from the repository root:

```bash
dotnet test HotelReservationSystem.sln
```

Test coverage and integration-test prerequisites depend on the current test configuration. Database-specific guarantees should be validated using integration tests against the appropriate SQL Server environment.

## 🚀 Quick Start

### Prerequisites

- .NET SDK compatible with the solution's target framework
- Docker and Docker Compose
- Git

### 1. Clone the repository

```bash
git clone https://github.com/MertcanKayirici/HotelReservationSystem.git
cd HotelReservationSystem
```

### 2. Configure environment variables

Copy `.env.example` to `.env` and configure the required values.

```bash
cp .env.example .env
```

On Windows PowerShell, use:

```powershell
Copy-Item .env.example .env
```

Do not commit `.env` or real credentials.

### 3. Start infrastructure

```bash
docker compose up -d
```

Check the container status:

```bash
docker compose ps
```

### 4. Apply database migrations

Ensure the SQL Server connection string and required environment variables are configured.

If the EF Core tools are installed and the API project is configured as the migration startup project, apply migrations with:

```bash
dotnet ef database update \
  --project src/HotelReservationSystem.Infrastructure \
  --startup-project src/HotelReservationSystem.API
```

If the repository uses a different migration assembly or startup configuration, follow the actual project setup.

### 5. Run the API

```bash
dotnet run --project src/HotelReservationSystem.API
```

Use the URL printed by the application to access the API. If Swagger is enabled in the active environment, use its displayed URL to inspect and test the endpoints.

## 🔐 Security Notes

- Store secrets in environment variables or an appropriate secret store.
- Never commit real JWT signing keys, database passwords, or credentials.
- Protect reservation endpoints with authentication.
- Enforce user ownership on reservation status queries.
- Do not expose internal exception details in API responses.
- Preserve idempotency and concurrency protections when modifying reservation flows.

## 📈 Project Status

**Status: Work in progress**

The project includes core reservation-processing components and backend tests. Deployment readiness, complete retry/DLQ behavior, and production-level concurrency guarantees must be evaluated against the current implementation and test evidence.

## 🗺️ Roadmap

- [ ] Complete API contract verification
- [ ] Align frontend integration with the documented API
- [ ] Expand automated integration and concurrency tests
- [ ] Verify retry and dead-letter handling
- [ ] Validate deployment and environment configuration
- [ ] Complete end-to-end QA
- [ ] Document operational recovery and troubleshooting procedures

## 🤝 Contributing

Before changing code:

1. Read `docs/README.md`.
2. Review the architecture and folder-structure rules.
3. Follow `docs/team-rules.md`.
4. Check `docs/api-contract.md` before modifying API behavior.
5. Add or update tests for behavioral changes.
6. Submit changes through a pull request.

AI coding assistants must follow [`docs/ai-development-rules.md`](docs/ai-development-rules.md) when that file is present in the repository.

## 📄 License

Check the repository's `LICENSE` file for licensing terms.
