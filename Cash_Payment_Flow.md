# FLOW THANH TOÁN TIỀN MẶT (CASH PAYMENT)
## Từ Order Confirmed → Order Paid

---

## 📋 TỔNG QUAN FLOW

```
Order Status: WaitingConfirmation 
    ↓ (Waiter xác nhận)
Order Status: Confirmed
    ↓ (Thu ngân chọn thanh toán tiền mặt)
Order Status: Paid
```

---

## 🔄 CHI TIẾT FLOW

### **BƯỚC 1: ORDER CONFIRMATION (Waiter xác nhận đơn)**

**Frontend:**
- **View:** `DashboardTable/OrderDetail.cshtml`
- **Controller:** `DashboardTableController.ConfirmOrder()`
- **Route:** `POST /DashboardTable/ConfirmOrder`

**Backend API:**
- **Endpoint:** `PUT /api/payment/orders/{orderId}/confirm`
- **Controller:** `PaymentController.ConfirmOrder()`
- **Service:** `PaymentService.ConfirmOrderAsync()`

**Xử lý:**
1. Waiter xác nhận số lượng món khách đã dùng (`QuantityUsed`)
2. Cập nhật trạng thái các món billable thành `"Done"`
3. Tính toán tổng tiền: `Subtotal + VAT (10%) + ServiceFee (5%) - Discount`
4. Lưu `TotalAmount` vào `Orders.TotalAmount`
5. **Cập nhật Order Status:**
   ```sql
   UPDATE Orders 
   SET Status = 'Confirmed',
       ConfirmedAt = GETUTCDATE(),
       ConfirmedByStaffId = {staffId},
       TotalAmount = {calculatedTotal}
   WHERE OrderId = {orderId}
   ```
6. Ghi `OrderHistory` với action `"Order Confirmation"`

**Kết quả:** `Order.Status = "Confirmed"` ✅

---

### **BƯỚC 2: THU NGÂN VÀO MÀN HÌNH THANH TOÁN**

**Frontend:**
- **View:** `CashierFlow/OrderSelection.cshtml`
- **Controller:** `CashierPaymentFlowController.OrderSelection()`
- **Route:** `GET /cashier-flow/orders`

**Hiển thị:**
- Danh sách đơn hàng có `Status = "Confirmed"` (chờ thanh toán)
- Thu ngân chọn đơn cần thanh toán

**Navigation:**
- Click vào đơn → `GET /cashier-flow/payment/{orderId}`
- **View:** `CashierFlow/Payment.cshtml`

---

### **BƯỚC 3: CHỌN PHƯƠNG THỨC THANH TOÁN TIỀN MẶT**

**Frontend:**
- **View:** `CashierFlow/Payment.cshtml` (dòng 171-175)
- **Button:** Nút "💵 Tiền mặt"
- **JavaScript:** `openCashPaymentModal()` → `CashPayment.openCashPaymentModal()`

**File JS:** `wwwroot/js/cashier/cash-payment.js`

**Xử lý:**
1. Mở modal `_CashPaymentModal.cshtml`
2. Hiển thị tổng tiền cần thanh toán
3. Input field để nhập số tiền khách đưa (`amountReceived`)

---

### **BƯỚC 4: NHẬP SỐ TIỀN VÀ XÁC NHẬN**

**Frontend JavaScript Logic:**
- **File:** `wwwroot/js/cashier/cash-payment.js`
- **Function:** `calculateCashChange()` (dòng 90-129)

**Validation:**
1. **Underpaid (thiếu tiền):**
   - Nếu `AmountReceived < TotalAmount` → Hiển thị cảnh báo
   - Disable nút "Xác nhận thanh toán"
   - Không cho phép submit

2. **Exact Payment (đúng số tiền):**
   - Nếu `AmountReceived == TotalAmount` → Enable nút xác nhận

3. **Overpaid (thừa tiền):**
   - Nếu `AmountReceived > TotalAmount` → Tính tiền thối
   - Hiển thị checkbox "Đã trả lại tiền thối cho khách"
   - Bắt buộc tick checkbox mới cho phép xác nhận

