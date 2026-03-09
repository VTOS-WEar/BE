# VTOS Backend Implementation Task List

This document tracks all tasks for the VTOS (Virtual Try-On System) backend implementation.

**Status Legend:**
- 🔲 **TODO** - Not started
- 🔄 **IN PROGRESS** - Currently working on
- ✅ **COMPLETED** - Finished and verified
- ⏸️ **BLOCKED** - Waiting on dependencies

---

## Phase 1: Foundation & Domain Layer

### 1.1 Project Setup
- ✅ Create Tasklist.md
- ✅ Create DevelopmentRules.md
- ✅ Set up project structure (folders)
- ✅ Configure NuGet packages (EF Core packages added)

### 1.2 Domain Entities
- ✅ Create Common base classes (BaseEntity, AuditableEntity, DomainEvent)
- ✅ Create Enums (UserRole, OrderStatus, PaymentStatus, UniformType, etc.)
- ✅ Create Value Objects (Money, Address)
- ✅ Create Domain Exceptions
- ✅ Create User & Organization entities (User, School, ChildProfile, AdminProfile)
- ✅ Create Outfit & Catalog entities (Outfit, ProductVariant, SizeChart, SizeChartDetail, Category, OutfitCategory)
- ✅ Create Core Functional entities (TryOnHistory, AIFitAnalysis, OutfitRecommendation, Feedback)
- ✅ Create Order & Payment entities (Order, OrderItem, PaymentTransaction, Invoice, Refund)
- ✅ Create Provider & Campaign entities (Provider, Campaign, CampaignOutfit, StudentDataImport, ProductionBatch)
- ✅ Align entities with DB.txt SQL schema (2026-01-16)

### 1.3 Domain Validation & Business Rules
- 🔲 Add domain validation logic
- 🔲 Implement domain events where needed

---

## Phase 2: Infrastructure Layer

### 2.1 Entity Framework Setup
- ✅ Install EF Core packages
- ✅ Create VTOSDbContext
- ✅ Create Entity Configurations for all entities
- ✅ Set up database connection string
- ✅ Align all configurations with DB.txt (singular table names)
- ✅ Create initial migration (InitialCreate - 2026-01-16)
- ✅ Test database creation (VTOSDatabase created)

### 2.2 Repository Pattern
- ✅ Create IRepository<T> interface
- ✅ Create Repository<T> implementation
- ✅ Create IUnitOfWork interface
- ✅ Create UnitOfWork implementation

### 2.3 Identity & Authentication
- ✅ Create JwtTokenService
- ✅ Create PasswordHasher
- ✅ Configure JWT settings

### 2.4 File Storage
- ✅ Create IFileStorage interface
- ✅ Create file upload implementation (avatar, verification photos)
- 🔲 Create AzureBlobStorage / cloud storage (optional)

### 2.5 Payment Gateways
- ✅ Create PayOS integration (PayOSController, payment link create/cancel/query)
- ✅ Create PayOS webhook handler
- 🔲 Create VNPay integration (optional)
- 🔲 Create MoMo integration (optional)

### 2.6 AI Integration
- ✅ Create GuestTryOn service (rate-limited, watermarked)
- 🔲 Create authenticated TryOn service (full features)

### 2.7 Dependency Injection
- ✅ Create DependencyInjection extension class
- ✅ Register all services (DbContext registered)

---

## Phase 3: Application Layer — Use Case Features

### 3.2 Authentication & Account Management
- ✅ Register (Command, Handler, Validator, DTOs)
- ✅ Login with email/password (Query, Handler, Validator, DTOs)
- 🔲 Sign In via Google
- 🔲 Sign Out
- ✅ Forgot Password (Command, Handler, Validator)
- ✅ Reset Password (Command, Handler, Validator)
- ✅ Change Password + Request OTP (Commands, Handlers)
- ✅ Verify Email (Command, Handler)
- ✅ Resend OTP (Command, Handler)
- ✅ Verify Phone + Link Children (Command, Handler)
- ✅ View Personal Profile (Query, Handler, DTOs)
- ✅ Edit Personal Profile (Command, Handler, Validator)
- ✅ Update Avatar (Command, Handler, Validator)
- ✅ Submit Verification (Command, Handler, Validator)
- ✅ View User List — Admin (Query, Handler)
- 🔲 View User Detail — Admin
- ✅ Approve/Suspend Account — Admin (Commands, Handlers)
- 🔲 Ban/Unban User — Admin
- 🔲 View User Report — Admin
- 🔲 Approve/Reject School Request — Admin
- 🔲 Approve/Reject Provider Request — Admin

