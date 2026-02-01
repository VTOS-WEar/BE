# Changelog

All notable changes to the VTOS Backend project.


---
## [2026-02-01] - UC-05 View Personal Information

### Added
- **UC-05: View Personal Information**
  - `GET /api/users/profile` endpoint for authenticated users
  - Returns complete user profile including avatar, contact info, account status

- **Application Layer** (`VTOS.Application/Features/Users/`)
  - **DTOs**
    - `UserProfileDto` - User profile response model
  - **Queries**
    - `GetUserProfileQuery` - Query to fetch current user profile
    - `GetUserProfileQueryHandler` - Handler with database lookup by UserID from JWT

- **API Layer**
  - `UserController.cs` - `GET /api/users/profile` endpoint

### Technical Details
- **Authentication**: JWT Bearer token required (`[Authorize]`)
- **Current User**: Retrieved via `ICurrentUserService` from JWT claims
- **Response Fields**: UserID, FullName, Email, Phone, AvatarURL, Address, CreatedAt, IsActive
- **Pattern**: Custom `IHandler` + `Result<T>`

---
## [2026-02-01] - UC-06 Update Personal Information

### Added
- **UC-06: Update Personal Information**
  - `PUT /api/users/profile` endpoint for profile field updates
  - `PUT /api/users/avatar` endpoint for avatar image upload
  - Integration with ImgBB for avatar storage

- **Application Layer** (`VTOS.Application/Features/Users/`)
  - **DTOs**
    - `UpdateProfileRequest` - Profile update request model
    - `UpdateAvatarRequest` - Avatar upload request model
  - **Commands**
    - `UpdateProfileCommand` + `UpdateProfileCommandHandler` - Update FullName, Phone, Address
    - `UpdateAvatarCommand` + `UpdateAvatarCommandHandler` - Upload & update avatar URL
  - **Validators**
    - `UpdateProfileCommandValidator` - FullName (required, max 100 chars), Phone (VN format)
    - `UpdateAvatarCommandValidator` - Max 5MB, jpg/png/webp only

- **API Layer**
  - `UserController.cs`:
    - `PUT /api/users/profile` - Update profile fields
    - `PUT /api/users/avatar` - Upload avatar (multipart/form-data)

### Technical Details
- **Authentication**: JWT Bearer token required (`[Authorize]`)
- **Avatar Upload**: Uses `IImageUploadService` (ImgBB integration, reused from UC-60)
- **Validation**: FluentValidation with Vietnamese error messages
- **Pattern**: Custom `IHandler` + `Result<T>`

---
## [2026-01-29] - UC-60 Guest Try-On Implementation

### Added
- **UC-60: Guest Virtual Try-On Feature**
  - `POST /api/tryon/guest` endpoint for anonymous users
  - Integration with 302.ai Virtual Try-On V2 API
  - Integration with ImgBB image hosting service
  - Rate limiting: 5 tries per guest session per day
  - Guest session tracking with `GuestSessionId`
  - Photo validation (max 10MB, jpg/png/webp only)
  - Try-on history persistence to database

- **Application Layer**
  - `IImageUploadService` interface for image upload abstraction
  - `IVirtualTryOnService` interface for AI try-on abstraction
  - `GuestTryOnCommand`, `GuestTryOnResponse` DTOs
  - `GuestTryOnCommandValidator` with FluentValidation
  - `GuestTryOnCommandHandler` with rate limiting logic
  - `IGuestTryOnCommandHandler` interface

- **Infrastructure Layer**
  - `VirtualTryOnService` - 302.ai API client
  - `VirtualTryOnSettings` - 302.ai configuration
  - `ImgBBImageService` - ImgBB upload client
  - `ImgBBSettings` - ImgBB configuration
  - HttpClientFactory registration for external APIs

- **API Layer**
  - `TryOnController` with `POST /api/tryon/guest` endpoint
  - `GuestTryOnRequest` DTO for Swagger compatibility
  - Configuration sections in `appsettings.Development.json`

### Fixed
- **Build Errors**
  - Fixed `Microsoft.AspNetCore.Http.Features` package not found by using `FrameworkReference` instead of `PackageReference`
  - Fixed Swagger error with multiple `[FromForm]` parameters by using single request DTO
  - Fixed 302.ai JSON deserialization by adding `[JsonPropertyName]` attributes for snake_case properties

### Technical Details
- **Pattern**: Custom `IHandler` + `Result<T>` (not MediatR)
- **External APIs**: 302.ai Virtual Try-On V2, ImgBB Image Upload
- **Rate Limiting**: Track by `GuestSessionID` + `Date` for daily limits
- **Validation**: 10MB max file size, jpg/png/webp formats only