**Function:** `confirmCashPayment()` (dòng 142-232)
- Validate số tiền
- Fill form data vào `cashPaymentForm`
- Submit form

---

### **BƯỚC 5: FRONTEND SUBMIT FORM**

**Form:** `cashPaymentForm` (Payment.cshtml dòng 490-499)
```html
<form id="cashPaymentForm"
      asp-action="ProcessCashPayment"
      asp-controller="CashierPaymentFlow"
      method="post">
    <input type="hidden" name="OrderId" id="cashPaymentOrderId" />
    <input type="hidden" name="AmountReceived" id="cashPaymentAmountReceived" />
    <input type="hidden" name="Notes" id="cashPaymentNotes" />
</form>
```

**Controller:** `CashierPaymentFlowController.ProcessCashPayment()` (dòng 384)
- **Route:** `POST /cashier-flow/payment/cash`
- **Request DTO:** `CashPaymentRequest`
  - `OrderId` (int)
  - `AmountReceived` (decimal)
  - `Notes` (string?)

**Xử lý Frontend Controller:**
1. Validate request (OrderId > 0)
2. Lấy token authentication
3. Gọi Backend API: `POST /api/payment/cash`
4. Xử lý response:
   - **Success:** Redirect đến `Receipt` page
   - **Error:** Redirect về `Payment` page với error message

---

### **BƯỚC 6: BACKEND API XỬ LÝ THANH TOÁN**

**Backend API:**
- **Endpoint:** `POST /api/payment/cash`
- **Controller:** `PaymentController.ProcessCashPayment()` (dòng 489)
- **Service:** `PaymentService.ProcessCashPaymentAsync()` (dòng 1096)

**Request DTO:** `CashPaymentRequestDto`
```csharp
{
    OrderId: int,
    AmountReceived: decimal,
    Notes: string?
}
```

---

### **BƯỚC 7: BUSINESS LOGIC XỬ LÝ (PaymentService.ProcessCashPaymentAsync)**

**File:** `Backend/BusinessAccessLayer/Services/PaymentService.cs`

#### **7.1. Validation & Calculation**
1. Lấy order từ database
2. Tính lại tổng tiền: `CalculateOrderAmounts()`
   - `Subtotal` = Tổng giá món
   - `VAT` = Subtotal × 10%
   - `ServiceFee` = Subtotal × 5%
   - `TotalAmount` = Subtotal + VAT + ServiceFee - Discount - Deposit

#### **7.2. Xử lý các trường hợp đặc biệt:**

**CASE A: Đã thanh toán đủ bằng tiền cọc (TotalAmount = 0, có tiền thừa)**
- Validate: `AmountReceived` phải = 0
- Tạo transaction với `RefundAmount` = tiền thừa từ cọc
- Ghi chú: "Đã thanh toán đủ bằng tiền cọc. Trả lại tiền thừa: X VND"

**CASE B: Underpaid (thiếu tiền)**
- Validate: `AmountReceived < TotalAmount`
- Throw exception: "⚠️ Số tiền chưa đủ..."
- Log audit: `attempt_underpaid`
- **KHÔNG** tạo transaction, **KHÔNG** cập nhật order status

**CASE C: Overpaid (thừa tiền)**
- Tính tiền thối: `RefundAmount = RoundUpToThousand(AmountReceived - TotalAmount)`
- Làm tròn lên mệnh giá 1000 VND

#### **7.3. Lock Order (Tránh race condition)**
```csharp
await LockOrderAsync(new OrderLockRequestDto { OrderId = request.OrderId }, userId, ct);
```

#### **7.4. Tạo Transaction**
```csharp
var transaction = new Transaction
{
    OrderId = request.OrderId,
    TransactionCode = $"TXN-{DateTime.UtcNow.Ticks}",
    Amount = totalAmount,                    // Số tiền cần thanh toán
    AmountReceived = request.AmountReceived,  // Số tiền khách đưa
    RefundAmount = refundAmount,              // Tiền thối lại (nếu có)
    PaymentMethod = "Cash",
    Status = "Paid",
    CreatedAt = DateTime.UtcNow,
    CompletedAt = DateTime.UtcNow,
    IsManualConfirmed = true,
    ConfirmedByUserId = userId,
    Notes = request.Notes ?? (refundAmount.HasValue ? $"Tiền thối lại: {refundAmount.Value:N0} VND" : null)
};

var savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);
```

