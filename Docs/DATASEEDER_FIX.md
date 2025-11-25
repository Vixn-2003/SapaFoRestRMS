# DataSeeder Fix - Order Status Consistency

**Date:** 20/01/2025  
**Issue:** DataSeeder.cs không nhất quán với OrderStatusConstants và logic nghiệp vụ

---

## 🔍 Các vấn đề đã phát hiện

### ❌ **Vấn đề 1: Hard-coded status strings**

**Trước khi sửa:**
```csharp
Status = "pending-payment"  // Line 629, 696
```

**Vấn đề:**
- Dùng string literal thay vì constant
- Không type-safe
- Dễ typo
- Khó maintain

---

### ❌ **Vấn đề 2: Sai logic nghiệp vụ**

**Trước khi sửa:**
```csharp
var order1 = new Order
{
    Status = "pending-payment",  // ❌ SAI!
    ...
};
```

**Vấn đề:**
Order được tạo với status `"pending-payment"` ngay từ đầu là **SAI LOGIC NGHIỆP VỤ**!

**Luồng đúng:**
```
1. Waiter tạo Order → "waiting-confirmation"
2. Khách kiểm tra & xác nhận món → "confirmed"
3. Cashier chọn phương thức thanh toán → "pending-payment"
4. Thanh toán thành công → "paid"
```

DataSeeder đang **skip bước 1 & 2**, tạo Order ở bước 3 luôn ❌

---

### ❌ **Vấn đề 3: OrderDetail Status không đúng**

**Trước khi sửa:**
```csharp
order1Details.Add(new OrderDetail
{
    Status = "Served",  // ❌ SAI!
    ...
});
```

**Vấn đề:**
1. **PascalCase** thay vì lowercase
2. **Logic sai**: 
   - Nếu Order = `"waiting-confirmation"` → OrderDetail phải là `"Pending"`
   - Nếu Order = `"confirmed"` → OrderDetail phải là `"Confirmed"`
   - `"Served"` chỉ dùng khi món đã được phục vụ cho khách (bước sau khi confirm)

---

### ❌ **Vấn đề 4: Payment records không nên tạo sẵn**

**Trước khi sửa:**
```csharp
var payment1 = new Payment
{
    OrderId = order1.OrderId,
    PaymentMethod = "QR",
    PaymentDate = null,
    ...
};
await context.Payments.AddAsync(payment1);
```

**Vấn đề:**
- Payment record chỉ nên được tạo khi cashier **initiate payment**
- Tạo sẵn Payment làm rối loạn workflow
- Frontend sẽ hiển thị sai thông tin

**Logic đúng:**
```
1. Order tạo → KHÔNG có Payment record
2. Khách xác nhận → KHÔNG có Payment record
3. Cashier chọn phương thức → InitiatePaymentAsync() tạo Transaction/Payment
4. Cashier xác nhận thanh toán → Update Payment status
```

---

### ⚠️ **Vấn đề 5: Cleanup query không đúng**

**Trước khi sửa:**
```csharp
var pendingOrderIds = await context.Orders
    .Where(o => o.Status == "pending-payment")  // Chỉ cleanup 1 status
    .Select(o => o.OrderId)
    .ToListAsync();
```

**Vấn đề:**
- Chỉ cleanup `"pending-payment"`, bỏ sót `"waiting-confirmation"` và `"confirmed"`
- Test data cũ sẽ tích lũy

---

### ❌ **Vấn đề 6: Thiếu import namespace**

**Trước khi sửa:**
File không có `using BusinessAccessLayer.Constants;`

---

## ✅ Giải pháp đã áp dụng

### 1. Thêm namespace import

```csharp
using BusinessAccessLayer.Constants;
```

---

### 2. Sửa Order 1 status và logic

**Sau khi sửa:**
```csharp
// Order 1: Waiting for customer confirmation
var order1 = new Order
{
    ReservationId = reservation.ReservationId,
    CustomerId = customer.CustomerId,
    OrderType = "DineIn",
    Status = OrderStatusConstants.WaitingConfirmation,  // ✅ Đúng!
    CreatedAt = DateTime.UtcNow.AddMinutes(-30),
    TotalAmount = 0m
};
```

**Kết quả:**
- ✅ Order 1 ở trạng thái chờ xác nhận
- ✅ Test cashier workflow từ đầu: Customer Confirm → Payment

---

### 3. Sửa Order 1 OrderDetails status

**Sau khi sửa:**
```csharp
order1Details.Add(new OrderDetail
{
    OrderId = order1.OrderId,
    MenuItemId = item1.MenuItemId,
    Quantity = 2,
    UnitPrice = item1.Price,
    Status = "Pending",  // ✅ Đúng! Lowercase & phù hợp với Order status
    CreatedAt = DateTime.UtcNow.AddMinutes(-29)
});
```

**Kết quả:**
- ✅ OrderDetail = "Pending" phù hợp với Order = "waiting-confirmation"
- ✅ Lowercase format nhất quán

