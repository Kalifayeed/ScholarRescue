# Phase 1 Corrective Fix — Wire Up Real Attachment Persistence

## Checklist

- [ ] 1. Create shared `OrderAttachmentValidation` constants
- [ ] 2. Add `UploadedFileData` to `CreateOrderViewModel`
- [ ] 3. Add `UploadedFileData` to `GuestOrderViewModel`
- [ ] 4. Create `IOrderAttachmentService` / `OrderAttachmentService`
- [ ] 5. Update `OrdersController.Create()` — persist real files, use real validation
- [ ] 6. Update `OrdersController.GuestCreate()` — same treatment
- [ ] 7. Update `Views/Orders/Create.cshtml` — fix file input name, add client-side guard
- [ ] 8. Update `Views/Orders/GuestCreate.cshtml` — add file upload widget if needed
- [ ] 9. Add grandfathering constant to `OrderAssignmentService`
- [ ] 10. Register service in `Program.cs`
- [ ] 11. Build and verify
- [ ] 12. Commit and push