#### **7.5. Cập nhật Order Status → PAID**
```csharp
order.Status = OrderStatusConstants.Paid;  // "Paid"
await _unitOfWork.Payments.UpdateAsync(order);
```

**SQL:**
```sql
UPDATE Orders 
SET Status = 'Paid',
    UpdatedAt = GETUTCDATE()
WHERE OrderId = {orderId}
```

#### **7.6. Log Audit**
```csharp
await _auditLogService.LogEventAsync(
    "payment_success",
    "Transaction",
    savedTransaction.TransactionId,
    $"Thanh toán tiền mặt thành công. Số tiền: {totalAmount:N0} VND" + 
    (refundAmount.HasValue ? $", Tiền thối: {refundAmount.Value:N0} VND" : ""),
    userId: userId
);
```

#### **7.7. Giải phóng bàn và hoàn thành Reservation**
```csharp
await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);
```

**Xử lý:**
- Lấy reservation từ order
- Cập nhật `Reservation.Status = "Completed"`
- Xóa các bàn khỏi `ReservationTables` (giải phóng bàn)
- Log audit: `reservation_completed`

#### **7.8. Trigger Post-Payment Actions**
```csharp
await TriggerPostPaymentActionsAsync(request.OrderId, savedTransaction.TransactionId, ct);
```

**Các actions được trigger:**
1. **VIP Status Update:**
   - Gọi `ICustomerVipService.AutoUpdateVipWhenPaymentCompletedAsync()`
   - Đánh giá lại VIP status của customer dựa trên tổng chi tiêu

2. **Loyalty Points:**
   - Tăng `LoyaltyPoints +1` cho customer
   - Ghi `CustomerLoyaltyHistory`

3. **Inventory Deduction:**
   - Tiêu thụ inventory đã reserve cho các món `ConsumptionBased`
   - Cập nhật `InventoryBatch.QuantityUsed`

4. **Revenue Recording:**
   - Ghi doanh thu vào báo cáo

**Lưu ý:** Nếu post-payment actions fail, chỉ log error, **KHÔNG** rollback payment

#### **7.9. Unlock Order**
```csharp
await UnlockOrderAsync(request.OrderId, ct);
```

#### **7.10. Save Changes**
```csharp
await _unitOfWork.SaveChangesAsync();
```

**QUAN TRỌNG:** Đảm bảo `Order.Status = "Paid"` được lưu vào database

#### **7.11. Return Transaction DTO**
```csharp
return _mapper.Map<TransactionDto>(savedTransaction);
```

---

### **BƯỚC 8: FRONTEND NHẬN RESPONSE**

**Backend Response:**
```json
{
    "transactionId": 123,
    "orderId": 456,
    "amount": 214500,
    "amountReceived": 250000,
    "refundAmount": 35500,
    "paymentMethod": "Cash",
    "status": "Paid",
    "transactionCode": "TXN-1234567890"
}
```

**Frontend Controller xử lý:**
- **Success:** 
  - Set `TempData["SuccessMessage"]` = "✅ Thanh toán thành công!"
  - Redirect: `RedirectToAction(nameof(Receipt), new { orderId = request.OrderId })`
  
- **Error:**
  - Set `TempData["ErrorMessage"]` = error message
  - Redirect: `RedirectToAction(nameof(Payment), new { id = request.OrderId })`

---

### **BƯỚC 9: HIỂN THỊ HÓA ĐƠN**

**Frontend:**
- **Route:** `GET /cashier-flow/receipt/{orderId}`
- **Controller:** `CashierPaymentFlowController.Receipt()` (dòng 582)
- **View:** `CashierFlow/Receipt.cshtml`

**Xử lý:**
1. Lấy order detail từ API
2. Validate order status = "Paid" (hoặc "Completed", "Success")
3. Hiển thị hóa đơn với:
   - Thông tin đơn hàng
   - Danh sách món đã thanh toán
   - Tổng tiền, VAT, Service Fee, Discount
   - Thông tin transaction (số tiền nhận, tiền thối)
   - Nút "Tải hóa đơn PDF"