### 3.3 School & Information Browsing (Public)
- ✅ View School List (Query, Handler, DTOs)
- 🔲 View School Information (single school detail)
- ✅ View Uniform Categories (Query, Handler, DTOs)
- 🔲 View Uniform List (filtered by school/category)
- ✅ View Uniform Detail (Query, Handler, DTOs)

### 3.4 Student & Children Management
- ✅ View Children List — Parent (Query, Handler)
- ✅ View Children Detail — Parent (Query, Handler)
- ✅ Update Child Profile — Parent (Command, Handler, Validator)
- ✅ View Student List — School (Query, Handler)
- ✅ View Student Detail — School
- ✅ Add Student — School (Command, Handler)
- ✅ Update Student — School (Command, Handler)
- ✅ Remove Student — School (Command, Handler)
- ✅ Import Student File — School (Command, Handler + CSV/XLSX parser)

### 3.5 Parent Management
- 🔲 View Parent List
- 🔲 View Parent Detail

### 3.6 Virtual Try-On Management
- ✅ Guest Try-On with watermark (Command, Handler, Validator)
- 🔲 Upload Photo
- 🔲 Upload Child Photo
- 🔲 Try On Uniform (authenticated)
- 🔲 View Try-On Preview
- 🔲 View Try-On Result
- 🔲 Adjust Try-On Result
- 🔲 Save Try-On Result
- 🔲 Download Try-On Image
- 🔲 Share Try-On Result
- 🔲 View Try-On History

### 3.7 Shopping Cart Management
- 🔲 Review Cart
- 🔲 Remove Uniform from Cart
- 🔲 Update Quantity
- 🔲 Add Uniform to Cart

### 3.8 Order Management
- ✅ Checkout (Command, Handler, DTOs — creates Order + PaymentTransaction + PayOS link)
- 🔲 Enter Shipping Information
- ✅ Make Payment (via PayOS payment link flow)
- ✅ View Order History (Query, Handler — paginated, filtered, sorted)
- ✅ Track Order Status (Query, Handler — with item details, payment status)
- ✅ Cancel Order (Command, Handler — Pending→cancel PayOS, Paid→create Refund)
- 🔲 Request Refund (standalone)
- 🔲 View Payment Status

### 3.9 Pre-Order & Production Management (School/Admin)
- ✅ Create Uniform Pre-Order / Publish Campaign (Command, Handler, Validator)
- ✅ Track Pre-order Progress (Query, Handler)
- 🔲 View Pre-Order List
- 🔲 View Pre-Order Detail
- 🔲 View Ordered Items
- 🔲 View Selected Size
- 🔲 View Pre-Order Summary
- 🔲 Lock Pre-Order Campaign
- 🔲 Calculate Total Quantity
- 🔲 Generate Production Order
- 🔲 Send Production Request
- 🔲 Confirm Production Order
- 🔲 View Production Complaint
- 🔲 View Production Order List / Detail
- 🔲 View Order Uniform Items / Required Quantity / Delivery Deadline
- 🔲 Process / Reject Production Order

### 3.10 Production & Delivery Management (Provider)
- 🔲 Provider Profile (GET/PUT)
- 🔲 View Batch List / Detail / History
- 🔲 Approve/Reject Batch
- 🔲 Produce Uniform (update status)
- 🔲 Deliver Uniforms
- 🔲 Confirm Uniform Delivery
- 🔲 Verify Delivered/Uniform Quantity
- 🔲 Report Defective Uniform

### 3.11 Contract Management
- 🔲 View Contract List / Detail
- 🔲 Create Production Contract
- 🔲 Approve/Reject Contract
- 🔲 Request Contract Termination

### 3.12 Complaint & Communication Management
- 🔲 Send Message
- 🔲 View Conversation
- 🔲 Mediate Complaint (Admin)
- 🔲 Submit Production Complaint
- 🔲 Respond/Resolve Complaint
- 🔲 View Production Complaint

