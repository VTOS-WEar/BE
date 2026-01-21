# Changelog

All notable changes to the VTOS Backend project.

---

## [2026-01-21] - API Documentation Sheet

### Added
- Comprehensive API Documentation Sheet (`docs/api/api_sheet.md`)
  - 12 API modules designed
  - 66 endpoints documented
  - Full request/response schemas
  - Error handling specifications
  - NuGet package recommendations

### Documentation
- API Sheet includes: Endpoint, Description, Method, Input, Output, Errors, Notes, Packages
- Modules: Authentication, Users, Children, Schools, Outfits, Try-On, Orders, Payments, Feedback, Categories, Providers, Campaigns

---

## [2026-01-16] - Database Created

### Added
- Initial EF Core migration (`InitialCreate`)
- Database `VTOSDatabase` created on SQL Server
- `Microsoft.EntityFrameworkCore.Design` package (v8.0.0) to VTOS.API
- Connection string configuration in `appsettings.Development.json`
- `README.md` with database setup tutorial for team
- `.gitignore` for .NET project (protects `appsettings.Development.json`)

### Infrastructure
- Server: `DESKTOP-P5MIN4R\SQLEXPRESS`
- Database: `VTOSDatabase`
- 26 tables created matching DB.txt schema

---

## [2026-01-16] - Schema Alignment

### Added
- New `Role` entity for user roles (replaces UserRole enum)
- New `Provider` entity (renamed from Supplier)
- `RoleConfiguration.cs` and `ProviderConfiguration.cs`

### Changed
- User entity: Added RoleID FK, IsActive, IsDeleted, LastLogin
- All 18 EF configurations: Table names changed to singular (matching DB.txt)
- ChildProfile: Added IsDeleted property
- ProductVariant: Added IsDeleted, removed SupplierStocks navigation
- CampaignOutfit: SupplierID → ProviderID
- ProductionBatch: SupplierID → ProviderID, Status to string

### Removed
- `Supplier.cs` entity (replaced by Provider)
- `SupplierStock.cs` entity (not in DB schema)
- `AdminProfile.cs` entity (not in DB schema)
- `UserRole.cs` enum (replaced by Role table)
- `SupplierStatus.cs` enum
- `BatchStatus.cs` enum
- `SupplierConfiguration.cs`, `SupplierStockConfiguration.cs`, `AdminProfileConfiguration.cs`

### Fixed
- All table names now match DB.txt SQL schema (singular form)
- Build: 0 errors, 0 warnings

---

## [2024] - Initial Setup

### Added
- Project structure (Clean Architecture + Modular Monolith)
- 26 Domain entities
- 11 Enums
- 2 Value Objects (Money, Address)
- VTOSDbContext with all entity configurations
- DevelopmentRules.md, Tasklist.md, Implement.md
