# KNOWN_ISSUES

## Critical

### Migration Pending for Phase 5B
`OrderSubmissions` and `RevisionRequests` tables are registered in DbContext but no EF Core migration has been created yet. Need to run `dotnet ef migrations add AddWorkDeliveryTables` when the database is available.

## CRITICAL: Database Connection Credentials
- **Status**: RESOLVED (guards added to prevent recurrence)
- **Problem**: The `ConnectionStrings__DefaultConnection` environment variable was set with `Username=postgres` instead of `Username=scholarrescue_user`. The `postgres` user is the default PostgreSQL superuser and does NOT point to the live website database. The correct credentials are: `Database=scholarrescue; Username=scholarrescue_user`.
- **Fix Applied**:
  1. **Program.cs**: Added startup validation that rejects non-`scholarrescue_user` usernames in Production/Staging environments. The app will fail-fast with a clear error message if the wrong user is used.
  2. **Deployment/deploy-remote.sh**: Added pre-deploy check that rejects connection strings containing `Username=postgres` or `User Id=postgres`.
  3. **Deployment/fix-db-connection.sh**: Updated with clear documentation about which credentials are correct.
- **Future implementations**: Always use `scholarrescue_user` for the live database. Never use `postgres` for production operations.
- **To verify**: Run `echo $ConnectionStrings__DefaultConnection` and check that `Username` is `scholarrescue_user` (not `postgres`).

## Non-Critical

### WriterApplicationDetails View Uses Legacy Fields
The admin writer application details view (`Views/Admin/WriterApplicationDetails.cshtml`) still references legacy field names like `EducationLevel`, `Institution`, `ExperienceYears`, `ResumePath`, `CertificatePath`. These were updated to use the new fields (HighestQualification, Specialization, Biography, CvFilePath, DegreeFilePath, WritingSampleFilePath) but some references may remain.

### Service Pages Not Linked from Navigation
The six service pages (Tutoring, Research Guidance, etc.) are linked from the homepage service cards but may not be included in the main navigation menu yet.

### No Unit Tests Yet
The project has no test project. All verification is done through build checks and manual testing.

### File Upload Paths Not Auto-Created
The `/wwwroot/uploads/` directories are created on-demand in services, but there is no startup initialization to create the base directories.

### SignalR Not Configured for HTTPS
The SignalR hubs use `/chatHub` and `/notificationHub` but may need configuration for production HTTPS environments.

### Profile.cshtml Missing
The `_Layout.cshtml` navigation may reference a Profile page that does not exist yet.

## Technical Debt

### Legacy Field Aliases on WriterApplication
The model has multiple `NotMapped` aliases (EducationLevel→HighestQualification, etc.) for backwards compatibility. These should be removed once all views are updated.

### Duplicate Service Registration
`WalletService` is registered as both `IWalletService` and within `IFinancialService`. The WorkDeliveryService previously had conflicting injection attempts. Current state is correct but fragile.

### No Soft-Delete on Orders
Orders use `OrderStatus.Cancelled` instead of a soft-delete pattern. This means cancelled orders still occupy database space and appear in queries.