---

### 4. Sửa Order 2 status và thêm ConfirmedAt

**Sau khi sửa:**
```csharp
// Order 2: Already confirmed by customer, ready for payment
var order2 = new Order
{
    ReservationId = reservation.ReservationId,
    CustomerId = customer.CustomerId,
    OrderType = "DineIn",
    Status = OrderStatusConstants.Confirmed,  // ✅ Đúng!
    ConfirmedAt = DateTime.UtcNow.AddMinutes(-5),  // ✅ Thêm timestamp
    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
    TotalAmount = 0m
};
```

**Kết quả:**
- ✅ Order 2 đã được khách xác nhận
- ✅ Có timestamp `ConfirmedAt` để audit
- ✅ Test cashier workflow từ bước Payment

---

### 5. Sửa Order 2 OrderDetails status

**Sau khi sửa:**
```csharp
order2Details.Add(new OrderDetail
{
    OrderId = order2.OrderId,
    MenuItemId = null,
    ComboId = combo1.ComboId,
    Quantity = 1,
    UnitPrice = combo1.Price,
    Status = "Confirmed",  // ✅ Đúng! Phù hợp với Order status
    CreatedAt = DateTime.UtcNow.AddMinutes(-9)
});
```

**Kết quả:**
- ✅ OrderDetail = "Confirmed" phù hợp với Order = "confirmed"
- ✅ Nhất quán về status lifecycle

---

### 6. Xóa Payment records

**Sau khi sửa:**
```csharp
// Note: Payment records should NOT be created here
// They will be created when cashier initiates payment via InitiatePaymentAsync()
// This ensures proper payment workflow: Order → Customer Confirm → Initiate Payment → Process Payment
```

**Kết quả:**
- ✅ Không tạo Payment sẵn
- ✅ Payment chỉ tạo khi user thực sự initiate
- ✅ Workflow đúng với business logic

---

### 7. Sửa cleanup query

**Sau khi sửa:**
```csharp
var pendingOrderIds = await context.Orders
    .Where(o => o.Status == OrderStatusConstants.WaitingConfirmation || 
               o.Status == OrderStatusConstants.Confirmed ||
               o.Status == OrderStatusConstants.PendingPayment)
    .Select(o => o.OrderId)
    .ToListAsync();
```

**Kết quả:**
- ✅ Cleanup tất cả test orders (3 trạng thái)
- ✅ Dùng constants thay vì strings
- ✅ Không để lại data rác

---

## 📊 Kết quả sau khi Seed

### Order 1: Chờ xác nhận
```
OrderId: 1
Status: "waiting-confirmation"
ConfirmedAt: NULL
OrderDetails (3 items):
  - Item 1: Quantity=2, Status="Pending"
  - Item 2: Quantity=1, Status="Pending"
  - Item 3: Quantity=1, Status="Pending"
Payments: (empty - chưa có)
```

**Use case:**
- Test luồng **Customer Confirm** từ đầu
- Cashier mở ConfirmOrder.cshtml
- Nhập QuantityUsed
- Nhấn "Khách đã xác nhận"
- ✅ Status chuyển sang "confirmed"

---

### Order 2: Đã xác nhận (sẵn sàng thanh toán)
```
OrderId: 2
Status: "confirmed"
ConfirmedAt: 5 phút trước
OrderDetails (4 items):
  - Combo 1: Quantity=1, Status="Confirmed"
  - Combo 2: Quantity=1, Status="Confirmed"
  - Item 4: Quantity=1, Status="Confirmed"
  - Item 5: Quantity=2, Status="Confirmed"
Payments: (empty - chưa có)
```

**Use case:**
- Test luồng **Payment** từ bước chọn phương thức
- Cashier mở Payment.cshtml
- Chọn phương thức (Cash/QR/Combined/Split)
- InitiatePayment → Tạo Payment/Transaction
- ConfirmPayment → Status chuyển sang "paid"

---

## 🎯 Test Cases

### Test 1: Seed data thành công
```powershell
# Run seeder
dotnet run --project Backend/SapaFoRestRMSAPI

# Verify in database
SELECT OrderId, Status, ConfirmedAt FROM Orders;
SELECT OrderId, MenuItemId, ComboId, Status FROM OrderDetails;
```

**Expected:**
- ✅ Order 1: Status = "waiting-confirmation"
- ✅ Order 2: Status = "confirmed"
- ✅ Tất cả OrderDetails có status phù hợp

---

### Test 2: Cashier Workflow - Order 1
```
1. Navigate to /cashier-flow/orders
2. Click Order 1 (status: "Chờ xác nhận")
3. Enter QuantityUsed for items
4. Click "Khách đã xác nhận"
5. ✅ Status → "confirmed"
6. Click "Thanh toán"
7. Select payment method (QR)
8. ✅ Payment initiated successfully
9. Confirm payment
10. ✅ Status → "paid"
```

