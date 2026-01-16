# VTOS Backend - System Architecture

## Overview

**VTOS (Virtual Try-On System)** - A backend API for a virtual uniform try-on system for students.

| Attribute | Value |
|-----------|-------|
| **Framework** | .NET 8 |
| **Architecture** | Clean Architecture + Modular Monolith |
| **Database** | SQL Server (`VTOSDatabase` on `DESKTOP-P5MIN4R\SQLEXPRESS`) |
| **Status** | Phase 2 - Infrastructure (Database Created ✅) |

---

## Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      VTOS.API                               │
│              (Controllers, Middlewares, Filters)            │
└─────────────────────────┬───────────────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
        ▼                 ▼                 ▼
┌───────────────┐  ┌─────────────┐  ┌──────────────────┐
│ VTOS.Application │ │ VTOS.Shared │ │ VTOS.Infrastructure │
│ (Use Cases)      │ │ (Helpers)   │ │ (EF, Repositories)  │
└────────┬─────────┘ └─────────────┘ └─────────┬────────────┘
         │                                      │
         └──────────────────┬───────────────────┘
                            │
                            ▼
                  ┌─────────────────┐
                  │   VTOS.Domain   │
                  │   (Entities)    │
                  │  NO DEPENDENCIES │
                  └─────────────────┘
```

---

## Domain Entities (22 entities)

### User & Organization
- `Role`, `User`, `School`, `ChildProfile`

### Outfit & Catalog
- `Outfit`, `ProductVariant`, `SizeChart`, `SizeChartDetail`, `Category`, `OutfitCategory`

### Core Functional
- `TryOnHistory`, `AIFitAnalysis`, `OutfitRecommendation`, `Feedback`

### Order & Payment
- `Order`, `OrderItem`, `PaymentTransaction`, `Invoice`, `Refund`

### Provider & Campaign
- `Provider`, `Campaign`, `CampaignOutfit`, `StudentDataImport`, `ProductionBatch`

---

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0.0 | ORM |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | SQL Server Provider |
| Microsoft.EntityFrameworkCore.Tools | 8.0.0 | Migrations |

---

## Last Updated
**Date**: 2026-01-16
**Changes**: Aligned all entities with DB.txt SQL schema (Role table, Provider rename, singular table names)
