# VTOS Backend API Endpoints

> **Last Updated**: 2026-03-08
> **Status**: Partially Implemented (9 controllers active)

## Authentication ✅
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| POST | `/api/auth/register` | User registration + OTP | ✅ Done |
| POST | `/api/auth/login` | Login (email/password) | ✅ Done |
| POST | `/api/auth/verify-email` | Verify email with OTP | ✅ Done |
| POST | `/api/auth/resend-otp` | Resend OTP code | ✅ Done |
| POST | `/api/auth/verify-phone` | Verify phone + link children | ✅ Done |
| POST | `/api/auth/forgot-password` | Request password reset | ✅ Done |
| POST | `/api/auth/reset-password` | Reset password with token | ✅ Done |
| POST | `/api/auth/change-password/request-otp` | Request OTP for password change | ✅ Done |
| POST | `/api/auth/change-password` | Change password with OTP | ✅ Done |
| POST | `/api/auth/google` | Sign in via Google | 🔲 TODO |
| POST | `/api/auth/logout` | Sign out (revoke token) | 🔲 TODO |
| POST | `/api/auth/refresh-token` | Refresh access token | 🔲 TODO |

## Users ✅
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET | `/api/users/me` | Get current user profile | ✅ Done |
| PUT | `/api/users/me/profile` | Update profile (name, DOB, gender) | ✅ Done |
| PUT | `/api/users/me/avatar` | Update avatar image | ✅ Done |
| POST | `/api/users/me/verify` | Submit verification (name, phone, avatar) | ✅ Done |

## Children (Students) ✅
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET | `/api/children` | List my children profiles | ✅ Done |
| GET | `/api/children/{id}` | Get specific child profile | ✅ Done |
| PUT | `/api/children` | Update child profile | ✅ Done |

## Public (No Auth) ✅
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET | `/api/public/schools` | List schools (search + pagination) | ✅ Done |
| GET | `/api/public/categories` | List uniform categories | ✅ Done |
| GET | `/api/public/outfits/{id}` | Get uniform detail | ✅ Done |
| GET | `/api/public/schools/{id}` | Get school detail | 🔲 TODO |
| GET | `/api/public/outfits` | List uniforms (filtered) | 🔲 TODO |

## Schools (School Role) ✅
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET | `/api/schools/me` | Get school profile | ✅ Done |
| PUT | `/api/schools/me` | Update school profile | ✅ Done |
| POST | `/api/schools/me/students/import` | Import student data (CSV/XLSX) | ✅ Done |
| GET | `/api/schools/me/students/import/template` | Download import template | ✅ Done |
| POST | `/api/schools/me/campaigns` | Publish pre-order campaign | ✅ Done |
| GET | `/api/schools/me/orders` | View parent orders | ✅ Done |
| GET | `/api/schools/me/campaigns/{id}/progress` | Track campaign progress | ✅ Done |
| GET | `/api/schools/me/reports/sales` | View sales reports | ✅ Done |
| GET | `/api/schools/me/reports/feedback` | View feedback reports | ✅ Done |
| GET | `/api/schools/me/outfits` | List school outfits | ✅ Done |
| POST | `/api/schools/me/outfits` | Create outfit | ✅ Done |
| PUT | `/api/schools/me/outfits/{id}` | Update outfit | ✅ Done |
| DELETE | `/api/schools/me/outfits/{id}` | Delete outfit | ✅ Done |

## Try-On ✅ Guest Only
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| POST | `/api/tryon/guest` | Guest try-on (watermarked, rate-limited) | ✅ Done |
| POST | `/api/tryon` | Authenticated try-on | 🔲 TODO |
| POST | `/api/tryon/upload` | Upload photo | 🔲 TODO |
| GET | `/api/tryon/history` | Get try-on history | 🔲 TODO |
| GET | `/api/tryon/{id}/download` | Download try-on result | 🔲 TODO |

## Orders (Parent Role) ✅ Partial
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| POST | `/api/orders/checkout` | Checkout → Order + PaymentTransaction + PayOS | ✅ Done |
| PUT | `/api/orders/{orderId}/cancel` | Cancel order (Pending/Paid flows) | ✅ Done |
| GET | `/api/orders/{orderId}/status` | Track order status | ✅ Done |
| GET | `/api/orders/history` | View order history (paginated) | ✅ Done |
| PUT | `/api/orders/{id}/shipping` | Enter shipping information | 🔲 TODO |
| POST | `/api/orders/{id}/refund` | Request refund | 🔲 TODO |

## Payments — PayOS ✅
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| POST | `/api/payos/create-payment-link` | Create PayOS payment link | ✅ Done |
| GET | `/api/payos/payment-link/{id}` | Get payment link info | ✅ Done |
| POST | `/api/payos/payment-link/{id}/cancel` | Cancel payment link | ✅ Done |
| GET | `/api/payos/payment-link/{id}/invoices` | Get payment invoices | ✅ Done |
| POST | `/api/payos/webhook` | PayOS webhook (AllowAnonymous) | ✅ Done |

## Admin ✅ Partial
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET | `/api/admin/users` | List all users | ✅ Done |
| GET | `/api/admin/feedbacks` | List all feedbacks | ✅ Done |
| POST | `/api/admin/users/{id}/approve` | Approve user | ✅ Done |
| POST | `/api/admin/users/{id}/suspend` | Suspend user | ✅ Done |
| DELETE | `/api/admin/feedback/{id}` | Remove feedback | ✅ Done |
| GET | `/api/admin/users/{id}` | View user detail | 🔲 TODO |
| POST | `/api/admin/users/{id}/ban` | Ban user | 🔲 TODO |
| POST | `/api/admin/schools/{id}/approve` | Approve school request | 🔲 TODO |
| POST | `/api/admin/providers/{id}/approve` | Approve provider request | 🔲 TODO |
| GET | `/api/admin/dashboard` | Dashboard analytics | 🔲 TODO |
| GET | `/api/admin/reports/{type}` | Generate reports | 🔲 TODO |
| GET/PUT | `/api/admin/config/ai` | AI try-on config | 🔲 TODO |

## Providers 🔲 TODO
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET/PUT | `/api/providers/me` | Provider profile | 🔲 TODO |
| GET | `/api/providers/me/batches` | View batch list | 🔲 TODO |
| POST | `/api/providers/me/batches/{id}/approve` | Approve batch | 🔲 TODO |
| POST | `/api/providers/me/batches/{id}/reject` | Reject batch | 🔲 TODO |
| PUT | `/api/providers/me/batches/{id}/status` | Update production status | 🔲 TODO |
| POST | `/api/providers/me/batches/{id}/deliver` | Deliver uniforms | 🔲 TODO |

---

## Controllers (9 active)

| Controller | File | Endpoints |
|------------|------|-----------|
| AuthController | `AuthController.cs` | 9 |
| PublicController | `PublicController.cs` | 3 |
| UserController | `UserController.cs` | 4 |
| ChildrenController | `ChildrenController.cs` | 3 |
| SchoolsController | `SchoolsController.cs` | 13 |
| TryOnController | `TryOnController.cs` | 1 |
| AdminController | `AdminController.cs` | 5 |
| OrdersController | `OrdersController.cs` | 4 |
| PayOSController | `PayOSController.cs` | 5 |
| **Total implemented** | | **47** |
