# ⚙️ Development Order (MANDATORY)

---

## 1. Purpose

Define strict development sequence.

Prevent:

* incorrect dependency usage
* broken integration
* parallel chaos

---

## 2. Order (NON-NEGOTIABLE)

### STEP 1 — Database (Tech Lead)

* Tables
* Constraints
* Idempotency table

---

### STEP 2 — Infrastructure (Tech Lead)

* DB connection
* Redis lock
* Kafka setup

---

### STEP 3 — Domain (Tech Lead)

* Entities

---

### STEP 4 — Application Core (Tech Lead)

* ReservationService
* IdempotencyService
* GRC

---

### STEP 5 — Auth (Dev1)

* Login / Register
* JWT

---

### STEP 6 — Room + UI (Dev2)

* Room CRUD
* UI pages

---

### STEP 7 — Integration (Tech Lead)

* Connect everything
* End-to-end flow

---

## 3. Rules

### FORBIDDEN

* Starting before previous step is complete
* Writing dependent code early
* Parallel core development

---

## 4. Enforcement

Violation → PR rejected

---

## 5. Final Principle

> Correct order = correct system
