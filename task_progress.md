# Phase 5, Round 1: Rename Order → TutoringRequest

## Progress

- [ ] Step 1: Rename Models/Order.cs → Models/TutoringRequest.cs (class + file)
- [ ] Step 2: Update Data/ScholarRescueDbContext.cs (DbSet type, ToTable mapping)
- [ ] Step 3: Update Models that reference Order (OrderDocument, OrderNote, OrderHistory, OrderAttachment, OrderSubmission, OrderBid, OrderMilestone, OrderTimelineEvent, OrderDispute, OrderApplication, OrderFinancialRecord, EscrowAccount, Payment, RevisionRequest, QaReview, DeadlineReminder, MonitoringAlert, Conversation, etc.)
- [ ] Step 4: Update Services/ files (all IOrder* and Order* services)
- [ ] Step 5: Update Controllers/ (internal types only, keep class names)
- [ ] Step 6: Update ViewModels/Order/ files
- [ ] Step 7: Update Views/Orders/*.cshtml files
- [ ] Step 8: Update other Views that reference Order
- [ ] Step 9: Update Hubs, Middleware, Program.cs
- [ ] Step 10: dotnet build and fix any remaining errors
- [ ] Step 11: Verify database schema unchanged