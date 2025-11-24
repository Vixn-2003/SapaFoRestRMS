# Payment Workflow Redesign - Implementation Summary

## 📋 Overview

This document summarizes the complete redesign and implementation of the **Payment Workflow** for the SapaForest Restaurant POS system, following the business flow specification.

---

## ✅ Completed Implementation

### 1. **Model Updates**

#### Transaction Model (`Backend/DomainAccessLayer/Models/Transaction.cs`)
- ✅ Added `GatewayReference` property to store payment gateway transaction IDs (VNPay, MoMo, etc.)
- ✅ All existing fields maintained (AmountReceived, RefundAmount, GatewayErrorCode, etc.)

#### TransactionDto (`Backend/BusinessAccessLayer/DTOs/Payment/TransactionDto.cs`)
- ✅ Added `GatewayReference` property to match model

---

### 2. **New DTOs Created**

#### PaymentStartRequestDto
- Used for `GET /api/payments/start/{orderId}`
- Fields: `OrderId`, `PaymentMethod`

#### PaymentConfirmRequestDto
- Used for `POST /api/payments/confirm`
- Fields: `OrderId`, `TransactionId`, `CashGiven`, `Change`, `GatewayReference`, `Notes`

#### PaymentCancelRequestDto
- Used for `POST /api/payments/cancel`
- Fields: `OrderId`, `TransactionId`, `Reason`

---

### 3. **PaymentService Methods**

#### New Methods Added:

1. **`StartPaymentAsync(int orderId, string paymentMethod, CancellationToken ct)`**
   - Validates order exists and is not already paid
   - Checks if order is locked
   - Calculates total amount
   - Locks order to prevent concurrent modifications
   - Creates transaction with appropriate status
   - Logs audit event
   - Returns transaction DTO

2. **`HandleCallbackAsync(PaymentNotifyRequestDto request, CancellationToken ct)`**
   - Finds transaction by gateway reference or transaction code
   - Updates transaction status based on gateway response
   - Updates order status to "Paid" on success
   - Triggers post-payment actions
   - Unlocks order
   - Logs audit events

3. **`ConfirmManualAsync(PaymentConfirmRequestDto request, int userId, CancellationToken ct)`**
   - Validates transaction exists and belongs to order
   - For cash payments: validates amount received >= amount due
   - Calculates refund amount for overpayment
   - Updates transaction status to "Paid"
   - Marks as manually confirmed
   - Updates order status
   - Triggers post-payment actions
   - Unlocks order

4. **`CancelPaymentAsync(PaymentCancelRequestDto request, int userId, CancellationToken ct)`**
   - Validates transaction exists
   - Only allows cancel if status is "WaitingForPayment" or "PaymentProcessing"
   - Updates transaction status to "Cancelled"
   - Logs audit event
   - Unlocks order

5. **`RetryPendingTransactionsAsync(CancellationToken ct)`**
   - Placeholder for background job to retry failed transactions
   - TODO: Implement proper retry logic with gateway API calls

6. **`TriggerPostPaymentActionsAsync(int orderId, int transactionId, CancellationToken ct)`** (Private)
   - Placeholder for post-payment triggers:
     - Inventory deduction
     - Reports sync
     - WebSocket events
   - Logs audit events

---

### 4. **PaymentController Endpoints**

#### New Endpoints:

1. **`GET /api/payments/start/{orderId}?paymentMethod={method}`**
   - Starts payment flow for an order
   - Returns transaction DTO

2. **`POST /api/payments/confirm`**
   - Confirms payment manually (cash or gateway timeout)
   - Requires authentication
   - Returns success message and transaction

3. **`POST /api/payments/cancel`**
   - Cancels a payment transaction
   - Requires authentication
   - Returns success status

4. **`GET /api/payments/check-status/{orderId}`** (Revised)
   - Checks payment status for an order
   - Returns status information

5. **`POST /api/payments/notify`** (Revised)
   - Gateway callback endpoint
   - Uses `HandleCallbackAsync` method
   - AllowAnonymous for webhook access

---

### 5. **Database Migration**

- ✅ Created migration: `AddGatewayReferenceToTransaction`
- ✅ Added `GatewayReference` column to `Transactions` table (nvarchar(100), nullable)
- ✅ Migration ready to apply: `dotnet ef database update`

---

### 6. **Frontend UI Updates**

#### Payment Status Banner
- ✅ Added status banner component in `OrderDetail.cshtml`
- ✅ Displays payment status with color-coded alerts:
  - 🟢 **Success** (Paid): Green alert
  - 🟡 **Processing**: Yellow alert
  - 🔴 **Failed**: Red alert
  - 🔵 **Waiting**: Blue alert

#### Status Polling
- ✅ Automatic status check on page load
- ✅ Polls every 5 seconds for real-time updates
- ✅ Updates banner dynamically based on payment status

#### CSS Styling
- ✅ Added payment status banner styles
- ✅ Color-coded alert classes for different states

---

## 🔄 Payment Flow States

| State | Description | UI Display |
|-------|-------------|------------|
| `Pending` | Order waiting for payment | 🔵 Blue banner: "Chờ thanh toán" |
| `Processing` | Transaction ongoing | 🟡 Yellow banner: "Đang xử lý thanh toán" |
| `Paid` | Payment completed | 🟢 Green banner: "Đã thanh toán" |
| `Failed` | Payment failed | 🔴 Red banner: "Thanh toán thất bại" |
| `Cancelled` | Aborted by user | 🔴 Red banner: "Đã hủy" |
| `PartialPaid` | Split bill | 🟡 Yellow banner: "Thanh toán một phần" |

---

## 🛡️ Error Handling & Edge Cases

