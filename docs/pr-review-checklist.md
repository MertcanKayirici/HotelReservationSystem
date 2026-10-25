# Pull Request Review Checklist

## 1. Purpose

This checklist defines the minimum review requirements for every pull request in the Hotel Reservation System project.

The goal is to prevent broken integrations, architectural violations, security regressions, and untested changes from entering `main`.

The Tech Lead is responsible for the final integration decision.

## 2. PR Information

Before reviewing, confirm that the PR includes:

- [ ] A clear title describing the change.
- [ ] A description explaining what changed and why.
- [ ] The related task or issue, if applicable.
- [ ] The affected modules and files.
- [ ] Tests executed and their actual results.
- [ ] Known limitations or follow-up work.

## 3. Scope and Code Quality

- [ ] Changes match the assigned task.
- [ ] No unrelated refactoring or generated files are included.
- [ ] No existing work from another developer was overwritten.
- [ ] No duplicate services, DTOs, endpoints, or configuration systems were introduced.
- [ ] Names and code organization follow existing project conventions.
- [ ] Error handling is appropriate and does not hide failures.
- [ ] No unnecessary package dependencies were added.
- [ ] No unexplained temporary code, debugging output, or TODOs were introduced.

## 4. Architecture

- [ ] Changes respect the existing project architecture.
- [ ] Domain does not depend on API or Infrastructure.
- [ ] Application does not depend on Infrastructure implementations.
- [ ] Controllers do not contain business workflows that belong in Application services.
- [ ] EF Core entities are not exposed directly as API responses.
- [ ] Existing dependency injection registrations remain correct.
- [ ] Database access follows existing persistence patterns.
- [ ] Transactions and asynchronous operations are handled correctly.

If architectural boundaries are unclear, consult `docs/architecture.md` and `docs/folder-structure.md`.

## 5. API Contract and Frontend Compatibility

- [ ] Routes and HTTP methods are correct.
- [ ] Request and response DTOs match the API contract.
- [ ] JSON property names remain compatible with existing consumers.
- [ ] Success and error status codes are appropriate.
- [ ] Validation errors are consistent and understandable.
- [ ] Authentication and authorization requirements are preserved.
- [ ] Frontend-required information is available in API responses.
- [ ] Breaking changes are documented and approved.
- [ ] `docs/api-contract.md` is updated when necessary.

## 6. Security

- [ ] No passwords, tokens, connection strings, or real secrets are committed.
- [ ] Authorization is enforced on the server.
- [ ] Users cannot access resources they do not own unless explicitly permitted.
- [ ] Input validation is appropriate.
- [ ] Sensitive information is not exposed in responses or logs.
- [ ] Authentication and token handling follow existing security requirements.
- [ ] Error responses do not expose stack traces or internal implementation details.
- [ ] CORS, TLS, and other security settings have not been weakened to bypass problems.

## 7. Database and Data Integrity

- [ ] Entity and relationship changes are intentional.
- [ ] Required EF Core configuration or migrations are included.
- [ ] Existing unique constraints and indexes are preserved.
- [ ] Transaction boundaries remain correct.
- [ ] Failure paths do not leave unintended tracked changes or partial data.
- [ ] Integration tests use an isolated test database.
- [ ] No destructive operation targets shared or production data.
- [ ] Idempotency and Outbox guarantees remain intact where applicable.

## 8. Testing and Regression

- [ ] The solution or affected projects build successfully.
- [ ] Relevant unit tests pass.
- [ ] Relevant integration tests pass.
- [ ] New behavior has appropriate tests.
- [ ] Existing behavior has not regressed.
- [ ] Invalid input and failure cases are covered.
- [ ] Authentication and authorization scenarios are covered when relevant.
- [ ] Concurrency and duplicate-request scenarios are tested when relevant.
- [ ] Test results are based on actual execution.
- [ ] CI checks pass, or any unavailable checks are explicitly documented.

A passing unit test does not automatically prove that an integration or concurrency scenario works in the real infrastructure.

## 9. Docker, Configuration, and Operations

When the PR affects deployment or infrastructure:

- [ ] Docker Compose configuration is valid.
- [ ] Service names, ports, networks, and health checks are consistent.
- [ ] Required environment variables are documented.
- [ ] Secrets are supplied safely.
- [ ] Startup dependencies and connection strings are correct.
- [ ] The application can connect to required services.
- [ ] Logging and health checks are appropriate.
- [ ] Backup and restore procedures remain valid when affected.

## 10. Documentation

- [ ] README changes are included when required.
- [ ] API documentation matches implementation.
- [ ] Environment variable changes are documented.
- [ ] Setup and testing instructions are accurate.
- [ ] Database or deployment changes include the necessary operational guidance.
- [ ] No unimplemented behavior is described as complete.

## 11. Git and Repository Hygiene

- [ ] The PR targets the correct base branch.
- [ ] The source branch is appropriate for the task.
- [ ] Only intended files are included.
- [ ] No local artifacts, secrets, build output, or unrelated files are included.
- [ ] Commit messages are understandable.
- [ ] The diff has been reviewed for whitespace errors and accidental changes.
- [ ] The PR does not bypass required review or CI checks.

## 12. Review Decision

Choose one outcome:

### Approve

Use only when all applicable critical checks pass, the implementation matches the task, and no blocking issue remains.

### Request Changes

Use when there are correctness, security, data-integrity, architecture, compatibility, or testing problems that must be fixed.

### Comment

Use for non-blocking suggestions or questions that do not prevent merging.

## 13. Final Merge Gate

Before merging into `main`:

- [ ] All blocking review comments are resolved.
- [ ] Required CI checks pass.
- [ ] Applicable tests have passed.
- [ ] API and architecture compatibility has been checked.
- [ ] No unresolved security or data-integrity issue remains.
- [ ] The PR description and documentation are accurate.
- [ ] The Tech Lead approves the merge.

Do not merge solely because the branch has no conflicts. A conflict-free merge does not prove that the implementation is correct.

## 14. Post-Merge Verification

After merging:

- [ ] Update the local `main` branch.
- [ ] Verify the expected commit is present.
- [ ] Run relevant smoke tests when appropriate.
- [ ] Confirm that the repository remains in the expected state.
- [ ] Create a version tag only when the release point and versioning decision are confirmed.
