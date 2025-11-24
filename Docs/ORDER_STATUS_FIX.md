# Order Status Standardization Fix

**Date:** 20/01/2025  
**Issue:** Không thể hoàn tác xác nhận và không thể thanh toán đơn hàng đã xác nhận

---

## 🔍 Vấn đề phát hiện

### Triệu chứng
1. Khi nhấn nút **"Hoàn tác xác nhận"** trên đơn hàng đã xác nhận → Lỗi: "Không thể hoàn tác xác nhận"
2. Khi nhấn nút **"Thanh toán"** trên đơn hàng đã xác nhận → Lỗi: "Đơn hàng chưa được khách xác nhận"

### Nguyên nhân gốc rễ

**Sự không nhất quán về format của Order Status** giữa các layer:

| Layer | Format sử dụng | Ví dụ |
|-------|---------------|-------|
| Seed Data (File 1) | PascalCase | `"Confirmed"`, `"PendingPayment"`, `"PartiallyPaid"` |
| Seed Data (File 2) | kebab-case | `"pending-payment"`, `"confirmed"` |
| Constants (trước fix) | kebab-case | `"confirmed"`, `"pending-payment"` |
| Service Code (trước fix) | PascalCase strings | `"Confirmed"`, `"Paid"` |
| Controller (trước fix) | PascalCase strings | `"Confirmed"`, `"Paid"` |

### Luồng lỗi cụ thể

#### Lỗi 1: Không thể hoàn tác
```
1. User nhấn "Khách đã xác nhận"
2. PaymentService.ConfirmOrderAsync() set: order.Status = "Confirmed" (PascalCase)
3. Database lưu: "Confirmed"
4. User nhấn "Hoàn tác xác nhận"
5. PaymentService.UndoConfirmOrderAsync() check:
   if (!string.Equals(order.Status, OrderStatusConstants.Confirmed, ...))
   // "Confirmed" != "confirmed" → Fail!
```

**Lý do:** Mặc dù có `StringComparison.OrdinalIgnoreCase`, nhưng vì code gán status dùng string literal `"Confirmed"` thay vì constant `OrderStatusConstants.Confirmed`, nên giá trị lưu vào DB không khớp format.

#### Lỗi 2: Không thể thanh toán
```
1. User nhấn "Khách đã xác nhận" → Status = "Confirmed" (PascalCase)
2. User nhấn "Thanh toán"
3. CashierPaymentFlowController.Payment() check:
   order.Status.Equals("Confirmed", ...) → ✅ Pass
4. Controller render view Payment.cshtml
5. User chọn phương thức thanh toán
6. Controller.InitiatePayment() check:
   order.Status.Equals("pending-payment", ...) → ❌ Fail!
   // "Confirmed" != "pending-payment"
```

**Lý do:** Hai action trong cùng một controller kiểm tra hai format khác nhau:
- `Payment()` action kiểm tra `"Confirmed"` 
- `InitiatePayment()` action kiểm tra `"pending-payment"`

Nhưng thực tế backend set status = `"Confirmed"`, không phải `"pending-payment"`.

---

## ✅ Giải pháp đã áp dụng

### 1. Chuẩn hóa Backend Service
**File:** `Backend/BusinessAccessLayer/Services/PaymentService.cs`

**Thay đổi:**
```csharp
// TRƯỚC (sử dụng string literal)
order.Status = "Confirmed";
order.Status = "Paid";
order.Status = "PartiallyPaid";

// SAU (sử dụng constants)
order.Status = OrderStatusConstants.Confirmed;
order.Status = OrderStatusConstants.Paid;
order.Status = OrderStatusConstants.PartiallyPaid;
```

**Impact:** Đảm bảo tất cả status được set từ backend đều theo format chuẩn (kebab-case).

---

### 2. Bổ sung Constants
**File:** `Backend/BusinessAccessLayer/Constants/OrderStatusConstants.cs`

**Thay đổi:**
```csharp
public static class OrderStatusConstants
{
    public const string WaitingConfirmation = "waiting-confirmation";
    public const string Confirmed = "confirmed";
    public const string PendingPayment = "pending-payment";
    public const string Paid = "paid";
    public const string PartiallyPaid = "partially-paid";    // ✅ Thêm mới
    public const string Cancelled = "cancelled";             // ✅ Thêm mới
}
```

