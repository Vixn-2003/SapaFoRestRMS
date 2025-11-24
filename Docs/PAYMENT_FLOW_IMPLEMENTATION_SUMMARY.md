# Payment Flow Implementation Summary

## ✅ Completed Components

### 1. Models & Enums
- ✅ `PaymentMethod` enum (Cash, QRBankTransfer, Card, EWallet, DiscountCoupon)
- ✅ `PaymentStatus` enum (WaitingForPayment, PaymentProcessing, Paid, Cancelled, Failed, PartiallyPaid)
- ✅ `Transaction` model extended với:
  - AmountReceived, RefundAmount (cho Cash)
  - GatewayErrorCode, GatewayErrorMessage
  - RetryCount, LastRetryAt
  - ParentTransactionId (cho Split Bill)
  - IsManualConfirmed, ConfirmedByUserId
- ✅ `AuditLog` model
- ✅ `OrderLock` model

### 2. DTOs
- ✅ `CashPaymentRequestDto`
- ✅ `PaymentStatusResponseDto`
- ✅ `PaymentRetryRequestDto`
- ✅ `PaymentNotifyRequestDto`
- ✅ `OrderLockRequestDto`
- ✅ `SplitBillRequestDto` & `SplitBillPartDto`

### 3. Services
- ✅ `IAuditLogService` & `AuditLogService`

## 🚧 Next Implementation Steps

### Phase 1: Repository Extensions
1. Extend `IPaymentRepository` với:
   - `GetTransactionByIdAsync(int transactionId)`
   - `GetTransactionsByOrderIdAsync(int orderId)`
   - `GetActiveOrderLockAsync(int orderId)`
   - `CreateOrderLockAsync(OrderLock lock)`
   - `RemoveOrderLockAsync(int orderId)`

2. Create `IAuditLogRepository` & implementation
3. Create `IOrderLockRepository` & implementation
4. Update `IUnitOfWork` to include new repositories

### Phase 2: PaymentService Extensions
Extend `IPaymentService` với các methods:

```csharp
// CASE 1: Cash Payment
Task<TransactionDto> ProcessCashPaymentAsync(CashPaymentRequestDto request, CancellationToken ct = default);

// CASE 2: Payment Status Check
Task<PaymentStatusResponseDto> CheckPaymentStatusAsync(int orderId, CancellationToken ct = default);

// CASE 3: Retry Payment
Task<TransactionDto> RetryPaymentAsync(PaymentRetryRequestDto request, CancellationToken ct = default);

// CASE 4: Sync Offline Payments
Task<List<TransactionDto>> SyncPaymentsAsync(List<int> transactionIds, CancellationToken ct = default);

// CASE 5: Gateway Callback
Task<bool> NotifyPaymentAsync(PaymentNotifyRequestDto request, CancellationToken ct = default);

// CASE 6: Order Locking
Task<bool> LockOrderAsync(OrderLockRequestDto request, int userId, CancellationToken ct = default);
Task<bool> UnlockOrderAsync(int orderId, CancellationToken ct = default);
Task<bool> IsOrderLockedAsync(int orderId, CancellationToken ct = default);

// CASE 7: Split Bill
Task<List<TransactionDto>> ProcessSplitBillAsync(SplitBillRequestDto request, CancellationToken ct = default);
```

### Phase 3: PaymentController Endpoints
```csharp
[HttpPost("cash")] // CASE 1: Cash payment với validation
[HttpGet("status/{orderId}")] // CASE 2: Check status
[HttpPost("retry")] // CASE 3: Retry
[HttpPost("sync")] // CASE 4: Sync offline
[HttpPost("notify")] // CASE 5: Gateway callback
[HttpPost("lock")] // CASE 6: Lock order
[HttpPost("unlock")] // CASE 6: Unlock order
[HttpPost("split-bill")] // CASE 7: Split bill
```

### Phase 4: Frontend Implementation
1. Cash Payment Modal với validation
2. Payment Status Polling component
3. Error Handling UI
4. Retry Logic
5. Offline Caching (localStorage)
6. Split Bill UI

## 📝 Implementation Notes

### Cash Payment Validation Logic
```csharp
if (amountReceived < totalAmount) {
    // Log: attempt_underpaid
    throw new InvalidOperationException("Số tiền chưa đủ");
}

if (amountReceived > totalAmount) {
    refund = amountReceived - totalAmount;
    // Require refund confirmation
}
```

### Payment Status Polling
- Poll interval: 5 seconds
- Timeout: 60 seconds
- Show manual confirmation option after timeout

### Error Recovery
- Retry with exponential backoff (max 3 retries)
- Manual confirmation option
- Switch payment method option

## 🔄 Database Migration Required

Cần tạo migration cho:
1. Transaction table extensions (AmountReceived, RefundAmount, etc.)
2. AuditLogs table
3. OrderLocks table

## 📚 Files to Create/Update

### Backend
- [ ] `DataAccessLayer/Repositories/Interfaces/IAuditLogRepository.cs`
- [ ] `DataAccessLayer/Repositories/AuditLogRepository.cs`
- [ ] `DataAccessLayer/Repositories/Interfaces/IOrderLockRepository.cs`
- [ ] `DataAccessLayer/Repositories/OrderLockRepository.cs`
- [ ] Update `IPaymentRepository` với new methods
- [ ] Update `PaymentRepository` implementation
- [ ] Update `IUnitOfWork` & `UnitOfWork`
- [ ] Update `IPaymentService` & `PaymentService`
- [ ] Update `PaymentController` với new endpoints
- [ ] Update `SapaFoRestRmsContext` với new entities

### Frontend
- [ ] `wwwroot/js/payment-flow.js` - Main payment flow handler
- [ ] `wwwroot/js/payment-cash.js` - Cash payment handler
- [ ] `wwwroot/js/payment-status.js` - Status polling
- [ ] `Views/Payment/CashPaymentModal.cshtml`
- [ ] `Views/Payment/SplitBillModal.cshtml`
- [ ] Update `Views/Payment/OrderDetail.cshtml`