### 3.13 Reporting & Analytics
- ✅ View Sales Reports — School (Query, Handler)
- ✅ View Feedback Reports — School (Query, Handler)
- ✅ View User Feedbacks — Admin (Query, Handler)
- ✅ Remove Feedback — Admin (Command, Handler)
- 🔲 View Dashboard Analytics (Admin)
- 🔲 View Total Order / Quantity Per Item / Revenue
- 🔲 View Payment Completion Rate
- 🔲 View School Revenue Report / Order Statistics
- 🔲 View / Export Report
- 🔲 Generate System Report
- 🔲 Export School Activity Logs

### 3.14 Category & Configuration Management
- ✅ View Uniform Categories (via Public endpoint)
- 🔲 Add / Update / Delete Uniform Category (Admin)
- 🔲 Configure Uniform Size Template
- 🔲 Configure Default Size Chart
- 🔲 Configure Payment Method
- 🔲 Configure AI Try-On Settings

### 3.15 Refund & Payment Monitoring
- ✅ PayOS Webhook processing (updates payment/order status, school wallet, inventory)
- 🔲 Monitor Payment Transactions (Admin)
- 🔲 Process Refund (Admin)

---

## Phase 4: API Layer

### 4.1 Base Setup
- ✅ Configure Swagger/OpenAPI
- ✅ Set up CORS
- ✅ Configure authentication middleware (JWT)
- 🔲 Create ApiResponse<T> wrapper (currently using raw Result<T>)

### 4.2 Middlewares
- 🔲 Create ExceptionMiddleware
- 🔲 Create LoggingMiddleware

### 4.3 Controllers
- ✅ AuthController (Register, Login, VerifyEmail, ResendOTP, VerifyPhone, ForgotPassword, ResetPassword, ChangePassword)
- ✅ PublicController (GetSchools, GetCategories, GetOutfitDetail)
- ✅ UserController (GetProfile, UpdateProfile, UpdateAvatar, SubmitVerification)
- ✅ ChildrenController (GetMyChildren, GetChild, UpdateChild)
- ✅ SchoolsController (Profile, ImportStudents, PublishCampaign, Orders, CampaignProgress, Reports, Outfits CRUD)
- ✅ TryOnController (GuestTryOn)
- ✅ AdminController (GetUsers, GetFeedbacks, ApproveUser, SuspendUser, RemoveFeedback)
- ✅ OrdersController (Checkout, CancelOrder, TrackOrderStatus, OrderHistory)
- ✅ PayOSController (CreatePaymentLink, GetPaymentLink, CancelPaymentLink, GetInvoices, Webhook)
- 🔲 ProvidersController
- 🔲 ContractsController
- 🔲 ComplaintsController / MessagesController

---

## Phase 5: Testing

### 5.1 Unit Tests
- 🔲 Create Domain unit tests
- 🔲 Create Application unit tests
- 🔲 Create Infrastructure unit tests

### 5.2 Integration Tests
- 🔲 Create API integration tests
- 🔲 Create database integration tests

---

## Phase 6: Deployment & CI/CD

### 6.1 Docker
- 🔲 Create Dockerfile
- 🔲 Create docker-compose.yml

### 6.2 CI/CD
- 🔲 Set up GitHub Actions workflow
- 🔲 Configure build pipeline
- 🔲 Configure deployment pipeline

### 6.3 Configuration
- ✅ Set up environment-specific configurations (appsettings.Development.json)
- ✅ Configure connection strings
- 🔲 Set up secrets management (production)

---

## Notes

- **Last Updated**: 2026-03-08
- **Current Phase**: Phase 3 — Use Case Features (active development)
- **Latest Addition**: Order Checkout, Cancel, Track, History + PayOS Payment Integration

---

## Progress Summary

| Phase | Completed | Total | % |
|-------|-----------|-------|---|
| Phase 1: Foundation & Domain | 15 | 17 | 88% |
| Phase 2: Infrastructure | 15 | 20 | 75% |
| Phase 3: Use Case Features | 35 | 105 | 33% |
| Phase 4: API Layer | 12 | 15 | 80% |
| Phase 5: Testing | 0 | 5 | 0% |
| Phase 6: Deployment | 2 | 8 | 25% |
| **Total** | **~79** | **~170** | **~46%** |
