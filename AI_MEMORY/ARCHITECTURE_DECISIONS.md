# ARCHITECTURE_DECISIONS

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
