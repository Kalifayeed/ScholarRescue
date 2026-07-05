# Phase 1 Corrective Fix — Round 2

## Checklist

- [ ] 1. Add `ValidateFiles` to `IOrderAttachmentService` interface
- [ ] 2. Extract `ValidateFiles` as public method in `OrderAttachmentService`
- [ ] 3. Fix `Create()`: check `UploadedFileData`, validate before DB writes
- [ ] 4. Wire `GuestCreate()`: pre-flight check, validate before account creation, save attachments
- [ ] 5. Add file upload widget + conditional JS to `GuestCreate.cshtml`
- [ ] 6. Build and verify
- [ ] 7. Commit and push