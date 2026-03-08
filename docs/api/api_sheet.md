# VTOS Backend - API Documentation Sheet

> **Version**: 1.2  
> **Last Updated**: 2026-03-08  
> **Status**: Partially Implemented

---

## 📋 Legend

| Column | Description |
|--------|-------------|
| **Endpoint** | URL path của API |
| **Description** | Mô tả chức năng |
| **Method** | HTTP method (GET, POST, PUT, DELETE, PATCH) |
| **Input** | Request body / Query params / Path params |
| **Output** | Response body (success case) |
| **Errors** | Các lỗi có thể xảy ra (unhappy path) |
| **Notes** | Ghi chú thêm (nếu có) |
| **Packages** | NuGet packages khuyến nghị |

---

## 🔐 1. Authentication Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/auth/register` | Đăng ký tài khoản mới (Gửi OTP email) | `POST` | `{ "email": "string", "password": "string", "fullName": "string" }` | `{ "userId": "guid", "email": "string", "message": "OTP sent to email" }` | `400` Email đã tồn tại | Password ≥8 ký tự | `FluentValidation`, `BCrypt`, `MailKit` |
| `/api/auth/login` | Đăng nhập hệ thống | `POST` | `{ "email": "string", "password": "string" }` | `{ "accessToken": "jwt_string", "refreshToken": "string", "expiresIn": 3600, "user": { "userId": "guid", "email": "string", "fullName": "string", "role": "string" } }` | `401` Sai email/password, `403` Tài khoản bị khóa | JWT + Refresh token | `System.IdentityModel.Tokens.Jwt` |
| `/api/auth/verify-email` | Xác thực OTP đăng ký | `POST` | `{ "email": "string", "otp": "string" }` | `{ "userId": "guid", "email": "string", "token": "jwt_string", "message": "Email verified" }` | `400` OTP sai/hết hạn | Creates Active User | - |
| `/api/auth/verify-phone` | Xác thực SĐT & Link trẻ em | `POST` | `{ "phoneNumber": "string" }` | `{ "success": true, "message": "Phone verified", "linkedChildren": [{ "childId": "guid", "fullName": "string" }], "schoolCount": 1 }` | `401` auth, `404` Not found | Requires Login | - |
| `/api/auth/resend-otp` | Gửi lại mã OTP | `POST` | `{ "email": "string" }` | `{ "message": "OTP resent" }` | `400` User not found | Rate limit 60s | - |
| `/api/auth/refresh-token` | Refresh access token | `POST` | `{ "refreshToken": "string" }` | `{ "accessToken": "jwt_string", "refreshToken": "new_refresh_token", "expiresIn": 3600 }` | `401` Token hết hạn/không hợp lệ | Refresh token 7 ngày | - |
| `/api/auth/logout` | Đăng xuất | `POST` | Header: `Authorization: Bearer {token}` | `{ "message": "Logout successful" }` | `401` Unauthorized | Revoke refresh token | - |
| `/api/auth/forgot-password` | Quên mật khẩu | `POST` | `{ "email": "string" }` | `{ "message": "Reset link sent to email" }` | `404` Email không tồn tại, `429` Rate limit | Token 15 phút | `MailKit` |
| `/api/auth/forgot-password` | Quên mật khẩu | `POST` | `{ "email": "string" }` | `{ "message": "Reset link sent to email" }` | `404` Email không tồn tại, `429` Rate limit | Token 15 phút | `MailKit` |
| `/api/auth/reset-password` | Đặt lại mật khẩu | `POST` | `{ "token": "string", "newPassword": "string" }` | `{ "message": "Password reset successful" }` | `400` Token hết hạn/Password không hợp lệ | - | - |
| `/api/auth/change-password/request-otp` | Yêu cầu OTP đổi mật khẩu | `POST` | Header: `Authorization` | `{ "message": "OTP sent to email" }` | `401` Auth | Rate limit 3/hour | - |
| `/api/auth/change-password` | Đổi mật khẩu (OTP) | `POST` | `{ "otp": "string", "currentPassword": "string", "newPassword": "string" }` | `{ "message": "Password changed successfully" }` | `400` OTP sai/Password sai | Requires Auth | - |

