# 🔄 LUỒNG THANH TOÁN HOÀN CHỈNH
## Từ Waiter xác nhận đơn → Thu ngân thanh toán → In hóa đơn

---

## 📋 TỔNG QUAN LUỒNG

```
1. Waiter xác nhận đơn hàng
   └── OrderDetail.cshtml → handleWaiterConfirmPayment()
   └── DashboardTableController.ConfirmOrder()
   └── PaymentService.ConfirmOrderAsync()
   └── Order.Status: "WaitingConfirmation" → "Confirmed" ✅

2. Thu ngân chọn đơn đã xác nhận
   └── OrderDetail.cshtml → handleCreatePaymentOrder()
   └── Redirect → CashierFlow/Payment.cshtml

3. Thu ngân chọn phương thức thanh toán
   └── Payment.cshtml → Modal (Cash/QR/Combined)
   └── cash-payment.js / qr-payment.js / combined-payment.js

4. Xác nhận thanh toán
   └── CashierPaymentFlowController.ProcessCashPayment()
   └── PaymentService.ProcessCashPaymentAsync()
   └── Order.Status: "Confirmed" → "Paid" ✅

5. In hóa đơn
   └── Receipt.cshtml → DownloadReceipt()
   └── ReceiptService.GenerateReceiptPdfAsync()
   └── PDF file generated ✅
```

---

## 🔍 CHI TIẾT TỪNG BƯỚC

---

### **BƯỚC 1: WAITER XÁC NHẬN ĐƠN HÀNG**

#### **Frontend: OrderDetail.cshtml**

**File:** `Frontend/WebSapaForestForStaff/Views/DashboardTable/OrderDetail.cshtml`

**Function:** `handleWaiterConfirmPayment()` (dòng 892-1015)

**Điều kiện hiển thị nút:**
- Chỉ hiển thị cho Waiter (`userPositionId == 1`)
- Chỉ hiển thị khi có ít nhất 1 món đang nấu/đã xong (`hasCookingOrReadyItems`)
- Nếu đã xác nhận (`orderStatus == "Confirmed"`), hiển thị nút disabled

**Luồng xử lý:**
```javascript
async function handleWaiterConfirmPayment() {
    // 1. Validate: Kiểm tra có món trong đơn
    const visibleItems = cartItems.filter(x => !x.isDeleted);
    if (visibleItems.length === 0) {
        alert('Chưa có món nào trong đơn hàng...');
        return;
    }

    // 2. Validate: Kiểm tra có thay đổi chưa lưu
    const hasUnsavedChanges = cartItems.some(x => x.isNew || x.isDirty);
    if (hasUnsavedChanges) {
        alert('Bạn có thay đổi chưa lưu...');
        return;
    }

    // 3. Hiển thị popup xác nhận với thông tin:
    //    - Bàn, Khách hàng
    //    - Số lượng món đã gọi
    //    - Đã phục vụ & đang nấu
    //    - Chưa nấu
    //    - Món đã hủy
    //    - Tạm tính
    const confirmed = await showConfirmPopup(...);

    // 4. Gọi API xác nhận
    const response = await fetch('/DashboardTable/ConfirmOrder', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({
            orderId: activeOrderId,
            items: visibleItems.map(item => ({
                orderDetailId: item.id,
                quantityUsed: item.quantity,
                isRemoved: false
            })),
            notes: null
        })
    });

    // 5. Reload trang sau khi thành công
    setTimeout(() => window.location.reload(), 1500);
}
```

**Route:** `POST /DashboardTable/ConfirmOrder`

---

#### **Frontend Controller: DashboardTableController.cs**

**File:** `Frontend/WebSapaForestForStaff/Controllers/DashboardTableController.cs`

