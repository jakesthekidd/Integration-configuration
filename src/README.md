# Field Mapping System - Setup and Execution Guide

**Complete guide for setting up and running the .NET Web API + Angular + PostgreSQL solution**

---

## 📋 Table of Contents
1. [Prerequisites](#prerequisites)
2. [Architecture Overview](#architecture-overview)
3. [Database Setup](#database-setup)
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
│   - Field Mapping Templates                                  │
│   - Real-time updates                                        │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP/REST API
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                  Backend (.NET 8 Web API)                    │
│                    http://localhost:63590                     │
│   - Minimal APIs                                             │
│   - Dependency Injection                                     │
│   - Swagger/OpenAPI Documentation                            │
│   - CORS enabled                                             │
└──────────────────────┬──────────────────────────────────────┘
                       │ Entity Framework Core + Npgsql
                       ↓
┌─────────────────────────────────────────────────────────────┐
│                  Database (PostgreSQL)                       │
│                    localhost:5432                            │
│   Tables:                                                    │
│   - tms_systems                                              │
│   - field_mapping_templates                                  │
│   - field_mappings                                           │
│   - lookup_tables                                            │
│   - transformation_logs                                      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Database Setup

### Step 1: Install PostgreSQL

1. Download and install PostgreSQL from the official website
2. During installation, remember your postgres user password
3. Keep the default port (5432)

### Step 2: Create the Database

Open psql or pgAdmin and run:

```sql
CREATE DATABASE fieldmapping;
```

Or use the command line:

```bash
psql -U postgres
CREATE DATABASE fieldmapping;
\q
```

### Step 3: Run the Initialization Script

Navigate to the migrations directory:

```bash
cd c:\Work\Projects\Transflo.Integration.WFAI\src\webapi\Migrations
```

Execute the SQL script:

```bash
psql -U postgres -d fieldmapping -f init-database.sql
```

Or manually open the file in pgAdmin and execute it.

### Step 4: Verify Database Creation

Connect to the database and check tables:

```bash
psql -U postgres -d fieldmapping

# List tables
\dt

# Check seeded data
SELECT * FROM tms_systems;
```

You should see:
- `tms_systems` table with 2 records (TruckMate, McLeod)
- `field_mapping_templates` table (empty)
- `field_mappings` table (empty)
- `lookup_tables` table (empty)
- `transformation_logs` table (empty)

---

## 🔧 Backend API Setup

### Step 1: Navigate to the API Project

```bash
cd c:\Work\Projects\Transflo.Integration.WFAI\src\webapi
```

### Step 2: Configure Database Connection

Edit `appsettings.json` if needed (default configuration shown):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fieldmapping;Username=postgres;Password=postgres"
  }
}
```

**Update the password** if you used a different one during PostgreSQL installation.

### Step 3: Restore NuGet Packages

```bash
dotnet restore
```

### Step 4: Build the Project

```bash
dotnet build
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Step 5: Create EF Core Migration (Optional)

If you want to use EF Core migrations instead of manual SQL:

```bash
# Install EF Core CLI tool
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

---

## 🎨 Frontend Setup

### Step 1: Navigate to Angular Project

```bash
cd c:\Work\Projects\Transflo.Integration.WFAI\src\angular-app
```

### Step 2: Install Dependencies

```bash
npm install
```

This will install all required packages from `package.json`.

### Step 3: Verify Angular CLI

```bash
ng version
```

You should see Angular CLI version 17+.

---

## 🚀 Running the Application

### Option 1: Run Both Backend and Frontend Separately

#### Terminal 1 - Start Backend API

```bash
cd c:\Work\Projects\Transflo.Integration.WFAI\src\webapi
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:63590
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

The API will be available at:
- **HTTP**: http://localhost:63590
- **Swagger UI**: http://localhost:63590/swagger

#### Terminal 2 - Start Angular Frontend

```bash
cd c:\Work\Projects\Transflo.Integration.WFAI\src\angular-app
ng serve
```

Expected output:
```
✔ Browser application bundle generation complete.
Initial Chunk Files   | Names     |  Raw Size
main.js               | main      | 250.5 kB
...
** Angular Live Development Server is listening on localhost:51748 **
```

The frontend will be available at: **http://localhost:51748**

### Option 2: Run Backend with Watch Mode (Auto-reload)

```bash
cd c:\Work\Projects\Transflo.Integration.WFAI\src\webapi
dotnet watch run
```

Changes to C# files will automatically reload the application.

---

## 📚 API Documentation

Once the backend is running, access the Swagger UI at:

**http://localhost:63590/swagger**

### Available Endpoints

#### TMS Systems API (`/api/v1/tms-systems`)

| Method | Endpoint | Description | Example |
|--------|----------|-------------|---------|
| GET | `/api/v1/tms-systems` | Get all TMS systems | `?activeOnly=true` |
| GET | `/api/v1/tms-systems/{id}` | Get TMS system by ID | `/api/v1/tms-systems/tms-truckmate-001` |
| POST | `/api/v1/tms-systems` | Create new TMS system | Body: `CreateTmsSystemRequest` |
| PUT | `/api/v1/tms-systems/{id}` | Update TMS system | Body: `UpdateTmsSystemRequest` |
| DELETE | `/api/v1/tms-systems/{id}` | Delete TMS system | - |

#### Templates API (`/api/v1/templates`)

| Method | Endpoint | Description | Example |
|--------|----------|-------------|---------|
| GET | `/api/v1/templates` | Get all templates | `?tmsSystemId=tms-truckmate-001` |
| GET | `/api/v1/templates/{templateId}` | Get template by ID | `?version=2` |
| POST | `/api/v1/templates` | Create new template | Body: `CreateTemplateRequest` |
| PUT | `/api/v1/templates/{templateId}` | Update template (creates new version) | Body: `UpdateTemplateRequest` |
| DELETE | `/api/v1/templates/{templateId}` | Delete template | `?version=2` |

### Example API Requests

#### Create TMS System

```bash
curl -X POST http://localhost:63590/api/v1/tms-systems \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MyTMS",
    "displayName": "My Transportation System",
    "description": "Custom TMS integration",
    "version": "1.0"
  }'
```

#### Get All Active TMS Systems

```bash
curl http://localhost:63590/api/v1/tms-systems?activeOnly=true
```

#### Create Template

```bash
curl -X POST http://localhost:63590/api/v1/templates \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TruckMate to WFAI Mapping",
    "description": "Maps TruckMate orders to WFAI format",
    "tmsSystemId": "tms-truckmate-001"
  }'
```

---

## 🧪 Testing

### Test Backend API with Swagger

1. Navigate to http://localhost:63590/swagger
2. Click on any endpoint (e.g., `GET /api/v1/tms-systems`)
3. Click **"Try it out"**
4. Click **"Execute"**
5. See the response below

### Test Frontend Application

1. Open http://localhost:51748
2. You should see the **TMS Systems** page
3. Verify you can see the 2 seeded systems (TruckMate, McLeod)
4. Click **"Create New System"** to test creation
5. Fill in the form and submit
6. Verify the new system appears in the table

### Test Database Directly

```bash
psql -U postgres -d fieldmapping

# Count records
SELECT COUNT(*) FROM tms_systems;

# View all systems
SELECT id, name, display_name, is_active FROM tms_systems;

# Check templates
SELECT * FROM field_mapping_templates;
```

---

## 🔧 Development Workflow

### Making Changes to the Backend

1. Edit C# files in `src/webapi`
2. If using `dotnet watch run`, changes auto-reload
3. Otherwise, stop (Ctrl+C) and restart: `dotnet run`
4. Test changes in Swagger or from Angular

### Making Changes to the Frontend

1. Edit TypeScript/HTML/CSS files in `src/angular-app/src`
2. Angular automatically reloads in the browser
3. Check browser console for errors (F12)

### Adding New Entity Models

1. Create model class in `src/webapi/Models/`
2. Add DbSet to `FieldMappingDbContext`
3. Create migration: `dotnet ef migrations add AddNewEntity`
4. Apply migration: `dotnet ef database update`

### Adding New API Endpoints

1. Add endpoint in `Program.cs`
2. Use `app.MapGet/Post/Put/Delete`
3. Add to appropriate route group
4. Test in Swagger

---

## 🐛 Troubleshooting

### Database Connection Issues

**Problem**: `Npgsql.PostgresException: password authentication failed`

**Solution**:
1. Check your PostgreSQL username/password
2. Update `appsettings.json` connection string
3. Verify PostgreSQL is running: `pg_isready`

**Problem**: `Database "fieldmapping" does not exist`

**Solution**:
```bash
createdb -U postgres fieldmapping
psql -U postgres -d fieldmapping -f Migrations/init-database.sql
```

### Backend API Not Starting

**Problem**: `Unable to bind to http://localhost:63590`

**Solution**:
1. Check if port 5000 is already in use
2. Change port in `Program.cs` or use environment variable
3. Or kill the process using port 5000

**Problem**: `Build failed` with errors

**Solution**:
1. Run `dotnet clean`
2. Run `dotnet restore`
3. Run `dotnet build --no-incremental`

### Angular Frontend Issues

**Problem**: `Error: Cannot GET /`

**Solution**:
1. Make sure you ran `npm install`
2. Check that `ng serve` completed successfully
3. Navigate to http://localhost:51748 (not 5000)

**Problem**: CORS errors in browser console

**Solution**:
1. Verify backend is running on port 5000
2. Check CORS configuration in `Program.cs`
3. Ensure `AllowAngular` policy includes `http://localhost:51748`

### API Returns Empty Data

**Problem**: API returns `[]` or empty list

**Solution**:
1. Check database has seed data: `SELECT * FROM tms_systems;`
2. Re-run the init script if needed
3. Check EF Core is connecting to correct database

---

## 📊 Database Maintenance

### Backup Database

```bash
pg_dump -U postgres fieldmapping > backup.sql
```

### Restore Database

```bash
psql -U postgres -d fieldmapping < backup.sql
```

### Clean Up Expired Logs (Manually)

```sql
SELECT cleanup_expired_logs();
```

### View Schema Information

```sql
-- List all tables
\dt

-- Describe a table
\d+ tms_systems

-- View indexes
\di

-- View foreign keys
SELECT * FROM information_schema.table_constraints
WHERE constraint_type = 'FOREIGN KEY';
```

---

## 🎯 Next Steps

### Extend the System

1. **Add Field Mappings CRUD** - Create UI and API for managing field mappings
2. **Add Lookup Tables Management** - UI for creating/editing lookup tables
3. **Implement Transformation Service** - Port the JsonParser and Transformation services
4. **Add Authentication** - Implement user authentication and authorization
5. **Add Testing** - Write unit and integration tests

### Deploy to Production

1. **Backend**: Deploy to Azure App Service, AWS Lambda, or IIS
2. **Frontend**: Build production bundle: `ng build --configuration production`
3. **Database**: Use managed PostgreSQL (Azure Database, AWS RDS, etc.)
4. **Environment Variables**: Use environment-specific config files

---

## 📝 Summary

You now have a complete **Field Mapping System** running with:

✅ **PostgreSQL Database** with 5 tables and seeded data
✅ **.NET 8 Web API** with minimal endpoints and Swagger
✅ **Angular Frontend** with TMS Systems management
✅ **Full CRUD operations** for TMS Systems and Templates
✅ **Version control** for field mapping templates
✅ **CORS enabled** for local development

### Quick Start Commands

```bash
# Terminal 1 - Backend
cd c:\Work\Projects\Transflo.Integration.WFAI\src\webapi
dotnet run

# Terminal 2 - Frontend
cd c:\Work\Projects\Transflo.Integration.WFAI\src\angular-app
ng serve

# Browser
# Backend API: http://localhost:63590/swagger
# Frontend App: http://localhost:51748
```

---

**Questions or Issues?** Check the troubleshooting section or refer to the inline code comments.