---

## 👤 2. User Management Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/users/profile` | Lấy profile user hiện tại | `GET` | Header: `Authorization: Bearer {token}` | `{ "userId": "guid", "email": "string", "fullName": "string", "phone": "string", "role": { "roleId": "guid", "roleName": "string" }, "isActive": true, "createdAt": "datetime", "lastLogin": "datetime" }` | `401` Unauthorized | - | - |
| `/api/users/profile` | Cập nhật profile | `PUT` | `{ "fullName": "string", "phone": "string" }` | `{ "userId": "guid", "fullName": "string", "phone": "string", "message": "Profile updated" }` | `401` Unauthorized, `400` Validation error | - | `FluentValidation` |
| `/api/users/change-password` | Đổi mật khẩu | `POST` | `{ "currentPassword": "string", "newPassword": "string" }` | `{ "message": "Password changed successfully" }` | `401` Unauthorized, `400` Sai password cũ | - | - |
| `/api/users` | Lấy danh sách users (Admin) | `GET` | Query: `?page=1&pageSize=10&role=Parent` | `{ "items": [{ "userId": "guid", "email": "string", "fullName": "string", "role": "string", "isActive": true }], "totalCount": 100, "page": 1, "pageSize": 10, "totalPages": 10 }` | `401` Unauthorized, `403` Forbidden | Phân trang | - |
| `/api/users/{id}` | Lấy user theo ID (Admin) | `GET` | Path: `id` (GUID) | `{ "userId": "guid", "email": "string", "fullName": "string", "phone": "string", "role": { "roleId": "guid", "roleName": "string" }, "isActive": true, "createdAt": "datetime" }` | `401`, `403`, `404` | - | - |
| `/api/users/{id}/status` | Activate/Deactivate user | `PATCH` | `{ "isActive": boolean }` | `{ "userId": "guid", "isActive": boolean, "message": "Status updated" }` | `401`, `403`, `404` | Admin only | - |

---

## 👶 3. Children (Students) Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/children` | Lấy danh sách con | `GET` | Header: `Authorization` | `{ "items": [{ "childId": "guid", "fullName": "string", "age": 10, "grade": "5A", "gender": "Male", "school": { "schoolId": "guid", "schoolName": "string" } }], "totalCount": 2 }` | `401` Unauthorized | Chỉ con của user | - |
| `/api/children` | Thêm hồ sơ con mới | `POST` | `{ "fullName": "string", "age": int, "grade": "string", "gender": "Male/Female", "schoolId": "guid" }` | `{ "childId": "guid", "fullName": "string", "age": 10, "grade": "5A", "gender": "Male", "schoolId": "guid", "message": "Child profile created" }` | `401`, `400`, `404` School không tồn tại | - | `FluentValidation` |
| `/api/children/{id}` | Lấy chi tiết hồ sơ con | `GET` | Path: `id` (GUID) | `{ "childId": "guid", "fullName": "string", "age": 10, "grade": "5A", "gender": "Male", "school": { "schoolId": "guid", "schoolName": "string", "logoURL": "string" }, "measurements": { "height": 140, "weight": 35, "chest": 70 } }` | `401`, `403`, `404` | Bao gồm measurements | - |
| `/api/children/{id}` | Cập nhật hồ sơ con | `PUT` | `{ "fullName": "string", "age": int, "grade": "string", "gender": "string" }` | `{ "childId": "guid", "fullName": "string", "age": 10, "message": "Profile updated" }` | `401`, `403`, `404` | Không đổi school | - |
| `/api/children/{id}` | Xóa hồ sơ con | `DELETE` | Path: `id` (GUID) | `{ "message": "Child profile deleted" }` | `401`, `403`, `404` | Soft delete | - |
| `/api/children/{id}/measurements` | Lưu số đo | `POST` | `{ "height": decimal, "weight": decimal, "chest": decimal, "waist": decimal, "hip": decimal }` | `{ "childId": "guid", "measurements": { "height": 140, "weight": 35, "chest": 70, "waist": 60, "hip": 75 }, "message": "Measurements saved" }` | `401`, `400` | Cho AI fit analysis | - |

