# ScholarRescue Database Migration Guide

## Prerequisites
- .NET EF Core Tools installed (`dotnet tool install --global dotnet-ef`)
- PostgreSQL running and accessible

## Creating Migrations

### Add a New Migration
```bash
dotnet ef migrations add MigrationName
```

### Apply Migrations
```bash
# Development
dotnet ef database update

# Specific environment
dotnet ef database update --connection "your-connection-string"
```

### Rollback Migration
```bash
# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Rollback all
dotnet ef database update 0
```

## Production Migration Strategy
1. Backup database before migration
2. Run migration in staging first
3. Apply migration during maintenance window
4. Verify data integrity after migration
5. Monitor application logs for errors

## Seeding Data
- Admin user seeded automatically on startup
- Writer resources seeded automatically on startup
- Roles seeded automatically on startup

## Integration with Backup System
- Every migration should be preceded by a manual backup
- Use the admin System → Backups page to create backup
- Verify backup before applying migration