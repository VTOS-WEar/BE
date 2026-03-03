# VTOS Backend API Endpoints

> **Status**: Not yet implemented (Phase 2 - Infrastructure)

## Planned API Structure

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/register` | User registration |
| POST | `/api/auth/refresh` | Refresh token |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List users (Admin) |
| GET | `/api/users/{id}` | Get user by ID |
| PUT | `/api/users/{id}` | Update user |
| POST | `/api/users/me/verify` | Submit verification profile info (UC-07) |

### Children (Students)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/children` | List all my children profiles (UC-08) |
| GET | `/api/children/{id}` | Get specific child profile (UC-08) |
| POST | `/api/children` | Create child profile |
| PUT | `/api/children` | Update child profile (UC-09) |
| DELETE | `/api/children/{id}` | Delete child profile |

### Outfits (Uniforms)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/public/outfits` | List outfits with filters (Public) |
| GET | `/api/public/outfits/{id}` | Get outfit details (Public) |
| GET | `/api/schools/me/outfits` | List outfits of logged-in school (School Admin) |
| POST | `/api/schools/me/outfits` | Create outfit for school (School Admin) |
| PUT | `/api/schools/me/outfits/{id}` | Update existing outfit (School Admin) |
| DELETE | `/api/schools/me/outfits/{id}` | Delete outfit (School Admin) |
| GET | `/api/outfits/recommend` | AI recommendations |

### Campaigns (Pre-orders)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/schools/me/campaigns` | Create and publish pre-order campaign (School Admin) |
| GET | `/api/schools/me/campaigns/{id}/progress` | Track campaign progress (School Admin) |

### Try-On
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/tryon` | Virtual try-on session |
| GET | `/api/tryon/history` | Get try-on history |

### Orders
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/orders` | Create order |
| GET | `/api/orders` | List orders |
| GET | `/api/orders/{id}` | Get order details |
| PUT | `/api/orders/{id}/status` | Update order status |

### Payments
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/payments` | Create payment |
| POST | `/api/payments/webhook` | Payment callback |
| POST | `/api/payments/{id}/refund` | Request refund |

---

## Implementation Status
- 🔲 Controllers not created yet
- 🔲 Waiting for Repository Pattern implementation