---

## 🏫 4. Schools Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/schools` | Lấy danh sách trường | `GET` | Query: `?search=keyword&page=1&pageSize=10` | `{ "items": [{ "schoolId": "guid", "schoolName": "string", "logoURL": "string", "contactInfo": "string", "outfitCount": 15 }], "totalCount": 50, "page": 1, "pageSize": 10 }` | - | Public API | - |
| `/api/schools/{id}` | Lấy chi tiết trường | `GET` | Path: `id` (GUID) | `{ "schoolId": "guid", "schoolName": "string", "logoURL": "string", "contactInfo": "string", "catalogId": "guid", "activeCampaigns": [{ "campaignId": "guid", "campaignName": "string" }] }` | `404` Not found | Bao gồm campaigns | - |
| `/api/schools/{id}/outfits` | Lấy đồng phục của trường | `GET` | Query: `?type=Uniform&available=true` | `{ "items": [{ "outfitId": "guid", "outfitName": "string", "price": 250000, "outfitType": "Uniform", "mainImageURL": "string", "isAvailable": true }], "totalCount": 15 }` | `404` School không tồn tại | Filter by type | - |
| `/api/schools/{id}/campaigns` | Lấy campaigns của trường | `GET` | Query: `?status=Active` | `{ "items": [{ "campaignId": "guid", "campaignName": "string", "startDate": "datetime", "endDate": "datetime", "status": "Active" }], "totalCount": 3 }` | `404` Not found | Filter by status | - |

---

## 👔 5. Outfits Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/outfits` | Lấy danh sách đồng phục | `GET` | Query: `?schoolId=guid&type=Uniform&categoryId=guid&minPrice=0&maxPrice=1000000&page=1&pageSize=20` | `{ "items": [{ "outfitId": "guid", "outfitName": "string", "price": 250000, "outfitType": "Uniform", "mainImageURL": "string", "school": { "schoolId": "guid", "schoolName": "string" }, "categories": ["Đồng phục"] }], "totalCount": 100, "page": 1, "pageSize": 20 }` | - | Public API | - |
| `/api/outfits/{id}` | Lấy chi tiết outfit | `GET` | Path: `id` (GUID) | `{ "outfitId": "guid", "outfitName": "string", "description": "string", "price": 250000, "outfitType": "Uniform", "mainImageURL": "string", "isAvailable": true, "isCustomizable": false, "school": {...}, "variants": [...], "sizeChart": {...}, "categories": [...], "averageRating": 4.5, "feedbackCount": 25 }` | `404` Not found | Full details | - |
| `/api/outfits/{id}/variants` | Lấy variants | `GET` | Path: `id` | `{ "items": [{ "productVariantId": "guid", "size": "M", "colorVariant": "Trắng", "materialType": "Cotton", "stockQuantity": 50, "price": 250000, "skuCode": "UNI-001-M-W", "variantImageURL": "string" }] }` | `404` Not found | Sizes, colors, stock | - |
| `/api/outfits/{id}/size-chart` | Lấy size chart | `GET` | Path: `id` | `{ "sizeChartId": "guid", "chartName": "string", "unit": "cm", "details": [{ "sizeLabel": "M", "chestMin": 80, "chestMax": 90, "waistMin": 65, "waistMax": 75, "heightMin": 150, "heightMax": 165 }] }` | `404` Not found | - | - |
| `/api/outfits/recommend` | AI gợi ý outfit | `GET` | Query: `?childId=guid&schoolId=guid` | `{ "recommendations": [{ "outfitId": "guid", "outfitName": "string", "recommendationScore": 95.5, "suggestedSize": "M", "reason": "Phù hợp với số đo" }] }` | `401`, `404` | Based on measurements | - |
| `/api/outfits` | Tạo outfit mới (Admin) | `POST` | `{ "schoolId": "guid", "outfitName": "string", "description": "string", "price": decimal, "outfitType": "Uniform", "mainImageURL": "string", "sizeChartId": "guid", "categoryIds": ["guid"] }` | `{ "outfitId": "guid", "outfitName": "string", "message": "Outfit created successfully" }` | `401`, `403`, `400` | Admin/School role | `FluentValidation` |
| `/api/outfits/{id}` | Cập nhật outfit | `PUT` | Same as POST | `{ "outfitId": "guid", "message": "Outfit updated successfully" }` | `401`, `403`, `404`, `400` | - | - |
| `/api/outfits/{id}` | Xóa outfit | `DELETE` | Path: `id` | `{ "message": "Outfit deleted successfully" }` | `401`, `403`, `404` | Soft delete | - |

