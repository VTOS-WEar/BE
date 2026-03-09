# VTOS Backend - Team Assignment Plan (Updated)

## Overview

| Member | Actor | Use Cases | Priority APIs |
|--------|-------|-----------|---------------|
| **KhoiNDQ** | Guest + Auth | 3.2.1 → 3.2.6, 3.3.1 → 3.3.5 | Login, Register, TryOn Demo, Public Browse |
| **HuyenCTT** | Parent | 3.4.1 → 3.4.2, 3.5.x, 3.6.x, 3.7.x, 3.8.x | Children, TryOn, Orders, Cart |
| **KhoiNDQ** | School | 3.4.3 → 3.4.8, 3.9.x | School Profile, Students, Campaigns |
| **TruyenVNG** | Provider | 3.10.x, 3.11.x | Provider, ProductionBatch, Contracts |
| **ThanhNLD** | Admin | 3.2.7 → 3.2.13, 3.12.x, 3.13.x, 3.14.x, 3.15.x | User Mgmt, Reports, Config, Refund |

---

## 3.2 Authentication & Account Management

### 👤 KhoiNDQ: Auth (3.2.1 → 3.2.6)

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.2.1a | Sign In (email/password) | `POST /api/auth/login` | ✅ Done |
| 3.2.1b | Sign In via Google | `POST /api/auth/google` | 🔲 TODO |
| 3.2.2 | Sign Out | `POST /api/auth/logout` | 🔲 TODO |
| 3.2.3 | Forgot Password | `POST /api/auth/forgot-password` + `POST /api/auth/reset-password` | ✅ Done |
| 3.2.4 | Change Password | `POST /api/auth/change-password` + `POST /api/auth/change-password/request-otp` | ✅ Done |
| 3.2.5 | View Personal Profile | `GET /api/users/me` | ✅ Done |
| 3.2.6 | Edit Personal Profile | `PUT /api/users/me/profile` + `PUT /api/users/me/avatar` | ✅ Done |

#### Bonus Features (beyond UCs) — All Done ✅
| Feature | API Endpoint | Status |
|---------|--------------|--------|
| Register | `POST /api/auth/register` | ✅ Done |
| Verify Email (OTP) | `POST /api/auth/verify-email` | ✅ Done |
| Resend OTP | `POST /api/auth/resend-otp` | ✅ Done |
| Verify Phone + Link Children | `POST /api/auth/verify-phone` | ✅ Done |
| Submit Verification Info | `POST /api/users/me/verify` | ✅ Done |

### 👑 ThanhNLD: Admin Account Management (3.2.7 → 3.2.13)

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.2.7 | View User List | `GET /api/admin/users` | ✅ Done |
| 3.2.8 | View User Detail | `GET /api/admin/users/{id}` | 🔲 TODO |
| 3.2.9 | Approve/Suspend Account | `POST /api/admin/users/{id}/approve` + `POST /api/admin/users/{id}/suspend` | ✅ Done |
| 3.2.10 | Ban/Unban User | `POST /api/admin/users/{id}/ban` + `POST /api/admin/users/{id}/unban` | 🔲 TODO |
| 3.2.11 | View User Report | `GET /api/admin/users/{id}/reports` | 🔲 TODO |
| 3.2.12 | Approve/Reject School Request | `POST /api/admin/schools/{id}/approve` + `POST /api/admin/schools/{id}/reject` | 🔲 TODO |
| 3.2.13 | Approve/Reject Provider Request | `POST /api/admin/providers/{id}/approve` + `POST /api/admin/providers/{id}/reject` | 🔲 TODO |

---

## 3.3 School & Information Browsing (Public + Parent) — 👤 KhoiNDQ

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.3.1 | View Homepage | Frontend composes from public endpoints | ✅ Done (FE) |
| 3.3.2 | View School List | `GET /api/public/schools` | ✅ Done |
| 3.3.3 | View School Information | `GET /api/public/schools/{id}` | ✅ Done |
| 3.3.4 | View Uniform List | `GET /api/public/schools/{schoolId}/uniforms` | ✅ Done |
| 3.3.5 | View Uniform Detail | `GET /api/public/outfits/{id}` | ✅ Done |

---

## 3.4 Student & Children Management

### 👨‍👩‍👧 HuyenCTT: Parent Side (3.4.1 → 3.4.2)

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.4.1 | View Children List | `GET /api/children` | ✅ Done |
| 3.4.2 | View Children Detail | `GET /api/children/{id}` | ✅ Done |

