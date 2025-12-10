# 🐛 BUGFIX: Sửa layout in hóa đơn (Receipt Print)

## 📋 Mô tả vấn đề

**Hiện tượng:**
Khi in hóa đơn cho đơn hàng có:
- Tiền đặt cọc (deposit)
- Thanh toán kết hợp (Cash + QR)
- Tiền thối lại

Layout in hóa đơn không rõ ràng và gây hiểu nhầm:
- Không phân biệt rõ "Tổng cộng thanh toán" vs "Số tiền khách phải trả"
- Thông tin tiền đặt cọc không được hiển thị
- Breakdown theo phương thức thanh toán không rõ ràng

**Ví dụ:**
```
Đơn hàng:
- Tổng bill: 690,000 ₫
- Đã cọc: -600,000 ₫
- Còn phải trả: 90,000 ₫
- Thanh toán: Cash 45,000 ₫ + QR 45,000 ₫
```

**Layout cũ (không rõ ràng):**
```
TỔNG THANH TOÁN: 90,000 ₫
Tiền mặt: 45,000 ₫
QR: 45,000 ₫
```

---

## ✅ Giải pháp

### Layout mới (rõ ràng hơn):

```
Tạm tính:                    600,000 ₫
VAT 10%:                      60,000 ₫
Phí dịch vụ 5%:              30,000 ₫
Giảm giá:                         0 ₫
─────────────────────────────────────
Tổng cộng thanh toán:       690,000 ₫
Đã đặt cọc:                -600,000 ₫
─────────────────────────────────────
SỐ TIỀN KHÁCH PHẢI TRẢ:      90,000 ₫

Thanh toán bằng:
  • Tiền mặt:                 45,000 ₫
  • QR:                       45,000 ₫
```

### Các trường hợp xử lý:

#### 1. Đơn hàng bình thường (không có cọc)
```
Tạm tính:                    300,000 ₫
VAT 10%:                      30,000 ₫
Phí dịch vụ 5%:              15,000 ₫
─────────────────────────────────────
SỐ TIỀN KHÁCH PHẢI TRẢ:     345,000 ₫

Thanh toán bằng:
  • Tiền mặt:               345,000 ₫
```

#### 2. Đơn hàng có đặt cọc (cọc < tổng bill)
```
Tạm tính:                    600,000 ₫
VAT 10%:                      60,000 ₫
Phí dịch vụ 5%:              30,000 ₫
─────────────────────────────────────
Tổng cộng thanh toán:       690,000 ₫
Đã đặt cọc:                -600,000 ₫
─────────────────────────────────────
SỐ TIỀN KHÁCH PHẢI TRẢ:      90,000 ₫

Thanh toán bằng:
  • Tiền mặt:                 45,000 ₫
  • QR:                       45,000 ₫
```

#### 3. Đơn hàng có cọc lớn hơn tổng bill (phải trả lại)
```
Tạm tính:                    300,000 ₫
VAT 10%:                      30,000 ₫
Phí dịch vụ 5%:              15,000 ₫
─────────────────────────────────────
Tổng cộng thanh toán:       345,000 ₫
Đã đặt cọc:                -400,000 ₫
Tiền cần trả lại cho khách: +55,000 ₫
─────────────────────────────────────
SỐ TIỀN KHÁCH PHẢI TRẢ:           0 ₫
```

#### 4. Thanh toán tiền mặt có tiền thối
```
SỐ TIỀN KHÁCH PHẢI TRẢ:     345,000 ₫

Thanh toán bằng:
  • Tiền mặt:               345,000 ₫
    Tiền thối lại:           +5,000 ₫
```

---

## 🔧 Chi tiết thay đổi

### 1. File: `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/receipt.js`

#### Thay đổi 1: Lấy thông tin tiền đặt cọc

**Before:**
```javascript
const total = parseFloat($(this).data('total') || 0);

// Không có xử lý deposit
```

**After:**
```javascript
const total = parseFloat($(this).data('total') || 0);

// ✅ Lấy thông tin tiền đặt cọc từ receiptData
const depositAmount = window.receiptData?.DepositAmount || 0;
const depositPaid = window.receiptData?.DepositPaid || false;
const depositRefundAmount = window.receiptData?.DepositRefundAmount || 0;

// ✅ Tính tổng thanh toán TRƯỚC KHI TRỪ CỌC
const totalBeforeDeposit = subtotal + vat + serviceFee - discount;
```

#### Thay đổi 2: Cập nhật HTML template in hóa đơn

**Before:**
```javascript
<tr style="font-size:20px; font-weight:bold; color:#16a34a;">
    <td>TỔNG THANH TOÁN</td>
    <td style="text-align:right;">${formatCurrency(total)}</td>
</tr>

${paymentBreakdown.map(p => `
    <tr>
        <td>${renderMethod(p.method)}</td>
        <td style="text-align:right;">${formatCurrency(p.amount)}</td>
    </tr>