---

### Test 3: Cashier Workflow - Order 2
```
1. Navigate to /cashier-flow/orders
2. Click Order 2 (status: "Đã xác nhận")
3. ✅ "Hoàn tác xác nhận" button available
4. Click "Thanh toán"
5. Select payment method (Cash)
6. ✅ Payment initiated successfully
7. Confirm payment
8. ✅ Status → "paid"
```

---

### Test 4: Undo Confirm - Order 2
```
1. Open Order 2 (status: "confirmed")
2. Click "Hoàn tác xác nhận"
3. Enter reason
4. Submit
5. ✅ Status → "waiting-confirmation"
6. ✅ ConfirmedAt → NULL
7. ✅ OrderHistory logged
```

---

## 📋 Migration Guide

### Nếu đã chạy seeder cũ

**Bước 1: Clean up old test data**
```sql
-- Delete old payment records
DELETE FROM Payments WHERE OrderId IN (
    SELECT OrderId FROM Orders 
    WHERE Status IN ('pending-payment', 'Confirmed', 'PendingPayment')
);

-- Delete old order details
DELETE FROM OrderDetails WHERE OrderId IN (
    SELECT OrderId FROM Orders 
    WHERE Status IN ('pending-payment', 'Confirmed', 'PendingPayment')
);

-- Delete old orders
DELETE FROM Orders 
WHERE Status IN ('pending-payment', 'Confirmed', 'PendingPayment');
```

**Bước 2: Chạy migration chuẩn hóa**
```powershell
sqlcmd -S your_server -d SapaForestDB -i Backend\DatabaseScripts\20250120_NormalizeOrderStatus.sql
```

**Bước 3: Chạy seeder mới**
```powershell
dotnet run --project Backend/SapaFoRestRMSAPI
```

---

## 🎓 Best Practices

### 1. Luôn dùng Constants
```csharp
// ❌ BAD
order.Status = "confirmed";

// ✅ GOOD
order.Status = OrderStatusConstants.Confirmed;
```

---

### 2. OrderDetail Status phải phù hợp với Order Status

| Order Status | OrderDetail Status |
|--------------|-------------------|
| `waiting-confirmation` | `"Pending"` |
| `confirmed` | `"Confirmed"` |
| (bếp nhận) | `"Cooking"` |
| (đã phục vụ) | `"Served"` |

---

### 3. Không tạo Payment/Transaction trước thời hạn

```csharp
// ❌ BAD - Tạo Payment khi seed
var payment = new Payment { OrderId = 1, ... };
await context.Payments.AddAsync(payment);

// ✅ GOOD - Để service tạo khi cần
// Payment sẽ được tạo bởi InitiatePaymentAsync()
```

---

### 4. Seed đa dạng test cases

```csharp
// Order 1: Chờ xác nhận (test Customer Confirm flow)
Status = OrderStatusConstants.WaitingConfirmation

// Order 2: Đã xác nhận (test Payment flow)
Status = OrderStatusConstants.Confirmed

// Order 3 (future): Đã thanh toán (test Receipt flow)
Status = OrderStatusConstants.Paid
```

---

### 5. Cleanup đúng cách

```csharp
// ✅ Cleanup tất cả test statuses
var testOrderIds = await context.Orders
    .Where(o => o.Status == OrderStatusConstants.WaitingConfirmation || 
               o.Status == OrderStatusConstants.Confirmed ||
               o.Status == OrderStatusConstants.PendingPayment)
    .Select(o => o.OrderId)
    .ToListAsync();
```

---

## 🔗 Related Documents

- [ORDER_STATUS_FIX.md](./ORDER_STATUS_FIX.md) - Fix cho PaymentService và Controllers
- [ORDER_STATUS_WORKFLOW.md](./ORDER_STATUS_WORKFLOW.md) - Chi tiết luồng status
- [DEBUG_UNDO_CONFIRM_ERROR.md](./DEBUG_UNDO_CONFIRM_ERROR.md) - Debug guide cho lỗi hoàn tác

---

## ✅ Summary

| Aspect | Trước | Sau |
|--------|-------|-----|
| **Status format** | `"pending-payment"` (string) | `OrderStatusConstants.WaitingConfirmation` |
| **Order 1 Status** | `"pending-payment"` ❌ | `"waiting-confirmation"` ✅ |
| **Order 2 Status** | `"pending-payment"` ❌ | `"confirmed"` ✅ |
| **OrderDetail Status** | `"Served"` (PascalCase) ❌ | `"Pending"` / `"Confirmed"` ✅ |
| **Payment records** | Tạo sẵn ❌ | Không tạo (để service tạo) ✅ |
| **Cleanup query** | 1 status ❌ | 3 statuses ✅ |
| **Import namespace** | Missing ❌ | Added ✅ |

---

**Status:** ✅ Fixed & Documented  
**Last Updated:** 20/01/2025

