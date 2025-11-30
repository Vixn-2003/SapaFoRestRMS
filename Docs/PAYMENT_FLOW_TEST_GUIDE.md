# Hướng Dẫn Test Luồng Thanh Toán Hoàn Chỉnh

## 📋 Tổng Quan

Tài liệu này hướng dẫn test toàn bộ luồng thanh toán từ lúc xác nhận hóa đơn đến khi thanh toán hoàn tất và in hóa đơn cho khách hàng.

---

## 🎯 Mục Tiêu Test

Test các chức năng sau:
1. ✅ Lấy danh sách đơn hàng chờ thanh toán
2. ✅ Xem chi tiết hóa đơn và tính toán tổng tiền
3. ✅ Áp dụng mã giảm giá (nếu có)
4. ✅ Chọn phương thức thanh toán (Tiền mặt hoặc QR)
5. ✅ Xác nhận thanh toán
6. ✅ Kiểm tra trạng thái thanh toán
7. ✅ Tải và in hóa đơn PDF

---

## 🔧 Chuẩn Bị

### 1. Đảm Bảo Backend Đang Chạy
```bash
cd Backend/SapaFoRestRMSAPI
dotnet run
```
Backend sẽ chạy tại: `http://localhost:5180`

### 2. Đảm Bảo Database Có Dữ Liệu Test
- Có ít nhất 1 đơn hàng với status = `"Pending"` hoặc `"PendingPayment"`
- Đơn hàng phải có OrderItems (món ăn)
- Có user với role Staff/Manager/Owner để login

### 3. Công Cụ Test
- **Visual Studio Code** với extension **REST Client** (để chạy file `.http`)
- Hoặc **Postman**
- Hoặc **curl** command line

---

## 📝 Các Bước Test

### Bước 1: Đăng Nhập

**Endpoint:** `POST /api/Auth/login`

**Request:**
```json
{
  "Email": "staff@example.com",
  "Password": "Staff@123"
}
```

**Response:**
```json
{
  "UserId": 1,
  "FullName": "Nguyễn Văn A",
  "Email": "staff@example.com",
  "RoleId": 2,
  "RoleName": "Staff",
  "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "RefreshToken": "..."
}
```

**Lưu ý:** Copy `Token` để dùng cho các request tiếp theo.

---

### Bước 2: Lấy Danh Sách Đơn Hàng Chờ Thanh Toán

**Endpoint:** `GET /api/payment/orders?status=pending-payment`

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
[
  {
    "orderId": 1,
    "orderCode": "RMS0000001",
    "status": "Pending",
    "totalAmount": 115000,
    "createdAt": "2025-01-15T10:00:00Z"
  },
  ...
]
```

**Kiểm tra:**
- ✅ Response trả về danh sách đơn hàng
- ✅ Chọn một `orderId` để test tiếp

---

### Bước 3: Xem Chi Tiết Hóa Đơn

**Endpoint:** `GET /api/payment/orders/{orderId}/details`

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "orderId": 1,
  "orderCode": "RMS0000001",
  "status": "Pending",
  "subtotal": 100000,
  "vatAmount": 10000,
  "serviceFee": 5000,
  "discountAmount": 0,
  "totalAmount": 115000,
  "orderItems": [
    {
      "menuItemName": "Phở Bò",
      "quantity": 2,
      "unitPrice": 50000,
      "totalPrice": 100000
    }
  ]
}
```

**Kiểm tra:**
- ✅ Thông tin đơn hàng đầy đủ
- ✅ Danh sách món ăn hiển thị đúng
- ✅ Giá tiền được tính đúng

---

### Bước 4: Lấy Tóm Tắt Đơn Hàng (Với Tính Toán Tổng Tiền)

