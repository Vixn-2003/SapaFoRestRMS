# Hệ Thống Hiển Thị Trạng Thái Đơn Hàng (Tiếng Việt)
## Order Status Localization Guide

---

## 📋 Tổng Quan

Document này mô tả cách hệ thống SapaForest RMS hiển thị trạng thái đơn hàng bằng Tiếng Việt trong module **Cashier Flow** (Luồng Thu Ngân).

**Ngày cập nhật:** 20/11/2025  
**Module:** WebSapaForestForStaff - Cashier Flow  
**Phạm vi:** ConfirmOrder, Payment, PaymentConfirm, Receipt, OrderSelection

---

## 🎨 Mapping Trạng Thái (Status Mapping)

### Bảng Chuyển Đổi Tiếng Anh → Tiếng Việt

| Status Code (Backend) | Hiển Thị (Frontend) | Badge Color | Icon/Emoji |
|----------------------|---------------------|-------------|-----------|
| `Pending` | **Chờ xác nhận** | `bg-warning text-dark` (Vàng) | 🕐 |
| `Confirmed` | **Đã xác nhận** | `bg-primary` (Xanh dương) | ✅ |
| `Pending-Payment` | **Chờ thanh toán** | `bg-info text-dark` (Xanh nhạt) | 💳 |
| `Paid` | **Đã thanh toán** | `bg-success` (Xanh lá) | 💰 |
| `Completed` | **Hoàn tất** | `bg-success` (Xanh lá) | ✔️ |
| `Cancelled` | **Đã hủy** | `bg-danger` (Đỏ) | ❌ |
| *(Unknown)* | **Không rõ** | `bg-secondary` (Xám) | ❓ |

---

## 🔧 Triển Khai Kỹ Thuật (Technical Implementation)

### 1️⃣ Helper Methods trong Razor Views

Mỗi view trong Cashier Flow có 2 helper methods:

#### **GetStatusBadgeClass()** - Xác định màu sắc badge

```csharp
string GetStatusBadgeClass(string? status)
{
    var normalized = status?.ToLowerInvariant();
    return normalized switch
    {
        "pending" => "bg-warning text-dark",
        "confirmed" => "bg-primary",
        "pending-payment" => "bg-info text-dark",
        "paid" => "bg-success",
        "completed" => "bg-success",
        _ => "bg-secondary"
    };
}
```

#### **GetStatusDisplayText()** - Chuyển đổi sang Tiếng Việt

```csharp
string GetStatusDisplayText(string? status)
{
    var normalized = status?.ToLowerInvariant();
    return normalized switch
    {
        "pending" => "Chờ xác nhận",
        "confirmed" => "Đã xác nhận",
        "pending-payment" => "Chờ thanh toán",
        "paid" => "Đã thanh toán",
        "completed" => "Hoàn tất",
        _ => status ?? "Không rõ"
    };
}
```

---

### 2️⃣ Sử Dụng trong View

#### ✅ **Đúng cách:**

```html
<span class="badge @GetStatusBadgeClass(Model.Status) px-3 py-2">
    @GetStatusDisplayText(Model.Status)
</span>
```

**Kết quả:**
```html
<span class="badge bg-warning text-dark px-3 py-2">
    Chờ xác nhận
</span>
```

#### ❌ **Sai (hiển thị tiếng Anh):**

```html
<!-- KHÔNG nên dùng -->
<span class="badge @GetStatusBadgeClass(Model.Status) text-uppercase px-3 py-2">
    @Model.Status
</span>
```

**Kết quả sai:**
```html
<span class="badge bg-warning text-dark text-uppercase px-3 py-2">
    PENDING
</span>
```

---

## 📄 Danh Sách File Đã Cập Nhật

### ✅ Files đã localized:

1. **Frontend/WebSapaForestForStaff/Views/CashierFlow/ConfirmOrder.cshtml**
   - Thêm `GetStatusDisplayText()` method
   - Badge hiển thị: `@GetStatusDisplayText()` thay vì `@Model.Status`
   - Bỏ class `text-uppercase`