---

## 🪞 6. Virtual Try-On Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/tryon` | Thực hiện try-on ảo | `POST` | `{ "childId": "guid?", "outfitId": "guid", "photo": "base64/file", "adjustments": {} }` | `{ "tryOnId": "guid", "resultPhotoURL": "string", "outfit": { "outfitId": "guid", "outfitName": "string" }, "analysis": { "suggestedSize": "M", "fitScore": 85 }, "tryOnTimestamp": "datetime" }` | `400` Photo không hợp lệ, `404`, `503` AI unavailable | childId optional | `SixLabors.ImageSharp` |
| `/api/tryon/guest` | Try-on cho guest | `POST` | `{ "guestSessionId": "string", "outfitId": "guid", "photo": "base64/file" }` | `{ "tryOnId": "guid", "resultPhotoURL": "string", "guestSessionId": "string", "remainingTries": 4 }` | `400`, `404`, `429` Rate limit | 5 lần/session | - |
| `/api/tryon/history` | Lấy lịch sử try-on | `GET` | Query: `?childId=guid&page=1&pageSize=10` | `{ "items": [{ "tryOnId": "guid", "outfit": { "outfitId": "guid", "outfitName": "string", "mainImageURL": "string" }, "resultPhotoURL": "string", "tryOnTimestamp": "datetime", "fitScore": 85 }], "totalCount": 20 }` | `401` Unauthorized | Filter by child | - |
| `/api/tryon/{id}` | Lấy chi tiết try-on | `GET` | Path: `id` (GUID) | `{ "tryOnId": "guid", "child": {...}, "outfit": {...}, "uploadedPhotoURL": "string", "resultPhotoURL": "string", "tryOnTimestamp": "datetime", "alignmentAdjustment": "string", "sourcePlatform": "Web", "analysis": { "suggestedSize": "M", "fitScore": 85, "detectedBodyProportions": "..." } }` | `401`, `404` | Full details | - |
| `/api/tryon/{id}/analysis` | Lấy AI fit analysis | `GET` | Path: `id` | `{ "analysisId": "guid", "tryOnId": "guid", "detectedBodyProportions": "string", "suggestedSize": "M", "fitScore": 85, "algorithmVersion": "v2.1" }` | `401`, `404` | - | - |
| `/api/tryon/{id}/download` | Tải ảnh kết quả | `GET` | Path: `id` | Binary file (image/png) with headers: `Content-Disposition: attachment; filename="tryon_result.png"` | `401`, `404` | Returns file | - |

---