**Endpoint:** `GET /api/payment/order/{orderId}`

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "orderId": 1,
  "orderCode": "RMS0000001",
  "subtotal": 100000,
  "tax": 10000,
  "serviceFee": 5000,
  "discount": 0,
  "total": 115000,
  "items": [
    {
      "name": "Phở Bò",
      "quantity": 2,
      "price": 50000,
      "total": 100000
    }
  ]
}
```

**Kiểm tra:**
- ✅ `total = subtotal + tax + serviceFee - discount`
- ✅ Công thức: `115000 = 100000 + 10000 + 5000 - 0`

---

### Bước 5: Áp Dụng Mã Giảm Giá (Tùy Chọn)

**Endpoint:** `POST /api/payment/discounts/validate`

**Headers:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Request:**
```json
{
  "OrderId": 1,
  "DiscountCode": "DISCOUNT10"
}
```

**Response:**
```json
{
  "orderId": 1,
  "orderCode": "RMS0000001",
  "discountAmount": 10000,
  "totalAmount": 105000
}
```

**Kiểm tra:**
- ✅ Discount được áp dụng đúng
- ✅ Total amount đã được cập nhật (trừ discount)

**Lưu ý:** Nếu không có mã giảm giá, có thể bỏ qua bước này.

---

### Bước 6A: Thanh Toán Tiền Mặt

**Endpoint:** `POST /api/payment/cash`

**Headers:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Request:**
```json
{
  "OrderId": 1,
  "Amount": 115000,
  "ReceivedAmount": 120000,
  "Notes": "Thanh toán tiền mặt"
}
```

**Response:**
```json
{
  "transactionId": 123,
  "transactionCode": "TXN-20250115123456-1-ABC12345",
  "orderId": 1,
  "amount": 115000,
  "receivedAmount": 120000,
  "refundAmount": 5000,
  "paymentMethod": "Cash",
  "status": "Paid",
  "createdAt": "2025-01-15T10:30:00Z"
}
```

**Kiểm tra:**
- ✅ Transaction được tạo thành công
- ✅ `refundAmount = receivedAmount - amount` (nếu có)
- ✅ Status = "Paid"

---

### Bước 6B: Thanh Toán Qua QR

#### 6B.1: Tạo QR Code

**Endpoint:** `GET /api/payment/qr/{orderId}`

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "qrUrl": "https://img.vietqr.io/image/VCB-0123456789-compact2.png?amount=115000&addInfo=RMS%23ORD0001",
  "amount": 115000,
  "description": "RMS#ORD0001",
  "orderId": 1,
  "orderCode": "RMS0000001",
  "transactionId": 124,
  "transactionCode": "TXN-20250115123457-1-XYZ67890"
}
```

**Kiểm tra:**
- ✅ QR URL được tạo thành công
- ✅ Transaction ID được trả về
- ✅ Mở QR URL trong browser để xem QR code

#### 6B.2: Xác Nhận Thanh Toán QR

**Endpoint:** `POST /api/payment/confirm`

**Headers:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Request:**
```json
{
  "OrderId": 1,
  "TransactionId": 124,
  "GatewayReference": null,
  "Notes": "Đã nhận tiền qua QR"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Xác nhận thanh toán thành công",
  "status": "PAID",
  "transaction": {
    "transactionId": 124,
    "status": "Paid",
    "amount": 115000
  }
}
```

**Kiểm tra:**
- ✅ Payment được xác nhận thành công
- ✅ Status = "PAID"

---

### Bước 7: Kiểm Tra Trạng Thái Thanh Toán

**Endpoint:** `GET /api/payment/status/{orderId}`

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "orderId": 1,
  "status": "Paid",
  "totalAmount": 115000,
  "paidAmount": 115000,
  "transactions": [
    {
      "transactionId": 124,
      "transactionCode": "TXN-...",
      "amount": 115000,
      "paymentMethod": "QRBankTransfer",
      "status": "Paid"
    }
  ]
}
```

**Kiểm tra:**
- ✅ Order status = "Paid"
- ✅ Paid amount = total amount
- ✅ Transaction có status = "Paid"

---

### Bước 8: Tải Hóa Đơn PDF

**Endpoint:** `GET /api/payment/receipt/{orderId}`

**Headers:**
```
Authorization: Bearer {token}
```

**Response:** PDF file (binary)

**Kiểm tra:**
- ✅ PDF được tải về thành công
- ✅ Tên file: `{orderCode}.pdf` (ví dụ: `RMS0000001.pdf`)
- ✅ PDF chứa đầy đủ thông tin:
  - Order code
  - Ngày giờ
  - Danh sách món ăn
  - Subtotal, VAT, Service Fee, Discount, Total
  - Phương thức thanh toán

**Lưu ý:**
- PDF được lưu tại: `wwwroot/receipts/{orderCode}.pdf`
- Nếu PDF chưa tồn tại, hệ thống sẽ tự động tạo

---

### Bước 9: Xác Minh Đơn Hàng Đã Được Cập Nhật

**Endpoint:** `GET /api/payment/orders/{orderId}/details`

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "orderId": 1,
  "orderCode": "RMS0000001",
  "status": "Paid",
  "totalAmount": 115000,
  ...
}
```

