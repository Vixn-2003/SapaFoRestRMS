# Seed Data cho Payment Flow Testing

## 📋 Mô tả

Script SQL này tạo dữ liệu test cho chức năng **Payment Flow** và **VietQR Payment** trong hệ thống SapaFoRest RMS.

## 🎯 Dữ liệu được tạo

### 1. **Orders chờ thanh toán (pending-payment)**
- **Order 3001**: 517,500 VND - Có đặt bàn (Reservation) - Subtotal: 450,000 VND
- **Order 3002**: 414,000 VND - Không đặt bàn - Subtotal: 360,000 VND
- **Order 3003**: 161,000 VND - Takeaway (mang đi) - Subtotal: 140,000 VND
- **Order 3005**: 828,000 VND - Order lớn - Subtotal: 720,000 VND

### 2. **Order đã thanh toán (Paid)**
- **Order 3004**: 258,750 VND - Đã thanh toán bằng tiền mặt - Subtotal: 225,000 VND

**Lưu ý**: TotalAmount = Subtotal + VAT (10%) + Service Fee (5%)

### 3. **MenuItems (Món ăn)**
- Phở Bò: 85,000 VND
- Bánh Mì Thịt Nướng: 45,000 VND
- Coca Cola: 25,000 VND
- Gỏi Cuốn: 80,000 VND
- Bún Chả: 90,000 VND

### 4. **Customers**
- Customer 1001: Khách hàng VIP (500 điểm)
- Customer 1002: Khách hàng thường (200 điểm)
- Customer 1003: Khách hàng mới (0 điểm)

### 5. **Reservations**
- Reservation 2001: Liên kết với Order 3001
- Reservation 2002: Dự phòng

### 6. **Tables**
- Bàn 1, Bàn 2, Bàn 3, Bàn 5

## 🚀 Cách sử dụng

### Bước 1: Chạy Script SQL

```sql
-- Mở SQL Server Management Studio (SSMS)
-- Kết nối đến database SapaFoRestRMS
-- Mở file: Backend/DatabaseScripts/20250115_SeedPaymentFlowData.sql
-- Execute (F5)
```

### Bước 2: Kiểm tra dữ liệu

```sql
-- Xem danh sách orders chờ thanh toán
SELECT OrderId, OrderType, TotalAmount, Status, CreatedAt
FROM Orders 
WHERE Status = 'pending-payment'
ORDER BY CreatedAt DESC;

-- Xem chi tiết order
SELECT o.OrderId, o.TotalAmount, o.Status,
       od.MenuItemId, mi.Name AS MenuItemName, od.Quantity, od.UnitPrice
FROM Orders o
INNER JOIN OrderDetails od ON o.OrderId = od.OrderId
INNER JOIN MenuItems mi ON od.MenuItemId = mi.MenuItemId
WHERE o.OrderId = 3001;
```

## 🧪 Test Cases

### Test Case 1: Thanh toán VietQR cho Order có Reservation
- **OrderId**: 3001
- **TotalAmount**: 517,500 VND (Subtotal: 450,000 VND)
- **Có Reservation**: Yes
- **Expected**: QR code hiển thị đúng số tiền, description có Order#RMS3001

### Test Case 2: Thanh toán VietQR cho Order không có Reservation
- **OrderId**: 3002
- **TotalAmount**: 414,000 VND (Subtotal: 360,000 VND)
- **Có Reservation**: No
- **Expected**: QR code hiển thị đúng, không có thông tin bàn

### Test Case 3: Thanh toán VietQR cho Order Takeaway
- **OrderId**: 3003
- **TotalAmount**: 161,000 VND (Subtotal: 140,000 VND)
- **OrderType**: Takeaway
- **Expected**: QR code hiển thị đúng cho order mang đi

### Test Case 4: Thanh toán VietQR cho Order lớn
- **OrderId**: 3005
- **TotalAmount**: 828,000 VND (Subtotal: 720,000 VND)
- **Expected**: QR code hiển thị số tiền lớn, có thể test với số tiền > 500k

## 📊 Cấu trúc dữ liệu

### Order 3001 (Pending Payment)
```
- Phở Bò x2: 170,000 VND
- Bánh Mì Thịt Nướng x1: 45,000 VND
- Coca Cola x3: 75,000 VND
- Gỏi Cuốn x2: 160,000 VND
-----------------------------------
Subtotal: 450,000 VND
VAT (10%): 45,000 VND
Service Fee (5%): 22,500 VND
Total: 517,500 VND
```

### Order 3002 (Pending Payment)
```
- Bún Chả x2: 180,000 VND
- Coca Cola x4: 100,000 VND
- Gỏi Cuốn x1: 80,000 VND
-----------------------------------
Subtotal: 360,000 VND
VAT (10%): 36,000 VND
Service Fee (5%): 18,000 VND
Total: 414,000 VND
```

## ⚠️ Lưu ý

1. **Script có thể chạy nhiều lần**: Script sử dụng `IF NOT EXISTS` để tránh duplicate data
2. **ID cố định**: Một số ID được set cố định (3001, 3002, etc.) để dễ test
3. **TotalAmount**: Có thể khác với tổng OrderDetails do tính VAT và Service Fee
4. **Password Hash**: Users được tạo với placeholder hash, cần update nếu muốn login

## 🔄 Reset dữ liệu (nếu cần)

```sql
-- Xóa dữ liệu test (cẩn thận!)
DELETE FROM Transactions WHERE OrderId IN (3001, 3002, 3003, 3004, 3005);
DELETE FROM Payments WHERE OrderId IN (3001, 3002, 3003, 3004, 3005);
DELETE FROM OrderDetails WHERE OrderId IN (3001, 3002, 3003, 3004, 3005);
DELETE FROM Orders WHERE OrderId IN (3001, 3002, 3003, 3004, 3005);
```

## 📝 Notes

- Script tạo dữ liệu phù hợp với logic tính toán trong `PaymentService.CalculateOrderAmounts()`
- Các orders được tạo với thời gian khác nhau để test sorting
- Một số orders có Reservation để test flow đầy đủ