## 🛒 7. Orders Module ✅ Partially Implemented

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Status |
|----------|-------------|--------|-------|------------------------|--------|-------|--------|
| `/api/orders/checkout` | Checkout: tạo Order + Payment + PayOS link | `POST` | `{ "campaignId": "guid?", "shippingAddress": "string", "deliveryMethod": "string", "items": [{ "productVariantId": "guid", "quantity": int, "sizeOrdered": "M" }] }` | `{ "orderId": "guid", "totalAmount": decimal, "paymentLinkId": "string", "checkoutUrl": "string" }` | `400` Validation/Stock, `401`, `404` | Creates Order(Pending) + PaymentTransaction(Pending) + PayOS link | ✅ Done |
| `/api/orders/{orderId}/cancel` | Hủy đơn hàng (2 flows) | `PUT` | Path: `orderId` | `{ "message": "Order cancelled" }` | `400` Invalid status, `401`, `403` Not owner, `404` | Pending→cancel PayOS link; Paid→create Refund | ✅ Done |
| `/api/orders/{orderId}/status` | Track order status | `GET` | Path: `orderId` | `{ "orderId": "guid", "status": "string", "items": [...], "paymentStatus": "string", "imageUrl": "string", "schoolName": "string" }` | `401`, `403` Not owner, `404` | Validates ownership | ✅ Done |
| `/api/orders/history` | Lịch sử đơn hàng (phân trang) | `GET` | Query: `?page=1&pageSize=10&status=&fromDate=&toDate=&sortBy=orderDate&sortOrder=desc&search=` | `{ "items": [...], "totalCount": int, "page": int, "pageSize": int, "totalPages": int }` | `401` Unauthorized | sortBy: orderDate/totalAmount/status; max pageSize=50 | ✅ Done |
| `/api/orders/{id}/shipping` | Nhập thông tin giao hàng | `PUT` | `{ "shippingAddress": "string", "receiverName": "string", "receiverPhone": "string" }` | `{ "message": "Shipping info updated" }` | `401`, `404` | - | 🔲 TODO |
| `/api/orders/{id}/refund` | Yêu cầu hoàn tiền | `POST` | `{ "reason": "string" }` | `{ "refundId": "guid", "status": "Pending" }` | `401`, `400`, `404` | Standalone refund request | 🔲 TODO |

---

## 💳 8. Payments Module (PayOS) ✅ Implemented

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Status |
|----------|-------------|--------|-------|------------------------|--------|-------|--------|
| `/api/payos/create-payment-link` | Tạo PayOS payment link | `POST` | `{ "amount": int, "description": "string", "returnUrl": "string", "cancelUrl": "string", "orderCode": long? }` | `{ "orderCode": long, "checkoutUrl": "string", "paymentLinkId": "string", "status": "string" }` | `400` Invalid amount/missing fields, `401`, `500` | Auth: Parent/Admin | ✅ Done |
| `/api/payos/payment-link/{paymentLinkId}` | Lấy thông tin payment link | `GET` | Path: `paymentLinkId` (string) | `{ "id": "string", "orderCode": long, "amount": int, "amountPaid": int, "amountRemaining": int, "status": "string", "transactions": [...] }` | `400`, `401`, `404` Not found, `500` | Returns status + transactions | ✅ Done |
| `/api/payos/payment-link/{paymentLinkId}/cancel` | Hủy payment link | `POST` | Path: `paymentLinkId` (string) | `{ "id": "string", "orderCode": long, "status": "CANCELLED", "cancelledAt": "datetime" }` | `400`, `401`, `404`, `500` | - | ✅ Done |
| `/api/payos/payment-link/{paymentLinkId}/invoices` | Lấy invoices | `GET` | Path: `paymentLinkId` (string) | `{ "invoices": [{ "invoiceId": "string", "amount": int, ... }] }` | `400`, `401`, `404`, `500` | - | ✅ Done |
| `/api/payos/webhook` | PayOS webhook (payment notification) | `POST` | PayOS webhook body: `{ "success": bool, "data": { "paymentLinkId": "string", "amount": int, ... } }` | `{ "message": "Webhook processed" }` | `400` Invalid data, `500` | AllowAnonymous; updates PaymentTransaction, Order, SchoolWallet, inventory | ✅ Done |

---

