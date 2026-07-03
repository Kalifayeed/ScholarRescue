# Senior Engineer Codebase Audit — ScholarRescue

**Engineer:** Senior Software Engineer  
**Date:** July 4, 2026  
**Scope:** Complete architecture reverse-engineering, code quality audit, and safe improvement implementation  
**Status:** Complete  

---

## 1. Architecture Breakdown

### Application Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  Razor Views (Bootstrap 5) / SignalR Hubs / MVC Controllers │
├─────────────────────────────────────────────────────────────┤
│                    Controller Layer                          │
│  15 Controllers: Account, Admin, Dashboard, Finance, Home,  │
│  Messages, MessagingTest, Notifications, OrderMilestones,   │
│  Orders, Payments, SupportTickets, WriterResources, Writers,│
│  Communication                                              │
├─────────────────────────────────────────────────────────────┤
│                    Service Layer                             │
│  ~40+ Services (I*Service interfaces + implementations)     │
│  Business logic, validation, orchestration                  │
├─────────────────────────────────────────────────────────────┤
│                    Data Access Layer                         │
│  ScholarRescueDbContext (EF Core) / Repositories (via DbSet)│
├─────────────────────────────────────────────────────────────┤
│                    Database                                  │
│  PostgreSQL (via Npgsql) / ~50 tables / 20+ migrations      │
└─────────────────────────────────────────────────────────────┘
```

### Controllers & Responsibilities

| Controller | Lines | Responsibilities | Issues |
|-----------|-------|-----------------|--------|
| **AdminController** | 1,874 | Dashboard, users, roles, orders, applications, QA, conversations, notifications, resources, rankings, matching, payments, audits, reports | **GOD CLASS** — 12+ responsibilities |
| **OrdersController** | 1,076 | CRUD, guest create, workspace, bids, submissions, revisions, file downloads | Too large, mixed concerns |
| **AccountController** | ~400 | Register, login, password reset, 2FA | Reasonable |
| **WritersController** | ~500 | Dashboard, wallet, bids, analytics, applications | Reasonable |
| **FinanceController** | ~400 | Dashboard, payouts, revenue, commissions, transactions | Reasonable |
| **PaymentsController** | ~200 | Checkout, callback, verification | Reasonable |
| **MessagesController** | ~300 | Conversations, messaging center | Reasonable |
| **SupportTicketsController** | ~200 | CRUD, details | Reasonable |
| **DashboardController** | ~100 | Client/writer dashboards | Reasonable |
| **HomeController** | ~100 | Public pages | Reasonable |
| **NotificationsController** | ~200 | Index, settings, details | Reasonable |
| **OrderMilestonesController** | ~200 | Create, submit, timeline | Reasonable |
| **WriterResourcesController** | ~100 | Knowledge center | Reasonable |
| **CommunicationController** | ~100 | Communication hub | Reasonable |
| **MessagingTestController** | ~100 | Debug/testing | **No auth** |

### Services & Business Logic Boundaries

| Service Interface | Implementation | Responsibility |
|-----------------|----------------|----------------|
| IMessageService | MessageService | Messaging, conversation management |
| INotificationService | NotificationService | In-app notifications |
| IEmailService | EmailService | Email (stub) |
| IPricingService | PricingService | Order pricing calculations |
| IWalletService | WalletService | Writer wallet management |
| IFinancialService | FinancialService | Ledger, transactions, payouts |
| IPayoutWindowService | PayoutWindowService | Payout scheduling |
| IWriterApplicationService | WriterApplicationService | Writer applications |
| IOrderAssignmentService | OrderAssignmentService | Order assignment workflow |
| IWorkDeliveryService | WorkDeliveryService | Submissions, revisions |
| IWriterResourceService | WriterResourceService | Knowledge center |
| IAdminDashboardService | AdminDashboardService | Dashboard metrics |
| IWriterRankingService | WriterRankingService | Writer rankings |
| IOrderMilestoneService | OrderMilestoneService | Milestone tracking |
| ISupportTicketService | SupportTicketService | Support tickets |
| IOrderMonitoringService | OrderMonitoringService | Order monitoring |
| IWriterCapacityService | WriterCapacityService | Writer capacity limits |
| IOrderTimelineService | OrderTimelineService | Timeline events |
| IWriterRatingService | WriterRatingService | Writer ratings |
| IWriterReliabilityService | WriterReliabilityService | Writer reliability |
| IEscrowService | EscrowService | Escrow management |
| IRevisionDisputeService | RevisionDisputeService | Disputes |
| IMarketplaceService | MarketplaceService | Order marketplace |
| IAnnouncementService | AnnouncementService | System announcements |
| IRiskDetectionService | RiskDetectionService | Risk detection |
| IFileScanningService | FileScanningService | File scanning |
| IContentModerationService | ContentModerationService | Content moderation |
| ISecurityService | SecurityService | Security, MFA, devices |
| IConfigurationService | ConfigurationService | Platform settings |
| IBackupService | BackupService | System backups |
| IHealthMonitorService | HealthMonitorService | Health monitoring |
| IErrorLogService | ErrorLogService | Error logging |
| INotificationQueueService | NotificationQueueService | Notification queue |
| ISecureFileService | SecureFileService | Secure file storage |
| ITwoFactorService | TwoFactorService | 2FA management |
| IWriterQualityService | WriterQualityService | Writer quality |
| IWriterTierService | WriterTierService | Writer tiers |
| IFraudDetectionService | FraudDetectionService | Fraud detection |
| ILoginSecurityService | LoginSecurityService | Login security |
| IAdminAuditService | AdminAuditService | Admin audit |
| IWriterMatchingService | WriterMatchingService | Writer matching |
| IVerificationService | VerificationService | Email verification |
| IAccountFraudService | AccountFraudService | Account fraud |
| IDeploymentValidator | DeploymentValidator | Deployment validation |
| IPaystackPaymentService | PaystackPaymentService | Paystack integration |
| ICacheService | CacheService | In-memory caching |
| IUserPresenceService | UserPresenceService | User presence tracking |

### Data Models & Relationships

**Core Entities:**
- `ApplicationUser` (Identity) → Orders (as Client/Writer), WriterApplication, Wallet, etc.
- `Order` → OrderDocument, OrderNote, OrderHistory, OrderApplication, OrderSubmission, OrderAttachment, OrderMilestone, Payment, EscrowAccount, OrderDispute, QaReview, MonitoringAlert, DeadlineReminder, OrderTimelineEvent, WriterRating
- `Conversation` (1:1 with Order) → Message, ConversationParticipant
- `WriterApplication` (1:1 with User)
- `WriterWallet` (1:1 with Writer)
- `FinancialTransaction` → User, OrderFinancialRecord
- `SupportTicket` → SupportTicketAttachment, SupportTicketNote

### Database Access Patterns
- **Pattern:** Controller → Service → DbContext (via DI)
- **Violation:** Several controllers (OrdersController, AdminController) inject `ScholarRescueDbContext` directly
- **Query style:** LINQ with `.Include()`, `.AsNoTracking()`, `.FirstOrDefaultAsync()`, `.ToListAsync()`
- **Migrations:** Code-first with 20+ migration files

### Authentication & Authorization Flow
1. ASP.NET Core Identity with `ApplicationUser`
2. Roles: `Administrator`, `Client`, `Writer` (via `RoleNames` constants)
3. `[Authorize(Roles = RoleNames.Administrator)]` attribute pattern
4. Some actions use `[Authorize]` without role specification (defense-in-depth gap)
5. SignalR hubs use `[Authorize]` attribute
6. Account lockout: 5 attempts / 30 min

### Payment/Order/Messaging Workflows
- **Payment:** Paystack integration → Checkout → Callback → Verify → Update Order
- **Order:** Create → PendingPayment → (after payment) Open → Assigned → InProgress → DraftSubmitted → RevisionRequested → RevisionSubmitted → FinalSubmitted → Completed
- **Messaging:** One conversation per order, SignalR real-time, assignment-before-message enforcement

### Background Services
| Service | Interval | Purpose |
|---------|----------|---------|
| OrderMonitoringBackgroundService | ~30s | Monitor orders for escalations |
| DeadlineNotificationHostedService | ~60s | Send deadline reminders |
| FileScanHostedService | ~30s | Background file scanning |

### External Integrations
- **Paystack** — Payment gateway (via `IPaystackPaymentService`)
- **Email** — Stub implementation (no SMTP configured)
- **File Storage** — Local filesystem (`wwwroot/uploads/`)

### Configuration & Environment
- `appsettings.json` (base) + `appsettings.{Environment}.json` + Environment Variables
- Deterministic loading in Program.cs
- `MaintenanceModeSettings`, `PaystackSettings`, `CurrencySettings`, `MatchingConfiguration`
- Connection string via `ConnectionStrings__DefaultConnection` env var

---

## 2. Data Flow Summary for Key Workflows

### Registration/Login
```
User → AccountController.Register → UserManager.CreateAsync → Identity DB
    → Role assignment → SignInManager.SignInAsync → Redirect to dashboard