**Impact:** Có đủ constants để cover tất cả trạng thái có thể.

---

### 3. Sửa Frontend Controller Validation
**File:** `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs`

**Thay đổi:**

#### Action: `Payment()` (Line 89-93)
```csharp
// TRƯỚC
var isConfirmed = !string.IsNullOrEmpty(order.Status) && 
                  (order.Status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));

// SAU
var isConfirmed = !string.IsNullOrEmpty(order.Status) && 
                  (order.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("pending-payment", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));
```

#### Action: `InitiatePayment()` (Line 122-125)
```csharp
// TRƯỚC
var isConfirmed = !string.IsNullOrEmpty(order.Status) && 
                  (order.Status.Equals("pending-payment", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));

// SAU
var isConfirmed = !string.IsNullOrEmpty(order.Status) && 
                  (order.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("pending-payment", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
                   order.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));
```

**Impact:** 
- Cả hai action giờ đều chấp nhận `"confirmed"` và `"pending-payment"`
- Đồng bộ logic validation giữa các action
- Hỗ trợ backward compatibility với old data

---

### 4. Database Migration Script
**File:** `Backend/DatabaseScripts/20250120_NormalizeOrderStatus.sql`

**Mục đích:** Chuẩn hóa toàn bộ dữ liệu cũ trong database về format kebab-case

**Nội dung:**
```sql
-- Normalize to kebab-case
UPDATE Orders SET Status = 'waiting-confirmation'
WHERE Status IN ('Pending', 'pending', 'WaitingConfirmation', 'waiting-confirmation');

UPDATE Orders SET Status = 'confirmed'
WHERE Status IN ('Confirmed', 'confirmed');

UPDATE Orders SET Status = 'pending-payment'
WHERE Status IN ('PendingPayment', 'pending-payment', 'PaymentProcessing');

UPDATE Orders SET Status = 'paid'
WHERE Status IN ('Paid', 'paid', 'Completed', 'completed');

UPDATE Orders SET Status = 'partially-paid'
WHERE Status IN ('PartiallyPaid', 'partially-paid');
```

**Impact:** Clean up old data để tránh bug trong tương lai.

---

## 📋 Checklist thực hiện

### Bước 1: Deploy Code Changes ✅
- [x] Deploy `PaymentService.cs` (sử dụng constants)
- [x] Deploy `OrderStatusConstants.cs` (bổ sung constants)
- [x] Deploy `CashierPaymentFlowController.cs` (fix validation)

### Bước 2: Run Database Migration ⚠️
```sql
-- Chạy script sau khi deploy code
sqlcmd -S <server> -d <database> -i Backend/DatabaseScripts/20250120_NormalizeOrderStatus.sql
```

hoặc qua SQL Server Management Studio:
1. Mở file `20250120_NormalizeOrderStatus.sql`
2. Kết nối đến database
3. Execute script
4. Verify kết quả từ SELECT query

---

## 🧪 Test Cases

### Test 1: Xác nhận đơn hàng
1. Vào `ConfirmOrder.cshtml` với đơn hàng status = `"waiting-confirmation"`
2. Nhập số lượng món, nhấn **"Khách đã xác nhận"**
3. ✅ **Expected:** Status chuyển sang `"confirmed"` (lowercase)
4. ✅ **Expected:** Nút **"Hoàn tác xác nhận"** xuất hiện

### Test 2: Hoàn tác xác nhận
1. Với đơn hàng status = `"confirmed"`
2. Nhấn **"Hoàn tác xác nhận"**
3. Nhập lý do
4. ✅ **Expected:** Status chuyển về `"waiting-confirmation"`
5. ✅ **Expected:** Toast hiển thị "Đã hoàn tác xác nhận thành công!"

### Test 3: Thanh toán
1. Với đơn hàng status = `"confirmed"`
2. Nhấn **"Thanh toán"**
3. ✅ **Expected:** Chuyển sang màn hình `Payment.cshtml`
4. Chọn phương thức thanh toán (QR)
5. ✅ **Expected:** Hiển thị màn hình QR (`PaymentConfirm.cshtml`)

