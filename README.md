# VTOS Backend

**Virtual Try-On System** - Backend API for school uniform virtual try-on.

## Tech Stack

- **.NET 8** 
- **Entity Framework Core 8.0**
- **SQL Server**
- **Clean Architecture + Modular Monolith**

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server Express)
- Visual Studio 2022 / VS Code / Rider

---

## 📦 Database Setup

### Step 1: Install EF Core Tools (one-time)

```bash
dotnet tool install --global dotnet-ef
```

### Step 2: Create your `appsettings.Development.json`

Create file `VTOS.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=VTOSDatabase;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Replace:**
- `YOUR_SERVER` → Your SQL Server name (e.g., `localhost`, `.\SQLEXPRESS`, `DESKTOP-XXX\SQLEXPRESS`)
- `YOUR_PASSWORD` → Your `sa` password

> ⚠️ **Note:** This file is in `.gitignore` - each developer creates their own.

### Step 3: Run Migration

```bash
cd Vtos.Backend

# Create database and tables
dotnet ef database update --project VTOS.Infrastructure --startup-project VTOS.API
```

### Step 4: Verify

Open SQL Server Management Studio and check if `VTOSDatabase` exists with 26 tables.

---

## 🔧 Common EF Commands

| Command | Description |
|---------|-------------|
| `dotnet ef migrations add <Name> --project VTOS.Infrastructure --startup-project VTOS.API` | Create new migration |
| `dotnet ef database update --project VTOS.Infrastructure --startup-project VTOS.API` | Apply migrations |
| `dotnet ef migrations remove --project VTOS.Infrastructure --startup-project VTOS.API` | Remove last migration |
| `dotnet ef database drop --project VTOS.Infrastructure --startup-project VTOS.API` | Drop database |

---

## ▶️ Run the Application

```bash
cd VTOS.API
dotnet run
```

Open: `https://localhost:5001/swagger`

---

## 📁 Project Structure

```
Vtos.Backend/
├── VTOS.API/            # Controllers, Middlewares
├── VTOS.Application/    # Use Cases, DTOs
├── VTOS.Domain/         # Entities, Enums, Value Objects
├── VTOS.Infrastructure/ # EF Core, Repositories
├── VTOS.Shared/         # Constants, Helpers
└── docs/                # Documentation
```

---

## 📚 Documentation

- [Development Rules](docs/DevelopmentRules.md)
- [Project Structure](docs/Structure.md)
- [Database Schema](docs/database/schema.md)
- [API Endpoints](docs/api/endpoints.md)

---

## 👥 Team

- **Project Manager**: Manages CHANGELOG, reviews PRs
- **Developers**: Create features, submit PRs

