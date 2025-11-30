# 💳 Comprehensive Payment Flow Implementation

## 📋 Overview

Đây là implementation plan cho một payment flow hoàn chỉnh với error handling, retry logic, audit logging, và support cho nhiều payment methods.

## ✅ Đã Hoàn Thành

### 1. Models & Enums
- ✅ `PaymentMethod` enum: Cash, QRBankTransfer, Card, EWallet, DiscountCoupon
- ✅ `PaymentStatus` enum: WaitingForPayment, PaymentProcessing, Paid, Cancelled, Failed, PartiallyPaid
- ✅ `Transaction` model đã được mở rộng với:
  - AmountReceived, RefundAmount (cho Cash payment)
  - GatewayErrorCode, GatewayErrorMessage
  - RetryCount, LastRetryAt
  - ParentTransactionId (cho Split Bill)
  - IsManualConfirmed, ConfirmedByUserId
- ✅ `AuditLog` model cho logging
- ✅ `OrderLock` model cho lock order khi payment

### 2. DTOs
- ✅ `CashPaymentRequestDto` - Request cho cash payment
- ✅ `PaymentStatusResponseDto` - Response cho status check
- ✅ `PaymentRetryRequestDto` - Request cho retry payment
- ✅ `PaymentNotifyRequestDto` - Request từ gateway callback
- ✅ `OrderLockRequestDto` - Request cho lock/unlock order
- ✅ `SplitBillRequestDto` & `SplitBillPartDto` - Request cho split bill

### 3. Services
- ✅ `IAuditLogService` & `AuditLogService` - Service cho audit logging

## 🚧 Cần Implement Tiếp

### Phase 1: Repository Layer
1. Tạo `IAuditLogRepository` & `AuditLogRepository`
2. Tạo `IOrderLockRepository` & `OrderLockRepository`
3. Extend `IPaymentRepository` với:
   - `GetTransactionByIdAsync(int transactionId)`
   - `GetTransactionsByOrderIdAsync(int orderId)`
   - `GetActiveOrderLockAsync(int orderId)`
   - `CreateOrderLockAsync(OrderLock lock)`
   - `RemoveOrderLockAsync(int orderId)`
4. Update `IUnitOfWork` & `UnitOfWork` để include new repositories

### Phase 2: Service Layer
Extend `IPaymentService` & `PaymentService` với:

#### CASE 1: Cash Payment
```csharp
Task<TransactionDto> ProcessCashPaymentAsync(CashPaymentRequestDto request, CancellationToken ct = default);
```
- Validate: amountReceived >= totalAmount
- Calculate refund if overpaid
- Log attempt_underpaid nếu thiếu tiền
- Require refund confirmation nếu có tiền thối

#### CASE 2: Payment Status Check
```csharp
Task<PaymentStatusResponseDto> CheckPaymentStatusAsync(int orderId, CancellationToken ct = default);
```
- Poll status từ database
- Return current status và error info

#### CASE 3: Retry Payment
```csharp
Task<TransactionDto> RetryPaymentAsync(PaymentRetryRequestDto request, CancellationToken ct = default);
```
- Increment RetryCount
- Update LastRetryAt
- Retry với exponential backoff

#### CASE 4: Sync Offline Payments
```csharp
Task<List<TransactionDto>> SyncPaymentsAsync(List<int> transactionIds, CancellationToken ct = default);
```
- Sync các transactions từ localStorage
- Update status từ backend

#### CASE 5: Gateway Callback
```csharp
Task<bool> NotifyPaymentAsync(PaymentNotifyRequestDto request, CancellationToken ct = default);
```
- Handle callback từ VNPAY, PayOS, etc.
- Update transaction status
- Mark order as paid

#### CASE 6: Order Locking
```csharp
Task<bool> LockOrderAsync(OrderLockRequestDto request, int userId, CancellationToken ct = default);
Task<bool> UnlockOrderAsync(int orderId, CancellationToken ct = default);
Task<bool> IsOrderLockedAsync(int orderId, CancellationToken ct = default);
```
- Lock order khi payment in progress
- Prevent adding items during payment
- Auto unlock sau 10 phút

#### CASE 7: Split Bill
```csharp
Task<List<TransactionDto>> ProcessSplitBillAsync(SplitBillRequestDto request, CancellationToken ct = default);
```
- Create multiple transactions
- Link với ParentTransactionId
- Mark order as paid khi tất cả parts paid

