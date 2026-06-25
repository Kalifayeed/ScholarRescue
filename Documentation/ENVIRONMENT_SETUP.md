# ScholarRescue Environment Setup Guide

## Development Environment

### Prerequisites
- Visual Studio 2022+ / VS Code
- .NET 10 SDK
- PostgreSQL 16
- pgAdmin 4 (optional)

### Setup Steps
1. Clone repository
2. Update `appsettings.json` with local PostgreSQL connection
3. Run database migrations: `dotnet ef database update`
4. Run the application: `dotnet run`
5. Default admin credentials seeded on first run

### Configuration Files
| Environment | File | Purpose |
|------------|------|---------|
| Development | `appsettings.Development.json` | Local overrides |
| Staging | `appsettings.Staging.json` | Pre-production testing |
| Production | `appsettings.Production.json` | Live environment |

### Environment Variables (Production)
| Variable | Purpose |
|----------|---------|
| `STRIPE_SECRET_KEY` | Stripe API secret |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signing secret |
| `STRIPE_PUBLISHABLE_KEY` | Stripe publishable key |
| `SMTP_PASSWORD` | Email server password |
| `PROD_DB_PASSWORD` | Production database password |