**Action:** `ConfirmOrder()` (dòng 241-267)

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmOrder([FromBody] ConfirmOrderRequest request)
{
    // 1. Validate request
    if (!ModelState.IsValid) {
        return BadRequest(new { message = "Dữ liệu không hợp lệ" });
    }

    // 2. Gọi PaymentApiService
    var result = await _paymentApiService.ConfirmCustomerOrderAsync(request);
    
    // 3. Trả về kết quả
    if (result.Success) {
        return Ok(new { message = result.Message ?? "Xác nhận đơn hàng thành công" });
    } else {
        return BadRequest(new { message = result.Message ?? "Không thể xác nhận đơn hàng" });
    }
}
```

**Service:** `PaymentApiService.ConfirmCustomerOrderAsync()` → Gọi API Backend

---

#### **Backend API: PaymentController.cs**

**File:** `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs`

**Endpoint:** `PUT /api/payment/orders/{orderId}/confirm` (dòng 97-131)

```csharp
[HttpPut("orders/{orderId}/confirm")]
public async Task<IActionResult> ConfirmOrder(int orderId, [FromBody] CustomerConfirmRequestDto request, CancellationToken ct = default)
{
    // 1. Validate request
    if (request == null) {
        return BadRequest(new { message = "Dữ liệu không hợp lệ" });
    }

    // 2. Lấy userId từ Claims
    var userId = GetUserIdFromClaims();
    if (!userId.HasValue) {
        return Unauthorized(new { message = "Không thể xác định người dùng" });
    }

    // 3. Gọi PaymentService
    var result = await _paymentService.ConfirmOrderAsync(request, userId.Value, ct);
    return Ok(result);
}
```

---

#### **Backend Service: PaymentService.cs**

**File:** `Backend/BusinessAccessLayer/Services/PaymentService.cs`

**Method:** `ConfirmOrderAsync()` (dòng 556-650)

**Xử lý chính:**

1. **Lấy đơn hàng với tất cả OrderDetails:**
   ```csharp
   var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
   ```

2. **Validate:**
   - Đơn hàng tồn tại
   - Đơn hàng có món

3. **Xác nhận từng món:**
   ```csharp
   foreach (var confirmed in request.Items) {
       var detail = order.OrderDetails.FirstOrDefault(d => d.OrderDetailId == confirmed.OrderDetailId);
       
       // Cập nhật QuantityUsed
       detail.QuantityUsed = confirmed.QuantityUsed;
       
       // Cập nhật Status thành "Done" nếu món đã sẵn sàng
       if (billableStatuses.Contains(detail.Status)) {
           detail.Status = "Done";
       }
   }
   ```

4. **Tính toán tổng tiền:**
   ```csharp
   decimal subtotal = 0;
   foreach (var od in order.OrderDetails) {
       // Chỉ tính món có status billable (Cooking/Done/Ready)
       if (billableStatuses.Contains(od.Status)) {
           int billableQuantity = od.QuantityUsed ?? od.Quantity;
           subtotal += od.UnitPrice * billableQuantity;
       }
   }
   
   // Làm tròn Subtotal lên mệnh giá 1000
   subtotal = RoundUpToThousand(subtotal);
   
   // Tính VAT (10%)
   var vatAmount = RoundUpToThousand(subtotal * 0.1m);
   
   // Tính phí dịch vụ (5%)
   var serviceFee = RoundUpToThousand(subtotal * 0.05m);
   
   // Lấy discount từ payment (nếu có)
   var discountAmount = RoundUpToThousand(
       order.Payments?.OrderByDescending(p => p.PaymentDate)
           .FirstOrDefault()?.DiscountAmount ?? 0
   );
   
   // Tổng cộng
   var totalAmount = subtotal + vatAmount + serviceFee - discountAmount;
   totalAmount = RoundUpToThousand(totalAmount);
   ```

5. **Cập nhật Order:**
   ```csharp
   order.Status = "Confirmed";
   order.ConfirmedAt = DateTime.UtcNow;
   order.ConfirmedByStaffId = userId;
   order.TotalAmount = totalAmount;
   ```

6. **Ghi OrderHistory:**
   ```csharp
   await _auditLogService.LogEventAsync(
       eventType: "Order Confirmation",
       entityType: "Order",
       entityId: order.OrderId,
       description: $"Confirmed by staff. Total amount: {totalAmount} VND",
       userId: userId,
       ct: ct
   );
   ```

7. **Save changes:**
   ```csharp
   await _unitOfWork.SaveChangesAsync();
   ```

**Kết quả:** `Order.Status = "Confirmed"` ✅

---

### **BƯỚC 2: THU NGÂN CHỌN ĐƠN ĐÃ XÁC NHẬN**

#### **Frontend: OrderDetail.cshtml**

**File:** `Frontend/WebSapaForestForStaff/Views/DashboardTable/OrderDetail.cshtml`

**Function:** `handleCreatePaymentOrder()` (dòng 1022-1098)

**Điều kiện hiển thị nút:**
- Chỉ hiển thị cho Cashier (`userPositionId == 2`)
- Chỉ hiển thị khi `orderStatus == "Confirmed"`
- Nếu chưa xác nhận, hiển thị nút disabled với tooltip

**Luồng xử lý:**
```javascript
async function handleCreatePaymentOrder() {
    // 1. Validate: Kiểm tra đơn đã được xác nhận
    const orderStatus = window.__ORDER_STATUS__ || '';
    if (!orderStatus || orderStatus.toLowerCase() !== 'confirmed') {
        alert('⚠️ Đơn hàng chưa được xác nhận!...');
        return;
    }

    // 2. Validate: Kiểm tra có món trong đơn
    const visibleItems = cartItems.filter(x => !x.isDeleted);
    if (visibleItems.length === 0) {
        alert('Chưa có món nào trong đơn hàng...');
        return;
    }

    // 3. Hiển thị popup xác nhận
    const confirmed = await showConfirmPopup(
        "💳 Thanh toán bàn",
        // Hiển thị: Bàn, Khách hàng, Trạng thái, Số lượng món, Tạm tính
        ...
    );

    // 4. Redirect tới màn hình Payment
    if (confirmed) {
        window.location.href = `/cashier-flow/payment/${activeOrderId}`;
    }
}
```

**Route:** `GET /cashier-flow/payment/{orderId}`

---

#### **Frontend Controller: CashierPaymentFlowController.cs**

**File:** `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs`

**Action:** `Payment()` (dòng 151-170)

```csharp
[HttpGet("payment/{id}")]
public async Task<IActionResult> Payment(int id)
{
    // 1. Xóa TempData cũ
    TempData.Remove("ErrorMessage");
    TempData.Remove("SuccessMessage");

    // 2. Lấy thông tin đơn hàng
    var order = await _paymentApiService.GetOrderDetailAsync(id);
    if (order == null) return NotFound();
    
    // 3. Load danh sách voucher phù hợp
    var availableVouchers = await GetAvailableVouchersAsync(order.Subtotal);
    ViewData["AvailableVouchers"] = availableVouchers;
    
    // 4. Trả về View Payment
    return View("~/Views/CashierFlow/Payment.cshtml", order);
}
```

**View:** `Frontend/WebSapaForestForStaff/Views/CashierFlow/Payment.cshtml`

---

### **BƯỚC 3: THU NGÂN CHỌN PHƯƠNG THỨC THANH TOÁN**

#### **Frontend: Payment.cshtml**

**File:** `Frontend/WebSapaForestForStaff/Views/CashierFlow/Payment.cshtml`

**Các phương thức thanh toán:**

1. **Tiền mặt (Cash):**
   - Button: `onclick="openCashPaymentModal()"`
   - Modal: `_CashPaymentModal.cshtml`
   - JavaScript: `cash-payment.js`

2. **QR / Ví điện tử:**
   - Button: `onclick="openQrPaymentModal()"`
   - Modal: `_QRPaymentModal.cshtml`
   - JavaScript: `qr-payment.js`

3. **Thanh toán kết hợp (Combined):**
   - Button: `onclick="openCombinedPaymentModal()"`
   - Modal: `_CombinedPaymentModal.cshtml`
   - JavaScript: `combined-payment.js`

4. **Ưu đãi / Khuyến mãi:**
   - Button: `onclick="openPromotionModal()"`
   - Modal: `_PromotionModal.cshtml`
   - JavaScript: `promotion-voucher.js`

---

#### **JavaScript: cash-payment.js**

**File:** `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/cash-payment.js`

**Function:** `confirmCashPayment()` (dòng 142-232)

**Luồng xử lý:**

1. **Validate:**
   ```javascript
   const total = parseFloat(window.PaymentCore.orderContext.Total ?? 0);
   const received = parseFloat(document.getElementById('amountReceived').value ?? 0);
   
   if (!received || received < total) {
       window.PaymentCore.showToast('Vui lòng nhập số tiền hợp lệ.', 'warning');
       return;
   }
   
   if (received > total && !document.getElementById('refundConfirmed').checked) {
       window.PaymentCore.showToast('Vui lòng xác nhận đã trả lại tiền thối...', 'warning');
       return;
   }
   ```

2. **Submit form:**
   ```javascript
   document.getElementById('cashPaymentOrderId').value = orderId;
   document.getElementById('cashPaymentAmountReceived').value = received;
   document.getElementById('cashPaymentNotes').value = document.getElementById('paymentNotes')?.value || '';
   
   // Set processing state
   sessionStorage.setItem('cashPaymentProcessing', 'true');
   sessionStorage.setItem('cashPaymentOrderId', orderId);
   
   // Show loading modal
   this.showCashPaymentLoadingModal();
   
   // Submit form
   document.getElementById('cashPaymentForm').submit();
   ```

**Form:** `cashPaymentForm` (dòng 489-498 trong Payment.cshtml)
```html
<form id="cashPaymentForm"
      asp-action="ProcessCashPayment"
      asp-controller="CashierPaymentFlow"
      method="post"
      class="d-none">
    @Html.AntiForgeryToken()
    <input type="hidden" name="OrderId" id="cashPaymentOrderId" />
    <input type="hidden" name="AmountReceived" id="cashPaymentAmountReceived" />
    <input type="hidden" name="Notes" id="cashPaymentNotes" />
