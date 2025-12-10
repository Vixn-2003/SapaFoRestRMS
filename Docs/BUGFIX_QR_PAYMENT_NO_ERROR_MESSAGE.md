# 🐛 BUGFIX: Thanh toán QR không hiển thị thông báo lỗi

## 📋 Mô tả lỗi

**URL:** `http://192.168.1.180:5054/cashier-flow/payment/8`

**Hiện tượng:**
- Khi thanh toán bằng QR và xác nhận nhận tiền
- Trang redirect về lại trang Payment cũ
- Không có hiển thị thông báo lỗi hoặc thành công

**Nguyên nhân:**
1. View `Payment.cshtml` không có code để hiển thị `TempData["ErrorMessage"]` và `TempData["SuccessMessage"]`
2. Thiếu logging để debug lỗi từ API

---

## ✅ Giải pháp đã áp dụng

### 1. Thêm hiển thị TempData messages trong Payment.cshtml

**File:** `Frontend/WebSapaForestForStaff/Views/CashierFlow/Payment.cshtml`

**Thay đổi:**
- Thêm code hiển thị toast notification từ `TempData["SuccessMessage"]` và `TempData["ErrorMessage"]`
- Sử dụng function `showToast()` có sẵn trong view

```cshtml
@if (TempData["SuccessMessage"] != null)
{
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            showToast('@Html.Raw(TempData["SuccessMessage"])', 'success');
        });
    </script>
}
@if (TempData["ErrorMessage"] != null)
{
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            showToast('@Html.Raw(TempData["ErrorMessage"])', 'error');
        });
    </script>
}
```

### 2. Thêm logging trong CashierPaymentFlowController.cs

**File:** `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs`

**Method:** `ConfirmQrPayment`

**Thay đổi:**
- Thêm debug logging để trace request/response
- Wrap logic trong try-catch để bắt exception
- Log chi tiết khi có lỗi

**Log points:**
- Request input (OrderId, Notes)
- Order detail (TotalAmount, Status)
- API request parameters
- API response (Success, Message)
- Exception details (Message, StackTrace)

### 3. Thêm logging trong PaymentApiService.cs

**File:** `Frontend/WebSapaForestForStaff/Services/Api/PaymentApiService.cs`

**Method:** `ConfirmPaymentAsync`

**Thay đổi:**
- Log request parameters (OrderId, PaymentMethod, Amount)
- Log response status code
- Log response body khi thất bại
- Log error message chi tiết

**Log levels:**
- `LogInformation`: Request/response thành công
- `LogWarning`: Request thất bại, response body, error message

---

## 🔍 Cách kiểm tra lỗi

### Bước 1: Reproduce lỗi
1. Truy cập `http://192.168.1.180:5054/cashier-flow/payment/8`
2. Click "Thanh toán QR"
3. Click "Đã nhận tiền" để xác nhận thanh toán

### Bước 2: Kiểm tra thông báo
- **Nếu thành công:** Hiển thị toast màu xanh "✅ Đã xác nhận thanh toán QR thành công!" và redirect đến Receipt
- **Nếu thất bại:** Hiển thị toast màu đỏ với message lỗi cụ thể từ API

### Bước 3: Xem log để debug

#### Frontend logs (Visual Studio Output)
```plaintext
[ConfirmQrPayment] Called with OrderId: 8, Notes: Thu ngân xác nhận đã nhận tiền qua QR
[ConfirmQrPayment] Order found: 8, TotalAmount: 350000, Status: Confirmed
[ConfirmQrPayment] Sending confirm request: OrderId=8, Amount=350000, PaymentMethod=QRBankTransfer
[ConfirmPaymentAsync] Sending request: OrderId=8, PaymentMethod=QRBankTransfer, Amount=350000
[ConfirmPaymentAsync] Response status: 400
[ConfirmPaymentAsync] Failed with status 400. Response body: {"message":"Đơn hàng đã được thanh toán"}
[ConfirmPaymentAsync] Error message: Đơn hàng đã được thanh toán
[ConfirmQrPayment] API result: Success=False, Message=Đơn hàng đã được thanh toán
[ConfirmQrPayment] Payment failed: Đơn hàng đã được thanh toán
```

#### Phân tích log
- Nếu status code là `400` → Lỗi business logic (ví dụ: đơn hàng đã thanh toán, chưa xác nhận món, etc.)
- Nếu status code là `401` → Lỗi authentication (token hết hạn)
- Nếu status code là `404` → Không tìm thấy order hoặc transaction
- Nếu status code là `500` → Lỗi server

---

## 🧪 Test Cases

### Test Case 1: Thanh toán QR thành công
**Preconditions:**
- Order status = "Confirmed"
- Order chưa được thanh toán
- Order items đã được xác nhận

**Steps:**
1. Truy cập Payment page
2. Click "Thanh toán QR"
3. QR code hiển thị
4. Click "Đã nhận tiền"

**Expected:**
- Hiển thị toast "✅ Đã xác nhận thanh toán QR thành công!"
- Redirect đến Receipt page
- Receipt hiển thị đầy đủ thông tin

