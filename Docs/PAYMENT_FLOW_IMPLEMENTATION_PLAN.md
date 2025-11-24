# Payment Flow Implementation Plan

## 📋 Overview
Implementation plan cho comprehensive payment flow với error handling, retry logic, và audit logging.

## ✅ Completed
1. ✅ Payment Models & Enums (PaymentMethod, PaymentStatus)
2. ✅ Transaction Model extensions (AmountReceived, RefundAmount, GatewayErrorCode, etc.)
3. ✅ AuditLog Model
4. ✅ OrderLock Model
5. ✅ DTOs (CashPaymentRequestDto, PaymentStatusResponseDto, etc.)

## 🚧 In Progress
1. ⏳ Repository interfaces & implementations (AuditLog, OrderLock)
2. ⏳ PaymentService extensions (Cash payment validation, Status check, Retry, Sync, Notify)
3. ⏳ PaymentController endpoints
4. ⏳ Frontend payment flow UI

## 📝 Next Steps

### Phase 1: Backend Core (Priority 1)
1. Create AuditLogRepository & OrderLockRepository
2. Update IUnitOfWork to include new repositories
3. Extend PaymentService with:
   - `ProcessCashPaymentAsync` - với validation (underpaid/overpaid)
   - `CheckPaymentStatusAsync` - poll status
   - `RetryPaymentAsync` - retry failed payments
   - `SyncPaymentAsync` - sync offline payments
   - `NotifyPaymentAsync` - gateway callback
   - `LockOrderAsync` / `UnlockOrderAsync`
   - `ProcessSplitBillAsync`

### Phase 2: API Endpoints (Priority 2)
1. POST `/api/payment/cash` - Cash payment với validation
2. GET `/api/payment/status/{orderId}` - Check payment status
3. POST `/api/payment/retry` - Retry payment
4. POST `/api/payment/sync` - Sync offline payments
5. POST `/api/payment/notify` - Gateway callback
6. POST `/api/payment/lock` - Lock order
7. POST `/api/payment/unlock` - Unlock order
8. POST `/api/payment/split-bill` - Split bill

### Phase 3: Frontend (Priority 3)
1. Cash Payment Modal với validation
2. Payment Status Polling
3. Error Handling UI
4. Retry Logic
5. Offline Caching
6. Split Bill UI

## 🔧 Technical Notes

### Cash Payment Validation
```csharp
if (amountReceived < totalAmount) {
    // Block confirmation
    // Log: attempt_underpaid
    // Show alert
}

if (amountReceived > totalAmount) {
    refund = amountReceived - totalAmount
    // Show refund amount
    // Require "Đã trả lại tiền" confirmation
}
```

### Payment Status Polling
- Poll every 5 seconds
- Timeout after 60 seconds
- Show manual confirmation option

### Offline Caching
- Store in localStorage
- Auto retry on reconnect
- Sync with backend

### Error Recovery
- Retry with exponential backoff
- Manual confirmation option
- Switch payment method option

