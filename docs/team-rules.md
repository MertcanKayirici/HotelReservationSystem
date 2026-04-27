# 📄 Team Rules — Hotel Reservation System

---

## 1. Purpose

This document defines **strict team rules** to ensure:

- Consistent development
- Clear ownership
- Predictable behavior
- Minimal bugs

---

## 2. Roles

| Role      | Responsibility                         |
|-----------|----------------------------------------|
| Tech Lead | Core system, architecture, integration |
| Dev1      | Auth + Security                        |
| Dev2      | UI + Room                              |

---

## 3. Ownership Rules (STRICT)

### MUST

- Each developer owns their module
- Only Tech Lead can modify:
  - Reservation system
  - Kafka
  - Idempotency
  - GRC
  - Database schema

---

### FORBIDDEN

- Editing another module without approval
- Cross-module changes without review
- “I will fix everything” behavior

---

## 4. Architecture Rules (NON-NEGOTIABLE)

```text id="gk2m8l"
API → Application → Domain
Infrastructure → Application + Domain
```

---

## Forbidden Dependencies

- API → Infrastructure access  
- Application → Infrastructure dependency  
- Domain referencing any other layer  

---

## 5. Code Rules (ENFORCED)

### CONTROLLER

**MUST**
- Receive request  
- Call service  
- Return response  

**FORBIDDEN**
- if / loop business logic  
- DB access  
- Kafka / Redis calls  

---

### SERVICE

**MUST**
- Contain ALL business logic  
- Validate rules  
- Orchestrate flow  

---

### REPOSITORY

**MUST**
- Only DB operations  

**FORBIDDEN**
- Business logic  
- Validation  

---

### INFRASTRUCTURE

**MUST**
- External systems only  

**FORBIDDEN**
- Business logic  

---

### 🔴 GRC (CORE SYSTEM)

**MUST**
- Handle lock + transaction  
- Ensure consistency  

**FORBIDDEN**
- Skipping lock  
- Partial execution  

---

## 6. Idempotency Rule (CRITICAL)

**MUST**
- Every write request uses idempotency  
- Same key + different payload → ERROR  

**FORBIDDEN**
- Skipping idempotency check  

---

## 7. PR Rules (STRICT)

### REQUIRED
- Every change MUST be a PR  

**PR MUST include:**
- Description  
- Scope  
- Impact  

---

### REVIEW REQUIREMENTS

| Change Type        | Approval Needed        |
|-------------------|-----------------------|
| UI / small fix    | 1 reviewer            |
| Service logic     | 2 reviewers           |
| Core system       | Tech Lead REQUIRED    |

---

### FORBIDDEN

- Direct push to main  
- Self-merge  
- Unreviewed merge  

---

## 8. PR Rejection Rules

PR will be automatically rejected if:

- Controller contains logic  
- Wrong layer usage  
- Missing validation  
- Missing idempotency  
- DB accessed outside repository  
- Kafka used outside infrastructure  
- Breaking API contract  

---

## 9. Branch Strategy

- `main` → production  
- `dev` → integration  
- `feature/*` → development  

---

## 10. Definition of Done (STRICT)

A task is complete **ONLY IF:**

- Code implemented  
- Validation added  
- Idempotency applied  
- Logs included  
- No runtime errors  
- Tests passing  
- PR approved  

---

## 11. Daily Workflow

Each developer MUST:

- Share yesterday's work  
- Share today's plan  
- Report blockers  

---

## 12. Failure Ownership

| Issue Type       | Owner     |
|------------------|----------|
| Reservation bug  | Tech Lead|
| Auth bug         | Dev1     |
| UI bug           | Dev2     |

---

## 13. Enforcement Rule

Violations:

- introduce bugs  
- break architecture  
- slow development  

**Result:**
- PR rejected immediately  

---

## 14. Global Principle

> No one owns everything  
> Everyone owns something  

---

## 15. Final Principle

> Good systems fail from bad coordination,  
> not bad code.

---

## 16. Final Result

- Clear ownership  
- Enforced architecture  
- Minimal bugs  
- Fast and controlled delivery  