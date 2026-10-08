# AcxiomCRM

AcxiomCRM is a production-quality, role-based Customer Relationship Management (CRM) application built with **ASP.NET Core 8 MVC** and **C#**. It provides comprehensive management for customers, leads, opportunities, follow-ups, sales activities, user administration, executive analytics, reporting, and audit logs.

---

## 🌟 Key Features

- **Authentication & Identity**: ASP.NET Core Identity with password complexity rules, sliding expiration, and automatic account lockout after 5 failed attempts.
- **Role-Based Access Control (RBAC)**: Fine-grained permissions across **Admin**, **Manager**, and **SalesExecutive** roles.
- **Server-Side Data Scoping**: Sales Executives only access assigned records; Managers view team-wide records; Admins maintain unrestricted enterprise oversight.
- **Customer Management**: Complete customer records, contact tracking, status workflow, and unique constraint enforcement (Email & Phone).
- **Lead Management & Qualification**: Lead generation, source tracking, prioritization, and seamless Lead-to-Customer conversion.
- **Opportunity Pipeline**: Stage-based sales pipeline tracking (Prospecting to Closed Won/Lost), weighted revenue forecasting, and interactive Kanban view.
- **Follow-Ups & Activity Logs**: Interaction scheduling, status management (Pending, Completed, Overdue), and activity tracking (Calls, Meetings, Emails, Tasks).
- **Executive Dashboard**: Real-time KPI summaries and interactive **Chart.js** data visualizations for lead distribution and sales stage breakdown.
- **Reports & Analytics Engine**: Multi-category reporting (Pipeline, Lead Conversion, Customer Geography, User Workload, Audit Trail) with instant CSV export.
- **REST Web APIs**: Secure API controllers using DTOs, uniform JSON response envelopes (`ApiResponse<T>`), and standard HTTP status codes (`200`, `201`, `400`, `401`, `403`, `404`, `409`).
- **Security & Hardening**: Rate limiting middleware, anti-CSRF protection, HTTP-only secured cookies, parameterization against SQL injection, and global exception handling without stack trace leaks.

---

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 8 MVC
- **Language**: C# 12
- **Data Access**: Entity Framework Core 8 (EF Core)
- **Identity & Security**: ASP.NET Core Identity
- **Databases**: SQL Server (Production ready) & SQLite (Local development zero-config option)
- **Frontend & UI**: Bootstrap 5, Bootstrap Icons, Razor Views
- **Visualizations**: Chart.js 4.4
- **API**: ASP.NET Core Web API Controllers with DTOs

---

## 🏗️ Clean Architecture

The application is structured following Clean Architecture principles:

```text
AcxiomCRM Architecture
│
├── Presentation Layer      → Controllers, Razor Views, Bootstrap 5 UI
├── Application Layer       → Web API Controllers, ViewModels, DTOs, ApiResponse<T>
├── Business Domain Layer   → Domain Entities (Customer, Lead, Opportunity, FollowUp, etc.)
├── Data Access Layer       → ApplicationDbContext, Generic Repository Pattern
├── Database Layer          → SQL Server / SQLite (EF Core Migrations & Seeders)
├── Security Layer          → Identity, Custom Policies, Rate Limiting, Cookie Security
└── Cross-Cutting Services  → CrmDataScopeService, AuditLogService, Global Exception Middleware
```

---

## 👥 Roles & Data Scoping Matrix

| Role | Operational Scope | Access Rights |
|---|---|---|
| **Admin** | Full Enterprise | Full system administration, User & Role management, Audit logs, Reports, REST APIs |
| **Manager** | Team Scope | Team CRM records, sales pipeline, follow-ups, team performance reports (Restricted from user security administration) |
| **SalesExecutive** | Assigned Records Only | Assigned Customers, Leads, Opportunities, Follow-ups, and Activities (Restricted from Admin/Manager pages) |

---

## 🔍 Validation Rules

- **Server-Side & Client-Side**: Validation attributes (`[Required]`, `[EmailAddress]`, `[Phone]`, `[StringLength]`, `[Range]`) paired with unobtrusive jQuery client validation.
- **Unique Constraint Verification**: Prevents duplicate Customer/Lead email addresses and phone numbers.
- **Financial & Probability Constraints**: Opportunity amount cannot be negative; probability must be bounded between `0%` and `100%`.
- **Date Constraints**: Validates follow-up dates and expected deal close dates.

---

## 🔒 Security Implementations

1. **Identity & Lockout**: Account lockout triggered after 5 consecutive failed login attempts (5-minute lockout duration).
2. **Cookie Hardening**: Cookies configured with `HttpOnly = true`, `SameSite = Lax`, and `SecurePolicy = SameAsRequest`.
3. **Rate Limiting**: Integrated fixed-window rate limiter (`100 requests / minute`).
4. **Anti-CSRF Tokens**: Form posts require valid anti-forgery tokens (`X-CSRF-TOKEN` header support for API/AJAX).
5. **SQL Injection Defense**: EF Core parameterized LINQ queries prevent SQL injection vulnerabilities.
6. **Audit Trails**: Security actions (logins, logouts, entity CRUD, lead conversion, role modifications) log to `AuditLogs`.

---

## 📊 Dashboard & Analytics

### KPI Cards
- **Total Customers**: Count of active/inactive accounts.
- **Total Leads & Conversion**: Total leads generated vs. converted.
- **Open Opportunities**: Count of active deals.
- **Pipeline Revenue**: Total pipeline value & probability-weighted forecast value ($).
- **Pending Follow-ups**: Count of upcoming and overdue tasks.

