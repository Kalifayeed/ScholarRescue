# DATABASE_SCHEMA

## Entity Models & Relationships

### Core Domain Tables

#### `Orders`
- **Primary Key:** `Id` (int)
- **Key Fields:** `OrderNumber` (unique), `Title`, `Description`, `Subject`, `AcademicLevel`, `CitationFormat`, `Deadline`, `Pages`, `WordCount`, `Budget`, `Priority`, `Status`
- **Financial Fields:** `CommissionAmount`, `WriterEarnings`
- **Assignment Fields:** `AssignedWriterId` (FK→User), `AssignedAt`, `AssignedByAdminId` (FK→User)
- **Client:** `ClientId` (FK→User)
- **Status Field:** `IsMarketplaceOpen`
- **Timestamps:** `CreatedAt`, `UpdatedAt`, `CompletedAt`
- **Navigation:** `Documents`, `Notes`, `History`, `Applications`

#### `WriterApplications`
- **Primary Key:** `Id` (int)
- **Key Fields:** `UserId` (FK→User), `PhoneNumber`, `HighestQualification`, `Specialization`, `Biography`, `CvFilePath`, `DegreeFilePath`, `WritingSampleFilePath`
- **Status:** `Status` (WriterApplicationStatus enum)
- **Workflow:** `SubmittedAt`, `ReviewedAt`, `ReviewedByAdminId` (FK→User), `AdminComments`

#### `OrderApplications`
- **Primary Key:** `Id` (int)
- **Key Fields:** `OrderId` (FK→Order), `WriterId` (FK→User), `AppliedAt`, `Status`, `Message`
- **Unique Index:** `(OrderId, WriterId)`

#### `OrderSubmissions` *(NEW - Phase 5B)*
- **Primary Key:** `Id` (int)
- **Key Fields:** `OrderId` (FK→Order), `WriterId` (FK→User), `VersionNumber`, `SubmissionType` (Draft/Revision/Final), `FilePath`, `FileName`, `Comments`, `SubmittedAt`

#### `RevisionRequests` *(NEW - Phase 5B)*
- **Primary Key:** `Id` (int)
- **Key Fields:** `OrderId` (FK→Order), `ClientId` (FK→User), `WriterId` (FK→User), `Comments`, `RequestedAt`, `Status` (Pending/Completed)

### Financial Tables

#### `WriterWallets`
- **PK:** `Id`, **Unique Index on:** `WriterId`
- **Fields:** `AvailableBalance`, `PendingBalance`, `LifetimeEarnings`, `LifetimeCommissionPaid`, `LastUpdated`

#### `FinancialTransactions`
- **PK:** `Id`, **Unique:** `TransactionNumber`
- **Fields:** `TransactionType`, `ReferenceId`, `ReferenceType`, `UserId`, `Description`, `DebitAmount`, `CreditAmount`, `BalanceAfter`, `CreatedBy`

#### `PlatformWallets`
- Singleton record for platform commission totals

#### `OrderFinancialRecords`
- **Unique Index on:** `OrderId`
- Links to `PlatformWallet`

#### `PayoutRequests`
- **Fields:** `WriterId`, `Amount`, `Status`, `RequestedDate`, `ApprovedDate`, `PayoutDate`, `TransactionNumber`

#### `WriterPaymentDetails`
- **Unique Index on:** `WriterId`
- Fields: payment method, account details

### Communication Tables

#### `Conversations`
- **Unique Index on:** `OrderId` (one chat per order)
- Fields: `OrderId` (FK→Order), `CreatedDate`, `LastMessageDate`, `IsArchived`

#### `Messages`
- **FK:** `ConversationId`, `SenderId`, `AttachmentId`
- Fields: `MessageText`, `IsRead`, `IsEdited`, `CreatedDate`

#### `MessageAttachments`
- Fields: `FileName`, `FilePath`, `FileUrl`, `FileSize`, `ContentType`, `UploadedBy`

#### `ConversationParticipants`
- **Unique Index on:** `(ConversationId, UserId)`

### System Tables

#### `Notifications`
- Fields: `UserId`, `Title`, `Message`, `NotificationType`, `RelatedEntityId`, `IsRead`, `CreatedDate`

#### `AuditLogs`
- Fields: `Action`, `PerformedById`, `TargetUserId`, `Description`, `CreatedDate`

#### `Orders` History
- `OrderHistory` linked to Order
- `OrderDocuments` linked to Order

### Enums

| Enum | Values |
|------|--------|
| `OrderStatus` | Draft, PendingReview, Open, Assigned, InProgress, DraftSubmitted, RevisionRequested, RevisionSubmitted, FinalSubmitted, Completed, Cancelled |
| `WriterApplicationStatus` | Pending, Approved, Rejected, MoreInformationRequired, Suspended |
| `OrderApplicationStatus` | Pending, Selected, Declined, Withdrawed |
| `SubmissionType` | Draft, Revision, Final |
| `RevisionRequestStatus` | Pending, Completed |
| `AcademicLevel` | HighSchool, College, Undergraduate, Masters, PhD |
| `PriorityLevel` | Low, Normal, High, Urgent |
| `CitationFormat` | APA_7th, MLA, Chicago, Harvard, IEEE, OSCOLA, etc. |
| `NotificationType` | OrderAssigned, NewMessage, RevisionRequested, OrderSubmitted, OrderCompleted, WriterApproved, SystemAlert, WriterApplied, WriterAssigned, WriterRejected, OrderReassigned, WriterApplicationRejected |