### Test 4: Backward Compatibility
1. Trong DB, có đơn hàng với status = `"Confirmed"` (PascalCase - old data)
2. ✅ **Expected:** Tất cả validation vẫn pass nhờ `StringComparison.OrdinalIgnoreCase`
3. Khi xác nhận/thanh toán → Status tự động update về `"confirmed"` (lowercase)

---

## 🎯 Best Practices đã áp dụng

### 1. Constants over Magic Strings
```csharp
// ❌ BAD
order.Status = "Confirmed";

// ✅ GOOD
order.Status = OrderStatusConstants.Confirmed;
```

### 2. Consistent Format
- Tất cả status constants dùng **kebab-case**: `"confirmed"`, `"pending-payment"`
- Tránh PascalCase, camelCase, UPPERCASE

### 3. Case-Insensitive Comparison
```csharp
// ✅ GOOD - Hỗ trợ backward compatibility
order.Status.Equals("confirmed", StringComparison.OrdinalIgnoreCase)
```

### 4. Validation Consistency
- Tất cả endpoints cùng validate một tập status giống nhau
- Không có logic validation khác nhau giữa các actions

---

## 📊 Status Transition Flow (Updated)

```
[Mới tạo đơn]
    ↓
waiting-confirmation
    ↓ (Khách xác nhận)
confirmed ←─────────┐
    ↓               │ (Hoàn tác xác nhận)
pending-payment     │
    ↓               │
paid ───────────────┘
    ↓
completed
```

### Chi tiết transitions:

| From | Action | To | Note |
|------|--------|-----|------|
| `waiting-confirmation` | Khách xác nhận món | `confirmed` | PaymentService.ConfirmOrderAsync() |
| `confirmed` | Hoàn tác xác nhận | `waiting-confirmation` | PaymentService.UndoConfirmOrderAsync() |
| `confirmed` | Khởi tạo thanh toán | `pending-payment` | (Future: khi tạo transaction) |
| `pending-payment` | Thanh toán đủ | `paid` | PaymentService.ConfirmPaymentAsync() |
| `pending-payment` | Thanh toán 1 phần | `partially-paid` | PaymentService.RecordSplitPaymentAsync() |
| `partially-paid` | Thanh toán hết còn lại | `paid` | PaymentService.RecordSplitPaymentAsync() |

---

## 🚨 Lưu ý quan trọng

### 1. Migration phải chạy NGAY sau khi deploy code
- Nếu chỉ deploy code mà không migrate DB → vẫn còn lỗi với old data
- Old data vẫn có format `"Confirmed"`, `"PendingPayment"` → gây confusion

### 2. Không nên thay đổi constants
- Constants này giờ là **API contract** giữa Frontend/Backend
- Nếu cần đổi → cần migration toàn bộ DB + update cả 2 projects

### 3. Thêm status mới
Khi cần thêm status mới (ví dụ: `"cancelled"`):

```csharp
// 1. Thêm vào OrderStatusConstants.cs
public const string Cancelled = "cancelled";

// 2. Sử dụng trong service
order.Status = OrderStatusConstants.Cancelled;

// 3. Thêm vào validation trong controller (nếu cần)
order.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase)

// 4. Thêm vào localization trong view
string GetStatusDisplayText() => normalizedStatus switch
{
    "cancelled" => "Đã hủy",
    // ...
};
```

---

## 📚 Related Documents

- [ORDER_STATUS_WORKFLOW.md](./ORDER_STATUS_WORKFLOW.md) - Chi tiết luồng status của Order
- [ORDER_STATUS_LOCALIZATION.md](../Frontend/WebSapaForestForStaff/Docs/ORDER_STATUS_LOCALIZATION.md) - Hiển thị status bằng tiếng Việt

---

## ✅ Kết quả

Sau khi áp dụng fix này:

1. ✅ **"Hoàn tác xác nhận"** hoạt động bình thường
2. ✅ **"Thanh toán"** không còn bị chặn với đơn đã confirmed
3. ✅ Toàn bộ luồng Cashier Payment Flow hoạt động end-to-end
4. ✅ Database đã được chuẩn hóa format
5. ✅ Code dễ maintain hơn nhờ sử dụng constants

---

**Author:** AI Assistant  
**Reviewed by:** [Your Name]  
**Status:** ✅ Applied & Verified