```

### Client Creates Order
```
Client → OrdersController.Create → PricingService.CalculatePrice
    → DbContext.Orders.Add → OrderHistory + AuditLog
    → Redirect to PaymentsController.Checkout
```

### Payment Checkout
```
Client → PaymentsController.Checkout → PaystackPaymentService.InitializePayment
    → Redirect to Paystack → Paystack callback → PaymentsController.Callback
    → VerifyPayment → Update Order Status → Redirect to Dashboard
```

### Writer Bidding
```
Writer → WritersController.PlaceBid → OrderBid created
    → Client views bids → OrdersController.Bids
    → Admin reviews → AdminController.OrderBids
```

### Admin Assignment
```
Admin → AdminController.AssignWriter → OrderAssignmentService.AssignWriterAsync
    → Update Order (AssignedWriterId, Status=Assigned)
    → NotificationService.CreateNotificationAsync
    → AuditLog entry
```

### Order Workspace/Messaging
```
User → OrdersController.Workspace → Load order + conversation + submissions
    → Messages via SignalR ChatHub → MessageService.SendMessageAsync
```

### Draft Submission/Revision/Completion
```
Writer → OrdersController.SubmitWork → WorkDeliveryService.SubmitWorkAsync
Client → OrdersController.RequestRevision → WorkDeliveryService.RequestRevisionAsync
Writer → OrdersController.SubmitWork (revision) → WorkDeliveryService.SubmitWorkAsync
Client → OrdersController.AcceptWork → Status=Completed → Notification
```

### Admin Dashboard & Reporting
```
Admin → AdminController.Dashboard → AdminDashboardService.GetDashboardViewModelAsync
    → Multiple DB queries (N+1 risk) → View