---
## [2026-01-29] - Frontend-Backend Integration & CORS Setup

### Added
- **CORS Configuration**
  - CORS policy in `Program.cs` allowing Frontend origins
  - Support for both HTTP and HTTPS origins (localhost:5173, 127.0.0.1:5173)
  - Credentials, headers, and methods enabled for multipart/form-data uploads
- **Frontend Integration**
  - Created `.env` with Backend API URL (`https://localhost:7093`)
  - Created `src/services/api.ts` - Base HTTP client with error handling
  - Created `src/services/tryOnService.ts` - TryOn API service with session management
  - Created `src/vite-env.d.ts` - TypeScript environment variable types
  - Updated `TryOnModal.tsx` with full API integration:
    - Photo upload functionality
    - Loading states and error handling
    - Result image display
    - Vietnamese error messages
  - Updated `ProductDetail.tsx` to pass outfit ID to TryOnModal

### Fixed
- **CORS 307 Redirect Issue**
  - Root cause: Backend redirecting HTTP → HTTPS
  - Solution: Updated Frontend to use HTTPS URL (`https://localhost:7093`)
  - Added HTTPS origins to CORS policy
- **Port Mismatch Issue**
  - Root cause: Frontend calling port 5000, Backend running on 5130/7093
  - Solution: Updated all URLs to match Backend's actual ports

### Technical Details
- Backend HTTPS: `https://localhost:7093`
- Backend HTTP: `http://localhost:5130`
- Frontend: `http://localhost:5173`
- Try-On endpoint: `POST /api/tryon/guest` (multipart/form-data)

---
## [2026-01-27] - Public Guest APIs (UC-57, UC-58, UC-59)

### Added
- **Public Module**
  - View School List (`GET /api/public/schools`) - search, pagination
  - View Uniform Categories (`GET /api/public/categories`) - with outfit counts
  - View Uniform Details (`GET /api/public/outfits/{id}`) - full details
- **Application Layer**
  - 7 DTOs: SchoolDto, CategoryDto, OutfitDetailResponse, etc.
  - 6 Query/Handlers for public data access
- **API Layer**
  - `PublicController.cs` with 3 endpoints (no auth required)

---
## [2026-01-25] - Password Management Implementation

### Added
- **Authentication Module**
  - Forgot Password flow (`POST /api/auth/forgot-password`)
  - Reset Password flow (`POST /api/auth/reset-password`)
    - Secure token generation (SHA256 hashed)
    - 1-hour token expiry
  - Change Password flow (`POST /api/auth/change-password`)
    - Two-step process: Request OTP -> Change Password
    - Secure OTP verification (10 minutes expiry)
- **Infrastructure**
  - `EmailVerification` entity updated with `Purpose` field
  - `EmailService` updated for Change Password OTPs


## [2026-01-24] - View User List & Feedback

### Added
- **Admin Function**
  - View User List (`GET /api/admin/users`)
  - View User Feedbacks (`GET /api/admin/feedbacks`)


## [2026-01-23] - Auth & Configuration Implementation

### Added
- **Authentication Module**
  - Registration with Email OTP (`POST /api/auth/register`)
  - Email Verification (`POST /api/auth/verify-email`)
  - Phone Verification for linking children (`POST /api/auth/verify-phone`)
  - OTP Resend mechanism (`POST /api/auth/resend-otp`)
- **Infrastructure**
  - `EmailService` using MailKit
  - `EmailVerification` entity and table
  - `OTPGenerator` utility
- **Configuration**
  - Secured sensitive settings in `appsettings.Development.json` (Email, JWT, DbConnection)

### Changed
- **Register Flow**: Deferred phone collection to post-login to reduce friction and improve security
- **Login Flow**: Enforced `IsActive` check before issuing JWT

## [2026-01-22] - Documentation Review (/save_brain)

### Added
- Team Assignment Plan (`docs/TeamAssignment.md`)
  - 5 team members phân công theo Actor: Guest/Auth, Parent, School, Supplier, Admin
  - 60 Use Cases mapped to API endpoints
  - Feature folder structure cho mỗi thành viên
  - Shared Infrastructure ownership
  - Sprint Plan và Git Workflow

### Verified
- 24 entities in Domain layer (was documented as 22)
- 8 enums in Domain layer
- 26 tables match DB.txt schema
- 66 API endpoints documented in `api_sheet.md`
- 0 controllers implemented yet

### Status Summary
- **Current Phase**: Phase 2 - Infrastructure
- **Next Priority**: Implement Repository Pattern → Application Layer
- **Ready for**: Development can continue with API implementation

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
- Server: `DESKTOP-P5MIN4R\\SQLEXPRESS`
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
