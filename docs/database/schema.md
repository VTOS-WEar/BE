# VTOS Backend - Database Schema

## Overview
- **Database**: SQL Server
- **Approach**: Code First (Entity Framework Core)
- **Schema Source**: `DB.txt`

---

## Tables Summary (26 tables)

### I. User & Organization Management
| Table | Primary Key | Description |
|-------|-------------|-------------|
| Role | RoleID | User roles (Admin, Parent, School, Provider) |
| User | UserID | System users with FK to Role |
| School | SchoolID | Schools with uniform catalogs |
| Children | ChildID | Student profiles (FK: User, School) |

### II. Outfit & Catalog Management
| Table | Primary Key | Description |
|-------|-------------|-------------|
| Outfit | OutfitID | Uniforms/outfits |
| ProductVariant | ProductVariantID | Size/color/material variants |
| SizeChart | SizeChartID | Size chart definitions |
| SizeChartDetail | DetailID | Size measurements |
| Category | CategoryID | Outfit categories |
| OutfitCategory | (OutfitID, CategoryID) | Many-to-many junction |

### III. Core Functional Tables
| Table | Primary Key | Description |
|-------|-------------|-------------|
| TryOnHistory | TryOnID | Virtual try-on sessions |
| AIFitAnalysis | AnalysisID | AI body analysis results |
| OutfitRecommendation | RecommendationID | AI recommendations |
| Feedback | FeedbackID | User ratings/comments |

### IV. Order & Payment Management
| Table | Primary Key | Description |
|-------|-------------|-------------|
| Order | OrderID | Customer orders |
| OrderItem | OrderItemID | Order line items |
| PaymentTransaction | PaymentID | Payment records |
| Invoice | InvoiceID | Generated invoices |
| Refund | RefundID | Refund requests |

### V. Provider & Campaign
| Table | Primary Key | Description |
|-------|-------------|-------------|
| Provider | ProviderID | Uniform providers/suppliers |
| Campaign | CampaignID | School uniform campaigns |
| CampaignOutfit | CampaignOutfitID | Campaign-outfit assignments |
| StudentDataImport | ImportID | Bulk student imports |
| ProductionBatch | BatchID | Production tracking |

---

## Key Relationships (Foreign Keys)

```
User → Role (RoleID)
Children → User (ParentUserID), School (SchoolID)
Outfit → School (SchoolID), SizeChart (SizeChartID)
Order → Children (ChildrenID), Campaign (CampaignID)
TryOnHistory → User, Children, Outfit
```

---

## Soft Delete Pattern
Most tables have `IsDeleted` (bit) column with index for query filtering.

---

## Last Updated
**Date**: 2026-01-16
**Source**: `DB.txt` (SQL CREATE TABLE format)
