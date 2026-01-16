# VTOS Backend Development Rules

This document compiles all development rules, guidelines, and best practices for the VTOS (Virtual Try-On System) backend project.

---

## 📋 Table of Contents

1. [Documentation Rules](#documentation-rules)
2. [Architecture Rules](#architecture-rules)
3. [Database Rules](#database-rules)
4. [Testing Rules](#testing-rules)
5. [Code Quality Rules](#code-quality-rules)
6. [Project Structure Rules](#project-structure-rules)
7. [Dependency Rules](#dependency-rules)
8. [Daily Workflow Rules](#daily-workflow-rules)

---

## 📝 Documentation Rules

### Allowed Documentation Files
- **ONLY** update these files:
  - `Implement.md` - Log all implementation progress, features completed, and bug fixes
  - `Tasklist.md` - Track what we will build, what we are building, and what we have built
  - `DevelopmentRules.md` - This file (compilation of all rules)

### Prohibited Actions
- ❌ **DO NOT** create any other `.md` files
- ❌ **DO NOT** create documentation files outside of the allowed list
- ✅ **DO** update `Implement.md` after completing any function or fixing bugs
- ✅ **DO** maintain `Tasklist.md` with current status of all tasks

---

## 🏗️ Architecture Rules

### Clean Architecture Principles
The project follows **Clean Architecture + Modular Monolith** pattern with strict layer separation:

```
┌─────────────────┐
│   VTOS.API      │  Presentation Layer
└────────┬────────┘
         │
    ┌────┴────┐
    │         │
┌───▼────┐ ┌──▼──────────────┐
│Application│ │ Infrastructure │
└───┬────┘ └──┬──────────────┘
    │         │
    └────┬────┘
         │
    ┌────▼────┐
    │ Domain  │  Core Business Logic (NO dependencies)
    └─────────┘
```

### Layer Responsibilities

#### VTOS.Domain
- **Purpose**: Core business logic and entities
- **Dependencies**: **NONE** (zero dependencies)
- **Contains**: Entities, Value Objects, Domain Events, Enums, Exceptions

#### VTOS.Application
- **Purpose**: Use cases, business logic orchestration, contracts/interfaces
- **Dependencies**: Only `VTOS.Domain`
- **Contains**: Features (CQRS-style), DTOs, Validators, Mappings, Abstractions

#### VTOS.Infrastructure
- **Purpose**: Technical implementations (database, external services, file storage)
- **Dependencies**: `VTOS.Application` and `VTOS.Domain`
- **Contains**: DbContext, Repositories, Payment Gateways, File Storage, AI Clients

#### VTOS.API
- **Purpose**: HTTP endpoints, request/response handling
- **Dependencies**: `VTOS.Application` and `VTOS.Infrastructure`
- **Contains**: Controllers, Middlewares, Filters, Extensions

#### VTOS.Shared
- **Purpose**: Cross-cutting concerns, shared utilities
- **Dependencies**: None (or minimal)
- **Contains**: Constants, Helpers, Results, Common types

---

## 🔗 Dependency Rules (CRITICAL)

**STRICT DEPENDENCY FLOW - NEVER VIOLATE:**

```
API → Application → Domain
API → Infrastructure
Infrastructure → Application → Domain
Application → Domain
Domain → NOTHING
```

### Rules:
- ✅ **Domain** can have **ZERO** dependencies
- ✅ **Application** can only depend on **Domain**
- ✅ **Infrastructure** can depend on **Application** and **Domain**
- ✅ **API** can depend on **Application** and **Infrastructure**
- ❌ **NEVER** let Domain reference any other project
- ❌ **NEVER** let Application reference Infrastructure or API
- ❌ **NEVER** create circular dependencies

---

## 🗄️ Database Rules

### Database Design Source
- **Primary Source**: `DB.txt` - Contains the complete database schema
- **Approach**: **Code First** (Entity Framework Core)
- **Database**: SQL Server

### Database Workflow
1. ✅ **Always check `DB.txt`** at the start of each new session
2. ✅ **Convert DB.txt schema** to Entity Framework Code First entities
3. ✅ **Create Entity Configurations** in `VTOS.Infrastructure/Persistence/Configurations/`
4. ✅ **Use Migrations** for database schema changes
5. ✅ **Never** modify database directly - always use migrations

### Entity Naming Conventions
- Entity classes: PascalCase (e.g., `User`, `Order`, `ProductVariant`)
- Primary keys: `{EntityName}ID` (e.g., `UserID`, `OrderID`)
- Foreign keys: `{ReferencedEntity}ID` (e.g., `SchoolID`, `ParentUserID`)
- Navigation properties: Entity name (e.g., `User`, `School`)

---

## 🧪 Testing Rules

### Test-Driven Development (TDD) Workflow

When creating a function or fixing a bug:

1. ✅ **Create a test** that validates the function/bug fix
2. ✅ **Run the test** to ensure it passes the condition
3. ✅ **Delete the test** after verification
4. ✅ **Write log** into `Implement.md` documenting:
   - What was implemented/fixed
   - Test conditions that were verified
   - Date and brief description

### Test Structure
```
tests/
├── VTOS.UnitTests/
│   ├── Domain/
│   └── Application/
└── VTOS.IntegrationTests/
    ├── API/
    └── Infrastructure/
```

### Testing Best Practices
- Write tests before or immediately after implementation
- Test should verify the specific condition/requirement
- Delete test after verification (keep only production code)
- Document test results in `Implement.md`

---

## 💻 Code Quality Rules

### Naming Conventions
- **Classes**: PascalCase (e.g., `UserService`, `OrderRepository`)
- **Interfaces**: PascalCase with `I` prefix (e.g., `IUserService`, `IRepository`)
- **Methods**: PascalCase (e.g., `GetUserById`, `CreateOrder`)
- **Properties**: PascalCase (e.g., `FullName`, `OrderDate`)
- **Private fields**: camelCase with `_` prefix (e.g., `_logger`, `_context`)
- **Constants**: PascalCase (e.g., `MaxRetryCount`, `DefaultPageSize`)

### Code Organization
- Follow the folder structure defined in `Structure.md`
- Group related functionality together
- Use feature folders in `VTOS.Application/Features/`
- Keep controllers thin - delegate to application layer

### Security Rules
- ✅ Always hash passwords (never store plain text)
- ✅ Use JWT tokens for authentication
- ✅ Validate all user inputs
- ✅ Implement proper authorization checks
- ✅ Sanitize data before database operations
- ✅ Use parameterized queries (EF Core handles this)

---

## 📁 Project Structure Rules

### Required Folder Structure
Follow the exact structure defined in `Structure.md`:

- **VTOS.Domain**: Common, Entities, Enums, ValueObjects, Exceptions
- **VTOS.Application**: Abstractions, Features, DTOs, Validators, Mappings
- **VTOS.Infrastructure**: Persistence, Repositories, Identity, Payments, FileStorage, AI
- **VTOS.API**: Controllers, Middlewares, Filters, Extensions, Contracts
- **VTOS.Shared**: Constants, Helpers, Results

### File Organization
- One class per file
- File name matches class name
- Group related files in appropriate folders
- Keep namespaces aligned with folder structure

---

## 🔄 Daily Workflow Rules

### Start of Each Session
1. ✅ **Check `DB.txt`** for any new database changes
2. ✅ **Review `Tasklist.md`** to see current progress
3. ✅ **Review `Implement.md`** to see what was completed
4. ✅ **Update `Tasklist.md`** if needed

### During Development
1. ✅ Follow TDD workflow (create test → verify → delete → log)
2. ✅ Maintain dependency rules strictly
3. ✅ Follow naming conventions
4. ✅ Update `Tasklist.md` as you progress

### End of Session
1. ✅ Update `Implement.md` with completed work
2. ✅ Update `Tasklist.md` status
3. ✅ Ensure all code compiles without errors
4. ✅ Commit changes with clear messages

---

## 🚫 Common Mistakes to Avoid

### Architecture Violations
- ❌ Don't add dependencies to Domain layer
- ❌ Don't reference Infrastructure from Application
- ❌ Don't put business logic in Controllers
- ❌ Don't put infrastructure code in Domain

### Database Violations
- ❌ Don't modify database directly
- ❌ Don't ignore `DB.txt` changes
- ❌ Don't create entities without corresponding configurations

### Documentation Violations
- ❌ Don't create new `.md` files
- ❌ Don't forget to update `Implement.md`
- ❌ Don't skip updating `Tasklist.md`

### Testing Violations
- ❌ Don't skip writing tests for new functions
- ❌ Don't commit code without verifying it works
- ❌ Don't forget to document test results

---

## 📚 Technology Stack

### Framework & Tools
- **.NET**: 8.0
- **Entity Framework**: Core (Code First)
- **Database**: SQL Server
- **IDE**: Visual Studio 2022
- **Version Control**: Git/GitHub
- **CI/CD**: Azure DevOps / GitHub Actions

### Key Packages (to be added)
- Entity Framework Core
- JWT Authentication
- AutoMapper (for DTOs)
- FluentValidation (for validators)
- Swagger/OpenAPI

---

## ✅ Checklist Before Committing

- [ ] All code compiles without errors
- [ ] Dependency rules are followed
- [ ] Tests created and verified (then deleted)
- [ ] `Implement.md` updated with changes
- [ ] `Tasklist.md` updated with status
- [ ] `DB.txt` checked for changes (if working on entities)
- [ ] Code follows naming conventions
- [ ] No hardcoded values (use configuration)
- [ ] Security best practices followed

---

## 📞 Important Notes

1. **This is a Capstone Project** - Follow best practices for thesis defense
2. **Production-Ready Code** - Write code as if it will be deployed
3. **Clean Code** - Maintainability and readability are priorities
4. **Documentation** - Keep `Implement.md` and `Tasklist.md` up to date
5. **Consistency** - Follow the established patterns throughout the project
6. **Following** - Update Tasklist.md and Implement.md after every request or change anything
---

**Last Updated**: 2024
**Version**: 1.0

