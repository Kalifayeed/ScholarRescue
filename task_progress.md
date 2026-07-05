# Phase 2 Implementation Checklist

- [x] Read all relevant source files
- [ ] 1. Add `ReviewedAttachmentId` to `Models/OrderSubmission.cs`
- [ ] 2. Update `Services/IWorkDeliveryService.cs` - add `reviewedAttachmentId` parameter
- [ ] 3. Update `Services/WorkDeliveryService.cs` - validation logic for DraftFeedback/ProofreadingOwnWork
- [ ] 4. Update `ViewModels/Order/OrderWorkspaceViewModel.cs` - add `RequestType`
- [ ] 5. Update `Controllers/OrdersController.cs` - pass new param + populate RequestType in VM
- [ ] 6. Update `Views/Orders/Workspace.cshtml` - conditional UI for attachment selector + comments
- [ ] 7. Create EF Core migration
- [ ] 8. Update `AI_MEMORY/DATABASE_SCHEMA.md`
- [ ] 9. Build and verify