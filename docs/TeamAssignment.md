# VTOS Backend - Team Assignment Plan

## Overview

Phân chia công việc cho **5 Team Members** dựa trên **5 Actors** từ SRS document.

| Member | Actor | Use Cases | Priority APIs |
|--------|-------|-----------|---------------|
| **KhoiNDQ + QuanNSH** | Guest + Auth | UC-01 → UC-04, UC-56 → UC-60 | Login, Register, TryOn Demo |
| **HuyenCTT** | Parent | UC-05 → UC-28 | Children, TryOn, Orders, Cart |
| **KhoiNDQ** | School | UC-42 → UC-50 | School Profile, Students, Campaigns |
| **TruyenVNG** | Supplier | UC-51 → UC-55 | Provider, ProductionBatch |
| **ThanhNLD** | Admin | UC-29 → UC-41 | User Mgmt, Reports, System |

---

## 👤 KhoiNDQ + QuanNSH: Guest + Authentication

### Use Cases (9 UCs)
| UC ID | Use Case | API Endpoint |
|-------|----------|--------------|
| UC-01 | Register | `POST /api/auth/register` |
| UC-02 | Login | `POST /api/auth/login` |
| UC-03 | Forget Password | `POST /api/auth/forgot-password` |
| UC-04 | Change Password | `PUT /api/auth/change-password` |
| UC-56 | View Homepage | `GET /api/public/home` |
| UC-57 | View School List | `GET /api/public/schools` |
| UC-58 | View Uniform Categories | `GET /api/public/categories` |
| UC-59 | View Uniform Details | `GET /api/public/outfits/{id}` |
| UC-60 | Try-On Demo (3 times max) | `POST /api/public/tryon-demo` |

### Screens
- LoginScreen, RegisterScreen, ResetPasswordScreen
- HomeScreen, ChooseSchoolScreen, ViewProductScreen
- ProductDetailScreen, TryOnDemoScreen

### Files to Create
```
Features/Auth/
├── Commands/Register, Login, ForgotPassword, ChangePassword
└── Queries/ValidateToken

Features/Public/
├── Queries/GetHome, GetSchools, GetCategories, GetOutfitDetail
└── Commands/TryOnDemo

Controllers/
├── AuthController.cs
└── PublicController.cs
```

---

## 👨‍👩‍👧 HuyenCTT: Parent

### Use Cases (24 UCs)
| UC ID | Use Case | API Endpoint |
|-------|----------|--------------|
| UC-05 | View Personal Information | `GET /api/users/me` |
| UC-06 | Update Personal Information | `PUT /api/users/me` |
| UC-07 | Submit Verification Info | `POST /api/users/me/verify` |
| UC-08 | View Child Profile | `GET /api/children` |
| UC-09 | Update Child Information | `PUT /api/children/{id}` |
| UC-10 | Confirm Child for Try-on | `POST /api/children/{id}/confirm` |
| UC-11 | Try-on Uniform | `POST /api/tryon` |
| UC-12 | Select Child Context | `POST /api/tryon/context` |
| UC-13 | Upload Child Photo | `POST /api/tryon/upload` |
| UC-14 | Generate Try-on Result | `POST /api/tryon/generate` |
| UC-15 | Adjust Try-on Result | `PUT /api/tryon/{id}/adjust` |
| UC-16 | Save Try-on Result | `POST /api/tryon/{id}/save` |
| UC-17 | Download Try-on Image | `GET /api/tryon/{id}/download` |
| UC-18 | Share Try-on Result | `POST /api/tryon/{id}/share` |
| UC-19 | View Try-on History | `GET /api/tryon/history` |
| UC-20 | View Uniform Detail | `GET /api/outfits/{id}` |
| UC-21 | Add Uniform to Cart | `POST /api/cart` |
| UC-22 | Review Cart | `GET /api/cart` |
| UC-23 | Place Uniform Order | `POST /api/orders` |
| UC-24 | Make Payment | `POST /api/payments` |
| UC-25 | Track Order Status | `GET /api/orders/{id}` |
| UC-26 | View Order History | `GET /api/orders` |
| UC-27 | Request Refund | `POST /api/orders/{id}/refund` |
| UC-28 | Give Feedback | `POST /api/feedback` |

### Screens
- FillInformationScreen, FindSchoolScreen, ViewProductScreen
- ProductDetailScreen, TryOnScreen, CartScreen
- PaymentShipScreen, PaymentSchoolScreen
- OrderManagementScreen, OrderDetailScreen
- MyProfileScreen, ChildManagementScreen, TryOnHistoryScreen