---

## 📊 DATABASE CHANGES SUMMARY

### **1. Orders Table:**
```sql
UPDATE Orders 
SET Status = 'Paid',                    -- Từ 'Confirmed' → 'Paid'
    UpdatedAt = GETUTCDATE()
WHERE OrderId = {orderId}
```

### **2. Transactions Table:**
```sql
INSERT INTO Transactions (
    OrderId,
    TransactionCode,
    Amount,
    AmountReceived,
    RefundAmount,
    PaymentMethod,
    Status,
    CreatedAt,
    CompletedAt,
    IsManualConfirmed,
    ConfirmedByUserId,
    Notes
) VALUES (
    {orderId},
    'TXN-{timestamp}',
    {totalAmount},
    {amountReceived},
    {refundAmount},  -- NULL nếu không có tiền thối
    'Cash',
    'Paid',
    GETUTCDATE(),
    GETUTCDATE(),
    1,
    {userId},
    {notes}
)
```

### **3. Reservations Table:**
```sql
UPDATE Reservations 
SET Status = 'Completed',
    UpdatedAt = GETUTCDATE()
WHERE ReservationId = (SELECT ReservationId FROM Orders WHERE OrderId = {orderId})
```

### **4. ReservationTables Table:**
```sql
DELETE FROM ReservationTables 
WHERE ReservationId = {reservationId}
-- Giải phóng các bàn
```

### **5. AuditLogs Table:**
```sql
INSERT INTO AuditLogs (
    EventType,
    EntityType,
    EntityId,
    Description,
    UserId,
    CreatedAt
) VALUES (
    'payment_success',
    'Transaction',
    {transactionId},
    'Thanh toán tiền mặt thành công...',
    {userId},
    GETUTCDATE()
)
```

### **6. Post-Payment Actions:**
- **Customers:** Cập nhật VIP status, LoyaltyPoints
- **CustomerLoyaltyHistory:** Ghi lịch sử tích điểm
- **InventoryBatches:** Cập nhật QuantityUsed (tiêu thụ inventory)

---

## 🔄 ORDER STATUS TRANSITIONS

```
WaitingConfirmation
    ↓ (Waiter xác nhận món)
Confirmed
    ↓ (Thu ngân thanh toán tiền mặt)
Paid ✅
```

---

## 🎯 KEY POINTS

1. **Order phải ở trạng thái "Confirmed"** trước khi thanh toán
2. **Validation số tiền:** Không cho phép underpaid
3. **Lock Order:** Tránh race condition khi nhiều request cùng lúc
4. **Transaction được tạo ngay** với status "Paid" (không có pending state)
5. **Order status chuyển sang "Paid"** ngay sau khi tạo transaction
6. **Post-payment actions** được trigger tự động (VIP, Loyalty, Inventory)
7. **Giải phóng bàn** và hoàn thành reservation tự động
8. **Error handling:** Nếu post-payment actions fail, payment vẫn thành công (chỉ log error)

---

## 📝 FILES LIÊN QUAN

### **Frontend:**
- `Controllers/CashierPaymentFlowController.cs` - ProcessCashPayment()
- `Views/CashierFlow/Payment.cshtml` - Màn hình thanh toán
- `Views/CashierFlow/Modals/_CashPaymentModal.cshtml` - Modal nhập tiền
- `wwwroot/js/cashier/cash-payment.js` - JavaScript xử lý cash payment

### **Backend:**
- `Controllers/PaymentController.cs` - ProcessCashPayment() API endpoint
- `Services/PaymentService.cs` - ProcessCashPaymentAsync() business logic
- `DTOs/Payment/CashPaymentRequestDto.cs` - Request DTO

---

## ✅ KẾT QUẢ CUỐI CÙNG

- ✅ Order.Status = **"Paid"**
- ✅ Transaction được tạo với Status = **"Paid"**
- ✅ Reservation.Status = **"Completed"**
- ✅ Bàn được giải phóng
- ✅ VIP status và Loyalty Points được cập nhật
- ✅ Inventory được tiêu thụ
- ✅ Hóa đơn có thể được tải về

