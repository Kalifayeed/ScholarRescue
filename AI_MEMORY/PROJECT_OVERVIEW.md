# PROJECT OVERVIEW

## ScholarRescue – Academic Support Platform

A full-stack ASP.NET Core MVC application connecting students with verified academic tutors, editors, and researchers.

### Core Technology
- **Framework:** ASP.NET Core 10 (.NET 10)
- **Database:** PostgreSQL with Entity Framework Core
- **Auth:** ASP.NET Core Identity
- **Real-time:** SignalR (ChatHub, NotificationHub)
- **Frontend:** Razor Views, Bootstrap 5, Bootstrap Icons

### User Roles
| Role | Description |
|------|-------------|
| **Client** | Students who place orders for academic support |
| **Writer** | Tutors/specialists who fulfill orders (approved after application) |
| **Administrator** | Platform managers with full access |

### Core Systems (Implemented)
1. **Authentication** – Registration, login, role assignment, access control
2. **Order System** – Full lifecycle: Draft → Open → Assigned → In Progress → Completed
3. **Writer Application Workflow** – Registration, admin approval/rejection/suspension
4. **Available Orders Marketplace** – Writers browse and apply to open orders
5. **Wallet System** – Writer earnings, pending/available balances, payout windows
6. **Financial System** – Commission tracking, transaction ledger, platform revenue
7. **Messaging System** – Per-order chat (Client, Writer, Admin), real-time via SignalR
8. **Notification System** – In-app notifications with categories and preferences
9. **Audit Logging** – Immutable audit trail for all administrative actions
10. **Public Site** – Homepage, About, Service pages, Contact, Become a Tutor

### Service Pages
Six dedicated service pages with production content: Tutoring, Research Guidance, Editing, Proofreading, Citation Assistance, Formatting Assistance.

### Directory Structure
```
ScholarRescue/
├── Controllers/        # MVC controllers
├── Models/             # Entity models + Enums
├── ViewModels/         # View models organized by feature
├── Views/              # Razor views
├── Services/           # Business logic services
├── Data/               # DbContext, Seeders
├── Hubs/               # SignalR hubs
├── Migrations/         # EF Core migrations
└── AI_MEMORY/          # This documentation