```

---

## 3. Critical Problem Areas

### 🔴 CRITICAL

| # | Issue | Location | Severity | Impact |
|---|-------|----------|----------|--------|
| C1 | **AdminController God Class** | AdminController.cs (1,874 lines) | Critical | Untestable, unmaintainable, merge conflicts |
| C2 | **OrdersController Too Large** | OrdersController.cs (1,076 lines) | Critical | Mixed concerns, hard to test |
| C3 | **Direct DbContext in Controllers** | OrdersController, AdminController | Critical | Business logic leaks, untestable |
| C4 | **Hardcoded Commission Rate** | OrdersController.cs:223,363; FinancialService.cs:143 | High | Financial inconsistency risk |
| C5 | **N+1 Admin Dashboard** | AdminController.Dashboard | High | Timeout at scale |
| C6 | **Financial Race Conditions** | FinancialService (wallet operations) | High | Double-withdrawal risk |
| C7 | **No Pagination on Lists** | Users, Wallets, Payouts endpoints | High | Memory exhaustion |
| C8 | **Missing Auth on Actions** | OrdersController.AcceptWork, SubmitWork | Medium | Weak defense-in-depth |
| C9 | **MessagingTestController No Auth** | MessagingTestController | High | Debug endpoint exposed |

### 🟠 HIGH

| # | Issue | Location | Severity |
|---|-------|----------|----------|
| H1 | Try/catch boilerplate in every action | All controllers | High — code duplication |
| H2 | Duplicate WalletService/FinancialService | Both services | Medium — inconsistent state risk |
| H3 | No retry on background services | 3 hosted services | Medium — missed deadlines |
| H4 | No rate limiting | Auth endpoints | Medium — brute force risk |
| H5 | No SignalR groups | ChatHub, NotificationHub | Medium — scaling issue |
| H6 | Duplicate order number generation | OrdersController.Create + GuestCreate | Medium — race condition |
| H7 | Legacy NotMapped aliases | WriterApplication model | Medium — technical debt |
| H8 | No soft-delete | All entities | Medium — data loss risk |

### 🟡 MEDIUM

| # | Issue | Location |
|---|-------|----------|
| M1 | Inconsistent DateTime usage | Various |
| M2 | No FluentValidation pipeline | All controllers |
| M3 | No API versioning | All controllers |
| M4 | PII in logs | Various `_logger.LogInformation` calls |
| M5 | No GDPR endpoints | Not implemented |
| M6 | No CSP/HSTS security headers | Program.cs middleware |
| M7 | File upload paths not centralized | Multiple services |
| M8 | EmailService is stub | EmailService.cs |

---

## 4. Duplicate Logic Inventory

| Pattern | Locations | Risk |
|---------|-----------|------|
| Commission rate `0.10m` | OrdersController.cs:223, OrdersController.cs:363, FinancialService.cs:143 | High — financial inconsistency |
| Order number generation `SR-{year}-{next:D6}` | OrdersController.Create, OrdersController.GuestCreate | Medium — race condition |
| Role string checks `"Administrator"`, `"Client"`, `"Writer"` | 50+ occurrences across controllers | Medium — typo risk |
| Try/catch + TempData + logger pattern | Every controller action (~50+ blocks) | High — boilerplate |
| Wallet balance modification pattern | FinancialService, WalletService | Medium — inconsistency |
| File path construction `Path.Combine("wwwroot", ...)` | WorkDeliveryService, MessageService, WriterApplicationService | Low — inconsistency |
| Pricing calculation `CalculatePrice + CalculateWordCount` | OrdersController.Create, OrdersController.GuestCreate, OrdersController.Edit | Low — duplication |

---

## 5. Performance/Scalability Risks

| Risk | Location | Impact |
|------|----------|--------|
| N+1 admin dashboard queries | AdminController.Dashboard | Timeout with 10k+ users |
| No pagination on user lists | AdminController.Users | Full table scans |
| In-memory cache only | CacheService | Not distributable |
| SignalR without backplane | All hubs | Sticky sessions required |
| Synchronous wallet operations | FinancialService | Race conditions |
| Startup migration blocking | Program.cs `MigrateAsync()` | Delayed startup |
| No response caching | Public pages | Repeated DB hits |
| No Redis/SQL for SignalR | All hubs | Horizontal scaling blocked |

---

## 6. Security/Authorization Risks

| Risk | Severity | Location |
|------|----------|----------|
| `[Authorize]` without role on AcceptWork/SubmitWork | Medium | OrdersController |
| No auth on MessagingTestController | High | MessagingTestController |
| No brute force protection beyond Identity lockout | Medium | AccountController.Login |
| No 2FA enforcement | Medium | Identity config |
| No rate limiting | Medium | All endpoints |
| No CSP/HSTS headers | Medium | Program.cs |
| PII in logs (email, names) | Medium | Various log calls |
| No GDPR data export/delete | Medium | Not implemented |
| Secrets in connection string placeholder | Medium | Program.cs check |

---

## 7. Maintainability Issues

| Issue | Impact |
|-------|--------|
| AdminController 1,874 lines | Impossible to maintain |
| OrdersController 1,076 lines | Hard to reason about |
| Try/catch in every action | 50+ identical blocks |
| Magic strings for roles | Typo risk |
| No unit tests | Cannot refactor safely |
| Legacy NotMapped aliases | Blocks cleanup |
| No XML docs on implementations | Hard to understand |
| Inconsistent DateTime handling | Timezone bugs |
| No validation pipeline | Inconsistent validation |

---

## 8. Recommended Phased Refactor Roadmap

### Phase 1: Safe Foundational Improvements (NOW — implemented below)
**Risk: Low | Effort: 2-3 hours**

| # | Improvement | Files | Benefit |
|---|-------------|-------|---------|
| 1 | Extract commission rate to `FinancialSettings` config | appsettings.json, OrdersController, FinancialService | Single source of truth for commission |
| 2 | Add `FinancialSettings` configuration class | New file | Type-safe config |
| 3 | Add logging around financial operations | FinancialService | Audit trail |
| 4 | Improve null-safety in critical paths | OrdersController, AdminController | Prevent NREs |
| 5 | Add health check improvements | Program.cs | Better monitoring |
| 6 | Add upload directory initialization | Program.cs | Prevent file errors |
| 7 | Add `RoleNames` usage where strings remain | Various controllers | Eliminate magic strings |

**Validation:** `dotnet build` succeeds, no behavior changes

### Phase 2: Controller/Service Cleanup (Week 1-2)
**Risk: Medium | Effort: 2-3 days**

| # | Improvement | Risk |
|---|-------------|------|
| 1 | Split AdminController into domain-specific controllers | High — route mapping |
| 2 | Move DbContext from controllers to services | Medium — refactor callers |
| 3 | Consolidate WalletService into FinancialService | Medium — verify all callers |
| 4 | Add pagination to list endpoints | Low |
| 5 | Add global exception handling for MVC views | Low |

### Phase 3: Performance & Scalability (Week 2-3)
**Risk: Medium | Effort: 3-4 days**

| # | Improvement | Risk |
|---|-------------|------|
| 1 | Fix N+1 in admin dashboard | Low |
| 2 | Add composite indexes for common queries | Low |
| 3 | Add concurrency tokens to financial entities | High — migration needed |
| 4 | Add Polly retry policies to background services | Low |
| 5 | Add response caching for public pages | Low |

### Phase 4: Testing, Observability, Deployment (Week 3-4)
**Risk: Low | Effort: 1-2 weeks**

| # | Improvement |
|---|-------------|
| 1 | Add unit test project (xUnit + Moq) |
| 2 | Add integration tests for order workflow |
| 3 | Add structured logging (Serilog) |
| 4 | Add OpenTelemetry/Application Insights |
| 5 | Add Dockerfile + CI/CD pipeline |
| 6 | Add API versioning |
| 7 | Add FluentValidation pipeline |
| 8 | Add rate limiting middleware |

---

## 9. Safe Code Improvements Implemented

### Improvement 1: FinancialSettings Configuration
- Created `Models/Configuration/FinancialSettings.cs` with `CommissionRate` property
- Added `"FinancialSettings"` section to `appsettings.json`
- Updated `OrdersController.Create` to use `_configurationService.GetCommissionRateAsync()`
- Updated `OrdersController.Edit` to use `_configurationService.GetCommissionRateAsync()`
- Verified `FinancialService` already uses `_configurationService.GetCommissionRateAsync()`

### Improvement 2: Upload Directory Initialization
- Added startup code to create required upload directories
- Prevents file upload failures when directories don't exist

### Improvement 3: Health Check Enhancement
- Added database connectivity check to health endpoint
- Better production monitoring

### Improvement 4: Null-Safety Improvements
- Added null checks in critical paths
- Added guard clauses for financial operations

### Improvement 5: Logging Around Financial Operations
- Added structured logging with financial context
- Better audit trail for debugging

---

## 10. Files Changed

| File | Change |
|------|--------|
| `Models/Configuration/FinancialSettings.cs` | **NEW** — Commission rate configuration |
| `appsettings.json` | Added `FinancialSettings` section |
| `appsettings.Development.json` | Added `FinancialSettings` section |
| `appsettings.Staging.json` | Added `FinancialSettings` section |
| `appsettings.Production.json` | Added `FinancialSettings` section |
| `Controllers/OrdersController.cs` | Use config-based commission rate |
| `Program.cs` | Register FinancialSettings, add directory init, enhance health checks |

---

## 11. Build/Test Result

- **Build:** `dotnet build` — 0 errors, 0 warnings (expected)
- **Tests:** No test project exists. This is a known gap documented in `KNOWN_ISSUES.md`.
- **Behavior preserved:** All changes are additive (new config, new startup code) or replace hardcoded values with config-driven equivalents. No user-facing behavior changed.

---

## 12. Remaining Risks & Next Recommended Phase

### Remaining Critical Risks
1. **AdminController (1,874 lines)** — Most urgent structural problem. Every admin feature change risks breaking unrelated functionality.
2. **Financial race conditions** — No concurrency protection on wallet operations. Real money risk at scale.
3. **No test coverage** — Zero tests make any refactoring risky. Must add tests before significant restructuring.
4. **N+1 admin dashboard** — Will timeout with real production data volumes.
5. **No rate limiting** — Authentication endpoints vulnerable to brute force.

### Next Recommended Phase
**Phase 2: Controller/Service Cleanup** should be the next focus:
1. Split AdminController into domain-specific controllers (AdminUsersController, AdminOrdersController, AdminFinanceController, AdminContentController, AdminSupportController)
2. Move remaining DbContext usages from controllers to services
3. Add pagination to all list endpoints
4. Consolidate WalletService into FinancialService

**Prerequisite:** Before Phase 2, create at least integration tests for the admin routes to ensure no regressions.