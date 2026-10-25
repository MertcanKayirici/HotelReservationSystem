# AI Development Rules

## 1. Purpose

This document defines the mandatory rules for AI-assisted development in the Hotel Reservation System project.

The goal is to prevent architectural violations, API mismatches, security issues, unnecessary changes, and untested code.

These rules apply to all developers and AI coding assistants.

## 2. Before Writing Code

Before making changes, the developer and AI assistant MUST:

1. Read `docs/architecture.md`.
2. Read `docs/folder-structure.md`.
3. Read `docs/team-rules.md`.
4. Read `docs/api-contract.md`.
5. Inspect the existing implementation related to the task.
6. Check the current Git branch and working tree.
7. Identify the files that need to change.
8. Explain the planned changes before performing broad or risky modifications.

Never assume a class, endpoint, package, configuration, or feature exists. Verify it in the repository first.

## 3. Scope Control

- Modify only files required for the assigned task.
- Do not rewrite entire files when a targeted change is sufficient.
- Do not perform unrelated refactoring.
- Do not delete existing code, tests, migrations, or documentation without approval.
- Do not overwrite another developer's uncommitted work.
- Do not create duplicate services, DTOs, entities, endpoints, or configuration systems.
- If requirements conflict with existing architecture, stop and report the conflict to the Tech Lead.

## 4. Architecture Rules

Follow the existing Clean Architecture boundaries.

- **API:** Controllers, HTTP request/response models, middleware, and HTTP-specific behavior.
- **Application:** Business workflows, application services, DTOs, and interfaces.
- **Domain:** Entities, value objects, enums, and business rules.
- **Infrastructure:** Database access, EF Core, repositories, Redis, Kafka, and external integrations.

Rules:

- Domain must not depend on API or Infrastructure.
- Application must not depend on Infrastructure implementations.
- Do not place business logic in controllers.
- Do not expose EF Core entities directly as API responses.
- Follow the actual project structure and existing architecture documentation.
- Do not introduce new architectural patterns without a clear reason and Tech Lead approval.

## 5. API Contract Protection

Before modifying an endpoint:

1. Inspect its route and HTTP method.
2. Inspect its request and response models.
3. Check authentication and authorization requirements.
4. Read `docs/api-contract.md`.
5. Identify frontend or other consumers that may be affected.

Do not silently change:

- Routes or HTTP methods.
- JSON property names.
- Response formats or status codes.
- Error response structures.
- Authentication requirements.
- Required request fields.
- Idempotency behavior.

If an API contract change is necessary, update the documentation and notify the Tech Lead.

## 6. Database Safety

- Inspect existing entities, EF Core configurations, migrations, indexes, and relationships before making changes.
- Follow the project's migration workflow.
- Do not delete or rewrite migrations without approval.
- Do not run destructive database commands against shared or production databases.
- Use an isolated database for integration tests.
- Verify the actual connection string before running database operations.
- Preserve transaction boundaries, unique constraints, idempotency guarantees, and Outbox behavior.
- Never log passwords, tokens, connection strings, or other secrets.

Database schema changes must include the appropriate migration and tests.

## 7. Security Requirements

- Never hardcode credentials, signing keys, API keys, or production secrets.
- Never commit real secrets or `.env` files containing credentials.
- Keep `.env.example` limited to placeholder values.
- Validate requests on the server; frontend validation is not a security boundary.
- Verify resource ownership and role-based access where applicable.
- Never disable authentication, authorization, or certificate validation just to make tests pass.
- Do not expose stack traces or sensitive exception details to API clients.
- Do not log passwords, access tokens, refresh tokens, or sensitive personal data.

Report suspected security vulnerabilities to the Tech Lead.

## 8. Dependencies and Configuration

Before adding a package, service, environment variable, or configuration provider:

1. Check whether an equivalent capability already exists.
2. Explain why the change is necessary.
3. Verify compatibility with the target framework and current packages.
4. Obtain Tech Lead approval for new dependencies or significant infrastructure changes.

Do not upgrade unrelated packages.

Do not silently change Docker service names, ports, network aliases, health checks, or environment variable names.

## 9. Testing Requirements

Every code change must receive appropriate verification.

Depending on the task, run:

- Build.
- Relevant unit tests.
- Relevant integration tests.
- API and authorization tests.
- Regression tests.

For API changes, consider:

- Valid requests.
- Invalid or missing fields.
- Unauthorized requests.
- Forbidden access.
- Missing resources.
- Duplicate requests.
- Conflict responses.
- Unexpected failures.

For database changes, use an isolated test database and verify transaction and constraint behavior.

If tests cannot be run because a dependency or environment is unavailable, explicitly report the limitation.

Never delete, weaken, or skip assertions merely to make tests pass.

Never claim that a test passed unless it was actually executed successfully.

## 10. AI Output Requirements

For each task, the AI assistant must provide:

1. A summary of the existing implementation.
2. The planned changes and affected files.
3. Assumptions, risks, and API/database implications.
4. The implementation.
5. Tests executed and actual results.
6. Remaining manual verification.
7. A concise pull request summary.

The AI assistant must not claim that code was executed, tested, committed, pushed, deployed, or merged unless the action actually occurred and the result was observed.

If requirements are ambiguous, ask the developer or Tech Lead instead of guessing.

## 11. Git and Pull Request Rules

- Work on a dedicated feature branch.
- Never commit directly to `main`.
- Never force-push to shared branches without explicit approval.
- Never run destructive Git commands such as `git reset --hard` or `git clean -fd` without explicit approval.
- Do not stage unrelated files, local artifacts, secrets, or build output.
- Review `git diff` and `git status` before committing.
- Run `git diff --check` before committing.
- Use descriptive commit messages.
- Open a pull request with a summary, test results, and known risks.
- Require review and applicable CI checks before merging.
- The Tech Lead makes the final integration decision.

## 12. Documentation Requirements

Update documentation when changes affect:

- API routes or response contracts.
- Environment variables or startup instructions.
- Database schema or migrations.
- Docker services or ports.
- Authentication or authorization.
- Testing procedures.
- Operational or recovery procedures.

Do not document a feature as complete if it has not been implemented and verified.

## 13. Definition of Done

A task is complete only when:

- [ ] The implementation matches the assigned scope.
- [ ] Architecture and API contracts are preserved or intentionally updated.
- [ ] Relevant tests are added or updated.
- [ ] Build and applicable tests are executed.
- [ ] No unrelated files or secrets are included.
- [ ] Required documentation is updated.
- [ ] The diff is reviewed.
- [ ] The PR explains the changes and verification results.
- [ ] Remaining risks or blocked checks are disclosed.
- [ ] The Tech Lead reviews the change before integration.

## 14. Instruction Priority

If instructions conflict, follow this order:

1. Security and data-safety requirements.
2. Approved architecture decisions and API contracts.
3. Team rules and this document.
4. The assigned task.
5. AI-generated suggestions.

AI recommendations never override project rules or approval requirements.