`).join('')}
```

**After:**
```javascript
<!-- ✅ Hiển thị tiền đặt cọc nếu có -->
${depositPaid && depositAmount > 0 ? `
    <tr style="border-top: 2px solid #ddd;">
        <td style="padding-top:8px;"><strong>Tổng cộng thanh toán</strong></td>
        <td style="text-align:right; padding-top:8px;"><strong>${formatCurrency(totalBeforeDeposit)}</strong></td>
    </tr>
    <tr style="color:#16a34a;">
        <td>Đã đặt cọc</td>
        <td style="text-align:right;">-${formatCurrency(depositAmount)}</td>
    </tr>
` : ''}

<!-- ✅ Hiển thị tiền trả lại nếu cọc > tổng bill -->
${depositRefundAmount > 0 ? `
    <tr style="color:#f59e0b;">
        <td><strong>Tiền cần trả lại cho khách</strong></td>
        <td style="text-align:right;"><strong>+${formatCurrency(depositRefundAmount)}</strong></td>
    </tr>
` : ''}

<tr style="font-size:20px; font-weight:bold; color:#16a34a; border-top: 3px double #16a34a;">
    <td style="padding-top:8px;">SỐ TIỀN KHÁCH PHẢI TRẢ</td>
    <td style="text-align:right; padding-top:8px;">${formatCurrency(total)}</td>
</tr>

<!-- ✅ Breakdown theo phương thức thanh toán -->
${paymentBreakdown.length > 0 ? `
    <tr style="border-top: 1px solid #ddd;">
        <td colspan="2" style="padding-top:8px; font-style:italic; color:#666; font-size:14px;">
            Thanh toán bằng:
        </td>
    </tr>
    ${paymentBreakdown.map(p => `
        <tr>
            <td style="padding-left:20px;">• ${renderMethod(p.method)}</td>
            <td style="text-align:right;">${formatCurrency(p.amount)}</td>
        </tr>
        ${p.method && p.method.toLowerCase() === 'cash' && p.refundAmount > 0
            ? `<tr style="color:#f59e0b;">
                <td style="padding-left:40px; font-size:14px;">Tiền thối lại</td>
                <td style="text-align:right; font-size:14px;">+${formatCurrency(p.refundAmount)}</td>
            </tr>`
            : ''
        }
    `).join('')}
` : ''}
```

**Cải tiến:**
- ✅ Phân tách rõ ràng "Tổng cộng thanh toán" và "Số tiền khách phải trả"
- ✅ Hiển thị tiền đặt cọc (nếu có)
- ✅ Hiển thị tiền trả lại (nếu cọc > tổng bill)
- ✅ Section "Thanh toán bằng:" với indent rõ ràng
- ✅ Hiển thị tiền thối lại cho từng phương thức Cash

---

### 2. File: `Frontend/WebSapaForestForStaff/Views/CashierFlow/Receipt.cshtml`

#### Thay đổi: Cải thiện hiển thị "Số tiền khách phải trả"

**Before:**
```cshtml
<tr class="table-success">
    <td colspan="4" class="fw-bold ps-3 align-middle" style="background:#f0faf9;">
        <span class="d-flex align-items-center gap-2">
            <i class="bi bi-cash-coin text-primary fs-4"></i>
            <span class="fs-5">Số tiền khách hàng phải trả</span>
        </span>
        <small class="text-muted d-block mt-1">
            Tổng cộng thanh toán (hiển thị theo phương thức thanh toán)
        </small>
    </td>
    <td class="text-end fw-bold text-primary align-middle" style="background:#f0faf9; font-size:1.1rem;">
        @foreach (var pay in payments)
        {
            <div class="d-flex justify-content-between">
                <span>@RenderMethodLabel(pay.PaymentMethod)</span>
                <span>@pay.Amount.ToString("N0") ₫</span>
            </div>
        }
    </td>
</tr>
```

