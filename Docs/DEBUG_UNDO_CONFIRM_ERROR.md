# Debug Guide: "Không thể hoàn tác xác nhận"

**Lỗi:** User nhấn nút **"Hoàn tác xác nhận"** → Hiển thị toast message đỏ: "Không thể hoàn tác xác nhận"

---

## 🔍 Lỗi này xảy ra khi nào?

API endpoint: `PUT /api/payment/orders/{orderId}/undo-confirm`

Controller xử lý các exception như sau:

```csharp
catch (KeyNotFoundException ex)          → 404 NotFound
catch (InvalidOperationException ex)     → 400 BadRequest
catch (Exception ex)                     → 500 Internal Server Error
```

Frontend hiển thị: `data?.message || 'Không thể hoàn tác xác nhận.'`

---

## ❌ 4 Trường hợp gây lỗi

### **1. Order không tồn tại** 
**Line:** `PaymentService.cs:291-293`

```csharp
if (order == null)
{
    throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
}
```

| HTTP Status | Message hiển thị |
|-------------|------------------|
| `404 Not Found` | "Không tìm thấy đơn hàng với ID: {orderId}" |

**Cách test:**
- Gọi API với OrderId không tồn tại (ví dụ: 99999)
- Hoặc Order bị xóa khỏi database

---

### **2. ⚠️ Order Status không phải "confirmed"** 
**Line:** `PaymentService.cs:296-299`

```csharp
if (!string.Equals(order.Status, OrderStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Order cannot be reverted at this stage.");
}
```

| HTTP Status | Message hiển thị |
|-------------|------------------|
| `400 Bad Request` | "Order cannot be reverted at this stage." |

**Khi nào xảy ra:**

| Order Status | Có thể hoàn tác? | Lý do |
|--------------|------------------|-------|
| `"waiting-confirmation"` | ❌ | Chưa xác nhận, không có gì để hoàn tác |
| `"confirmed"` | ✅ | Đúng trạng thái! |
| `"pending-payment"` | ❌ | Đã chuyển sang bước thanh toán |
| `"paid"` | ❌ | Đã thanh toán xong |
| `"partially-paid"` | ❌ | Đã thanh toán 1 phần |
| `"completed"` | ❌ | Đã hoàn tất |

**⚠️ ĐÂY LÀ LỖI PHỔ BIẾN NHẤT!**

**Nguyên nhân:**
1. Database lưu status sai format: `"Confirmed"` (PascalCase) thay vì `"confirmed"` (lowercase)
2. Backend đã chuyển status sang `"pending-payment"` hoặc `"paid"`
3. User refresh page nhiều lần → status đã thay đổi

**Cách debug:**
```sql
-- Kiểm tra status hiện tại của Order
SELECT OrderId, Status, ConfirmedAt, CreatedAt 
FROM Orders 
WHERE OrderId = {your_order_id};
```

**Fix đã áp dụng:**
- ✅ Backend dùng `OrderStatusConstants.Confirmed` thay vì string literal
- ✅ Frontend controller validation chấp nhận nhiều format hơn
- ✅ Migration script chuẩn hóa data cũ

---

### **3. Đã có Payment transaction**
**Line:** `PaymentService.cs:301-304`

```csharp
if (order.Payments != null && order.Payments.Any(p => p.PaymentDate.HasValue))
{
    throw new InvalidOperationException("Không thể hoàn tác vì đơn hàng đã bắt đầu thanh toán.");
}
```

| HTTP Status | Message hiển thị |
|-------------|------------------|
| `400 Bad Request` | "Không thể hoàn tác vì đơn hàng đã bắt đầu thanh toán." |

**Khi nào xảy ra:**
- User đã nhấn "Thanh toán" → Tạo Payment transaction
- Payment có `PaymentDate` (không null)

**Business Logic:** 
- Không cho hoàn tác nếu đã bắt đầu thanh toán
- Tránh gian lận: khách thanh toán rồi, staff hoàn tác để giảm số tiền phải trả