## ⭐ 9. Feedback Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/feedback` | Gửi feedback | `POST` | `{ "outfitId": "guid", "rating": 1-5, "comment": "string" }` | `{ "feedbackId": "guid", "outfitId": "guid", "rating": 5, "comment": "string", "timestamp": "datetime", "message": "Feedback submitted" }` | `401`, `400` Rating/Already reviewed | 1 feedback/outfit | `FluentValidation` |
| `/api/feedback/outfits/{outfitId}` | Lấy feedback của outfit | `GET` | Query: `?page=1&pageSize=10` | `{ "items": [{ "feedbackId": "guid", "user": { "userId": "guid", "fullName": "string" }, "rating": 5, "comment": "string", "timestamp": "datetime" }], "averageRating": 4.5, "totalCount": 25 }` | `404` Outfit không tồn tại | Public API | - |
| `/api/feedback/{id}` | Xóa feedback | `DELETE` | Path: `id` | `{ "message": "Feedback deleted" }` | `401`, `403` Not owner, `404` | Soft delete | - |
| `/api/feedback/{id}/moderate` | Moderate feedback | `PATCH` | `{ "moderationStatus": "Approved/Rejected" }` | `{ "feedbackId": "guid", "moderationStatus": "Approved", "message": "Moderation updated" }` | `401`, `403`, `404` | Admin only | - |

---

## 📦 10. Categories Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/categories` | Lấy danh sách | `GET` | - | `{ "items": [{ "categoryId": "guid", "categoryName": "Đồng phục", "outfitCount": 50 }] }` | - | Public API | - |
| `/api/categories/{id}` | Lấy chi tiết | `GET` | Path: `id` | `{ "categoryId": "guid", "categoryName": "Đồng phục", "outfitCount": 50, "outfits": [{ "outfitId": "guid", "outfitName": "string" }] }` | `404` Not found | Includes outfits | - |
| `/api/categories` | Tạo category | `POST` | `{ "categoryName": "string" }` | `{ "categoryId": "guid", "categoryName": "string", "message": "Category created" }` | `401`, `403`, `400` Tên trùng | Admin only | - |
| `/api/categories/{id}` | Cập nhật | `PUT` | `{ "categoryName": "string" }` | `{ "categoryId": "guid", "categoryName": "string", "message": "Category updated" }` | `401`, `403`, `404`, `400` | - | - |
| `/api/categories/{id}` | Xóa category | `DELETE` | Path: `id` | `{ "message": "Category deleted" }` | `401`, `403`, `404`, `400` Có outfits | Soft delete | - |

---

