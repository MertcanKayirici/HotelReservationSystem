# 📚 Documentation — Hotel Reservation System

---

## 1. System Overview

This project is a **conflict-safe hotel reservation system** designed to prevent:

- double booking
- inconsistent state
- invalid reservation flows

### Core Principles

- Idempotency guarantees correctness
- Queue guarantees stability
- Database guarantees truth
- Lock guarantees concurrency safety

---

## 2. Purpose

This folder contains **all technical and organizational rules of the system**.

### Goals

- Eliminate ambiguity
- Standardize implementation
- Enable fast onboarding
- Prevent incorrect development
- Enforce system correctness

---

## 3. Document Structure

### 🧠 Architecture

- `architecture.md`
  → Defines layers, system boundaries, and flow

---

### ⚙️ Implementation

- `execution-guide.md`
  → Defines EXACT implementation rules (MANDATORY)

---

### 🔌 API

- `api-contract.md`
  → Defines request/response format (CANNOT CHANGE)

---

### 🛠️ Runtime

- `production-kit.md`
  → Defines how to run the system (Docker, DB, tests)

---

### 🧑‍💻 Team

- `team-rules.md`
  → Defines ownership, workflow, and PR rules

---

### 🧱 Stack

- `tech-stack.md`
  → Defines all technologies used

---

## 4. How to Read (MANDATORY)

### 🔹 New Developer (STRICT ORDER)

1. `architecture.md`
2. `tech-stack.md`
3. `execution-guide.md`
4. `api-contract.md`
5. `team-rules.md`
6. `production-kit.md`

---

### 🔹 Existing Developer

Use based on need:

- Implementation → `execution-guide.md`
- API changes → `api-contract.md`
- Workflow → `team-rules.md`

---

## 5. System Flow (HIGH LEVEL)

Client  
→ API Controller  
→ Application Service  
→ Idempotency  
→ Kafka  
→ Consumer  
→ GRC (Lock + Transaction)  
→ Database  
→ Outbox → Event

---

## 6. Source of Truth

| Area              | Source                  |
|------------------|-------------------------|
| API              | api-contract.md         |
| Implementation   | execution-guide.md      |
| Architecture     | architecture.md         |
| Team behavior    | team-rules.md           |
| Runtime          | production-kit.md       |

---

## 7. Enforcement Rules

### MUST

- Follow documents strictly
- Follow defined flow
- Respect layer boundaries
- Use idempotency for all write operations

---

### FORBIDDEN

- Guessing implementation
- Breaking API contract
- Writing logic in wrong layer
- Skipping execution guide
- Direct DB/Kafka usage outside rules

---

## 8. Conflict Resolution Rule

If documents conflict:

```text
execution-guide.md > api-contract.md > architecture.md
```

---

## 9. Developer Responsibility

Every developer MUST:

- Read documentation before coding  
- Follow defined flow  
- Not introduce new patterns without approval  

---

## 10. Final Principle

> Documentation is part of the system.

Ignoring documentation leads to:

- bugs  
- inconsistent behavior  
- production failures  

---

## 11. Final Result

- Fast onboarding  
- Predictable implementation  
- Minimal bugs  
- Enforced architecture  
- Scalable team development  