### Implemented:

1. **Underpaid (Cash)**
   - ✅ Validation in `ConfirmManualAsync`
   - ✅ Error message: "Số tiền chưa đủ. Cần: X VND, Nhận: Y VND"
   - ✅ Blocks confirm button

2. **Overpaid (Cash)**
   - ✅ Auto-calculates refund = `AmountReceived - Amount`
   - ✅ Stores in `RefundAmount` field

3. **Order Locking**
   - ✅ Prevents concurrent payment processing
   - ✅ Automatic unlock on success/failure/cancel

4. **Transaction Validation**
   - ✅ Validates order exists
   - ✅ Validates order not already paid
   - ✅ Validates transaction belongs to order
   - ✅ Validates payment method

5. **Gateway Timeout**
   - ✅ Manual confirmation available via `ConfirmManualAsync`
   - ✅ Status polling for real-time updates

### TODO (Future Implementation):

1. **QR Not Loaded / Network Error**
   - Retry mechanism (5s intervals)
   - Show "Thử lại / Chuyển phương thức khác" option

2. **Payment Pending > 60s**
   - Auto-timeout and manual confirmation prompt

3. **Printer Error**
   - Retry / send email option

4. **Browser Reload / Disconnect**
   - Offline caching with localStorage
   - Auto-retry when back online

---

## 📦 Post-Payment Triggers (Stubbed)

The following are placeholders for future implementation:

1. **Inventory Deduction**
   ```csharp
   // TODO: await _inventoryService.DeductItemsAsync(orderId, ct);
   ```

2. **Reports Sync**
   ```csharp
   // TODO: await _reportService.SyncPaymentAsync(orderId, transactionId, ct);
   ```

3. **WebSocket Events**
   ```csharp
   // TODO: await _hubContext.Clients.All.SendAsync("PAYMENT_SUCCESS", new { orderId, transactionId }, ct);
   ```

---

## 🔐 Security & Audit

- ✅ All endpoints require authentication (except gateway callback)
- ✅ User ID captured from JWT claims
- ✅ All payment events logged to audit log:
  - `PaymentStarted`
  - `PaymentSuccess`
  - `PaymentFailed`
  - `PaymentConfirmed`
  - `PaymentCancelled`
  - `PaymentCallbackError`
  - `PostPaymentActions`

---

## 📝 API Endpoints Summary

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/payments/start/{orderId}` | Start payment flow | ✅ Yes |
| POST | `/api/payments/confirm` | Confirm payment manually | ✅ Yes |
| POST | `/api/payments/cancel` | Cancel payment | ✅ Yes |
| GET | `/api/payments/check-status/{orderId}` | Check payment status | ✅ Yes |
| POST | `/api/payments/notify` | Gateway callback | ❌ No (webhook) |

---

## 🚀 Next Steps

1. **Apply Database Migration**
   ```bash
   cd Backend/SapaFoRestRMSAPI
   dotnet ef database update --context SapaFoRestRmsContext
   ```

2. **Implement Gateway Integration**
   - Add actual VNPay/MoMo/ZaloPay API calls in `StartPaymentAsync`
   - Implement webhook signature verification in `HandleCallbackAsync`

3. **Implement Post-Payment Triggers**
   - Connect to InventoryService
   - Connect to ReportService
   - Set up SignalR hub for real-time notifications

4. **Add Retry Logic**
   - Implement `RetryPendingTransactionsAsync` with proper gateway API calls
   - Set up background job (Hangfire/Quartz)

5. **Add Offline Support**
   - Implement localStorage caching
   - Add sync mechanism for offline payments

---

## 📚 Files Modified/Created

### Backend:
- ✅ `DomainAccessLayer/Models/Transaction.cs` - Added GatewayReference
- ✅ `BusinessAccessLayer/DTOs/Payment/PaymentStartRequestDto.cs` - New
- ✅ `BusinessAccessLayer/DTOs/Payment/PaymentConfirmRequestDto.cs` - New
- ✅ `BusinessAccessLayer/DTOs/Payment/PaymentCancelRequestDto.cs` - New
- ✅ `BusinessAccessLayer/DTOs/Payment/TransactionDto.cs` - Added GatewayReference
- ✅ `BusinessAccessLayer/Services/Interfaces/IPaymentService.cs` - Added new methods
- ✅ `BusinessAccessLayer/Services/PaymentService.cs` - Implemented new methods
- ✅ `SapaFoRestRMSAPI/Controllers/PaymentController.cs` - Added new endpoints
- ✅ `DataAccessLayer/Dbcontext/SapaFoRestRmsContext.cs` - Added GatewayReference config
- ✅ `DataAccessLayer/Migrations/...AddGatewayReferenceToTransaction.cs` - New migration

### Frontend:
- ✅ `Views/Payment/OrderDetail.cshtml` - Added status banner and polling
- ✅ `wwwroot/css/payment.css` - Added status banner styles

---

## ✨ Key Features

1. **Clear Payment Flow**: Step-by-step flow from start to completion
2. **Status Visibility**: Real-time status updates with visual feedback
3. **Error Handling**: Comprehensive validation and error messages
4. **Audit Trail**: All payment events logged for compliance
5. **Order Locking**: Prevents concurrent payment processing
6. **Manual Confirmation**: Fallback for gateway timeouts
7. **Extensible Design**: Ready for gateway integration and post-payment triggers

---

## 📞 Support

For questions or issues, refer to:
- Business Flow Specification: See original requirements document
- API Documentation: Swagger UI at `/swagger`
- Audit Logs: Check `AuditLogs` table for payment events

---

**Last Updated**: 2025-01-XX
**Version**: 1.0.0