</form>
```

---

#### **JavaScript: qr-payment.js**

**File:** `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/qr-payment.js`

**Function:** `confirmQRPayment()` (dòng 88-117)

**Luồng xử lý:**

1. **Generate QR Code:**
   ```javascript
   const amount = window.PaymentCore.orderContext.Total;
   const orderCode = window.PaymentCore.orderContext.OrderCode;
   const bank = "MB";
   const account = "0397604824";
   const addInfo = `RMS#${orderCode}`;
   
   const qrUrl = window.PaymentCore.generateVietQrUrl(bank, account, amount, addInfo);
   ```

2. **Submit form:**
   ```javascript
   document.getElementById('qrConfirmOrderId').value = window.PaymentCore.orderContext.OrderId;
   document.getElementById('qrConfirmNotes').value = 'Thu ngân xác nhận đã nhận tiền qua QR';
   
   sessionStorage.setItem('qrPaymentProcessing', 'true');
   sessionStorage.setItem('qrPaymentOrderId', window.PaymentCore.orderContext.OrderId);
   
   this.showQrPaymentLoadingModal();
   document.getElementById('qrConfirmForm').submit();
   ```

**Form:** `qrConfirmForm` (dòng 500-508 trong Payment.cshtml)

---

#### **JavaScript: combined-payment.js**

**File:** `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/combined-payment.js`

**Function:** `confirmCombinedPayment()` (dòng 238-283)

**Luồng xử lý:**

1. **Validate:**
   ```javascript
   const cashAmount = parseFloat(document.getElementById('cashAmount').value || 0);
   const cashReceived = parseFloat(document.getElementById('cashReceived').value || 0) || null;
   const total = parseFloat(window.PaymentCore.orderContext.Total || 0);
   const qrAmount = total - cashAmount;
   
   if (cashAmount <= 0 || qrAmount <= 0) {
       window.PaymentCore.showToast('Vui lòng nhập số tiền hợp lệ cho cả hai phần.', 'warning');
       return;
   }
   ```

2. **Submit form:**
   ```javascript
   document.getElementById('combinedPaymentOrderId').value = window.PaymentCore.orderContext.OrderId;
   document.getElementById('combinedPaymentCashAmount').value = cashAmount;
   document.getElementById('combinedPaymentCashReceived').value = cashReceived ?? '';
   document.getElementById('combinedPaymentQrAmount').value = qrAmount;
   document.getElementById('combinedPaymentNotes').value = notes;
   
   sessionStorage.setItem('combinedPaymentProcessing', 'true');
   sessionStorage.setItem('combinedPaymentOrderId', window.PaymentCore.orderContext.OrderId);
   
   this.showCombinedPaymentLoadingModal();
   document.getElementById('combinedPaymentForm').submit();
   ```

**Form:** `combinedPaymentForm` (dòng 540-551 trong Payment.cshtml)

---

### **BƯỚC 4: XÁC NHẬN THANH TOÁN**

#### **Frontend Controller: CashierPaymentFlowController.cs**

**File:** `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs`

#### **4.1. Thanh toán tiền mặt**

**Action:** `ProcessCashPayment()` (dòng 390-492)

```csharp
[HttpPost("payment/cash")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ProcessCashPayment(CashPaymentRequest request)
{
    // 1. Validate request
    if (request == null || request.OrderId <= 0) {
        TempData["ErrorMessage"] = "Dữ liệu thanh toán không hợp lệ.";
        return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
    }

    try {
        // 2. Lấy token
        var token = GetToken();
        if (string.IsNullOrEmpty(token)) {
            TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn...";
            return RedirectToAction("Login", "Auth");
        }

        // 3. Gọi API Backend
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var apiUrl = $"{GetApiBaseUrl()}/Payment/cash";
        var response = await _httpClient.PostAsJsonAsync(apiUrl, new {
            orderId = request.OrderId,
            amountReceived = request.AmountReceived,
            notes = request.Notes
        });

        // 4. Xử lý response
        if (!response.IsSuccessStatusCode) {
            var errorMessage = await ReadApiMessageAsync(response);
            TempData["ErrorMessage"] = errorMessage ?? "Không thể xử lý thanh toán...";
            return RedirectToAction(nameof(Payment), new { id = request.OrderId });
        }

        // 5. Parse refund amount (nếu có)
        var transaction = await response.Content.ReadFromJsonAsync<JsonElement>();
        decimal? refundAmount = null;
        if (transaction.TryGetProperty("refundAmount", out var refund)) {
            refundAmount = refund.GetDecimal();
        }

        // 6. Set success message
        if (refundAmount.HasValue && refundAmount.Value > 0) {
            TempData["SuccessMessage"] = $"✅ Thanh toán thành công! Đã trả lại tiền thừa: {refundAmount.Value:N0} ₫";
        } else {
            TempData["SuccessMessage"] = "✅ Thanh toán thành công!";
        }

        // 7. Redirect đến Receipt
        return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
    }
    catch (Exception ex) {
        TempData["ErrorMessage"] = $"Lỗi khi xử lý thanh toán: {ex.Message}";
        return RedirectToAction(nameof(Payment), new { id = request.OrderId });
    }
}
```

**Route:** `POST /cashier-flow/payment/cash`

**API Backend:** `POST /api/Payment/cash`

---

#### **4.2. Xác nhận thanh toán QR**

**Action:** `ConfirmQrPayment()` (dòng 175-244)

```csharp
[HttpPost("payment/confirm-qr")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmQrPayment([FromForm] ConfirmQrPaymentRequest request)
{
    // 1. Validate request
    if (request == null || request.OrderId <= 0) {
        TempData["ErrorMessage"] = "Dữ liệu không hợp lệ";
        return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
    }

    try {
        // 2. Lấy thông tin đơn hàng
        var order = await _paymentApiService.GetOrderDetailAsync(request.OrderId);
        if (order == null) {
            TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng {request.OrderId}";
            return RedirectToAction(nameof(OrderSelection));
        }

        // 3. Validate TotalAmount
        if (order.TotalAmount <= 0) {
            TempData["ErrorMessage"] = "Tổng tiền đơn hàng không hợp lệ...";
            return RedirectToAction(nameof(Payment), new { id = request.OrderId });
        }

        // 4. Tạo PaymentConfirmRequest
        var confirmRequest = new PaymentConfirmRequest {
            OrderId = request.OrderId,
            PaymentMethod = "QRBankTransfer",
            Amount = order.TotalAmount,
            Notes = request.Notes ?? "Thu ngân xác nhận đã nhận tiền qua QR",
            SessionId = string.Empty,
            CashGiven = null
        };

        // 5. Gọi API xác nhận thanh toán
        var result = await _paymentApiService.ConfirmPaymentAsync(confirmRequest);
        
        if (!result.Success) {
            TempData["ErrorMessage"] = result.Message ?? "Xác nhận thanh toán thất bại";
            return RedirectToAction(nameof(Payment), new { id = request.OrderId });
        }

        // 6. Redirect đến Receipt
        TempData["SuccessMessage"] = "✅ Đã xác nhận thanh toán QR thành công!";
        return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
    }
    catch (Exception ex) {
        TempData["ErrorMessage"] = $"Lỗi khi xác nhận thanh toán: {ex.Message}";
        return RedirectToAction(nameof(Payment), new { id = request.OrderId });
    }
}
```

**Route:** `POST /cashier-flow/payment/confirm-qr`

**API Backend:** `POST /api/payment/payments/confirm`

---

#### **4.3. Thanh toán kết hợp**

**Action:** `ProcessCombinedPayment()` (dòng 497-524)

```csharp
[HttpPost("payment/combined")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ProcessCombinedPayment(CombinedPaymentRequest request)
{
    // 1. Validate request
    if (request == null || request.OrderId <= 0) {
        TempData["ErrorMessage"] = "Dữ liệu thanh toán không hợp lệ.";
        return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
    }

    try {
        // 2. Gọi PaymentApiService
        var result = await _paymentApiService.ProcessCombinedPaymentAsync(request);
        
        if (!result.Success) {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Payment), new { id = request.OrderId });
        }

        // 3. Redirect đến Receipt
        TempData["SuccessMessage"] = result.Message ?? "✅ Thanh toán kết hợp thành công!";
        return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
    }
    catch (Exception ex) {
        TempData["ErrorMessage"] = $"Lỗi khi xử lý thanh toán kết hợp: {ex.Message}";
        return RedirectToAction(nameof(Payment), new { id = request.OrderId });
    }
}
```

**Route:** `POST /cashier-flow/payment/combined`

**API Backend:** `POST /api/Payment/combined`

---

#### **Backend API: PaymentController.cs**

**File:** `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs`

#### **4.1. Thanh toán tiền mặt**

**Endpoint:** `POST /api/Payment/cash` (dòng 465-565)

```csharp
[HttpPost("cash")]
public async Task<IActionResult> ProcessCashPayment([FromBody] CashPaymentRequestDto request, CancellationToken ct = default)
{
    // 1. Validate request
    if (request == null || request.OrderId <= 0) {
        return BadRequest(new { message = "Dữ liệu thanh toán không hợp lệ" });
    }

    // 2. Lấy userId từ Claims
    var userId = GetUserIdFromClaims();
    if (!userId.HasValue) {
        return Unauthorized(new { message = "Không thể xác định người dùng" });
    }

    // 3. Gọi PaymentService
    var transaction = await _paymentService.ProcessCashPaymentAsync(request, userId.Value, ct);
    
    // 4. Trả về transaction với refundAmount
    return Ok(new {
        transactionId = transaction.TransactionId,
        transactionCode = transaction.TransactionCode,
        amount = transaction.Amount,
        amountReceived = transaction.AmountReceived,
        refundAmount = transaction.RefundAmount,
        status = transaction.Status
    });
}
```

---

#### **Backend Service: PaymentService.cs**

**File:** `Backend/BusinessAccessLayer/Services/PaymentService.cs`

**Method:** `ProcessCashPaymentAsync()` (dòng 1412-2031)

**Xử lý chính:**

1. **Lấy đơn hàng:**
   ```csharp
   var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
   if (order == null) {
       throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
   }
   ```

2. **Validate đơn hàng đã được xác nhận:**
   ```csharp
   if (order.Status != "Confirmed") {
       throw new InvalidOperationException($"Đơn hàng chưa được xác nhận. Trạng thái hiện tại: {order.Status}");
   }
   ```

3. **Tính toán tổng tiền (giống như ConfirmOrder):**
   ```csharp
   decimal subtotal = 0;
   // ... tính subtotal từ OrderDetails có status billable
   subtotal = RoundUpToThousand(subtotal);
   var vatAmount = RoundUpToThousand(subtotal * 0.1m);
   var serviceFee = RoundUpToThousand(subtotal * 0.05m);
   var discountAmount = RoundUpToThousand(
       order.Payments?.OrderByDescending(p => p.PaymentDate)
           .FirstOrDefault()?.DiscountAmount ?? 0
   );
   var totalAmount = subtotal + vatAmount + serviceFee - discountAmount;
   totalAmount = RoundUpToThousand(totalAmount);
   ```

4. **Tạo Transaction:**
   ```csharp
   var transaction = new Transaction {
       OrderId = request.OrderId,
       TransactionCode = GenerateTransactionCode(),
       PaymentMethod = "Cash",
       Amount = totalAmount,
       AmountReceived = request.AmountReceived,
       RefundAmount = request.AmountReceived > totalAmount 
           ? request.AmountReceived - totalAmount 
           : 0,
       Status = "Paid",
       CreatedAt = DateTime.UtcNow,
       CompletedAt = DateTime.UtcNow,
       IsManualConfirmed = true,
       ConfirmedByUserId = userId,
       Notes = request.Notes
   };
   ```

5. **Cập nhật Order:**
   ```csharp
   order.Status = "Paid";
   order.TotalAmount = totalAmount;
   ```

6. **Ghi OrderHistory:**
   ```csharp
   await _auditLogService.LogEventAsync(
       eventType: "PaymentCompleted",
       entityType: "Order",
       entityId: order.OrderId,
       description: $"Cash payment completed. Amount: {totalAmount} VND",
       userId: userId,
       ct: ct
   );
   ```

7. **Trigger post-payment actions:**
   ```csharp
   await TriggerPostPaymentActionsAsync(request.OrderId, transaction.TransactionId, ct);
   // - VIP Status Update
   // - Loyalty Points +1
   // - Inventory Deduction
   // - Revenue Recording
   ```

8. **Giải phóng bàn:**
   ```csharp
   await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);
   ```

9. **Save changes:**
   ```csharp
   await _unitOfWork.SaveChangesAsync();
   ```

**Kết quả:** `Order.Status = "Paid"` ✅

---

### **BƯỚC 5: IN HÓA ĐƠN**

#### **Frontend Controller: CashierPaymentFlowController.cs**

**File:** `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs`

**Action:** `Receipt()` (dòng 589-659)

```csharp
[HttpGet("receipt/{orderId}")]
public async Task<IActionResult> Receipt(int orderId)
{
    try {
        // 1. Lấy thông tin đơn hàng
        var order = await _paymentApiService.GetOrderDetailAsync(orderId);
        
        // 2. Retry logic nếu order chưa có (có thể do database chưa commit)
        if (order == null) {
            await Task.Delay(500);
            order = await _paymentApiService.GetOrderDetailAsync(orderId);
            
            if (order == null) {
                TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng với ID: {orderId}";
                return RedirectToAction(nameof(OrderSelection));
            }
        }

        // 3. Kiểm tra đơn hàng đã được thanh toán
        if (string.IsNullOrEmpty(order.Status) || 
            (!order.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) &&
             !order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) &&
             !order.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))) {
            
            // Retry sau 500ms
            await Task.Delay(500);
            order = await _paymentApiService.GetOrderDetailAsync(orderId);
            
            // Nếu vẫn chưa Paid nhưng có SuccessMessage, cho phép xem
            if (order != null && 
                (order.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                 order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                 order.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))) {
                return View("~/Views/CashierFlow/Receipt.cshtml", order);
            }
            
            if (TempData.ContainsKey("SuccessMessage")) {
                return View("~/Views/CashierFlow/Receipt.cshtml", order);
            }
            
            TempData["ErrorMessage"] = $"Đơn hàng chưa được thanh toán. Trạng thái hiện tại: {order?.Status ?? "N/A"}";
            return RedirectToAction(nameof(Payment), new { id = orderId });
        }

        // 4. Trả về View Receipt
        return View("~/Views/CashierFlow/Receipt.cshtml", order);
    }
    catch (Exception ex) {
        TempData["ErrorMessage"] = $"Lỗi khi tải thông tin đơn hàng: {ex.Message}";
        return RedirectToAction(nameof(OrderSelection));
    }
}
```

**Route:** `GET /cashier-flow/receipt/{orderId}`

**View:** `Frontend/WebSapaForestForStaff/Views/CashierFlow/Receipt.cshtml`

---

#### **Download Receipt**

**Action:** `DownloadReceipt()` (dòng 661-715)

```csharp
[HttpGet("receipt/{orderId}/download")]
public async Task<IActionResult> DownloadReceipt(int orderId)
{
    try {
        // 1. Gọi PaymentApiService để generate receipt
        var file = await _paymentApiService.GenerateReceiptAsync(orderId);
        
        // 2. Validate file
        if (file == null || !file.Success) {
            var errorMsg = file?.ErrorMessage ?? "Không thể tải hóa đơn.";
            
            // Xử lý các trường hợp lỗi
            if (file?.StatusCode == HttpStatusCode.NotFound) {
                errorMsg = "Không tìm thấy đơn hàng hoặc hóa đơn chưa được tạo.";
            } else if (file?.StatusCode == HttpStatusCode.BadRequest) {
                errorMsg = "Đơn hàng chưa được thanh toán. Vui lòng thanh toán trước khi tải hóa đơn.";
            }
            
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Receipt), new { orderId });
        }

        // 3. Validate file bytes
        if (file.FileBytes == null || file.FileBytes.Length == 0) {
            TempData["ErrorMessage"] = "File hóa đơn bị trống. Vui lòng thử lại.";
            return RedirectToAction(nameof(Receipt), new { orderId });
        }

        // 4. Trả về PDF file
        return File(file.FileBytes, "application/pdf", file.FileName);
    }
    catch (Exception ex) {
        TempData["ErrorMessage"] = $"Lỗi khi tải hóa đơn: {ex.Message}";
        return RedirectToAction(nameof(Receipt), new { orderId });
    }
}
```

**Route:** `GET /cashier-flow/receipt/{orderId}/download`

**API Backend:** `GET /api/payment/receipt/{orderId}`

---

#### **Backend API: PaymentController.cs**

**File:** `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs`

**Endpoint:** `GET /api/payment/receipt/{orderId}` (dòng 989-1051)

```csharp
[HttpGet("receipt/{orderId}")]
public async Task<IActionResult> GetReceipt(int orderId, CancellationToken ct = default)
{
    try {
        // 1. Lấy đơn hàng để verify
        var order = await _paymentService.GetOrderDetailAsync(orderId, ct);
        if (order == null) {
            return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {orderId}" });
        }

        // 2. Kiểm tra đơn hàng đã được thanh toán
        if (!IsPaidStatus(order.Status)) {
            return BadRequest(new { message = $"Đơn hàng chưa được thanh toán. Trạng thái hiện tại: {order.Status}" });
        }

        // 3. Generate order code và file path
        var orderCode = $"RMS{orderId:D6}";
        var pdfFileName = $"{orderCode}.pdf";
        var pdfPath = Path.Combine(_env.WebRootPath, "receipts", pdfFileName);

        // 4. Kiểm tra PDF đã tồn tại chưa
        string receiptUrl;
        if (!System.IO.File.Exists(pdfPath)) {
            // Generate receipt (có thể trả về Cloudinary URL hoặc local path)
            receiptUrl = await _receiptService.GenerateReceiptPdfAsync(orderId, ct);
        } else {
            receiptUrl = $"/receipts/{pdfFileName}";
        }

        // 5. Nếu là Cloudinary URL, redirect
        if (!string.IsNullOrEmpty(receiptUrl) && receiptUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            return Redirect(receiptUrl);
        }

        // 6. Fallback: trả về file từ local storage
        if (!System.IO.File.Exists(pdfPath)) {
            return NotFound(new { message = "Không thể tạo hóa đơn. Vui lòng thử lại." });
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath, ct);
        return File(fileBytes, "application/pdf", pdfFileName);
    }
    catch (KeyNotFoundException ex) {
        return NotFound(new { message = ex.Message });
    }
    catch (InvalidOperationException ex) {
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex) {
        return StatusCode(500, new { message = "Lỗi khi tạo hóa đơn", error = ex.Message });
    }
}
```

---

#### **Backend Service: ReceiptService.cs**

**File:** `Backend/BusinessAccessLayer/Services/ReceiptService.cs`

**Method:** `GenerateReceiptPdfAsync()` (dòng 68-459)

**Xử lý chính:**

1. **Lấy đơn hàng:**
   ```csharp
   var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
   if (order == null) {
       throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
   }
   ```

2. **Kiểm tra đơn hàng đã được thanh toán:**
   ```csharp
   if (!IsPaidStatus(order.Status)) {
       throw new InvalidOperationException($"Đơn hàng chưa được thanh toán. Trạng thái hiện tại: {order.Status}");
   }
   ```

3. **Tính toán tổng tiền (giống như ConfirmOrder và ProcessCashPayment):**
   ```csharp
   decimal subtotal = 0;
   foreach (var od in order.OrderDetails) {
       // Bỏ qua món đã bị hủy
       var status = (od.Status ?? "").Trim().ToLower();
       if (status == "removed" || status == "cancelled" || status == "đã hủy") {
           continue;
       }
       
       // Chỉ tính món có status billable
       var billableStatuses = new[] { "cooking", "done", "ready", "served", "đang chế biến", "đã xong", "sẵn sàng" };
       bool isBillable = billableStatuses.Any(s => status == s);
       
       if (!isBillable) {
           continue;
       }
       
       // Xử lý combo
       if (od.ComboId.HasValue && od.OrderComboItems != null) {
           // Kiểm tra combo có món sẵn sàng không
           var hasReadyComboItem = od.OrderComboItems.Any(oci => {
               var comboItemStatus = (oci.Status ?? "").Trim().ToLower();
               return billableStatuses.Any(s => comboItemStatus == s);
           });
           
           if (!hasReadyComboItem) {
               continue;
           }
       }
       
       // Tính tiền
       int billableQuantity = od.QuantityUsed ?? od.Quantity;
       subtotal += od.UnitPrice * billableQuantity;
   }
   
   // Làm tròn và tính VAT, ServiceFee, Discount
   subtotal = RoundUpToThousand(subtotal);
   var vatAmount = RoundUpToThousand(subtotal * 0.1m);
   var serviceFee = RoundUpToThousand(subtotal * 0.05m);
   var discountAmount = RoundUpToThousand(
       order.Payments?.OrderByDescending(p => p.PaymentDate)
           .FirstOrDefault()?.DiscountAmount ?? 0
   );
   var totalAmount = RoundUpToThousand(subtotal + vatAmount + serviceFee - discountAmount);
   ```

4. **Lấy thông tin thanh toán:**
   ```csharp
   var latestTransaction = order.Transactions?.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
   var paymentMethod = latestTransaction?.PaymentMethod ?? "N/A";
   var confirmedBy = latestTransaction?.ConfirmedByUser?.FullName ?? "N/A";
   var paidAt = latestTransaction?.CompletedAt ?? order.CreatedAt ?? DateTime.Now;
   ```

5. **Lấy thông tin khách hàng và bàn:**
   ```csharp
   var tableNumber = order.Reservation?.ReservationTables?.FirstOrDefault()?.Table?.TableNumber?.ToString() ?? "N/A";
   var customerName = order.Customer?.User?.FullName ?? "Khách vãng lai";
   ```

6. **Generate PDF bằng QuestPDF:**
   ```csharp
   var document = Document.Create(container => {
       container.Page(page => {
           page.Size(PageSizes.A4);
           page.Margin(25);
           
           // Header: Restaurant name, address, phone
           page.Header().Column(column => {
               column.Item().AlignCenter().Text(restaurantName).FontSize(18).Bold();
               column.Item().AlignCenter().Text(restaurantAddress).FontSize(9);
               column.Item().AlignCenter().Text($"ĐT: {restaurantPhone}").FontSize(9);
               column.Item().AlignCenter().Text("HÓA ĐƠN THANH TOÁN").FontSize(16).Bold();
               column.Item().Row(row => {
                   row.RelativeItem().AlignLeft().Text($"Số HĐ: {orderId:D4}");
                   row.RelativeItem().AlignRight().Text($"Ngày: {paidAt:dd/MM/yyyy HH:mm}");
               });
           });
           
           // Content: Order info, Items table, Totals
           page.Content().Column(column => {
               // Order info
               column.Item().Column(infoColumn => {
                   infoColumn.Item().Text($"Bàn: {tableNumber}");
                   infoColumn.Item().Text($"Thu ngân: {confirmedBy}");
                   infoColumn.Item().Text($"Khách hàng: {customerName}");
               });
               
               // Items table
               column.Item().Table(table => {
                   // Header: STT, Tên món, SL, Đơn giá, Thành tiền
                   table.Header(header => {
                       header.Cell().Text("STT").Bold();
                       header.Cell().Text("Tên món").Bold();
                       header.Cell().AlignRight().Text("SL").Bold();
                       header.Cell().AlignRight().Text("Đơn giá").Bold();
                       header.Cell().AlignRight().Text("Thành tiền").Bold();
                   });
                   
                   // Rows: Items
                   int itemNumber = 1;
                   foreach (var item in order.OrderDetails) {
                       // Filter: chỉ hiển thị món billable
                       table.Cell().Text($"({itemNumber})");
                       table.Cell().Text(item.MenuItem?.Name ?? "N/A");
                       table.Cell().AlignRight().Text(item.Quantity.ToString());
                       table.Cell().AlignRight().Text($"{item.UnitPrice:N0} đ");
                       table.Cell().AlignRight().Text($"{item.UnitPrice * item.Quantity:N0} đ").Bold();
                       itemNumber++;
                   }
               });
               
               // Totals
               column.Item().AlignRight().Column(summaryColumn => {
                   summaryColumn.Item().Row(row => {
                       row.RelativeItem().AlignLeft().Text("Tổng cộng:");
                       row.RelativeItem().AlignRight().Text($"{subtotal:N0} đ").Bold();
                   });
                   summaryColumn.Item().Row(row => {
                       row.RelativeItem().AlignLeft().Text("VAT (10%):");
                       row.RelativeItem().AlignRight().Text($"{vatAmount:N0} đ");
                   });
                   summaryColumn.Item().Row(row => {
                       row.RelativeItem().AlignLeft().Text("Phí dịch vụ (5%):");
                       row.RelativeItem().AlignRight().Text($"{serviceFee:N0} đ");
                   });
                   if (discountAmount > 0) {
                       summaryColumn.Item().Row(row => {
                           row.RelativeItem().AlignLeft().Text("Giảm giá:").FontColor(Colors.Red.Darken2);
                           row.RelativeItem().AlignRight().Text($"-{discountAmount:N0} đ").FontColor(Colors.Red.Darken2).Bold();
                       });
                   }
                   summaryColumn.Item().Row(row => {
                       row.RelativeItem().AlignLeft().Text("TỔNG CỘNG:").Bold();
                       row.RelativeItem().AlignRight().Text($"{totalAmount:N0} đ").Bold();
                   });
                   summaryColumn.Item().Row(row => {
                       row.RelativeItem().AlignLeft().Text("Phương thức:");
                       row.RelativeItem().AlignRight().Text(paymentMethodUpper).Bold();
                   });
                   summaryColumn.Item().Text($"Bằng chữ: {amountInWords}").Italic();
               });
           });
           
           // Footer
           page.Footer().AlignCenter().Text("Cảm ơn Quý Khách – Hẹn Gặp Lại!").Bold();
       });
   });
   ```

7. **Generate PDF file:**
   ```csharp
   var receiptsPath = Path.Combine(_webRootPath, "receipts");
   if (!Directory.Exists(receiptsPath)) {
       Directory.CreateDirectory(receiptsPath);
   }
   
   var pdfFileName = $"{orderCode}.pdf";
   var pdfPath = Path.Combine(receiptsPath, pdfFileName);
   
   document.GeneratePdf(pdfPath);
   ```

8. **Upload lên Cloudinary (nếu có):**
   ```csharp
   string? cloudinaryUrl = null;
   if (_cloudinaryService != null) {
       try {
           var pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfPath, ct);
           cloudinaryUrl = await _cloudinaryService.UploadPdfAsync(pdfBytes, pdfFileName, "receipts");
       }
       catch (Exception cloudinaryEx) {
           // Log error nhưng không fail
       }
   }
   
   // Trả về Cloudinary URL nếu có, không thì trả về local path
   return cloudinaryUrl ?? $"/receipts/{pdfFileName}";
   ```

**Kết quả:** PDF file được tạo tại `/receipts/{orderCode}.pdf` ✅

---

## 📁 DANH SÁCH FILE LIÊN QUAN

### **Frontend Views:**
- `Frontend/WebSapaForestForStaff/Views/DashboardTable/OrderDetail.cshtml` - Màn hình waiter xác nhận đơn
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/Payment.cshtml` - Màn hình thu ngân thanh toán
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/Receipt.cshtml` - Màn hình xem hóa đơn
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/Modals/_CashPaymentModal.cshtml` - Modal thanh toán tiền mặt
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/Modals/_QRPaymentModal.cshtml` - Modal thanh toán QR
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/Modals/_CombinedPaymentModal.cshtml` - Modal thanh toán kết hợp
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/Modals/_PromotionModal.cshtml` - Modal ưu đãi

### **Frontend Controllers:**
- `Frontend/WebSapaForestForStaff/Controllers/DashboardTableController.cs` - Controller xác nhận đơn
- `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs` - Controller thanh toán

### **Frontend JavaScript:**
- `Frontend/WebSapaForestForStaff/wwwroot/js/waiter/OrderDetail.js` - JS xử lý OrderDetail
- `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/cash-payment.js` - JS thanh toán tiền mặt
- `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/qr-payment.js` - JS thanh toán QR
- `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/combined-payment.js` - JS thanh toán kết hợp
- `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/payment-core.js` - JS core payment
- `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/promotion-voucher.js` - JS ưu đãi

### **Frontend Services:**
- `Frontend/WebSapaForestForStaff/Services/Api/PaymentApiService.cs` - Service gọi API Backend

### **Backend Controllers:**
- `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs` - API Controller thanh toán

### **Backend Services:**
- `Backend/BusinessAccessLayer/Services/PaymentService.cs` - Service xử lý thanh toán
- `Backend/BusinessAccessLayer/Services/ReceiptService.cs` - Service tạo hóa đơn PDF

### **Backend DTOs:**
- `Backend/BusinessAccessLayer/DTOs/Payment/*.cs` - Các DTO liên quan thanh toán

---

## 🔑 CÁC TRẠNG THÁI ORDER

1. **WaitingConfirmation** - Đơn hàng đang chờ waiter xác nhận
2. **Confirmed** - Đơn hàng đã được waiter xác nhận, chờ thu ngân thanh toán
3. **Paid** - Đơn hàng đã được thanh toán
4. **Completed** - Đơn hàng đã hoàn tất (tương đương Paid)
5. **Cancelled** - Đơn hàng đã bị hủy

---

## 📊 FLOW DIAGRAM

```
┌─────────────────────────────────────────────────────────────┐
│ 1. WAITER XÁC NHẬN ĐƠN                                      │
│    OrderDetail.cshtml → handleWaiterConfirmPayment()        │
│    ↓                                                          │
│    DashboardTableController.ConfirmOrder()                   │
│    ↓                                                          │
│    PaymentService.ConfirmOrderAsync()                         │
│    ↓                                                          │
│    Order.Status: "WaitingConfirmation" → "Confirmed" ✅        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. THU NGÂN CHỌN ĐƠN                                        │
│    OrderDetail.cshtml → handleCreatePaymentOrder()            │
│    ↓                                                          │
│    Redirect → CashierFlow/Payment.cshtml                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. THU NGÂN CHỌN PHƯƠNG THỨC                                 │
│    Payment.cshtml → Modal (Cash/QR/Combined)                 │
│    ↓                                                          │
│    cash-payment.js / qr-payment.js / combined-payment.js    │
│    ↓                                                          │
│    Submit form → CashierPaymentFlowController                │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. XÁC NHẬN THANH TOÁN                                       │
│    CashierPaymentFlowController.ProcessCashPayment()          │
│    ↓                                                          │
│    PaymentService.ProcessCashPaymentAsync()                   │
│    ↓                                                          │
│    Order.Status: "Confirmed" → "Paid" ✅                     │
│    ↓                                                          │
│    Trigger post-payment actions                              │
│    - VIP Status Update                                       │
│    - Loyalty Points +1                                       │
│    - Inventory Deduction                                     │
│    - Revenue Recording                                       │
│    - Table Release                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 5. IN HÓA ĐƠN                                                │
│    Receipt.cshtml → DownloadReceipt()                        │
│    ↓                                                          │
│    ReceiptService.GenerateReceiptPdfAsync()                   │
│    ↓                                                          │
│    PDF file generated tại /receipts/{orderCode}.pdf ✅       │
└─────────────────────────────────────────────────────────────┘
```

---

## ✅ KẾT LUẬN

Luồng thanh toán hoàn chỉnh từ đầu đến cuối bao gồm:

1. ✅ Waiter xác nhận đơn hàng → Order.Status = "Confirmed"
2. ✅ Thu ngân chọn đơn đã xác nhận → Chuyển sang màn hình Payment
3. ✅ Thu ngân chọn phương thức thanh toán → Modal (Cash/QR/Combined)
4. ✅ Xác nhận thanh toán → Order.Status = "Paid" + Post-payment actions
5. ✅ In hóa đơn → PDF file generated

Tất cả các file và luồng xử lý đã được mô tả chi tiết trong tài liệu này.