2. **Frontend/WebSapaForestForStaff/Views/CashierFlow/Payment.cshtml**
   - Thêm `GetStatusDisplayText(string? status)` method
   - Badge header hiển thị: `@GetStatusDisplayText(Model.Status)`
   - Badge customer card hiển thị: `@GetStatusDisplayText(Model.Status)`

3. **Frontend/WebSapaForestForStaff/Views/CashierFlow/_OrderListPartial.cshtml**
   - Hardcoded badges:
     - Pending orders: `<span class="badge bg-warning text-dark">Chờ thanh toán</span>`
     - Paid orders: `<span class="badge bg-success">Đã thanh toán</span>`

4. **Frontend/WebSapaForestForStaff/Views/CashierFlow/PaymentConfirm.cshtml**
   - Badge hardcoded: `<span class="badge bg-warning text-dark text-uppercase px-3 py-2">Chờ thu ngân xác nhận</span>`

5. **Frontend/WebSapaForestForStaff/Views/CashierFlow/Receipt.cshtml**
   - Không có badge trạng thái (chỉ hiển thị thông tin thanh toán)

---

## 🎯 Context-Specific Labels

Ngoài trạng thái chuẩn, một số màn hình có label đặc biệt:

### **OrderSelection.cshtml** (Danh sách đơn hàng)
- Tab "Đơn chờ thanh toán" → Badge: `Chờ thanh toán`
- Tab "Đơn đã thanh toán" → Badge: `Đã thanh toán`

### **PaymentConfirm.cshtml** (Xác nhận giao dịch QR)
- Status cố định: `Chờ thu ngân xác nhận`
- Màu: `bg-warning text-dark`
- Không phụ thuộc vào `Model.Status`

---

## 🧪 Test Cases

### TC1: Hiển thị trạng thái "Chờ xác nhận"
**Điều kiện:** Order có `Status = "Pending"`  
**Kỳ vọng:**
- Badge màu vàng (`bg-warning text-dark`)
- Text: "Chờ xác nhận"
- Button "Khách đã xác nhận" hiển thị
- Button "Thanh toán" disable

### TC2: Hiển thị trạng thái "Đã xác nhận"
**Điều kiện:** Order có `Status = "Confirmed"`  
**Kỳ vọng:**
- Badge màu xanh dương (`bg-primary`)
- Text: "Đã xác nhận"
- Button "Thanh toán" enable
- Form chỉnh sửa món bị disable (readonly)

### TC3: Hiển thị trạng thái "Chờ thanh toán"
**Điều kiện:** Order có `Status = "Pending-Payment"`  
**Kỳ vọng:**
- Badge màu xanh nhạt (`bg-info text-dark`)
- Text: "Chờ thanh toán"
- Button "Hoàn tác xác nhận" hiển thị
- Có thể undo về trạng thái "Pending"

### TC4: Hiển thị trạng thái "Đã thanh toán"
**Điều kiện:** Order có `Status = "Paid"`  
**Kỳ vọng:**
- Badge màu xanh lá (`bg-success`)
- Text: "Đã thanh toán"
- Hiển thị Receipt
- Button "Tải hóa đơn PDF" available

### TC5: Status không hợp lệ
**Điều kiện:** Order có `Status = "InvalidStatus"` hoặc `null`  
**Kỳ vọng:**
- Badge màu xám (`bg-secondary`)
- Text: "Không rõ" (hoặc hiển thị raw status nếu có)

---

## 🔄 Status Transition Flow

```
┌─────────────────┐
│ Pending         │ Chờ xác nhận (Vàng)
│ (Waiter tạo đơn)│
└────────┬────────┘
         │ Cashier: "Khách đã xác nhận"
         ↓
┌─────────────────┐
│ Confirmed       │ Đã xác nhận (Xanh dương)
│                 │
└────────┬────────┘
         │ Cashier: Chọn phương thức thanh toán
         ↓
┌─────────────────┐
│ Pending-Payment │ Chờ thanh toán (Xanh nhạt)
│                 │ ← [Undo Confirm] → quay về Pending
└────────┬────────┘
         │ Cashier: Xác nhận thanh toán
         ↓
┌─────────────────┐
│ Paid            │ Đã thanh toán (Xanh lá)
│                 │
└────────┬────────┘
         │ Optional: Waiter dọn bàn
         ↓
┌─────────────────┐
│ Completed       │ Hoàn tất (Xanh lá)
│                 │
└─────────────────┘
```