### Files to Create
```
Features/Users/
├── Commands/UpdateProfile, SubmitVerification
└── Queries/GetProfile

Features/Children/
├── Commands/Create, Update, Delete, Confirm
└── Queries/GetAll, GetById

Features/TryOn/
├── Commands/Upload, Generate, Adjust, Save, Share
└── Queries/GetHistory, Download

Features/Cart/
├── Commands/AddItem, UpdateItem, RemoveItem
└── Queries/GetCart

Features/Orders/
├── Commands/CreateOrder, RequestRefund
└── Queries/GetOrders, GetOrderById, TrackStatus

Features/Payments/
├── Commands/CreatePayment, ProcessWebhook
└── Queries/GetPaymentStatus

Features/Feedback/
├── Commands/SubmitFeedback
└── Queries/GetMyFeedback

Controllers/
├── UsersController.cs
├── ChildrenController.cs
├── TryOnController.cs
├── CartController.cs
├── OrdersController.cs
├── PaymentsController.cs
└── FeedbackController.cs
```

---

## 🏫 KhoiNDQ: School

### Use Cases (9 UCs)
| UC ID | Use Case | API Endpoint |
|-------|----------|--------------|
| UC-42 | Maintain School Profile | `GET/PUT /api/schools/me` |
| UC-43 | Import Student Data | `POST /api/schools/me/students/import` |
| UC-44 | Publish Uniform Pre-order | `POST /api/schools/me/campaigns` |
| UC-45 | View Parent Orders | `GET /api/schools/me/orders` |
| UC-46 | Track Pre-order Progress | `GET /api/schools/me/campaigns/{id}/progress` |
| UC-47 | Confirm Goods Received | `POST /api/schools/me/batches/{id}/confirm` |
| UC-48 | Confirm Uniform Order | `POST /api/schools/me/campaigns/{id}/confirm` |
| UC-49 | View Sales Reports | `GET /api/schools/me/reports/sales` |
| UC-50 | View Feedback Reports | `GET /api/schools/me/reports/feedback` |

### Additional APIs (from Screens)
| Feature | API Endpoint |
|---------|--------------|
| Manage Students | `GET/POST/PUT/DELETE /api/schools/me/students` |
| Manage Uniforms | `GET/POST/PUT/DELETE /api/schools/me/outfits` |
| Choose Supplier | `GET /api/schools/me/suppliers` |
| Send to Supplier | `POST /api/schools/me/batches` |
| Dashboard | `GET /api/schools/me/dashboard` |

### Screens
- SchoolProfileScreen, StudentListScreen, ImportDataScreen
- CheckPreviewScreen, ConfirmSaveScreen, UniformManagementScreen
- OpenOrderScreen, DashboardScreen, OrderManagementScreen
- DistributionScreen, ReportsStatisticsScreen, FeedbackScreen
- ChooseSupplierScreen, SendSupplierScreen

### Files to Create
```
Features/Schools/
├── Commands/UpdateProfile, ImportStudents, CreateCampaign
├── Commands/ConfirmOrder, ConfirmGoods, SendToSupplier
└── Queries/GetProfile, GetStudents, GetOrders, GetDashboard

Features/SchoolStudents/
├── Commands/Create, Update, Delete, BulkImport
└── Queries/GetAll, GetById, PreviewImport

Features/Campaigns/
├── Commands/Create, Update, Close, ConfirmOrder
└── Queries/GetAll, GetById, GetProgress

Features/SchoolReports/
└── Queries/GetSalesReport, GetFeedbackReport, GetRevenue

Controllers/
└── SchoolsController.cs
```

---

## 🏭 TruyenVNG: Supplier (Provider)

### Use Cases (5 UCs)
| UC ID | Use Case | API Endpoint |
|-------|----------|--------------|
| UC-51 | Provide Supplier Information | `GET/PUT /api/providers/me` |
| UC-52 | Receive Production Order | `GET /api/providers/me/batches?status=pending` |
| UC-53 | Confirm Production Order | `POST /api/providers/me/batches/{id}/approve` |
| UC-53b | Reject Production Order | `POST /api/providers/me/batches/{id}/reject` |
| UC-54 | Produce Uniforms (Update Status) | `PUT /api/providers/me/batches/{id}/status` |
| UC-55 | Deliver Uniforms to School | `POST /api/providers/me/batches/{id}/deliver` |

### Additional APIs
| Feature | API Endpoint |
|---------|--------------|
| View Batch History | `GET /api/providers/me/batches/history` |
| View Batch Details | `GET /api/providers/me/batches/{id}` |

### Screens
- SupplierProfileScreen, SupplierProfileEditScreen
- ProductionBatchListScreen, ProductionBatchDetailScreen
- BatchStatusUpdateScreen, BatchHistoryScreen

