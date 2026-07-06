# Task: Pay Later, Order Editability & Funded→Open Fix

## Remaining Fixes
- [ ] `Edit()` GET: populate `RequestType`/`IsRequestTypeLocked`
- [ ] `Edit()` POST: lock Pages/Deadline when not Draft
- [ ] `InitiatePaystack`: accept deferred orders
- [ ] `Details.cshtml`: add Pay Now button for deferred orders

## Already Implemented
- ✅ `Order.PaymentDeferred` model + migration
- ✅ `Create` POST: Pay Later branch
- ✅ `GuestCreate` POST: Pay Later branch
- ✅ `PaystackPaymentService` webhook: Funded→Open fix
- ✅ `PaymentsController.PaystackCallback`: Funded→Open fix
- ✅ `PaymentsController.Checkout`: accepts deferred orders
- ✅ `WorkDeliveryService.AcceptWorkAsync`: escrow-funding check
- ✅ `CreateOrderViewModel`/`GuestOrderViewModel`: Pay Later field
- ✅ Views: Pay Later UI in Create/GuestCreate