**After:**
```cshtml
<tr class="table-success">
    <td colspan="4" class="fw-bold ps-3 align-middle" style="background:#f0faf9;">
        <span class="d-flex align-items-center gap-2">
            <i class="bi bi-cash-coin text-primary fs-4"></i>
            <span class="fs-5">Số tiền khách hàng phải trả</span>
        </span>
        <small class="text-muted d-block mt-1">
            @if (Model.DepositPaid == true && Model.DepositAmount.HasValue && Model.DepositAmount.Value > 0)
            {
                <text>Tổng cộng (@totalBeforeDeposit.ToString("N0") ₫) - Đã cọc (@Model.DepositAmount.Value.ToString("N0") ₫)</text>
            }
            else
            {
                <text>Tổng cộng thanh toán</text>
            }
        </small>
    </td>
    <td class="text-end fw-bold text-primary align-middle" style="background:#f0faf9; font-size:1.8rem;">
        @Model.TotalAmount.ToString("N0") ₫
    </td>
</tr>
@if (payments.Any())
{
    <tr class="table-light">
        <td colspan="5" class="ps-3 pt-3">
            <div class="text-muted small fw-semibold mb-2">
                <i class="bi bi-credit-card me-1"></i>
                Chi tiết thanh toán:
            </div>
            @foreach (var pay in payments)
            {
                <div class="d-flex justify-content-between align-items-center mb-2 ps-3">
                    <span>
                        <i class="bi bi-arrow-right-short text-primary"></i>
                        @RenderMethodLabel(pay.PaymentMethod)
                    </span>
                    <span class="fw-bold">@pay.Amount.ToString("N0") ₫</span>
                </div>
                if (pay.PaymentMethod != null &&
                    pay.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase) &&
                    pay.RefundAmount.HasValue &&
                    pay.RefundAmount.Value > 0)
                {
                    <div class="d-flex justify-content-between align-items-center mb-2 ps-4 text-warning">
                        <span class="small">
                            <i class="bi bi-arrow-return-right"></i>
                            Tiền thối lại
                        </span>
                        <span class="fw-bold small">+@pay.RefundAmount.Value.ToString("N0") ₫</span>
                    </div>
                }
            }
        </td>
    </tr>
}
```

**Cải tiến:**
- ✅ Hiển thị công thức tính "Số tiền khách phải trả" (nếu có cọc)
- ✅ Tách riêng section "Chi tiết thanh toán" với indent và icon rõ ràng
- ✅ Hiển thị tiền thối lại với sub-indent
- ✅ Tăng font size cho số tiền chính (1.8rem)

---

## 📊 So sánh Before/After

### Before: Layout không rõ ràng

**Màn hình Receipt:**
```
┌─────────────────────────────────────┐
│ Số tiền khách hàng phải trả         │
│                           90,000 ₫  │
│ Tiền mặt        45,000 ₫           │
│ QR              45,000 ₫           │
└─────────────────────────────────────┘
```

**Hóa đơn in:**
```
TỔNG THANH TOÁN           90,000 ₫
Tiền mặt                  45,000 ₫
QR                        45,000 ₫
```

❌ **Vấn đề:**
- Không rõ 90,000 ₫ là tổng bill hay số tiền sau khi trừ cọc
- Tiền đặt cọc 600,000 ₫ đã đóng ở đâu?
- Tổng bill ban đầu 690,000 ₫ không được hiển thị

---

### After: Layout rõ ràng

**Màn hình Receipt:**
```
┌─────────────────────────────────────┐
│ Tổng cộng thanh toán    690,000 ₫  │
│ Đã đặt cọc             -600,000 ₫  │
├─────────────────────────────────────┤
│ Số tiền khách hàng phải trả         │
│ (690,000 - 600,000)                 │
│                           90,000 ₫  │
├─────────────────────────────────────┤
│ Chi tiết thanh toán:                │
│   → Tiền mặt            45,000 ₫   │
│   → QR                  45,000 ₫   │
└─────────────────────────────────────┘
```

**Hóa đơn in:**
```
Tạm tính                 600,000 ₫
VAT 10%                   60,000 ₫
Phí dịch vụ 5%           30,000 ₫
─────────────────────────────────
Tổng cộng thanh toán    690,000 ₫
Đã đặt cọc             -600,000 ₫
─────────────────────────────────
SỐ TIỀN KHÁCH PHẢI TRẢ   90,000 ₫

Thanh toán bằng:
  • Tiền mặt              45,000 ₫
  • QR                    45,000 ₫
```

✅ **Cải tiến:**
- Hiển thị đầy đủ "Tổng cộng thanh toán" (690,000 ₫)
- Hiển thị "Đã đặt cọc" (-600,000 ₫)
- Rõ ràng "Số tiền khách phải trả" = 690,000 - 600,000 = 90,000 ₫
- Breakdown theo phương thức thanh toán

---

## 🧪 Test Cases

### Test Case 1: Đơn hàng bình thường (không có cọc)

**Input:**
- Subtotal: 300,000 ₫
- VAT: 30,000 ₫
- Service Fee: 15,000 ₫
- Discount: 0 ₫
- Deposit: 0 ₫
- Payment: Cash 345,000 ₫

**Expected Receipt:**
```
Tạm tính                 300,000 ₫
VAT 10%                   30,000 ₫
Phí dịch vụ 5%           15,000 ₫
─────────────────────────────────
SỐ TIỀN KHÁCH PHẢI TRẢ  345,000 ₫

Thanh toán bằng:
  • Tiền mặt             345,000 ₫
```

---

### Test Case 2: Đơn hàng có đặt cọc (cọc < tổng)

