# ScholarRescue Deployment Guide

## Overview
This guide covers the complete deployment process for ScholarRescue to production.

## Prerequisites
- .NET 10 SDK
- PostgreSQL 16+
- SMTP Server (SendGrid, Mailgun, or custom)
- Stripe Account (Production keys)
- SSL Certificate
- Domain Name

## Environment Configuration

### Development
```bash
# No special variables needed - uses appsettings.Development.json
dotnet run
```

### Staging
```bash
# Set environment variables
setx STRIPE_SECRET_KEY "sk_test_..."
setx STRIPE_WEBHOOK_SECRET "whsec_..."
setx SMTP_PASSWORD "your-smtp-password"
setx STAGING_DB_PASSWORD "your-db-password"

# Run with staging config
dotnet run --environment Staging
```

### Production
```bash
# Set production environment variables
setx ASPNETCORE_ENVIRONMENT "Production"
setx STRIPE_SECRET_KEY "sk_live_..."
setx STRIPE_WEBHOOK_SECRET "whsec_..."
setx SMTP_PASSWORD "your-smtp-password"
setx PROD_DB_PASSWORD "your-db-password"
setx STRIPE_PUBLISHABLE_KEY "pk_live_..."
```

## Deployment Steps

1. **Build Application**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Database Migration**
   ```bash
   dotnet ef database update
   ```

3. **Configure IIS/Web Server**
   - Set environment to Production
   - Enable HTTPS
   - Configure application pool

4. **Verify Deployment**
   - Check `/Admin/SystemCenter`
   - Verify health checks
   - Test user registration flow

## Rollback Procedure
1. Keep previous publish folder
2. Restore database from latest backup
3. Swap deployment slot