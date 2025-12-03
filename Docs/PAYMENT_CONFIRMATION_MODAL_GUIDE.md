# 📋 Payment Confirmation Modal - Hướng Dẫn Sử Dụng

## 🎯 Mục đích

Thêm popup xác nhận trước khi thu ngân chuyển sang màn hình xử lý thanh toán, đảm bảo người dùng không vô tình nhấn nhầm.

---

## 🔧 Các File Đã Thay Đổi

### 1. **Modal Component**
**File:** `Frontend/WebSapaForestForStaff/Views/CashierFlow/Modals/_PaymentConfirmationModal.cshtml`

**Tính năng:**
- ✅ Hiển thị thông tin bàn
- ✅ Hiển thị mã đơn hàng
- ✅ Hiển thị tổng tiền
- ✅ Nút xác nhận/hủy
- ✅ Design đẹp mắt với gradient

### 2. **Updated Views**

#### `ConfirmOrder.cshtml` (Màn hình xác nhận order)
- Thay link "Thanh toán" thành button mở modal
- Include modal partial view
- Pass parameters: orderId, tableNumber, totalAmount

#### `_OrderListPartial.cshtml` (Danh sách orders)
- Thay link "Thanh toán" thành button mở modal
- Thêm icon cho UX tốt hơn

#### `OrderSelection.cshtml` (Màn hình chọn order)
- Include modal partial view

---

## 📱 User Flow

### Trước đây:
```
OrderDetail → [Click "Thanh toán"] → Payment Page (ngay lập tức)
```

### Bây giờ:
```
OrderDetail 
    ↓ [Click "Xử lý thanh toán"]
Popup xác nhận
    ↓ [User xác nhận]
Payment Page
```

---

## 🎨 UI/UX Design

### Modal Layout

```
┌─────────────────────────────────────────┐
│ ✅ Xác nhận thanh toán            [X]   │ ← Header (gradient blue)
├─────────────────────────────────────────┤
│                                         │
│         🍽️ (Icon)                       │
│                                         │
│  Khách hàng tại bàn T01 đã dùng bữa    │
│              xong.                      │
│                                         │
│  Bạn có muốn bắt đầu xử lý thanh toán  │
│      cho đơn hàng này không?           │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │ Mã đơn hàng: #123                 │ │
│  │ Tổng tiền:   475,000 ₫           │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ℹ️ Lưu ý: Sau khi nhấn "Tiến hành    │
│     thanh toán", bạn sẽ được chuyển... │
│                                         │
│         [❌ Hủy]  [✅ Tiến hành]       │
└─────────────────────────────────────────┘
```