**Cách debug:**
```sql
-- Kiểm tra Payment transactions của Order
SELECT t.TransactionId, t.Amount, t.PaymentMethod, t.Status, t.PaymentDate
FROM Transactions t
WHERE t.OrderId = {your_order_id};
```

**Flow bình thường:**
```
[confirmed] → User nhấn "Thanh toán" 
           → Tạo Transaction (PaymentDate = NOW)
           → Không thể hoàn tác nữa ✅ Đúng!
```

---

### **4. Bếp đã bắt đầu chế biến món**
**Line:** `PaymentService.cs:306-311`

```csharp
if (order.OrderDetails != null && order.OrderDetails.Any(od =>
    string.Equals(od.Status, "Cooking", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(od.Status, "Served", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Không thể hoàn tác vì bếp đã bắt đầu chế biến món.");
}
```

| HTTP Status | Message hiển thị |
|-------------|------------------|
| `400 Bad Request` | "Không thể hoàn tác vì bếp đã bắt đầu chế biến món." |

**Khi nào xảy ra:**
- Có ít nhất 1 OrderDetail có Status = `"Cooking"` (đang nấu)
- Hoặc Status = `"Served"` (đã phục vụ)

**Business Logic:**
- Không cho hoàn tác nếu bếp đã làm món
- Tránh lãng phí: món đã nấu rồi, không thể hủy được

**Cách debug:**
```sql
-- Kiểm tra status các món trong đơn
SELECT od.OrderDetailId, od.MenuItemId, mi.Name, od.Quantity, od.Status
FROM OrderDetails od
LEFT JOIN MenuItems mi ON od.MenuItemId = mi.MenuItemId
WHERE od.OrderId = {your_order_id};
```

**OrderDetail Status flow:**
```
Pending → Confirmed → Cooking → Served
   ↑         ↑          ❌        ❌
   ↓         ↓        Không thể hoàn tác
Có thể      Có thể
hoàn tác    hoàn tác
(nếu Order
Status đúng)
```

---

## 🔧 Cách debug lỗi của bạn

### Bước 1: Mở Browser DevTools
1. Nhấn `F12` (Chrome/Edge)
2. Chuyển sang tab **Network**
3. Filter: `undo-confirm`

### Bước 2: Reproduce lỗi
1. Nhấn nút **"Hoàn tác xác nhận"**
2. Nhập lý do → Submit

### Bước 3: Xem Response
Click vào request `undo-confirm` → Tab **Response**

**Ví dụ response:**
```json
{
  "message": "Order cannot be reverted at this stage."
}
```

hoặc

```json
{
  "message": "Không thể hoàn tác vì đơn hàng đã bắt đầu thanh toán."
}
```

### Bước 4: Check Database
```sql
-- Script đầy đủ để debug
DECLARE @OrderId INT = {your_order_id};

-- 1. Check Order status
SELECT 'ORDER INFO' as Section, 
       OrderId, Status, ConfirmedAt, CreatedAt, CreatedBy
FROM Orders 
WHERE OrderId = @OrderId;

-- 2. Check OrderDetails status
SELECT 'ORDER ITEMS' as Section,
       od.OrderDetailId, mi.Name, od.Quantity, od.Status
FROM OrderDetails od
LEFT JOIN MenuItems mi ON od.MenuItemId = mi.MenuItemId
WHERE od.OrderId = @OrderId;

-- 3. Check Payment transactions
SELECT 'PAYMENTS' as Section,
       t.TransactionId, t.Amount, t.PaymentMethod, 
       t.Status, t.PaymentDate, t.CreatedAt
FROM Transactions t
WHERE t.OrderId = @OrderId;
```

---

## 🎯 Giải pháp cho từng trường hợp

### Trường hợp 1: Order không tồn tại
**Giải pháp:** Bug frontend, không nên xảy ra. Reload lại page.

---

