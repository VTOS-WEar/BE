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

### Children (Students)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/children` | List children for parent |
| POST | `/api/children` | Create child profile |
| PUT | `/api/children/{id}` | Update child profile |
| DELETE | `/api/children/{id}` | Delete child profile |

### Outfits (Uniforms)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/outfits` | List outfits with filters |
| GET | `/api/outfits/{id}` | Get outfit details |
| POST | `/api/outfits` | Create outfit (Admin) |
| GET | `/api/outfits/recommend` | AI recommendations |

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
