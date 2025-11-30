# Payment Flow Demo Guide

## 📋 Overview

Hướng dẫn demo các tính năng Payment Flow đã được implement, bao gồm:
1. Cash Payment với validation (underpaid/overpaid)
2. Payment Status Polling
3. Error Handling UI
4. Retry Logic
5. Offline Caching
6. Split Bill UI

## 🚀 Setup

### 1. Chạy Migration
```bash
cd Backend/DataAccessLayer
dotnet ef database update --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext
```

### 2. Chạy Seed Data
```sql
-- Chạy script seed data demo
-- File: Backend/DatabaseScripts/20250115_SeedPaymentFlowDemoData.sql
```

### 3. Start Backend API
```bash
cd Backend/SapaFoRestRMSAPI
dotnet run
```

### 4. Start Frontend
```bash
cd Frontend/WebSapaForestForStaff
dotnet run
```

## 📊 Demo Data

Sau khi chạy seed data, bạn sẽ có:

### Orders để test:
- **Order 1001**: PendingPayment (517,500 VND) - Test Cash Payment
- **Order 1002**: PendingPayment (828,000 VND) - Test Split Bill
- **Order 1003**: PendingPayment (414,000 VND) - Test Status Polling (Đang bị lock)
- **Order 1004**: PaymentProcessing (322,000 VND) - Test Retry Logic (Có failed transaction)
- **Order 1005**: PartiallyPaid (690,000 VND) - Test Split Bill Completion

## 🎯 Demo Scenarios

### Scenario 1: Cash Payment - Underpaid
1. Vào `/Payment/OrderDetail?id=1001`
2. Click "Thanh toán tiền mặt"
3. Nhập số tiền < 517,500 VND (ví dụ: 500,000)
4. **Expected**: 
   - Hiển thị warning "⚠️ Số tiền chưa đủ!"
   - Button "Xác nhận thanh toán" bị disable
   - Audit log: `attempt_underpaid`

### Scenario 2: Cash Payment - Overpaid
1. Vào `/Payment/OrderDetail?id=1001`
2. Click "Thanh toán tiền mặt"
3. Nhập số tiền > 517,500 VND (ví dụ: 600,000)
4. **Expected**:
   - Hiển thị "Tiền thối lại: 82,500 VND"
   - Checkbox "Đã trả lại tiền thối" xuất hiện
   - Phải check checkbox mới được confirm
   - Transaction có `RefundAmount = 82,500`

### Scenario 3: Cash Payment - Exact Amount
1. Vào `/Payment/OrderDetail?id=1001`
2. Click "Thanh toán tiền mặt"
3. Nhập số tiền = 517,500 VND
4. **Expected**:
   - Không có warning
   - Button "Xác nhận thanh toán" enabled
   - Transaction status = "Paid"
   - Order status = "Paid"

### Scenario 4: Split Bill
1. Vào `/Payment/OrderDetail?id=1002`
2. Click "Chia hóa đơn"
3. Chọn "Chia đều" → Chọn 2 phần
4. **Expected**:
   - Mỗi phần = 414,000 VND
   - Có thể chọn payment method cho từng phần
   - Validate tổng = 828,000 VND
5. Click "Xác nhận chia hóa đơn"
6. **Expected**:
   - Tạo 1 parent transaction (Split)
   - Tạo 2 child transactions
   - Order status = "PartiallyPaid" hoặc "Paid" (nếu tất cả parts đã paid)

### Scenario 5: Payment Status Polling
1. Vào `/Payment/OrderDetail?id=1003`
2. Click "Thanh toán QR" hoặc bất kỳ online payment method
3. **Expected**:
   - Poll status mỗi 5 giây
   - Sau 60 giây → Hiển thị option "Xác nhận thủ công"
   - Nếu status = "Paid" → Reload page
   - Nếu status = "Failed" → Hiển thị retry option

