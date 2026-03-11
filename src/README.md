# Field Mapping System - Setup and Execution Guide

**Complete guide for setting up and running the .NET Web API + Angular + PostgreSQL solution**

---

## 📋 Table of Contents
1. [Prerequisites](#prerequisites)
2. [Architecture Overview](#architecture-overview)
3. [Database Setup (EF Core Migrations)](#database-setup-ef-core-migrations)
4. [Backend API Setup](#backend-api-setup)
5. [Frontend Setup](#frontend-setup)
6. [Running the Application](#running-the-application)
7. [API Documentation](#api-documentation)
8. [Testing](#testing)
9. [Troubleshooting](#troubleshooting)

---

## ✅ Prerequisites

Make sure you have the following installed on your system:

### Required Software

1. **PostgreSQL 14 or higher**
   - Download: https://www.postgresql.org/download/
   - Default port: 5432
   - Create a user with password (e.g., postgres/postgres)

2. **.NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation: `dotnet --version`

3. **Node.js 18+ and npm**
   - Download: https://nodejs.org/
   - Verify installation: `node --version` and `npm --version`

4. **Angular CLI**
   - Install globally: `npm install -g @angular/cli`
   - Verify installation: `ng version`

### Optional but Recommended

- **pgAdmin 4** - PostgreSQL GUI tool
- **Postman** or **Thunder Client** - For API testing
- **Visual Studio Code** - With C# and Angular extensions

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      Frontend (Angular)                      │
│                    http://localhost:51748                     │
│   - TMS Systems Management                                   │
│   - Templates & Versioning UI                                │
│   - Real-time updates                                        │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP/REST API
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                  Backend (.NET 8 Web API)                    │
│                    http://localhost:63590                     │
│   - Controllers & Core Logic                                 │
│   - Automated Auditing (BaseEntity)                          │
│   - Swagger/OpenAPI Documentation                            │
│   - CORS enabled                                             │
└──────────────────────┬──────────────────────────────────────┘
                       │ Entity Framework Core + Npgsql
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                  Database (PostgreSQL)                       │
│                    localhost:5432                            │
│   Key Entities:                                              │
│   - Partners (Organizational Units)                          │
│   - Templates (Root Definitions)                             │
│   - TemplateVersions (Schema & Validation)                   │
│   - TemplateAssignments (Mapping Links)                      │
│   - FieldMappings (Transformation Rules)                     │
│   - TmsSystems, Customers, LookupTables                      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Database Setup (EF Core Migrations)

We use **Entity Framework Core Migrations** to manage the database schema.

### Step 1: Install EF Core CLI Tool
If you haven't already:
```powershell
dotnet tool install --global dotnet-ef
```

### Step 2: Create the Database
Ensure PostgreSQL is running, then run:
```powershell
psql -U postgres -c "CREATE DATABASE fieldmapping;"
```

### Step 3: Apply Migrations
Navigate to the WebApi directory and apply the initial schema:
```powershell
cd src/Transflo.Platform.Transformer/Transflo.Platform.Transformer.WebApi
dotnet ef database update --project ../Transflo.Platform.Transformer.Core --startup-project .
```

### Step 4: Verify Database Structure
Use `psql` or `pgAdmin` to check the created tables:
```bash
psql -U postgres -d fieldmapping -c "\dt"
```
You should see tables like `partners`, `templates`, `template_versions`, `template_assignments`, etc.

---

## 🔧 Backend API Setup

### Step 1: Navigate to the API Project
```powershell
cd src/Transflo.Platform.Transformer/Transflo.Platform.Transformer.WebApi
```

### Step 2: Configure Database Connection
Edit `appsettings.json` (or `appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fieldmapping;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### Step 3: Restore and Build
```powershell
dotnet restore
dotnet build
```

---

## 🎨 Frontend Setup

### Step 1: Navigate to Angular Project
```powershell
cd src/angular-app
```

### Step 2: Install Dependencies
```powershell
npm install
```

---

## 🚀 Running the Application

### Option 1: Run Separately (Standard Development)

#### Terminal 1 - Start Backend API
```powershell
cd src/Transflo.Platform.Transformer/Transflo.Platform.Transformer.WebApi
dotnet run
```
- **Swagger UI**: http://localhost:63590/swagger

#### Terminal 2 - Start Angular Frontend
```powershell
cd src/angular-app
ng serve
```
- **Frontend App**: http://localhost:51748

### Option 2: Watch Mode (Auto-reload)
```powershell
cd src/Transflo.Platform.Transformer/Transflo.Platform.Transformer.WebApi
dotnet watch run
```

---

## 📚 API Documentation

Once running, access Swagger at: **http://localhost:63590/swagger**

### Mapping System Features
- **BaseEntity**: All entities include automatic audit tracking (`created_at`, `updated_at`, `created_by`, etc.).
- **Soft Delete**: Entities support logical deletion (`deleted_at`).
- **Versioning**: Templates are versioned via `TemplateVersion`.
- **JSONB Metadata**: All entities have a `metadata` column for flexible data storage.
- **Concurrency**: `Revision` column used for optimistic concurrency control.

---

## 🧪 Testing

### Backend Tests
```powershell
cd src/Transflo.Platform.Transformer/Transflo.Platform.Transformer.Core.Tests
dotnet test
```

---

## 🌍 Development Workflow

### Adding/Modifying Models
1. Update classes in `Transflo.Platform.Transformer.Core/Models`.
2. Update `FieldMappingDbContext`.
3. Create a migration:
   ```powershell
   dotnet ef migrations add <MigrationName> --project ../Transflo.Platform.Transformer.Core --startup-project .
   ```
4. Apply to DB:
   ```powershell
   dotnet ef database update --project ../Transflo.Platform.Transformer.Core --startup-project .
   ```

---

## 🐛 Troubleshooting

**Problem**: `Npgsql.PostgresException: 28P01: password authentication failed`
- **Solution**: Check your `appsettings.json` connection string password.

**Problem**: `dotnet ef` command not found
- **Solution**: Run `dotnet tool install --global dotnet-ef` and restart your terminal.

**Problem**: Migration failed due to identity column
- **Solution**: If changing ID types (e.g., int to GUID), manual migration editing (`DropColumn`/`AddColumn`) may be required as EF `AlterColumn` doesn't support identity type changes on all providers.

---

## 📝 Summary
The system provides a robust, version-aware field mapping engine with:
- ✅ **EF Core Migrations** for version-controlled schema.
- ✅ **Multi-Tenant Ready** with Partner/Template assignments.
- ✅ **JSONB Support** for flexible schemas and mapping configs.
- ✅ **Automated Auditing** in the database layer.