### Files to Create
```
Features/Providers/
├── Commands/UpdateProfile, ApproveBatch, RejectBatch
├── Commands/UpdateBatchStatus, DeliverBatch
└── Queries/GetProfile, GetBatches, GetBatchById, GetHistory

Controllers/
└── ProvidersController.cs
```

---

## 👑 ThanhNLD: Admin

### Use Cases (13 UCs)
| UC ID | Use Case | API Endpoint |
|-------|----------|--------------|
| UC-29 | Approve School Account | `POST /api/admin/schools/{id}/approve` |
| UC-30 | Approve Supplier Account | `POST /api/admin/providers/{id}/approve` |
| UC-31 | Suspend User Account | `POST /api/admin/users/{id}/suspend` |
| UC-32 | Register School Profile | `POST /api/admin/schools` |
| UC-33 | Verify School Information | `POST /api/admin/schools/{id}/verify` |
| UC-34 | Publish Uniform Catalog | `POST /api/admin/outfits/{id}/publish` |
| UC-35 | Review Uniform Content | `GET /api/admin/outfits/pending` |
| UC-36 | Update Uniform Availability | `PUT /api/admin/outfits/{id}/availability` |
| UC-37 | Review User Feedback | `GET /api/admin/feedback` |
| UC-38 | Remove Inappropriate Feedback | `DELETE /api/admin/feedback/{id}` |
| UC-39 | Configure AI Try-on Rules | `GET/PUT /api/admin/config/ai` |
| UC-40 | View Dashboard Analytics | `GET /api/admin/dashboard` |
| UC-41 | Generate System Reports | `GET /api/admin/reports/{type}` |

### Additional APIs
| Feature | API Endpoint |
|---------|--------------|
| User Management | `GET/POST/PUT/DELETE /api/admin/users` |
| School Management | `GET/PUT/DELETE /api/admin/schools` |
| Provider Management | `GET/PUT/DELETE /api/admin/providers` |
| Category Management | `GET/POST/PUT/DELETE /api/admin/categories` |
| Role Management | `GET/POST/PUT/DELETE /api/admin/roles` |

### Screens
- AdminDashboardScreen, SchoolApprovalScreen, SupplierApprovalScreen
- UserManagementScreen, SchoolProfileManagementScreen
- SchoolVerificationScreen, UniformReviewScreen
- UniformCatalogPublishScreen, UniformAvailabilityScreen
- FeedbackListScreen, FeedbackModerationScreen
- AIConfigurationScreen, SystemReportScreen

### Files to Create
```
Features/Admin/
├── Users/Commands/Suspend, Activate, Create, Update, Delete
├── Users/Queries/GetAll, GetById
├── Schools/Commands/Create, Approve, Verify, Update, Delete
├── Schools/Queries/GetAll, GetById, GetPending
├── Providers/Commands/Approve, Suspend, Update
├── Providers/Queries/GetAll, GetById, GetPending
├── Outfits/Commands/Publish, UpdateAvailability
├── Outfits/Queries/GetPending, GetAll
├── Feedback/Commands/Delete
├── Feedback/Queries/GetAll
├── Config/Commands/UpdateAI
├── Config/Queries/GetAI
├── Dashboard/Queries/GetAnalytics
└── Reports/Queries/Generate

Controllers/
└── AdminController.cs
```

---

## 📦 Shared Infrastructure (All Members)

| Component | Primary Owner | Status |
|-----------|--------------|--------|
| Repository Pattern | ThanhNLD | 🔲 TODO |
| Unit of Work | ThanhNLD | 🔲 TODO |
| JWT Service | KhoiNDQ + QuanNSH | 🔲 TODO |
| Current User Service | KhoiNDQ + QuanNSH | 🔲 TODO |
| File Storage Service | HuyenCTT | 🔲 TODO |
| Payment Gateway | HuyenCTT | 🔲 TODO |
| AI Try-On Service | KhoiNDQ + QuanNSH | 🔲 TODO |

---

## 📅 Sprint Plan

| Sprint | Focus | Members |
|--------|-------|---------|
| Sprint 1 | Infrastructure + Auth | M1, M5 |
| Sprint 2 | Core Features (P0) | All |
| Sprint 3 | Secondary Features (P1) | All |
| Sprint 4 | Reports + Polish (P2) | All |

---

## 🔄 Git Workflow

```
main
  └── develop
       ├── feature/auth-login (M1)
       ├── feature/parent-children (M2)
       ├── feature/school-profile (M3)
       ├── feature/provider-batches (M4)
       └── feature/admin-users (M5)
```