**Kiểm tra:**
- ✅ Order status = "Paid" hoặc "PAID"
- ✅ Total amount đã được tính đúng
- ✅ Có transaction với status = "Paid"

---

## 🧪 Test Cases

### Test Case 1: Thanh Toán Tiền Mặt Đầy Đủ

**Mục đích:** Test luồng thanh toán tiền mặt với số tiền chính xác.

**Các bước:**
1. Login
2. Lấy order detail (orderId = 1)
3. Lấy order summary
4. Thanh toán tiền mặt với `Amount = ReceivedAmount`
5. Kiểm tra status = Paid
6. Tải hóa đơn PDF
7. Verify order đã được cập nhật

**Kết quả mong đợi:**
- ✅ Payment thành công
- ✅ Order status = "Paid"
- ✅ PDF được tạo thành công
- ✅ Không có refund (vì số tiền chính xác)

---

### Test Case 2: Thanh Toán QR Đầy Đủ

**Mục đích:** Test luồng thanh toán QR từ đầu đến cuối.

**Các bước:**
1. Login
2. Lấy order detail (orderId = 2)
3. Lấy order summary
4. Tạo QR code
5. Xác nhận thanh toán QR
6. Kiểm tra status = Paid
7. Tải hóa đơn PDF
8. Verify order đã được cập nhật

**Kết quả mong đợi:**
- ✅ QR code được tạo thành công
- ✅ Payment được xác nhận thành công
- ✅ Order status = "Paid"
- ✅ PDF được tạo thành công

---

### Test Case 3: Thanh Toán Với Mã Giảm Giá

**Mục đích:** Test áp dụng mã giảm giá và thanh toán.

**Các bước:**
1. Login
2. Lấy order detail (orderId = 3)
3. Lấy order summary (ghi nhận total ban đầu)
4. Áp dụng mã giảm giá
5. Lấy lại order summary để xem discount đã được áp dụng
6. Thanh toán (Cash hoặc QR)
7. Kiểm tra total amount đã trừ discount
8. Tải hóa đơn PDF và verify discount được hiển thị

**Kết quả mong đợi:**
- ✅ Discount được áp dụng đúng
- ✅ Total amount đã được cập nhật (trừ discount)
- ✅ PDF hiển thị discount amount

---

### Test Case 4: Thanh Toán Tiền Mặt Với Tiền Thừa (Refund)

**Mục đích:** Test tính toán refund khi khách trả tiền thừa.

**Các bước:**
1. Login
2. Lấy order detail (orderId = 4)
3. Lấy order summary (total = 115000)
4. Thanh toán tiền mặt với `ReceivedAmount = 150000`
5. Kiểm tra `refundAmount = 35000` trong response
6. Kiểm tra status = Paid
7. Tải hóa đơn PDF

**Kết quả mong đợi:**
- ✅ Payment thành công
- ✅ Refund amount được tính đúng: `150000 - 115000 = 35000`
- ✅ Order status = "Paid"
- ✅ PDF được tạo thành công

---

### Test Case 5: Test Lỗi - Đơn Hàng Không Tồn Tại

**Mục đích:** Test xử lý lỗi khi order không tồn tại.

**Các bước:**
1. Login
2. Gọi API với orderId không tồn tại (ví dụ: 99999)
3. Kiểm tra response error

**Kết quả mong đợi:**
- ✅ Response: 404 Not Found
- ✅ Message: "Không tìm thấy đơn hàng với ID: 99999"

---

### Test Case 6: Test Lỗi - Thanh Toán Đơn Hàng Đã Thanh Toán

**Mục đích:** Test xử lý lỗi khi thanh toán đơn hàng đã được thanh toán.

**Các bước:**
1. Login
2. Lấy order detail (orderId đã được thanh toán)
3. Thử thanh toán lại

**Kết quả mong đợi:**
- ✅ Response: 400 Bad Request
- ✅ Message: "Đơn hàng đã được thanh toán"

---

## 🔍 Kiểm Tra Database

Sau khi test, kiểm tra database để verify:

### Bảng `Orders`
```sql
SELECT OrderId, OrderCode, Status, TotalAmount, UpdatedAt
FROM Orders
WHERE OrderId = {orderId}
```
- ✅ `Status` = "Paid"
- ✅ `TotalAmount` đã được cập nhật
- ✅ `UpdatedAt` đã được cập nhật

### Bảng `Transactions`
```sql
SELECT TransactionId, TransactionCode, OrderId, Amount, PaymentMethod, Status, CreatedAt
FROM Transactions
WHERE OrderId = {orderId}
```
- ✅ Có transaction mới được tạo
- ✅ `Status` = "Paid"
- ✅ `PaymentMethod` đúng với phương thức đã chọn
- ✅ `Amount` đúng với số tiền thanh toán

### Bảng `AuditLogs` (nếu có)
```sql
SELECT * FROM AuditLogs
WHERE EntityType = 'Order' AND EntityId = {orderId}
ORDER BY CreatedAt DESC
```
- ✅ Có log ghi nhận payment
- ✅ Có log ghi nhận receipt generation

### File PDF
Kiểm tra thư mục: `wwwroot/receipts/`
- ✅ File PDF được tạo: `{orderCode}.pdf`
- ✅ File PDF có thể mở được
- ✅ Nội dung PDF đúng với thông tin đơn hàng

---

## 📊 Checklist Test

### Backend API
- [ ] Login thành công
- [ ] Lấy danh sách đơn hàng chờ thanh toán
- [ ] Lấy chi tiết đơn hàng
- [ ] Lấy tóm tắt đơn hàng với tính toán tổng tiền
- [ ] Áp dụng mã giảm giá (nếu có)
- [ ] Thanh toán tiền mặt thành công
- [ ] Tạo QR code thành công
- [ ] Xác nhận thanh toán QR thành công
- [ ] Kiểm tra trạng thái thanh toán
- [ ] Tải hóa đơn PDF thành công
- [ ] Xử lý lỗi khi order không tồn tại
- [ ] Xử lý lỗi khi order đã được thanh toán

### Database
- [ ] Order status được cập nhật thành "Paid"
- [ ] Transaction được tạo trong database
- [ ] Transaction có status = "Paid"
- [ ] Audit log được ghi nhận

### File System
- [ ] PDF được tạo trong `wwwroot/receipts/`
- [ ] PDF có thể mở được
- [ ] Nội dung PDF đúng

---

## 🐛 Troubleshooting

### Lỗi 401 Unauthorized
**Nguyên nhân:** Token hết hạn hoặc không hợp lệ.
**Giải pháp:** Login lại để lấy token mới.

### Lỗi 404 Not Found
**Nguyên nhân:** OrderId không tồn tại trong database.
**Giải pháp:** Kiểm tra database và sử dụng orderId hợp lệ.

### Lỗi 400 Bad Request
**Nguyên nhân:** 
- Order đã được thanh toán
- Số tiền không hợp lệ
- Request body thiếu thông tin

**Giải pháp:** Kiểm tra lại request body và trạng thái đơn hàng.

### PDF Không Được Tạo
**Nguyên nhân:** 
- Thư mục `wwwroot/receipts/` không tồn tại
- Không có quyền ghi file

**Giải pháp:** 
- Tạo thư mục `wwwroot/receipts/` nếu chưa có
- Kiểm tra quyền ghi file

---

## 📚 Tài Liệu Tham Khảo

- [Payment Flow Documentation](./COMPLETE_PAYMENT_WORKFLOW.md)
- [PDF Receipt Generation](./PDF_RECEIPT_GENERATION.md)
- [QR VietQR Manual Confirmation](./QR_VIETQR_MANUAL_CONFIRMATION.md)
- [Payment Frontend-Backend Integration](./PAYMENT_FRONTEND_BACKEND_INTEGRATION.md)

---

## ✅ Kết Luận

Sau khi hoàn thành tất cả các test cases, bạn đã verify được:
1. ✅ Luồng thanh toán hoạt động đúng từ đầu đến cuối
2. ✅ Tính toán tổng tiền chính xác
3. ✅ Xử lý thanh toán tiền mặt và QR
4. ✅ Tạo và tải hóa đơn PDF thành công
5. ✅ Database được cập nhật đúng
6. ✅ Xử lý lỗi đúng cách

Nếu có bất kỳ vấn đề nào, hãy kiểm tra logs và database để debug.