**Input:**
- Subtotal: 600,000 ₫
- VAT: 60,000 ₫
- Service Fee: 30,000 ₫
- Discount: 0 ₫
- Deposit: 600,000 ₫
- Total Before Deposit: 690,000 ₫
- Total After Deposit: 90,000 ₫
- Payment: Cash 45,000 ₫ + QR 45,000 ₫

**Expected Receipt:**
```
Tạm tính                 600,000 ₫
VAT 10%                   60,000 ₫
Phí dịch vụ 5%           30,000 ₫
─────────────────────────────────
Tổng cộng thanh toán    690,000 ₫
Đã đặt cọc             -600,000 ₫
─────────────────────────────────
SỐ TIỀN KHÁCH PHẢI TRẢ   90,000 ₫

Thanh toán bằng:
  • Tiền mặt              45,000 ₫
  • QR                    45,000 ₫
```

---

### Test Case 3: Đơn hàng có cọc lớn hơn tổng (phải trả lại)

**Input:**
- Subtotal: 300,000 ₫
- VAT: 30,000 ₫
- Service Fee: 15,000 ₫
- Discount: 0 ₫
- Deposit: 400,000 ₫
- Total Before Deposit: 345,000 ₫
- Total After Deposit: 0 ₫ (đã thanh toán đủ bằng cọc)
- Deposit Refund: 55,000 ₫

**Expected Receipt:**
```
Tạm tính                 300,000 ₫
VAT 10%                   30,000 ₫
Phí dịch vụ 5%           15,000 ₫
─────────────────────────────────
Tổng cộng thanh toán    345,000 ₫
Đã đặt cọc             -400,000 ₫
Tiền cần trả lại        +55,000 ₫
─────────────────────────────────
SỐ TIỀN KHÁCH PHẢI TRẢ        0 ₫
```

---

### Test Case 4: Thanh toán tiền mặt có tiền thối

**Input:**
- Total: 345,000 ₫
- Payment: Cash 350,000 ₫
- Refund: 5,000 ₫

**Expected Receipt:**
```
SỐ TIỀN KHÁCH PHẢI TRẢ  345,000 ₫

Thanh toán bằng:
  • Tiền mặt             345,000 ₫
    Tiền thối lại         +5,000 ₫
```

---

## 📝 Notes

### Dữ liệu cần thiết từ `window.receiptData`

File `receipt.js` cần các fields sau từ `window.receiptData`:

```javascript
{
  OrderId: number,
  OrderCode: string,
  CustomerName: string,
  CustomerPhone: string,
  CreatedAt: string,
  PaidAt: string,
  StaffName: string,
  PaymentMethod: string,
  Subtotal: number,
  VatAmount: number,
  ServiceFee: number,
  DiscountAmount: number,
  TotalAmount: number,
  
  // ✅ Deposit fields (REQUIRED)
  DepositAmount: number,
  DepositPaid: boolean,
  DepositRefundAmount: number,
  
  // ✅ Transactions (REQUIRED for breakdown)
  Transactions: [
    {
      PaymentMethod: string,
      Amount: number,
      AmountReceived: number,
      RefundAmount: number,
      Status: string
    }
  ],
  
  // ✅ Items
  Items: [
    {
      Name: string,
      IsCombo: boolean,
      QuantityUsed: number,
      UnitPrice: number,
      TotalPrice: number
    }
  ]
}
```

### Visual Hierarchy

**Priority:**
1. **SỐ TIỀN KHÁCH PHẢI TRẢ** (font-size: 20px, bold, màu xanh)
2. Tổng cộng thanh toán (bold, border-top)
3. Đã đặt cọc (màu xanh)
4. Thanh toán bằng: (section header, italic, màu xám)
5. Chi tiết phương thức (indent với bullet)
6. Tiền thối lại (sub-indent, màu vàng)

---

## 📅 Change Log

**Date:** 2025-12-10
**Author:** AI Assistant
**Status:** ✅ Fixed

### Files Changed:
1. `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/receipt.js`
   - Added deposit amount, depositPaid, depositRefundAmount variables
   - Added totalBeforeDeposit calculation
   - Updated HTML template with clear sections for deposit and payment breakdown

2. `Frontend/WebSapaForestForStaff/Views/CashierFlow/Receipt.cshtml`
   - Updated "Số tiền khách hàng phải trả" display with formula
   - Added separate "Chi tiết thanh toán" section
   - Improved visual hierarchy with icons and indentation

---

## 🔗 Related Documents

- [CASHIER_PAYMENT_COMPLETE_WORKFLOW.md](./CASHIER_PAYMENT_COMPLETE_WORKFLOW.md)
- [PAYMENT_FRONTEND_BACKEND_INTEGRATION.md](./PAYMENT_FRONTEND_BACKEND_INTEGRATION.md)