### 🏫 KhoiNDQ: School/Admin Side (3.4.3 → 3.4.8)

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.4.3 | View Student List (Admin/School) | `GET /api/schools/me/students` | ✅ Done |
| 3.4.4 | View Student Detail | `GET /api/schools/me/students/{id}` | ✅ Done |
| 3.4.5 | Add Student | `POST /api/schools/me/students` | ✅ Done |
| 3.4.6 | Update Student | `PUT /api/schools/me/students/{id}` | ✅ Done |
| 3.4.7 | Remove Student Request | `DELETE /api/schools/me/students/{id}` | ✅ Done |
| 3.4.8 | Import Student's File | `POST /api/schools/me/students/import` | ✅ Done |

---

## 3.5 Parent Management — 👑 ThanhNLD / 🏫 KhoiNDQ

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.5.1 | View Parent List | `GET /api/admin/parents` | 🔲 TODO |
| 3.5.2 | View Parent Detail | `GET /api/admin/parents/{id}` | 🔲 TODO |

---

## 3.6 Virtual Try-On Management — 👨‍👩‍👧 HuyenCTT (Logged-in) + 👤 KhoiNDQ (Guest)

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.6.1 | Upload Photo | `POST /api/tryon/upload` | 🔲 TODO |
| 3.6.2 | Upload Child Photo | `POST /api/tryon/child/upload` | 🔲 TODO |
| 3.6.3 | Try On Uniform | `POST /api/tryon` | 🔲 TODO |
| 3.6.4 | View Try-On Preview | `GET /api/tryon/{id}/preview` | 🔲 TODO |
| 3.6.5 | View Try-On Result | `GET /api/tryon/{id}` | 🔲 TODO |
| 3.6.6 | View Try-On Result with Watermark (Guest) | `POST /api/tryon/guest` | ✅ Done |
| 3.6.7 | Adjust Try-On Result | `PUT /api/tryon/{id}/adjust` | 🔲 TODO |
| 3.6.8 | Save Try-On Result | `POST /api/tryon/{id}/save` | 🔲 TODO |
| 3.6.9 | Download Try-On Image | `GET /api/tryon/{id}/download` | 🔲 TODO |
| 3.6.10 | Share Try-On Result | `POST /api/tryon/{id}/share` | 🔲 TODO |
| 3.6.11 | View Try-On History | `GET /api/tryon/history` | 🔲 TODO |

---

## 3.7 Shopping Cart Management — 👨‍👩‍👧 HuyenCTT

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.7.1 | Review Cart | `GET /api/cart` | 🔲 TODO |
| 3.7.2 | Remove Uniform from Cart | `DELETE /api/cart/{itemId}` | 🔲 TODO |
| 3.7.3 | Update Quantity | `PUT /api/cart/{itemId}` | 🔲 TODO |

> **Note:** "Add Uniform to Cart" (`POST /api/cart`) is implied by the uniform detail page and listed as a bonus.

---

## 3.8 Order Management — 👨‍👩‍👧 HuyenCTT

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.8.1 | Check Out | `POST /api/orders` | 🔲 TODO |
| 3.8.2 | Enter Shipping Information | `PUT /api/orders/{id}/shipping` | 🔲 TODO |
| 3.8.3 | Make Payment | `POST /api/payments` | 🔲 TODO |
| 3.8.4 | View Order History | `GET /api/orders` | 🔲 TODO |
| 3.8.5 | Track Order Status | `GET /api/orders/{id}` | 🔲 TODO |
| 3.8.6 | Request Refund | `POST /api/orders/{id}/refund` | 🔲 TODO |
| 3.8.7 | View Payment Status | `GET /api/payments/{id}/status` | 🔲 TODO |

---

