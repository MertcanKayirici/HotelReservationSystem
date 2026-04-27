# 🏨 Hotel Reservation System — Complete Technology Stack (ASP.NET Core MVC)

---

## 1. Purpose

This document defines the **final and mandatory technology stack** using **C# ASP.NET Core MVC**.

### Goals

* Standardize all development technologies
* Ensure compatibility across backend and frontend
* Enable scalable and maintainable architecture
* Prevent technology fragmentation

---

## 2. Backend Technology

### Framework

* **ASP.NET Core MVC (.NET 8 preferred)**

### Language

* **C#**

### Reason

* Strong typing (type safety)
* Mature ecosystem
* Built-in MVC architecture
* Enterprise-level performance and security

---

## 3. Database Layer

### Primary Database

* **Microsoft SQL Server (MSSQL)**

### ORM

* **Entity Framework Core**

### Reason

* Strong integration with .NET
* LINQ support
* Migration support
* Transaction management

---

## 4. Messaging & Async Processing

### Message Broker

* **Apache Kafka**

### .NET Client

* **Confluent.Kafka**

### Usage

* Reservation request queue
* Retry mechanism
* Event-driven processing

---

## 5. Distributed Locking

### Lock System

* **Redis**

### Library

* **StackExchange.Redis**

### Usage

* Room-level locking
* Prevent concurrent reservation conflicts

---

## 6. Outbox Pattern

### Storage

* MSSQL (`OutboxEvents` table)

### Worker

* **BackgroundService (ASP.NET Core Hosted Service)**

### Usage

* Ensure atomic DB + event consistency
* Reliable event publishing

---

## 7. Frontend Technology

### Markup

* **HTML5**

### Styling

* **CSS3**

### UI Framework

* **Bootstrap 5**

### Optional Enhancements

* JavaScript (vanilla or minimal usage)

### Reason

* Fast development
* Responsive design
* Clean UI without complexity

---

## 8. API Layer

### Pattern

* MVC Controllers

### Validation

* **Data Annotations**
* Model validation in controllers

---

## 9. Authentication (Phase 2)

### System

* **ASP.NET Core Identity** or JWT (optional)

---

## 10. Configuration Management

### Method

* **appsettings.json**
* Environment-specific configs:

  * appsettings.Development.json
  * appsettings.Production.json

---

## 11. Logging

### Built-in Logging

* **Microsoft.Extensions.Logging**

### Optional

* **Serilog**

### Requirements

* Structured logs
* Include:

  * RequestId
  * UserId
  * Step

---

## 12. Testing

### Unit / Integration Testing

* **xUnit**

### HTTP Testing

* **Microsoft.AspNetCore.Mvc.Testing**

### Load Testing

* **k6**

---

## 13. Containerization

### Tools

* **Docker**
* **Docker Compose**

### Services

* MSSQL
* Redis
* Kafka

---

## 14. Code Quality

### Formatting & Analysis

* **.NET built-in analyzers**
* **StyleCop (optional)**

---

## 15. Version Control

### System

* **Git**

### Platform

* **GitHub**

---

## 16. Development Tools

### IDE

* **Visual Studio / Visual Studio Code**

### Package Manager

* **NuGet**

---

## 17. Optional (Phase 2+)

* Swagger (API documentation)
* SignalR (real-time updates)
* Background job tools (Hangfire)
* Monitoring (Prometheus + Grafana)

---

## 18. Infrastructure Summary

| Layer     | Technology                 |
| --------- | -------------------------- |
| Backend   | ASP.NET Core MVC           |
| Language  | C#                         |
| Database  | MSSQL                      |
| ORM       | Entity Framework Core      |
| Messaging | Kafka                      |
| Locking   | Redis                      |
| Frontend  | HTML5 + CSS3 + Bootstrap 5 |
| Logging   | Built-in / Serilog         |
| Testing   | xUnit + k6                 |
| Container | Docker                     |

---

## 19. Critical Rules

### MUST

* Use ASP.NET Core MVC architecture
* Use MSSQL + EF Core for all data access
* Use Kafka for async processing
* Use Redis for locking
* Use Outbox pattern
* Follow layered architecture

---

### FORBIDDEN

* Direct DB access outside service/repository
* Business logic in controllers
* Skipping idempotency
* Mixing frontend logic into backend

---

## 20. Final Principle

> One system → one stack → strict consistency.

---

## 21. Final Result

* Clean architecture
* Strong typing
* Scalable backend
* Maintainable frontend
* Production-ready system

---

## 22. Final Note

Technology stack is **locked for Phase 1**.

Any changes:

* require Tech Lead approval
* must provide measurable benefit
