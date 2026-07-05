# Implementation Progress: Pay Later + Order Editability + Funded→Open Fix

## ✅ Completed
1. **Models/Order.cs** - Added `PaymentDeferred` property
2. **Migrations** - Created `AddPaymentDeferred` migration file (applies cleanly to production)
3. **ViewModels/Order/CreateOrderViewModel.cs** - Added `PayLater` field
4. **ViewModels/Order/GuestOrderViewModel.cs** - Added `PayLater` field
5. **Services/Payments/PaystackPaymentService.cs** - Fixed Funded→Open in webhook handler (line 240: `OrderStatus.Funded` → `OrderStatus.Open`), set `PaymentDeferred = false`
6. **Controllers/PaymentsController.cs** - Fixed Funded→Open in callback handler (line 180), added `IEscrowService` DI, updated `Checkout` to accept deferred orders (can pay when `Status == Open && PaymentDeferred`)
7. **Services/WorkDeliveryService.cs** - Added funded escrow check in `AcceptWorkAsync` for deferred orders

## ❌ Remaining
8. **Controllers/OrdersController.cs** - Need to:
   - Inject `IEscrowService`
   - Pay Later branch in `Create` POST: set `PaymentDeferred = true`, `Status = Open`, `IsMarketplaceOpen = true`, create escrow in `PendingFunding`, redirect to Details
   - Pay Later branch in `GuestCreate` POST (same logic)
   - Fix `Edit` GET: populate `viewModel.RequestType = order.RequestType`, `viewModel.IsRequestTypeLocked = order.Status != OrderStatus.Draft`
   - Fix `Edit` POST: re-read `Pages`/`Deadline` from DB when not Draft
9. **ViewModels/Order/OrderDetailsViewModel.cs** - Add `PaymentDeferred` and `HasPendingEscrow` properties
10. **ViewModels/Order/EditOrderViewModel.cs** - Add `IsPagesLocked`/`IsDeadlineLocked`
11. **Views** - Create.cshtml, GuestCreate.cshtml, Details.cshtml, Edit.cshtml updates
12. **Build, verify, commit**