## 3.9 Pre-Order & Production Management (School/Admin) — 🏫 KhoiNDQ

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.9.1 | Create Uniform Pre-Order | `POST /api/schools/me/campaigns` | ✅ Done |
| 3.9.2 | View Pre-Order List | `GET /api/schools/me/campaigns` | 🔲 TODO |
| 3.9.3 | View Pre-Order Detail | `GET /api/schools/me/campaigns/{id}` | 🔲 TODO |
| 3.9.4 | View Ordered Items | `GET /api/schools/me/campaigns/{id}/items` | 🔲 TODO |
| 3.9.5a | View Selected Size | `GET /api/schools/me/campaigns/{id}/sizes` | 🔲 TODO |
| 3.9.5b | Lock Pre-Order Campaign | `POST /api/schools/me/campaigns/{id}/lock` | 🔲 TODO |
| 3.9.6 | View Pre-Order Summary | `GET /api/schools/me/campaigns/{id}/summary` | 🔲 TODO |
| 3.9.7 | Calculate Total Quantity | `GET /api/schools/me/campaigns/{id}/quantity` | 🔲 TODO |
| 3.9.8 | Generate Production Order | `POST /api/schools/me/campaigns/{id}/production-order` | 🔲 TODO |
| 3.9.9 | Send Production Request | `POST /api/schools/me/batches` | 🔲 TODO |
| 3.9.10 | Confirm Production Order | `POST /api/schools/me/campaigns/{id}/confirm` | 🔲 TODO |
| 3.9.11 | View Production Complaint | `GET /api/schools/me/complaints` | 🔲 TODO |
| 3.9.12 | View Production Order List | `GET /api/schools/me/production-orders` | 🔲 TODO |
| 3.9.13 | View Production Order Detail | `GET /api/schools/me/production-orders/{id}` | 🔲 TODO |
| 3.9.14 | View Order Uniform Items | `GET /api/schools/me/production-orders/{id}/items` | 🔲 TODO |
| 3.9.15 | View Required Quantity | `GET /api/schools/me/production-orders/{id}/quantity` | 🔲 TODO |
| 3.9.16 | View Delivery Deadline | `GET /api/schools/me/production-orders/{id}/deadline` | 🔲 TODO |
| 3.9.17 | Process Uniform Production Order | `POST /api/schools/me/production-orders/{id}/process` | 🔲 TODO |
| 3.9.18 | Reject Production Order | `POST /api/schools/me/production-orders/{id}/reject` | 🔲 TODO |

> **Note:** UC 3.9.5 appears twice in the original list. Assigned as 3.9.5a (View Selected Size) and 3.9.5b (Lock Pre-Order Campaign).

### Existing Done (tracked separately)

| Feature | API Endpoint | Status |
|---------|--------------|--------|
| View Parent Orders | `GET /api/schools/me/orders` | ✅ Done |
| Track Pre-order Progress | `GET /api/schools/me/campaigns/{id}/progress` | ✅ Done |
| View Sales Reports | `GET /api/schools/me/reports/sales` | ✅ Done |
| View Feedback Reports | `GET /api/schools/me/reports/feedback` | ✅ Done |
| School Profile (GET/PUT) | `GET/PUT /api/schools/me` | ✅ Done |
| Manage Outfits (CRUD) | `GET/POST/PUT/DELETE /api/schools/me/outfits` | ✅ Done |

---

## 3.10 Production & Delivery Management (Provider) — 🏭 TruyenVNG

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.10.1 | Produce Uniform | `PUT /api/providers/me/batches/{id}/status` | 🔲 TODO |
| 3.10.2 | Deliver Uniforms | `POST /api/providers/me/batches/{id}/deliver` | 🔲 TODO |
| 3.10.3 | Confirm Uniform Delivery | `POST /api/providers/me/batches/{id}/confirm-delivery` | 🔲 TODO |
| 3.10.4 | Verify Delivered Quantity | `GET /api/providers/me/batches/{id}/verify-quantity` | 🔲 TODO |
| 3.10.5 | Verify Uniform Quantity | `GET /api/providers/me/batches/{id}/verify` | 🔲 TODO |
| 3.10.6 | Report Defective Uniform | `POST /api/providers/me/batches/{id}/defect-report` | 🔲 TODO |

### Additional Provider APIs
| Feature | API Endpoint | Status |
|---------|--------------|--------|
| View Provider Profile | `GET /api/providers/me` | 🔲 TODO |
| Update Provider Profile | `PUT /api/providers/me` | 🔲 TODO |
| View Batch List | `GET /api/providers/me/batches` | 🔲 TODO |
| View Batch Detail | `GET /api/providers/me/batches/{id}` | 🔲 TODO |
| Approve Batch | `POST /api/providers/me/batches/{id}/approve` | 🔲 TODO |
| Reject Batch | `POST /api/providers/me/batches/{id}/reject` | 🔲 TODO |
| View Batch History | `GET /api/providers/me/batches/history` | 🔲 TODO |

---

