# 🏨 Hotel Reservation System

## 📌 Overview

This project is a **conflict-safe, distributed hotel reservation system** built with ASP.NET Core.

It is designed to handle:

* concurrent reservation requests
* duplicate API calls
* eventual consistency via messaging

---

## ⚙️ Tech Stack

* ASP.NET Core MVC (.NET)
* MSSQL + Entity Framework Core
* Redis (distributed locking)
* Kafka (event-driven processing)
* Outbox Pattern (reliable messaging)
* Serilog (structured logging)

---

## 🧠 Core Concepts

* Idempotency (safe retries)
* Distributed Locking
* Event-driven architecture
* Outbox pattern
* Eventually consistent system

---

## 🚀 Features

* Reservation creation with conflict prevention
* Idempotent request handling
* Async processing with Kafka
* Status tracking (PENDING → COMPLETED)
* Retry & DLQ support
* Structured logging with CorrelationId

---

## 🔄 Flow

Client → API → Idempotency → Lock → DB
→ Outbox → Kafka → Consumer → DB update

---

## 🧪 Status

🚧 Work in progress
Core system is implemented and tested.

---

## 📎 Notes

Detailed documentation and final README will be added.

---

## 🚀 Quick Start

1. Copy .env.example → .env
2. Run: docker-compose up -d
3. Run: dotnet ef database update
4. Run: dotnet run --project src/HotelReservationSystem.API