### Trường hợp 2: Status không phải "confirmed"
**Nếu Status = `"waiting-confirmation"`:**
- Order chưa được xác nhận
- Không có gì để hoàn tác
- ✅ **Đúng behavior**

**Nếu Status = `"Confirmed"` (PascalCase):**
- ⚠️ Database có data cũ chưa chuẩn hóa
- **Fix:** Chạy migration script:
  ```powershell
  sqlcmd -S your_server -d SapaForestDB -i Backend\DatabaseScripts\20250120_NormalizeOrderStatus.sql
  ```

**Nếu Status = `"pending-payment"` hoặc `"paid"`:**
- Order đã qua bước xác nhận rồi
- Không thể hoàn tác nữa
- ✅ **Đúng behavior** (nên ẩn nút "Hoàn tác xác nhận" ở frontend)

**Frontend fix suggestion:**
```razor
@* ConfirmOrder.cshtml - Line 21 *@
@{
    var canUndoConfirm = normalizedStatus == "confirmed"; 
    // Chỉ show nút khi status = "confirmed", không show với "pending-payment" hay "paid"
}
```

---

### Trường hợp 3: Đã có Payment transaction
**Giải pháp:**
- Nếu cần hoàn tác → Phải **Cancel Payment** trước
- Sau đó mới hoàn tác xác nhận
- Hoặc: Đây là business rule đúng, không nên hoàn tác

**API flow đúng:**
```
POST /api/payment/payments/cancel  (Cancel payment trước)
  ↓
PUT /api/payment/orders/{id}/undo-confirm  (Sau đó hoàn tác)
```

---

### Trường hợp 4: Bếp đã chế biến món
**Giải pháp:**
- Không thể hoàn tác (business rule)
- Nếu thực sự cần → Liên hệ bếp để xử lý thủ công
- Có thể cần quy trình riêng: "Hủy món đã làm" + ghi log

**Suggestion:** Thêm permission đặc biệt cho Manager/Owner để force undo trong trường hợp đặc biệt.

---

## 📋 Checklist khi gặp lỗi

- [ ] Check Browser DevTools → Network → Response message
- [ ] Check Database → Order Status hiện tại
- [ ] Check Database → OrderDetails Status
- [ ] Check Database → Có Payment transactions không?
- [ ] Đã chạy migration script `20250120_NormalizeOrderStatus.sql` chưa?
- [ ] Code backend đã được deploy chưa? (dùng constants thay vì string literals)
- [ ] Code frontend controller đã được deploy chưa? (validation đúng format)

---

## ✅ Expected Behavior

### Happy Path: Hoàn tác thành công
```
1. Order Status = "confirmed"
2. Chưa có Payment transaction (hoặc PaymentDate = NULL)
3. Tất cả OrderDetails Status = "Confirmed" hoặc "Pending"
4. ✅ Hoàn tác thành công → Status chuyển về "waiting-confirmation"
5. Toast hiển thị: "Đã hoàn tác xác nhận thành công!"
6. Page reload → Nút "Khách đã xác nhận" xuất hiện lại
```

### Sad Path: Không thể hoàn tác
```
1. Order Status != "confirmed" 
   HOẶC đã có Payment
   HOẶC món đang Cooking/Served
2. ❌ API trả về 400 Bad Request với message cụ thể
3. Toast hiển thị message từ API (tiếng Việt, dễ hiểu)
4. User hiểu lý do và biết cách xử lý
```

---

## 🔗 Related Files

- `Backend/BusinessAccessLayer/Services/PaymentService.cs` (Line 288-334) - Logic hoàn tác
- `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs` (Line 119-143) - API endpoint
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/ConfirmOrder.cshtml` (Line 375-415) - Frontend call API
- `Backend/DatabaseScripts/20250120_NormalizeOrderStatus.sql` - Migration script
- `Docs/ORDER_STATUS_FIX.md` - Giải thích chi tiết về fix

---

**Last Updated:** 20/01/2025  
**Status:** ✅ Applied & Documented