## 3.11 Contract Management — 🏭 TruyenVNG + 👑 ThanhNLD

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.11.1 | View Contract List | `GET /api/contracts` | 🔲 TODO |
| 3.11.2 | View Contract Detail | `GET /api/contracts/{id}` | 🔲 TODO |
| 3.11.3 | Create Production Contract | `POST /api/contracts` | 🔲 TODO |
| 3.11.4 | Approve/Reject Contract | `POST /api/contracts/{id}/approve` + `POST /api/contracts/{id}/reject` | 🔲 TODO |
| 3.11.5 | Request Contract Termination | `POST /api/contracts/{id}/terminate` | 🔲 TODO |

---

## 3.12 Complaint & Communication Management — 👑 ThanhNLD + HuyenCTT

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.12.1 | Send Message | `POST /api/messages` | 🔲 TODO |
| 3.12.2 | View Conversation | `GET /api/conversations/{id}` | 🔲 TODO |
| 3.12.3 | Mediate Complaint | `POST /api/admin/complaints/{id}/mediate` | 🔲 TODO |
| 3.12.4 | Submit Production Complaint | `POST /api/complaints` | 🔲 TODO |
| 3.12.5 | Respond/Resolve Complaint | `PUT /api/complaints/{id}/resolve` | 🔲 TODO |
| 3.12.6 | View Production Complaint | `GET /api/complaints` | 🔲 TODO |

---

## 3.13 Reporting & Analytics — 👑 ThanhNLD

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.13.1 | View Dashboard Analytics | `GET /api/admin/dashboard` | 🔲 TODO |
| 3.13.2 | View Total Order | `GET /api/admin/reports/total-orders` | 🔲 TODO |
| 3.13.3 | View Total Quantity Per Item | `GET /api/admin/reports/quantity-per-item` | 🔲 TODO |
| 3.13.4 | View Total Revenue | `GET /api/admin/reports/revenue` | 🔲 TODO |
| 3.13.5 | View Payment Completion Rate | `GET /api/admin/reports/payment-rate` | 🔲 TODO |
| 3.13.6 | View School Revenue Report | `GET /api/admin/reports/school-revenue` | 🔲 TODO |
| 3.13.7 | View School Order Statistics | `GET /api/admin/reports/school-orders` | 🔲 TODO |
| 3.13.8 | View Report | `GET /api/admin/reports/{type}` | 🔲 TODO |
| 3.13.9 | Export Report | `GET /api/admin/reports/{type}/export` | 🔲 TODO |
| 3.13.10 | Generate System Report | `POST /api/admin/reports/generate` | 🔲 TODO |
| 3.13.11 | Export School Activity Logs | `GET /api/admin/reports/school-activity-logs/export` | 🔲 TODO |

---

## 3.14 Category & Configuration Management — 👑 ThanhNLD

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.14.1 | View Uniform Categories | `GET /api/admin/categories` | ✅ Done (via Public) |
| 3.14.2 | Add Uniform Category | `POST /api/admin/categories` | 🔲 TODO |
| 3.14.3 | Update Uniform Category | `PUT /api/admin/categories/{id}` | 🔲 TODO |
| 3.14.4 | Delete Uniform Category | `DELETE /api/admin/categories/{id}` | 🔲 TODO |
| 3.14.5 | Configure Uniform Size Template | `GET/PUT /api/admin/config/size-template` | 🔲 TODO |
| 3.14.6 | Configure Default Size Chart | `GET/PUT /api/admin/config/size-chart` | 🔲 TODO |
| 3.14.7 | Configure Payment Method | `GET/PUT /api/admin/config/payment` | 🔲 TODO |
| 3.14.8 | Configure AI Try-On Settings | `GET/PUT /api/admin/config/ai` | 🔲 TODO |

---

## 3.15 Refund & Payment Monitoring — 👑 ThanhNLD

| UC ID | Use Case | API Endpoint | Status |
|-------|----------|--------------|--------|
| 3.15.1 | Monitor Payment Transactions | `GET /api/admin/payments` | 🔲 TODO |
| 3.15.2 | Process Refund | `POST /api/admin/refunds/{id}/process` | 🔲 TODO |

---

## 📊 Progress Summary

