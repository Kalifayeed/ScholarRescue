# ARCHITECTURE_DECISIONS

## Database Connection Architecture

### Decision: Environment Variable-Only Connection Strings
- **Context**: Connection strings are never stored in `appsettings.json` or any file committed to the repository.
- **Decision**: The `ConnectionStrings__DefaultConnection` environment variable is the single source of truth for the database connection in production and staging.
- **Rationale**: Keeps credentials out of source control, enables different databases per environment, and simplifies rotation of passwords.
- **Configuration loading order** (Program.cs):
  1. `appsettings.json` (base, no secrets)
  2. `appsettings.{Environment}.json` (environment-specific, no secrets)
  3. Environment variables (the ONLY place for secrets)

### Decision: Database Credentials Validation
- **Context**: Three major recent implementations were applied to the wrong database because the environment variable was set with `Username=postgres` instead of `Username=scholarrescue_user`.
- **Decision**: Added startup validation in `Program.cs` that rejects non-`scholarrescue_user` usernames in non-Development environments. This ensures the application will fail-fast with a clear error message if configured against the wrong database.
- **Correct credentials**: `Host=localhost;Port=5432;Database=scholarrescue;Username=scholarrescue_user;Password=...`
- **See also**: `Deployment/fix-db-connection.sh` for the systemd override file.

## Key Design Decisions

### Service Layer Pattern
Every business operation goes through a service interface. Controllers never access DbContext directly for domain logic.
- `IWriterApplicationService` – writer application workflow
- `IOrderAssignmentService` – order marketplace and assignment
- `IWorkDeliveryService` – work submissions and revisions
- `IFinancialService` – accounting and wallet operations
- `IWalletService` – writer payout processing
- `IMessageService` – messaging operations
- `INotificationService` – notification creation

### One Conversation Per Order
The `Conversations` table has a **unique index on OrderId**. Every order gets exactly one communication channel. This prevents duplicate threads and makes it easy to find all messages for an order.

### Communication Enforcement
Messages can only be sent after a writer is assigned. The `MessageService.GetOrCreateConversationAsync` method checks `order.AssignedWriterId` before creating a conversation. Participants: Client + Assigned Writer + all Admins.

### Versioned File Submissions
Work submissions never overwrite files. Each upload creates a new file with a unique timestamp+GUID name. The `OrderSubmission.VersionNumber` increments automatically per order.

### Financial Integration on Completion
When a client accepts work (or admin force-completes), the system:
1. Sets OrderStatus to Completed
2. Calculates writer earnings (90%) and commission (10%)
3. Calls `IFinancialService.ProcessOrderCompletionAsync` to create ledger entries
4. Credits writer's pending wallet balance
5. Records platform revenue

### Order Status Lifecycle
The new workflow extends the original statuses:
```
Open → Assigned → InProgress → DraftSubmitted → RevisionRequested → RevisionSubmitted → FinalSubmitted → Completed
```
Each submission type (Draft/Revision/Final) has specific status transitions enforced in `WorkDeliveryService`.

### Entity Framework Code-First
All database tables are created via EF Core migrations. The `ScholarRescueDbContext.OnModelCreating` configures foreign keys, indexes, and relationships. New entities use `Set<T>()` for flexibility or explicit DbSet properties.

### Notification Triggers
All state changes create notifications via `INotificationService.CreateNotificationAsync`. Notifications are broadcast to specific users and real-time delivery happens via SignalR.

### Audit Trail
Every significant action creates an `AuditLog` entry with: Action name, PerformerId, TargetUserId, Description, and timestamp. This is immutable and used for compliance.

### Writer Knowledge Center (CMS-style)
A dedicated content management system for writer resources. Content is stored in `WriterResources` table and managed via admin CRUD. Key design:
- Single `WriterResource` entity supports multiple content types (FAQ, guides, checklists, citation references) via the `WriterResourceCategory` enum
- FAQ entries use the `Question` field for search; guides use `Title` for the heading
- `SubCategory` string groups items within a category (e.g., "APA 7" under CitationGuides)
- `Tags` comma-separated field enables keyword search across all fields
- Search queries case-insensitively match across Question, Title, Content, and Tags
- Only verified (approved) writers can access the Knowledge Center; enforced in controller
- Content is seeded on first run via `WriterResourceSeeder` (28 items across 7 categories)

### File Storage
Files stored in `wwwroot/uploads/` organized by category:
- `/uploads/submissions/{orderId}/` – work submissions
- `/uploads/writer-applications/{category}/` – writer documents
- `/uploads/messages/{conversationId}/` – message attachments