### Phase 3: Controller Layer
Update `PaymentController` với các endpoints:

```csharp
[HttpPost("cash")] // CASE 1: Cash payment
[HttpGet("status/{orderId}")] // CASE 2: Check status
[HttpPost("retry")] // CASE 3: Retry
[HttpPost("sync")] // CASE 4: Sync offline
[HttpPost("notify")] // CASE 5: Gateway callback
[HttpPost("lock")] // CASE 6: Lock order
[HttpPost("unlock")] // CASE 6: Unlock order
[HttpPost("split-bill")] // CASE 7: Split bill
```

### Phase 4: Frontend
1. **Cash Payment Modal** (`Views/Payment/CashPaymentModal.cshtml`)
   - Input: Amount Received
   - Validation: Show error nếu thiếu tiền
   - Show refund amount nếu thừa tiền
   - Require "Đã trả lại tiền" confirmation

2. **Payment Status Component** (`wwwroot/js/payment-status.js`)
   - Poll status every 5 seconds
   - Timeout after 60 seconds
   - Show manual confirmation option

3. **Error Handling** (`wwwroot/js/payment-error-handler.js`)
   - Handle gateway errors
   - Show user-friendly messages
   - Retry option
   - Switch payment method option

4. **Offline Caching** (`wwwroot/js/payment-offline.js`)
   - Store in localStorage
   - Auto retry on reconnect
   - Sync with backend

5. **Split Bill UI** (`Views/Payment/SplitBillModal.cshtml`)
   - Divide order into parts
   - Show payment status for each part
   - Mark full order as paid when all parts paid

## 🔄 Database Migration

Cần tạo migration cho:
1. Transaction table extensions
2. AuditLogs table
3. OrderLocks table

## 📝 Implementation Priority

1. **Priority 1**: Cash Payment với validation ✅ (Models & DTOs done)
2. **Priority 2**: VietQR Payment improvements (đã có cơ bản)
3. **Priority 3**: Payment Status Check & Polling
4. **Priority 4**: Order Locking
5. **Priority 5**: Error Recovery & Retry
6. **Priority 6**: Split Bill
7. **Priority 7**: Offline Caching
8. **Priority 8**: Gateway Integration (VNPAY, PayOS)

## 🧪 Testing Scenarios

### Cash Payment
- ✅ Underpaid: Block confirmation, show error
- ✅ Overpaid: Calculate refund, require confirmation
- ✅ Exact amount: Process immediately

### Online Payment
- QR failed to load: Retry, show error
- Payment pending: Poll status, timeout option
- Payment declined: Show error, allow switch method
- Payment success but timeout: Sync on reconnect

### Error Recovery
- Network error: Cache locally, retry on reconnect
- Page reload: Show pending transactions banner
- Gateway timeout: Manual confirmation option

## 📚 File Structure

```
Backend/
├── DomainAccessLayer/
│   ├── Enums/
│   │   ├── PaymentMethod.cs ✅
│   │   └── PaymentStatus.cs ✅
│   └── Models/
│       ├── Transaction.cs ✅ (extended)
│       ├── AuditLog.cs ✅
│       └── OrderLock.cs ✅
├── BusinessAccessLayer/
│   ├── DTOs/Payment/
│   │   ├── CashPaymentRequestDto.cs ✅
│   │   ├── PaymentStatusResponseDto.cs ✅
│   │   └── ... (other DTOs) ✅
│   └── Services/
│       ├── AuditLogService.cs ✅
│       └── PaymentService.cs (needs extension)
└── SapaFoRestRMSAPI/
    └── Controllers/
        └── PaymentController.cs (needs extension)

Frontend/
└── WebSapaForestForStaff/
    ├── Views/Payment/
    │   └── CashPaymentModal.cshtml (to create)
    └── wwwroot/js/
        └── payment-flow.js (to create)
```

## 🚀 Next Steps

1. Tạo repositories cho AuditLog và OrderLock
2. Extend PaymentService với các methods mới
3. Update PaymentController với các endpoints
4. Create frontend components
5. Test với seed data

## 📖 Documentation

Xem thêm:
- `PAYMENT_FLOW_IMPLEMENTATION_PLAN.md` - Chi tiết implementation plan
- `PAYMENT_FLOW_IMPLEMENTATION_SUMMARY.md` - Summary của các components

