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

## Domain Entities (24 entities)

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

## API Documentation

| Document | Status | Description |
|----------|--------|-------------|
| [api_sheet.md](../api/api_sheet.md) | ✅ Complete | 66 endpoints across 12 modules |
| [endpoints.md](../api/endpoints.md) | Draft | Original endpoint outline |

---

## Recommended Packages (Phase 3)

| Package | Purpose |
|---------|---------|
| FluentValidation.AspNetCore | Request validation |
| BCrypt.Net-Next | Password hashing |
| System.IdentityModel.Tokens.Jwt | JWT tokens |
| AutoMapper | Object mapping |
| Serilog.AspNetCore | Logging |
| Swashbuckle.AspNetCore | Swagger docs |

---

## Last Updated
**Date**: 2026-01-22
**Changes**: Corrected entity count (24 entities, not 22)