### Test Case 2: Thanh toán QR với order đã thanh toán
**Preconditions:**
- Order status = "Paid"
- Order đã được thanh toán trước đó

**Steps:**
1. Truy cập Payment page với order đã thanh toán
2. Click "Thanh toán QR"
3. Click "Đã nhận tiền"

**Expected:**
- Hiển thị toast lỗi "❌ Đơn hàng đã được thanh toán"
- Vẫn ở lại Payment page
- Không redirect đến Receipt

### Test Case 3: Thanh toán QR với order chưa xác nhận món
**Preconditions:**
- Order status = "WaitingConfirmation"
- Order items chưa được xác nhận

**Steps:**
1. Truy cập Payment page
2. Click "Thanh toán QR"
3. Click "Đã nhận tiền"

**Expected:**
- Hiển thị toast lỗi "❌ Đơn hàng chưa được khách xác nhận"
- Vẫn ở lại Payment page

---

## 🔧 Backend API Contract

### Endpoint: POST /api/payment/payments/confirm

**Request:**
```json
{
  "orderId": 8,
  "paymentMethod": "QRBankTransfer",
  "amount": 350000,
  "notes": "Thu ngân xác nhận đã nhận tiền qua QR",
  "sessionId": "",
  "cashGiven": null
}
```

**Response (Success - 200 OK):**
```json
{
  "success": true,
  "message": "Thanh toán thành công",
  "transaction": {
    "transactionId": 123,
    "orderId": 8,
    "status": "Paid",
    "amount": 350000
  }
}
```

**Response (Error - 400 Bad Request):**
```json
{
  "message": "Đơn hàng đã được thanh toán"
}
```

**Response (Error - 404 Not Found):**
```json
{
  "message": "Không tìm thấy đơn hàng với ID: 8"
}
```

**Response (Error - 401 Unauthorized):**
```json
{
  "message": "User not authenticated"
}
```

---

## 📝 Các lỗi có thể xảy ra

### 1. Đơn hàng đã được thanh toán
**Message:** "Đơn hàng đã được thanh toán"
**Nguyên nhân:** Order đã có status = "Paid"
**Giải pháp:** Kiểm tra status trước khi cho phép thanh toán

### 2. Đơn hàng chưa được xác nhận
**Message:** "Đơn hàng chưa được khách xác nhận"
**Nguyên nhân:** Order status != "Confirmed"
**Giải pháp:** Waiter phải xác nhận món trước khi thu ngân thanh toán

### 3. Token hết hạn
**Message:** "User not authenticated"
**Nguyên nhân:** JWT token hết hạn hoặc không hợp lệ
**Giải pháp:** Đăng xuất và đăng nhập lại

### 4. Không tìm thấy đơn hàng
**Message:** "Không tìm thấy đơn hàng với ID: {orderId}"
**Nguyên nhân:** OrderId không tồn tại trong database
**Giải pháp:** Kiểm tra lại OrderId

### 5. Lỗi server
**Message:** "Lỗi khi xử lý thanh toán"
**Nguyên nhân:** Exception trong backend (database error, network error, etc.)
**Giải pháp:** Kiểm tra backend logs, database connection

---

## ✨ Improvements

### Hiện tại
- ✅ Hiển thị toast notification khi có lỗi
- ✅ Log chi tiết request/response để debug
- ✅ Error handling đầy đủ với try-catch
- ✅ Message lỗi rõ ràng từ backend

### Cải tiến trong tương lai
- [ ] Thêm loading spinner khi đang xử lý thanh toán
- [ ] Disable button "Đã nhận tiền" sau khi click để tránh double submit
- [ ] Thêm confirmation dialog trước khi xác nhận thanh toán
- [ ] Tự động kiểm tra status order trước khi submit
- [ ] Hiển thị error message trong modal thay vì toast (để user có thời gian đọc)

---

## 📅 Change Log

**Date:** 2025-12-10
**Author:** AI Assistant
**Status:** ✅ Fixed

### Files Changed:
1. `Frontend/WebSapaForestForStaff/Views/CashierFlow/Payment.cshtml`
   - Added TempData message display

2. `Frontend/WebSapaForestForStaff/Controllers/CashierPaymentFlowController.cs`
   - Added debug logging in `ConfirmQrPayment` method
   - Added try-catch error handling

3. `Frontend/WebSapaForestForStaff/Services/Api/PaymentApiService.cs`
   - Added debug logging in `ConfirmPaymentAsync` method
   - Log response body on failure

---

## 🔗 Related Issues

- None (first report of this bug)

---

## 📚 References

- [QR_VIETQR_MANUAL_CONFIRMATION.md](./QR_VIETQR_MANUAL_CONFIRMATION.md)
- [PAYMENT_FRONTEND_BACKEND_INTEGRATION.md](./PAYMENT_FRONTEND_BACKEND_INTEGRATION.md)
- [CASHIER_PAYMENT_COMPLETE_WORKFLOW.md](./CASHIER_PAYMENT_COMPLETE_WORKFLOW.md)

