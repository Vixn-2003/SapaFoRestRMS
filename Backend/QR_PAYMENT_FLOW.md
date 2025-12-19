# Luồng Thanh Toán QR Code - Backend đến Frontend

## Tổng Quan

Tài liệu này mô tả chi tiết luồng thanh toán QR code (VietQR) từ backend đến frontend trong hệ thống SapaFoRestRMS.

## Kiến Trúc Tổng Quan

```
Frontend (Cashier) → Backend API → PaymentService → Database
     ↓                    ↓              ↓
  QR Modal          QR Generation   Transaction
  Display           & Validation    Creation
```

---

## 1. FRONTEND - Khởi Tạo Thanh Toán QR

### 1.1. Người Dùng Chọn Thanh Toán QR

**File:** `Frontend/WebSapaForestForStaff/Views/CashierFlow/Payment.cshtml`

- Thu ngân click nút **"Thanh toán QR"** trên màn hình thanh toán
- JavaScript module `QrPayment` được gọi

### 1.2. Mở QR Payment Modal

**File:** `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/qr-payment.js`

```javascript
// Function: openQrPaymentModal()
// Line: 39-56

openQrPaymentModal: function() {
    // Reset modal state
    document.getElementById('qrLoading').classList.remove('d-none');
    document.getElementById('qrContent').classList.add('d-none');
    
    // Show modal
    window.PaymentCore.modals.qrModal.show();
    
    // Load QR preview after 600ms
    setTimeout(() => this.loadQrPreview(), 600);
}
```

### 1.3. Tạo QR Code Preview (Client-side)

**File:** `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/qr-payment.js`

```javascript
// Function: loadQrPreview()
// Line: 58-80

loadQrPreview: function() {
    const amount = window.PaymentCore.orderContext.Total;
    const orderCode = window.PaymentCore.orderContext.OrderCode;
    const bank = "MB"; // MBBank
    const account = "0397604824";
    const addInfo = `RMS#${orderCode}`;
    
    // Generate VietQR URL (client-side)
    const qrUrl = window.PaymentCore.generateVietQrUrl(bank, account, amount, addInfo);
    
    // Update UI
    document.getElementById('qrImage').src = qrUrl;
    document.getElementById('qrAmount').textContent = formatCurrency(amount);
    document.getElementById('qrDescription').textContent = addInfo;
}
```

**Lưu ý:** Hiện tại frontend tạo QR code trực tiếp từ client-side. Có thể cải thiện bằng cách gọi API backend để tạo QR code.

---

## 2. FRONTEND - Xác Nhận Thanh Toán QR

### 2.1. Thu Ngân Xác Nhận Đã Nhận Tiền

**File:** `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/qr-payment.js`

```javascript
// Function: confirmQRPayment()
// Line: 87-117

confirmQRPayment: function() {
    // Submit form to controller
    document.getElementById('qrConfirmOrderId').value = orderId;
    document.getElementById('qrConfirmNotes').value = 'Thu ngân xác nhận đã nhận tiền qua QR';
    
    // Set processing state
    sessionStorage.setItem('qrPaymentProcessing', 'true');
    sessionStorage.setItem('qrPaymentOrderId', orderId);
    
    // Show loading modal
    this.showQrPaymentLoadingModal();
    
    // Submit form (POST to /cashier-flow/payment/confirm-qr)
    document.getElementById('qrConfirmForm').submit();
}
```

### 2.2. Frontend Controller Xử Lý

**File:** `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs`

```csharp
// Endpoint: POST /cashier-flow/payment/confirm-qr
// Line: 175-244