### Scenario 6: Retry Logic
1. Vào `/Payment/OrderDetail?id=1004`
2. Order này đã có failed transaction
3. Click "Thử lại" (nếu có UI button)
4. **Expected**:
   - Retry count tăng lên
   - Status = "PaymentProcessing"
   - Tự động start polling lại

### Scenario 7: Order Locking
1. Vào `/Payment/OrderDetail?id=1003`
2. Order này đang bị lock
3. Thử thêm món hoặc thay đổi order
4. **Expected**:
   - Hiển thị message: "Đơn hàng này đang được xử lý thanh toán bởi người dùng khác"
   - Không thể thêm món

### Scenario 8: Offline Caching
1. Tắt mạng (Disconnect internet)
2. Thử thanh toán
3. **Expected**:
   - Hiển thị: "💾 Đã lưu tạm giao dịch cục bộ"
   - Transaction được lưu vào localStorage
4. Bật mạng lại
5. **Expected**:
   - Tự động sync khi page load
   - Hiển thị: "Đã đồng bộ X giao dịch từ offline cache"

## 🔍 Kiểm Tra Audit Logs

Sau mỗi action, kiểm tra bảng `AuditLogs`:
```sql
SELECT * FROM AuditLogs 
WHERE EntityId IN (1001, 1002, 1003, 1004, 1005)
ORDER BY CreatedAt DESC;
```

Các event types:
- `payment_attempt` - Khi bắt đầu thanh toán
- `attempt_underpaid` - Khi số tiền chưa đủ
- `payment_success` - Khi thanh toán thành công
- `payment_error` - Khi có lỗi
- `payment_retry` - Khi retry
- `order_locked` - Khi lock order
- `order_unlocked` - Khi unlock order
- `split_bill_processed` - Khi chia hóa đơn

## 📁 Files Created/Updated

### Frontend:
1. `Frontend/WebSapaForestForStaff/Views/Payment/_CashPaymentModal.cshtml` - Cash payment modal
2. `Frontend/WebSapaForestForStaff/Views/Payment/_SplitBillModal.cshtml` - Split bill modal
3. `Frontend/WebSapaForestForStaff/wwwroot/js/payment-flow.js` - Payment flow logic
4. `Frontend/WebSapaForestForStaff/wwwroot/js/split-bill.js` - Split bill logic
5. `Frontend/WebSapaForestForStaff/Views/Payment/OrderDetail.cshtml` - Updated với buttons mới

### Backend:
1. `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs` - Added 8 new endpoints
2. `Backend/DatabaseScripts/20250115_SeedPaymentFlowDemoData.sql` - Demo seed data

## 🧪 Testing Checklist

- [ ] Cash Payment - Underpaid validation
- [ ] Cash Payment - Overpaid với refund
- [ ] Cash Payment - Exact amount
- [ ] Split Bill - Equal split
- [ ] Split Bill - Custom split
- [ ] Payment Status Polling
- [ ] Retry Logic
- [ ] Order Locking
- [ ] Offline Caching
- [ ] Error Handling UI
- [ ] Audit Logging

## 🐛 Troubleshooting

### Issue: Modal không hiển thị
- **Fix**: Kiểm tra xem đã include `payment-flow.js` và `split-bill.js` chưa
- **Fix**: Kiểm tra Bootstrap 5 đã được load chưa

### Issue: API calls fail với 401
- **Fix**: Kiểm tra JWT token trong localStorage/sessionStorage
- **Fix**: Đảm bảo user đã login với role Staff/Manager/Owner

### Issue: Seed data không chạy
- **Fix**: Kiểm tra database connection string
- **Fix**: Đảm bảo các bảng Orders, OrderDetails, Transactions đã tồn tại
- **Fix**: Kiểm tra foreign key constraints

## 📝 Notes

- Tất cả payment actions đều được log vào `AuditLogs`
- Offline cache được lưu trong `localStorage` với key `payment_offline_cache`
- Order locks tự động expire sau 10 phút
- Payment polling timeout sau 60 giây