| Section | Total UCs | Done | TODO | % Complete |
|---------|-----------|------|------|------------|
| 3.2 Auth & Account Mgmt | 13 | 6 | 7 | 46% |
| 3.3 School Browsing | 5 | 5 | 0 | 100% |
| 3.4 Student & Children | 8 | 8 | 0 | 100% |
| 3.5 Parent Management | 2 | 0 | 2 | 0% |
| 3.6 Virtual Try-On | 11 | 1 | 10 | 9% |
| 3.7 Shopping Cart | 3 | 0 | 3 | 0% |
| 3.8 Order Management | 7 | 0 | 7 | 0% |
| 3.9 Pre-Order & Production | 18 | 1 | 17 | 6% |
| 3.10 Provider Delivery | 6 | 0 | 6 | 0% |
| 3.11 Contract Management | 5 | 0 | 5 | 0% |
| 3.12 Complaint & Communication | 6 | 0 | 6 | 0% |
| 3.13 Reporting & Analytics | 11 | 0 | 11 | 0% |
| 3.14 Category & Config | 8 | 1 | 7 | 13% |
| 3.15 Refund & Payment | 2 | 0 | 2 | 0% |
| **TOTAL** | **105** | **22** | **83** | **21%** |

---

## 📦 Shared Infrastructure (All Members)

| Component | Primary Owner | Status |
|-----------|--------------|--------|
| Repository Pattern | ThanhNLD | ✅ Done |
| Unit of Work | ThanhNLD | ✅ Done |
| JWT Service | KhoiNDQ | ✅ Done |
| Current User Service | KhoiNDQ | ✅ Done |
| File Storage Service | HuyenCTT | 🔲 TODO |
| Payment Gateway | HuyenCTT | 🔲 TODO |
| AI Try-On Service | KhoiNDQ | ✅ Done (Guest) |

---

## 📅 Sprint Plan

| Sprint | Focus | Members |
|--------|-------|---------|
| Sprint 1 | Infrastructure + Auth | KhoiNDQ, ThanhNLD |
| Sprint 2 | Core Features (Children, School, TryOn) | All |
| Sprint 3 | Orders, Cart, Payments, Providers | HuyenCTT, TruyenVNG |
| Sprint 4 | Reports, Contracts, Config, Polish | ThanhNLD, All |

---

## 🔄 Git Workflow

```
main
  └── develop
       ├── feature/auth-login (KhoiNDQ)
       ├── feature/parent-children (HuyenCTT)
       ├── feature/school-profile (KhoiNDQ)
       ├── feature/provider-batches (TruyenVNG)
       └── feature/admin-users (ThanhNLD)
```

---

## Files Created (Existing Implementation)

```
Features/Auth/
├── Commands/Register, Login, ForgotPassword, ResetPassword, ChangePassword
├── Commands/VerifyEmail, ResendOTP, VerifyPhone, RequestChangePasswordOTP
├── Queries/LoginQuery
├── DTOs/RegisterRequest, LoginRequest, ForgotPasswordRequest, etc.
└── Validators/RegisterCommandValidator, LoginQueryValidator, etc.

Features/Users/
├── Commands/UpdateProfile, UpdateAvatar, SubmitVerification
├── Queries/GetProfile
├── DTOs/GetProfileResponse, UpdateProfileRequest, etc.
└── Validators/UpdateProfileValidator, UpdateAvatarValidator, etc.

Features/Children/
├── Commands/UpdateChildProfile
├── Queries/GetMyChildProfile, GetChildProfile
├── DTOs/GetChildProfileResponse, UpdateChildProfileRequest, etc.
├── Mappings/ChildProfileMappingProfile
└── Validators/UpdateChildProfileValidator

Features/Public/
├── Queries/GetSchools, GetCategories, GetOutfitDetail
└── DTOs/SchoolDto, CategoryDto, OutfitDetailResponse, etc.

Features/Schools/
├── Commands/UpdateSchoolProfile, ImportStudentData, PublishCampaign
├── Commands/CreateOutfit, UpdateOutfit, DeleteOutfit
├── Queries/GetSchoolProfile, GetSchoolOrders, GetSchoolOutfits
├── Queries/GetCampaignProgress, GetSalesReport, GetFeedbackReport
├── DTOs/SchoolProfileDto, SchoolOrderDto, OutfitDto, etc.
└── Validators/UpdateSchoolProfileCommandValidator, etc.

Features/TryOn/
└── Commands/GuestTryOn (Command, Handler, Validator, Response)

Features/Admin/
├── Commands/ApproveUser, SuspendUser, RemoveFeedback
└── Queries/GetAllUsers, GetAllFeedbacks

Controllers/
├── AuthController.cs
├── PublicController.cs
├── UserController.cs
├── ChildrenController.cs
├── SchoolsController.cs
├── TryOnController.cs
└── AdminController.cs
```