[HttpPost("payment/confirm-qr")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ConfirmQrPayment([FromForm] ConfirmQrPaymentRequest request)
{
    // 1. Validate request
    if (request == null || request.OrderId <= 0)
    {
        TempData["ErrorMessage"] = "Dữ liệu không hợp lệ";
        return RedirectToAction(nameof(Payment), new { id = request?.OrderId ?? 0 });
    }
    
    // 2. Get order details
    var order = await _paymentApiService.GetOrderDetailAsync(request.OrderId);
    if (order == null)
    {
        TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng {request.OrderId}";
        return RedirectToAction(nameof(OrderSelection));
    }
    
    // 3. Validate TotalAmount
    if (order.TotalAmount <= 0)
    {
        TempData["ErrorMessage"] = "Tổng tiền đơn hàng không hợp lệ";
        return RedirectToAction(nameof(Payment), new { id = request.OrderId });
    }
    
    // 4. Create payment confirmation request
    var confirmRequest = new PaymentConfirmRequest
    {
        OrderId = request.OrderId,
        PaymentMethod = "QRBankTransfer",
        Amount = order.TotalAmount,
        Notes = request.Notes ?? "Thu ngân xác nhận đã nhận tiền qua QR",
        SessionId = string.Empty,
        CashGiven = null
    };
    
    // 5. Call backend API
    var result = await _paymentApiService.ConfirmPaymentAsync(confirmRequest);
    
    // 6. Handle result
    if (!result.Success)
    {
        TempData["ErrorMessage"] = result.Message ?? "Xác nhận thanh toán thất bại";
        return RedirectToAction(nameof(Payment), new { id = request.OrderId });
    }
    
    // 7. Success - redirect to receipt
    TempData["SuccessMessage"] = "✅ Đã xác nhận thanh toán QR thành công!";
    return RedirectToAction(nameof(Receipt), new { orderId = request.OrderId });
}
```

---

## 3. BACKEND API - Xác Nhận Thanh Toán

### 3.1. API Endpoint

**File:** `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs`

```csharp
// Endpoint: POST /api/payment/confirm
// Line: 331-407

[HttpPost("confirm")]
[Authorize(Policy = "Position:Owner,Manager,Staff")]
public async Task<IActionResult> ConfirmPayment(
    [FromBody] PaymentConfirmRequestDto request,
    CancellationToken ct = default)
{
    // 1. Get user ID from JWT token
    var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
    
    // 2. Call PaymentService
    var transaction = await _paymentService.ConfirmPaymentAsync(request, userId, ct);
    
    // 3. Return success response
    return Ok(new
    {
        success = true,
        message = "Thanh toán thành công",
        transactionId = transaction.TransactionId,
        transactionCode = transaction.TransactionCode
    });
}
```

### 3.2. PaymentService - Xử Lý Logic Thanh Toán

**File:** `Backend/BusinessAccessLayer/Services/PaymentService.cs`

```csharp
// Method: ConfirmPaymentAsync()
// Line: 400-536

public async Task<TransactionDto> ConfirmPaymentAsync(
    PaymentConfirmRequestDto request,
    int userId,
    CancellationToken ct = default)
{
    // 1. Get order with items
    var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
    if (order == null)
    {
        throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
    }
    
    // 2. Check for existing transaction (from InitiatePayment)
    var existingTransaction = await _unitOfWork.Payments
        .GetTransactionBySessionIdAsync(request.SessionId);
    
    decimal expectedAmount;
    if (existingTransaction != null)
    {
        // Use amount from existing transaction
        expectedAmount = existingTransaction.Amount;
        
        // Validate amount matches
        if (Math.Abs(request.Amount - existingTransaction.Amount) > 0.01m)
        {
            throw new InvalidOperationException(
                $"Số tiền thanh toán không khớp với phiên thanh toán");
        }
    }
    else
    {
        // Calculate amount from order
        var orderDto = _mapper.Map<OrderDto>(order);
        CalculateOrderAmounts(order, orderDto);
        expectedAmount = orderDto.TotalAmount ?? 0;
        
        // Validate amount
        if (Math.Abs(request.Amount - expectedAmount) > 0.01m)
        {
            throw new InvalidOperationException(
                $"Số tiền thanh toán không khớp. Mong đợi: {expectedAmount:N0} ₫");
        }
    }
    
    // 3. Update or create transaction
    Transaction savedTransaction;
    if (existingTransaction != null)
    {
        // Update existing transaction
        existingTransaction.Status = "Paid";
        existingTransaction.CompletedAt = DateTime.Now;
        existingTransaction.Notes = request.Notes ?? existingTransaction.Notes;
        await _unitOfWork.Payments.UpdateTransactionAsync(existingTransaction);
        savedTransaction = existingTransaction;
    }
    else
    {
        // Create new transaction
        var transaction = new Transaction
        {
            OrderId = request.OrderId,
            TransactionCode = $"TXN-{DateTime.Now.Ticks}",
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod, // "QRBankTransfer"
            Status = "Paid",
            CreatedAt = DateTime.Now,
            CompletedAt = DateTime.Now,
            SessionId = request.SessionId,
            Notes = request.Notes
        };
        savedTransaction = await _unitOfWork.Payments.SaveTransactionAsync(transaction);
    }
    
    // 4. Update order status
    order.Status = OrderStatusConstants.Paid;
    await _unitOfWork.Payments.UpdateAsync(order);
    
    // 5. Release tables and complete reservation
    await ReleaseTablesAndCompleteReservationAsync(request.OrderId, userId, ct);
    
    // 6. Save changes
    await _unitOfWork.SaveChangesAsync();
    
    // 7. Trigger post-payment actions (VIP update, LoyaltyPoints, etc.)
    await TriggerPostPaymentActionsAsync(request.OrderId, savedTransaction.TransactionId, ct);
    
    // 8. Return transaction DTO
    return _mapper.Map<TransactionDto>(savedTransaction);
}
```

---

## 4. BACKEND - Tạo QR Code (Optional - Hiện Chưa Được Sử Dụng)

### 4.1. API Endpoint Tạo QR Code

**File:** `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs`

```csharp
// Endpoint: GET /api/payment/qr/{orderId}
// Line: 771-810

[HttpGet("qr/{orderId}")]
[Authorize(Policy = "Position:Owner,Manager,Staff")]
public async Task<IActionResult> GenerateQRForManualConfirmation(
    int orderId,
    CancellationToken ct = default)
{
    // 1. Get bank configuration from appsettings.json
    var bankCode = _configuration["BankSettings:BankCode"] ?? "VCB";
    var account = _configuration["BankSettings:Account"] ?? "0123456789";
    
    // 2. Validate order exists
    var order = await _paymentService.GetOrderDetailAsync(orderId, ct);
    if (order == null)
    {
        return NotFound(new { message = $"Không tìm thấy đơn hàng với ID: {orderId}" });
    }
    
    // 3. Start payment flow - creates transaction with status "PaymentProcessing"
    var transaction = await _paymentService.StartPaymentAsync(orderId, "QRBankTransfer", ct);
    
    // 4. Generate VietQR URL
    var qrResponse = await _paymentService.GenerateVietQRAsync(orderId, bankCode, account, null, ct);
    
    // 5. Return QR data
    return Ok(new
    {
        qrUrl = qrResponse.QrUrl,
        amount = qrResponse.Total,
        description = qrResponse.Description,
        orderId = qrResponse.OrderId,
        orderCode = qrResponse.OrderCode,
        transactionId = transaction.TransactionId,
        transactionCode = transaction.TransactionCode
    });
}
```

### 4.2. PaymentService - Generate VietQR

**File:** `Backend/BusinessAccessLayer/Services/PaymentService.cs`

```csharp
// Method: GenerateVietQRAsync()
// Line: 1092-1130

public async Task<VietQRResponseDto> GenerateVietQRAsync(
    int orderId,
    string bankCode,
    string account,
    decimal? customAmount,
    CancellationToken ct = default)
{
    // 1. Get order
    var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
    if (order == null)
    {
        throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {orderId}");
    }
    
    // 2. Calculate total amount
    var orderDto = _mapper.Map<OrderDto>(order);
    CalculateOrderAmounts(order, orderDto);
    var totalAmount = customAmount ?? orderDto.TotalAmount ?? 0;
    
    // 3. Create order code
    var orderCode = $"RMS{orderId:D6}";
    
    // 4. Create description
    var description = customAmount.HasValue
        ? $"Order#{orderCode} (Partial: {customAmount:N0} VND)"
        : $"Order#{orderCode}";
    
    // 5. Encode description for URL
    var encodedDescription = WebUtility.UrlEncode(description);
    
    // 6. Generate VietQR URL
    // Format: https://img.vietqr.io/image/{BANKCODE}-{ACCOUNT}.png?amount={AMOUNT}&addInfo={DESCRIPTION}
    var qrUrl = $"https://img.vietqr.io/image/{bankCode}-{account}-compact2.png?amount={(int)totalAmount}&addInfo={encodedDescription}";
    
    // 7. Return response
    return new VietQRResponseDto
    {
        QrUrl = qrUrl,
        OrderId = orderId,
        Total = totalAmount,
        OrderCode = orderCode,
        Description = description
    };
}
```

---

## 5. LUỒNG DỮ LIỆU CHI TIẾT

### 5.1. Sequence Diagram

```
[Cashier] → [Frontend JS] → [Frontend Controller] → [Backend API] → [PaymentService] → [Database]
    |              |                  |                    |                |              |
    |  Click QR    |                  |                    |                |              |
    |------------->|                  |                    |                |              |
    |              |  Show Modal      |                    |                |              |
    |              |  Generate QR     |                    |                |              |
    |              |  (Client-side)   |                    |                |              |
    |              |                  |                    |                |              |
    |  Confirm     |                  |                    |                |              |
    |------------->|                  |                    |                |              |
    |              |  Submit Form     |                    |                |              |
    |              |----------------->|                    |                |              |
    |              |                  |  POST /confirm-qr  |                |              |
    |              |                  |------------------>|                |              |
    |              |                  |                    |  ConfirmPayment|              |
    |              |                  |                    |--------------->|              |
    |              |                  |                    |                |  Get Order   |
    |              |                  |                    |                |------------->|
    |              |                  |                    |                |<-------------|
    |              |                  |                    |                |  Create/Update|
    |              |                  |                    |                |  Transaction |
    |              |                  |                    |                |------------->|
    |              |                  |                    |                |<-------------|
    |              |                  |                    |                |  Update Order|
    |              |                  |                    |                |------------->|
    |              |                  |                    |                |<-------------|
    |              |                  |                    |<----------------|              |
    |              |                  |<------------------|                |              |
    |              |                  |  Redirect Receipt |                |              |
    |              |<-----------------|                  |                |              |
    |<-------------|                  |                    |                |              |
```

### 5.2. Data Flow

#### Request Flow:
1. **Frontend Form Data:**
   ```json
   {
     "OrderId": 123,
     "Notes": "Thu ngân xác nhận đã nhận tiền qua QR"
   }
   ```

2. **Frontend Controller → Backend API:**
   ```json
   {
     "OrderId": 123,
     "PaymentMethod": "QRBankTransfer",
     "Amount": 500000,
     "Notes": "Thu ngân xác nhận đã nhận tiền qua QR",
     "SessionId": "",
     "CashGiven": null
   }
   ```

3. **Backend API Response:**
   ```json
   {
     "success": true,
     "message": "Thanh toán thành công",
     "transactionId": 456,
     "transactionCode": "TXN-20250101120000-ABC123"
   }
   ```

---

## 6. CẤU HÌNH VÀ THIẾT LẬP

### 6.1. Bank Settings (appsettings.json)

```json
{
  "BankSettings": {
    "BankCode": "VCB",  // Vietcombank
    "Account": "0123456789"
  }
}
```

### 6.2. VietQR URL Format

```
https://img.vietqr.io/image/{BANKCODE}-{ACCOUNT}-compact2.png?amount={AMOUNT}&addInfo={DESCRIPTION}
```

**Ví dụ:**
```
https://img.vietqr.io/image/VCB-0123456789-compact2.png?amount=500000&addInfo=Order%23RMS000123
```

### 6.3. Transaction Status Flow

```
PaymentProcessing → Paid → (Order Status: Paid)
```

---

## 7. XỬ LÝ LỖI

### 7.1. Frontend Error Handling

- **Invalid OrderId:** Redirect về Payment page với error message
- **API Failure:** Hiển thị error message từ TempData
- **Network Error:** Show toast notification

### 7.2. Backend Error Handling

- **Order Not Found:** `KeyNotFoundException` → 404
- **Invalid Amount:** `InvalidOperationException` → 400
- **Database Error:** `Exception` → 500

---

## 8. CẢI THIỆN ĐỀ XUẤT

### 8.1. Hiện Tại

- Frontend tạo QR code trực tiếp từ client-side
- Không có validation QR code từ backend
- Không có webhook để xác nhận tự động từ ngân hàng

### 8.2. Đề Xuất Cải Thiện

1. **Sử dụng Backend API để tạo QR:**
   - Gọi `GET /api/payment/qr/{orderId}` thay vì tạo client-side
   - Đảm bảo QR code được tạo với thông tin chính xác từ database

2. **Thêm Transaction Tracking:**
   - Tạo transaction với status "PaymentProcessing" khi hiển thị QR
   - Cập nhật transaction khi xác nhận thanh toán

3. **Thêm Webhook Integration:**
   - Tích hợp webhook từ ngân hàng để xác nhận tự động
   - Giảm thiểu việc xác nhận thủ công

4. **Thêm QR Code Expiry:**
   - QR code có thời gian hết hạn (ví dụ: 15 phút)
   - Tự động tạo QR code mới nếu hết hạn

---

## 9. FILES LIÊN QUAN

### Frontend:
- `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs`
- `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/qr-payment.js`
- `Frontend/WebSapaForestForStaff/wwwroot/js/cashier/payment-core.js`
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/Modals/_QRPaymentModal.cshtml`
- `Frontend/WebSapaForestForStaff/Views/CashierFlow/Payment.cshtml`

### Backend:
- `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs`
- `Backend/BusinessAccessLayer/Services/PaymentService.cs`
- `Backend/BusinessAccessLayer/Services/Interfaces/IPaymentService.cs`
- `Backend/DataAccessLayer/Repositories/PaymentRepository.cs`

---

## 10. TESTING

### 10.1. Test Cases

1. **Happy Path:**
   - Thu ngân chọn QR payment → Hiển thị QR code → Xác nhận thanh toán → Redirect đến Receipt

2. **Error Cases:**
   - Order không tồn tại
   - TotalAmount <= 0
   - API failure
   - Network timeout

3. **Edge Cases:**
   - Order đã được thanh toán
   - Order status không hợp lệ
   - Concurrent payment attempts

---

## KẾT LUẬN

Luồng thanh toán QR code hiện tại hoạt động với cơ chế xác nhận thủ công bởi thu ngân. Frontend tạo QR code từ client-side và gửi request xác nhận đến backend khi thu ngân xác nhận đã nhận tiền.

**Điểm mạnh:**
- Đơn giản, dễ triển khai
- Không cần tích hợp phức tạp với ngân hàng
- Thu ngân có quyền kiểm soát xác nhận thanh toán

**Điểm cần cải thiện:**
- Nên sử dụng backend API để tạo QR code
- Có thể thêm webhook integration cho xác nhận tự động
- Thêm transaction tracking tốt hơn