### Color Scheme
- **Header**: Gradient blue (#0d6efd → #0a58ca)
- **Icon background**: Light blue gradient
- **Confirm button**: Gradient green (#198754 → #146c43)
- **Cancel button**: Outline secondary

---

## 💻 Code Implementation

### JavaScript Function

```javascript
function showPaymentConfirmationModal(orderId, tableNumber, totalAmount, paymentUrl) {
    // Update modal content
    document.getElementById('modalOrderId').textContent = orderId;
    document.getElementById('modalTableNumber').textContent = tableNumber;
    document.getElementById('modalTotalAmount').textContent = totalAmount.toLocaleString('vi-VN') + ' ₫';

    // Set up confirm button
    const confirmBtn = document.getElementById('confirmPaymentBtn');
    confirmBtn.onclick = function() {
        window.location.href = paymentUrl;
    };

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('paymentConfirmationModal'));
    modal.show();
}
```

### Usage in Razor View

```cshtml
<!-- Button to trigger modal -->
<button type="button"
        class="btn btn-success"
        onclick="showPaymentConfirmationModal(
            @Model.OrderId, 
            '@tables', 
            @Model.TotalAmount, 
            '@Url.Action("Payment", "CashierPaymentFlow", new { id = Model.OrderId })')">
    <i class="fa-solid fa-credit-card me-2"></i>
    Xử lý thanh toán
</button>

<!-- Include modal partial -->
@await Html.PartialAsync("~/Views/CashierFlow/Modals/_PaymentConfirmationModal.cshtml")
```

---

## ✅ Test Cases

### Test 1: Modal hiển thị đúng thông tin
**Steps:**
1. Vào trang Order Selection hoặc Order Detail
2. Click nút "Xử lý thanh toán" trên order bất kỳ
3. Modal hiển thị

**Expected:**
- ✅ Số bàn hiển thị đúng (vd: T01)
- ✅ Mã order hiển thị đúng
- ✅ Tổng tiền format đúng (có dấu phẩy)
- ✅ Modal có animation fade in

### Test 2: Nút "Tiến hành thanh toán" hoạt động
**Steps:**
1. Mở modal
2. Click "Tiến hành thanh toán"

**Expected:**
- ✅ Chuyển đến trang Payment
- ✅ URL đúng: `/CashierFlow/Payment?id={orderId}`

### Test 3: Nút "Hủy" hoạt động
**Steps:**
1. Mở modal
2. Click "Hủy" hoặc click bên ngoài modal

**Expected:**
- ✅ Modal đóng lại
- ✅ Không chuyển trang
- ✅ Vẫn ở màn hình hiện tại

### Test 4: Responsive design
**Steps:**
1. Mở modal trên mobile (Chrome DevTools)
2. Mở modal trên tablet
3. Mở modal trên desktop

**Expected:**
- ✅ Modal hiển thị tốt trên mọi kích thước màn hình
- ✅ Text không bị tràn
- ✅ Button layout hợp lý

---

## 🎨 Customization

### Thay đổi màu sắc

```css
/* Header gradient */
#paymentConfirmationModal .modal-header {
    background: linear-gradient(135deg, #YOUR_COLOR_1 0%, #YOUR_COLOR_2 100%);
}

/* Button gradient */
#paymentConfirmationModal .btn-success {
    background: linear-gradient(135deg, #YOUR_COLOR_3 0%, #YOUR_COLOR_4 100%);
}
```

### Thay đổi icon

```html
<!-- Thay icon utensils thành icon khác -->
<i class="fa-solid fa-receipt fa-3x text-primary"></i>
```

### Thay đổi text

Sửa trong file `_PaymentConfirmationModal.cshtml`:
```cshtml
<h5 class="mb-3">
    Khách hàng tại <strong>bàn <span id="modalTableNumber">@tableNumber</span></strong> 
    đã dùng bữa xong.
</h5>
```

---

## 🐛 Troubleshooting

### Modal không hiển thị
**Nguyên nhân:** Bootstrap JS chưa load
**Giải pháp:** Kiểm tra có import Bootstrap bundle:
```html
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
```

### Function `showPaymentConfirmationModal` not defined
**Nguyên nhân:** Partial view chưa được include
**Giải pháp:** Thêm vào view:
```cshtml
@await Html.PartialAsync("~/Views/CashierFlow/Modals/_PaymentConfirmationModal.cshtml")
```

### Tổng tiền không format đúng
**Nguyên nhân:** `totalAmount` không phải là number
**Giải pháp:** Parse sang number:
```javascript
const total = parseFloat(totalAmount) || 0;
document.getElementById('modalTotalAmount').textContent = total.toLocaleString('vi-VN') + ' ₫';
```

---

## 🚀 Next Steps

### Enhancements (Optional)

1. **Thêm animation**
```css
#paymentConfirmationModal .confirmation-icon {
    animation: pulse 1.5s infinite;
}

@keyframes pulse {
    0%, 100% { transform: scale(1); }
    50% { transform: scale(1.05); }
}
```

2. **Thêm sound effect**
```javascript
const audio = new Audio('/sounds/confirmation.mp3');
audio.play();
```

3. **Thêm keyboard shortcut**
```javascript
// Enter = Confirm, Esc = Cancel
document.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') confirmBtn.click();
    if (e.key === 'Escape') modal.hide();
});
```

4. **Auto-focus vào nút xác nhận**
```javascript
modal._element.addEventListener('shown.bs.modal', () => {
    confirmBtn.focus();
});
```

---

## 📖 References

- Bootstrap 5 Modal: https://getbootstrap.com/docs/5.3/components/modal/
- Font Awesome Icons: https://fontawesome.com/icons
- Payment Flow: `Docs/PAYMENT_FLOW_README.md`

---

**Created:** 2025-11-28  
**Version:** 1.0  
**Status:** ✅ Ready to Use