## 🏭 11. Providers Module (Admin)

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/providers` | Lấy danh sách | `GET` | Query: `?status=Active&page=1&pageSize=10` | `{ "items": [{ "providerId": "guid", "providerName": "string", "contactPersonName": "string", "phone": "string", "email": "string", "status": "Active" }], "totalCount": 20 }` | `401`, `403` | Admin only | - |
| `/api/providers/{id}` | Lấy chi tiết | `GET` | Path: `id` | `{ "providerId": "guid", "providerName": "string", "contactPersonName": "string", "phone": "string", "email": "string", "address": "string", "status": "Active", "campaignCount": 5, "batchCount": 10 }` | `401`, `403`, `404` | - | - |
| `/api/providers` | Tạo provider | `POST` | `{ "providerName": "string", "contactPersonName": "string", "phone": "string", "email": "string", "address": "string" }` | `{ "providerId": "guid", "providerName": "string", "status": "Active", "message": "Provider created" }` | `401`, `403`, `400` Email trùng | - | `FluentValidation` |
| `/api/providers/{id}` | Cập nhật | `PUT` | Same as POST | `{ "providerId": "guid", "message": "Provider updated" }` | `401`, `403`, `404`, `400` | - | - |
| `/api/providers/{id}/status` | Cập nhật status | `PATCH` | `{ "status": "Active/Inactive/Suspended" }` | `{ "providerId": "guid", "status": "Inactive", "message": "Status updated" }` | `401`, `403`, `404` | - | - |

---

## 📣 12. Campaigns Module

| Endpoint | Description | Method | Input | Output (Response Body) | Errors | Notes | Packages |
|----------|-------------|--------|-------|------------------------|--------|-------|----------|
| `/api/campaigns` | Lấy danh sách | `GET` | Query: `?schoolId=guid&status=Active` | `{ "items": [{ "campaignId": "guid", "campaignName": "string", "school": { "schoolId": "guid", "schoolName": "string" }, "startDate": "datetime", "endDate": "datetime", "status": "Active", "outfitCount": 10 }], "totalCount": 15 }` | - | Public for Active | - |
| `/api/campaigns/{id}` | Lấy chi tiết | `GET` | Path: `id` | `{ "campaignId": "guid", "campaignName": "string", "school": {...}, "startDate": "datetime", "endDate": "datetime", "status": "Active", "description": "string", "outfits": [{ "outfitId": "guid", "outfitName": "string", "campaignPrice": 200000, "provider": {...}, "maxQuantity": 100 }] }` | `404` Not found | Full details | - |
| `/api/campaigns` | Tạo campaign | `POST` | `{ "schoolId": "guid", "campaignName": "string", "startDate": "datetime", "endDate": "datetime", "description": "string", "outfits": [{ "outfitId": "guid", "providerId": "guid", "campaignPrice": decimal, "maxQuantity": int }] }` | `{ "campaignId": "guid", "campaignName": "string", "status": "Draft", "message": "Campaign created" }` | `401`, `403`, `400` | Admin/School | `FluentValidation` |
| `/api/campaigns/{id}` | Cập nhật | `PUT` | Same as POST | `{ "campaignId": "guid", "message": "Campaign updated" }` | `401`, `403`, `404`, `400` | - | - |
| `/api/campaigns/{id}/status` | Cập nhật status | `PATCH` | `{ "status": "Draft/Active/Ended/Cancelled" }` | `{ "campaignId": "guid", "status": "Active", "message": "Status updated" }` | `401`, `403`, `404`, `400` Invalid transition | - | - |

---

## 📦 Recommended NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `FluentValidation.AspNetCore` | 11.3.0 | Request validation |
| `BCrypt.Net-Next` | 4.0.3 | Password hashing |
| `System.IdentityModel.Tokens.Jwt` | 7.0.0 | JWT token generation |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 | JWT authentication middleware |
| `MailKit` | 4.3.0 | Email sending |
| `SixLabors.ImageSharp` | 3.1.0 | Image processing |
| `Swashbuckle.AspNetCore` | 6.5.0 | Swagger/OpenAPI documentation |
| `ClosedXML` | - | Excel file (.xlsx) parsing for student import |
| `Net.payOS` | - | PayOS payment gateway integration |

---

## 📊 Summary

| Module | Endpoints | Auth Required | Implementation |
|--------|-----------|---------------|----------------|
| Authentication | 11 | Mixed | ✅ Done |
| User Management | 6 | Yes | ✅ Partial |
| Children | 6 | Yes | ✅ Partial |
| Schools | 4 | No | ✅ Done |
| Outfits | 8 | Mixed | ✅ Partial |
| Try-On | 6 | Mixed | ✅ Guest only |
| Orders | 6 | Yes | ✅ 4/6 Done |
| Payments (PayOS) | 5 | Mixed | ✅ Done |
| Feedback | 4 | Mixed | ✅ Partial |
| Categories | 5 | Mixed | ✅ Read only |
| Providers | 5 | Admin Only | 🔲 TODO |
| Campaigns | 5 | Mixed | ✅ Partial |
| **TOTAL** | **71** | - | - |

---

## 🔗 Related Documents

- [endpoints.md](endpoints.md) - Original endpoint outline
- [../database/schema.md](../database/schema.md) - Database schema
- [../DevelopmentRules.md](../DevelopmentRules.md) - Development guidelines