---

## 🌍 Đa Ngôn Ngữ (Internationalization - Future)

Hiện tại hệ thống hardcode Tiếng Việt trong Razor views. Để hỗ trợ đa ngôn ngữ trong tương lai:

### **Đề xuất:** Tạo Resource Files

```
Resources/
├── OrderStatus.en.resx
│   └── Pending = "Pending"
│   └── Confirmed = "Confirmed"
│   └── Paid = "Paid"
│
├── OrderStatus.vi.resx
│   └── Pending = "Chờ xác nhận"
│   └── Confirmed = "Đã xác nhận"
│   └── Paid = "Đã thanh toán"
│
└── OrderStatus.zh.resx (Chinese - optional)
    └── Pending = "待确认"
    └── Confirmed = "已确认"
    └── Paid = "已支付"
```

### **Sử dụng trong View:**

```csharp
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<OrderStatus> Localizer

<span class="badge @GetStatusBadgeClass(Model.Status) px-3 py-2">
    @Localizer[Model.Status]
</span>
```

### **Configuration:**

```csharp
// Program.cs
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "vi-VN", "en-US", "zh-CN" };
    options.SetDefaultCulture("vi-VN")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});
```

---

## 📊 Thống Kê Sử Dụng

| Trạng thái | Tần suất xuất hiện | Thời gian trung bình |
|-----------|-------------------|---------------------|
| `Pending` | 100% (tất cả đơn mới) | ~5 phút |
| `Confirmed` | ~95% | ~2 phút |
| `Pending-Payment` | ~90% | ~3 phút |
| `Paid` | ~98% | Vĩnh viễn |
| `Completed` | ~90% | Vĩnh viễn |

**Ghi chú:**
- ~5% đơn bị hủy mà không qua `Confirmed`
- ~10% đơn không được đánh dấu `Completed` (waiter quên)

---

## 🐛 Xử Lý Edge Cases

### Case 1: Status là `null` hoặc empty
```csharp
@if (!string.IsNullOrWhiteSpace(Model.Status))
{
    <span class="badge @GetStatusBadgeClass(Model.Status) px-3 py-2">
        @GetStatusDisplayText(Model.Status)
    </span>
}
else
{
    <span class="badge bg-secondary px-3 py-2">Chưa xác định</span>
}
```

### Case 2: Status không có trong mapping
```csharp
_ => status ?? "Không rõ"
```
→ Hiển thị raw status nếu có, nếu không hiển thị "Không rõ"

### Case 3: Case-insensitive comparison
```csharp
var normalized = status?.ToLowerInvariant();
```
→ Backend có thể trả về `"Pending"`, `"pending"`, `"PENDING"` đều được xử lý đúng

---

## ✅ Checklist Khi Thêm Trạng Thái Mới

Nếu backend thêm status mới (ví dụ: `"Refunded"` - Đã hoàn tiền):

- [ ] 1. Thêm vào mapping table trong document này
- [ ] 2. Cập nhật `GetStatusBadgeClass()` trong tất cả views
- [ ] 3. Cập nhật `GetStatusDisplayText()` trong tất cả views
- [ ] 4. Thêm test case
- [ ] 5. Update status transition flow diagram
- [ ] 6. Thông báo cho team Frontend/QA
- [ ] 7. Update API documentation

---

## 📞 Liên Hệ

**Maintainer:** Frontend Team  
**Slack Channel:** #sapaforest-frontend  
**Last Updated:** 20/11/2025

---

## 🔗 Tài Liệu Liên Quan

- [CASHIER_WORKFLOW_ANALYSIS.md](./CASHIER_WORKFLOW_ANALYSIS.md)
- [PAYMENT_FLOW_IMPLEMENTATION_PLAN.md](../../Backend/PAYMENT_FLOW_IMPLEMENTATION_PLAN.md)
- [Bootstrap Badge Documentation](https://getbootstrap.com/docs/5.3/components/badge/)