### Chart.js Visualizations
- **Sales Pipeline Stage Chart**: Breakdown of deal count and total value per sales stage.
- **Lead Status Chart**: Distribution of leads across stages (New, In Progress, Qualified, Converted).

---

## 📈 Reports & Analytics

- **Customer Summary Report**: Active status and geographic city breakdown.
- **Lead Conversion Report**: Lead sources, conversion counts, and conversion percentage.
- **Sales Pipeline Report**: Detailed stage analysis with total vs. weighted deal value.
- **Follow-Up Metrics Report**: Scheduled, completed, missed, and overdue interaction breakdown.
- **User Activity Report**: Sales executive workload summary (assigned leads, opportunities, completed tasks).
- **Audit Summary Report**: System security log inspection (Admin only).
- **CSV Data Export**: Instant CSV streaming download for all report categories.

---

## 🌐 REST Web API Specification

| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/auth/login` | Authenticate user & initiate session | Anonymous |
| `POST` | `/api/auth/logout` | Terminate active user session | Authorized |
| `GET` | `/api/customers` | Get paginated customer list | Authorized |
| `GET` | `/api/customers/{id}` | Get customer details by ID | Authorized |
| `POST` | `/api/customers` | Create new customer | Authorized |
| `PUT` | `/api/customers/{id}` | Update existing customer | Authorized |
| `DELETE` | `/api/customers/{id}` | Deactivate customer | Admin, Manager |
| `GET` | `/api/leads` | Get paginated lead list | Authorized |
| `GET` | `/api/leads/{id}` | Get lead details by ID | Authorized |
| `POST` | `/api/leads` | Create new lead | Authorized |
| `GET` | `/api/opportunities` | Get paginated opportunities | Authorized |
| `GET` | `/api/opportunities/{id}`| Get opportunity details | Authorized |
| `POST` | `/api/opportunities` | Create new opportunity | Authorized |
| `GET` | `/api/followups` | Get paginated follow-ups | Authorized |
| `GET` | `/api/followups/{id}` | Get follow-up details | Authorized |
| `POST` | `/api/followups` | Create new follow-up | Authorized |
| `GET` | `/api/reports/pipeline` | Get pipeline metrics report | Authorized |
| `GET` | `/api/dashboard/metrics` | Get dashboard KPI metrics | Authorized |
| `GET` | `/api/auditlog` | Get recent audit log entries | Admin, Manager |

---

## 🗄️ Database Setup & Migrations

### Dual Database Provider Configuration
Configured in `appsettings.json`:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AcxiomCRMDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
    "SqliteConnection": "Data Source=AcxiomCRM.db"
  }
}
```

### EF Core CLI Commands

To restore dependencies and apply migrations:

```bash
dotnet restore
dotnet ef database update
```

---

## 🚀 Running Locally

```bash
# 1. Clone the repository
git clone https://github.com/YOUR_GITHUB_USERNAME/AcxiomCRM.git
cd AcxiomCRM

# 2. Restore NuGet packages
dotnet restore

# 3. Build solution
dotnet build

# 4. Run the application
dotnet run --project AcxiomCRM/AcxiomCRM.csproj
```

Open your browser and navigate to `http://localhost:5288` or `http://localhost:5050`.

---

## 🔐 Local Demo Accounts (Development Only)

> [!IMPORTANT]
> The following accounts are seeded for **LOCAL DEMO & TESTING PURPOSES ONLY**. Change passwords immediately in production environments.

| Role | Demo Email | Demo Password |
|---|---|---|
| **Admin** | `admin@acxiomcrm.com` | `Admin@12345!` |
| **Manager** | `manager@acxiomcrm.com` | `Manager@12345!` |
| **Sales Executive** | `sales@acxiomcrm.com` | `Sales@12345!` |

---

## 📂 Project Structure

```text
AcxiomCRM/
├── AcxiomCRM.sln
└── AcxiomCRM/
    ├── API/                      # REST Web API Controllers
    ├── Authorization/            # Security Roles & Policies
    ├── Controllers/              # MVC Page Controllers
    ├── Data/                     # DbContext & Database Seeder
    ├── DTOs/                     # Data Transfer Objects & API Envelopes
    ├── Middleware/               # Exception & Security Middleware
    ├── Migrations/               # EF Core Migration Files
    ├── Models/                   # Domain Entities
    ├── Repositories/             # Generic Repository Pattern
    ├── Services/                 # Data Scoping & Audit Logging Services
    ├── ViewModels/               # Razor View ViewModels
    ├── Views/                    # Razor Views (Bootstrap 5 UI)
    ├── wwwroot/                  # Static Files (CSS, JS, Libraries)
    ├── appsettings.json          # Application Configuration
    ├── appsettings.example.json  # Configuration Template
    └── Program.cs                # Entry Point & DI Container
```

---

## ✅ QA Verification Results

A complete end-to-end QA verification suite was executed prior to publication:

- **Total Verification Tests**: 73
- **Passed**: 73 (100%)
- **Failed**: 0
- **Discovered & Fixed**: 2 (API route mapping & SQLite EF Core GroupBy translation)
- **Build Status**: **0 Warnings, 0 Errors**

---

## ⚠️ Known Limitations

1. **Email Provider Integration**: Confirmation emails log to the audit service; connect an SMTP service (`IEmailSender`) for outbound email delivery.
2. **API Authentication**: REST APIs utilize cookie session authentication. A JWT Bearer token middleware can be configured for standalone mobile clients.
