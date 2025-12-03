# 📋 Hướng Dẫn Seed Data cho Cashier Payment Workflow

## 🎯 Mục đích

Tạo dữ liệu test cho nhân viên thu ngân xử lý hóa đơn khách hàng với trạng thái **"waiting-confirmation"** (chờ khách xác nhận số lượng món đã dùng).

---

## 📦 Dữ liệu được tạo

### 1. **Orders (4 orders)**

| OrderId | Bàn | Khách | Món | Tổng tiền | Trạng thái | Đặc điểm |
|---------|-----|-------|-----|-----------|------------|----------|
| #1 | T01 | 4 người | 8 món | ~475K | waiting-confirmation | Standard order |
| #2 | T02 | 6 người | 17 món | ~825K | waiting-confirmation | Large order |
| #3 | T03 | 2 người | 4 món | ~140K | waiting-confirmation | Small order |
| #4 | T04 | 8 người | 24 món | ~1,025K | waiting-confirmation | Mixed billing (có món ConsumptionBased) |

### 2. **Menu Items (10 món)**

- **MainCourse**: Phở Bò (85K), Cơm Gà (95K), Bún Chả (75K)
- **Appetizer**: Bánh Mì (35K), Gỏi Cuốn (65K)
- **Beverage**: Trà Đào (45K), Cafe (35K), Nước Cam (40K), Coca (25K)
- **SideDish**: Salad Rau (55K) - **ConsumptionBased** ⚠️

### 3. **Test Accounts**

- **Cashier**: `cashier@test.com` / `Staff@123`
- **Customer**: `customer.test@example.com` / `Test@123`

### 4. **Tables & Reservations**

- 4 bàn: **T01, T02, T03, T04** (status: Occupied)
- 4 reservations tương ứng với status: **Guest Seated**

---

## 🚀 Cách sử dụng

### Option 1: Chạy SQL Script (Recommended)

#### **Bước 1:** Mở SQL Server Management Studio (SSMS)

#### **Bước 2:** Kết nối đến database `SapaFoRestRMSDb`

#### **Bước 3:** Mở file SQL
```
Backend/DataAccessLayer/Migrations/SeedCashierTestData.sql
```

#### **Bước 4:** Chạy script (F5 hoặc Execute)

✅ Script sẽ tự động:
- Cleanup dữ liệu test cũ
- Tạo Area, Tables, Customer, Cashier staff
- Tạo Menu items
- Tạo Reservations
- Tạo 4 Orders với status "waiting-confirmation"

---

### Option 2: Sử dụng C# DataSeeder

Nếu bạn muốn dùng code C# thay vì SQL:

```bash
cd Backend/SapaFoRestRMSAPI
dotnet run
```

Method `SeedCashierWorkflowTestAsync` sẽ tự động chạy khi app khởi động (đã config trong `Program.cs`).

---

## 🧪 Test Scenarios

### Scenario 1: Xem danh sách orders chờ xác nhận

**API:** `GET /api/Payment/orders?status=waiting-confirmation`

**Expected Result:**
```json
{
  "selectedDate": "2024-11-28",
  "totalOrders": 4,
  "pendingOrders": 4,
  "processedOrders": 0,
  "orders": [
    {
      "orderId": 1,
      "tableNumber": "T01",
      "customerName": "Khách Hàng Test",
      "status": "waiting-confirmation",
      "totalAmount": 475000,
      "createdAt": "..."
    },
    ...
  ]
}
```

---

### Scenario 2: Xem chi tiết order

**API:** `GET /api/Payment/orders/1/details`

**Expected Result:**
```json
{
  "orderId": 1,
  "tableNumber": "T01",
  "customerName": "Khách Hàng Test",
  "status": "waiting-confirmation",
  "items": [
    {
      "orderDetailId": 1,
      "itemName": "Phở Bò Đặc Biệt",
      "quantity": 2,
      "quantityUsed": null,
      "unitPrice": 85000,
      "totalPrice": 170000,
      "status": "Pending",
      "billingType": "FixedPrice"
    },
    ...
  ],
  "subtotal": 415000,
  "vatAmount": 41500,
  "serviceFee": 20750,
  "totalAmount": 477250
}
```

---

### Scenario 3: Khách xác nhận số lượng món

**API:** `PUT /api/Payment/orders/1/confirm`

**Request Body:**
```json
{
  "orderId": 1,
  "items": [
    {
      "orderDetailId": 1,
      "quantityUsed": 2,
      "isRemoved": false
    },
    {
      "orderDetailId": 2,
      "quantityUsed": 2,
      "isRemoved": false
    },
    {
      "orderDetailId": 3,
      "quantityUsed": 3,
      "isRemoved": false
    },
    {
      "orderDetailId": 4,
      "quantityUsed": 1,
      "isRemoved": false
    }
  ]
}
```

**Expected Result:**
- Order status chuyển sang: **"Confirmed"**
- OrderDetails status chuyển sang: **"Confirmed"**
- `QuantityUsed` được cập nhật

---

### Scenario 4: Xử lý thanh toán tiền mặt

**API:** `POST /api/Payment/cash-payment`

**Request Body:**
```json
{
  "orderId": 1,
  "amountReceived": 500000,
  "notes": "Khách đưa 500K"
}
```

**Expected Result:**
```json
{
  "transactionId": 1,
  "orderId": 1,
  "amount": 477250,
  "amountReceived": 500000,
  "refundAmount": 22750,
  "paymentMethod": "Cash",
  "status": "Paid"
}
```

- Order status chuyển sang: **"Paid"**
- Bàn T01 được giải phóng: status = **"Available"**

---

### Scenario 5: Test món ConsumptionBased (Order #4)

Order #4 có món **Salad Rau Trộn** (8 phần đặt) - **ConsumptionBased**

Khách có thể chỉ dùng 5/8 phần:

**Request:**
```json
{
  "orderId": 4,
  "items": [
    {
      "orderDetailId": 15,
      "quantityUsed": 5,
      "isRemoved": false
    }
  ]
}
```

**Kết quả:**
- Tính tiền 5 phần (không phải 8 phần đặt)
- Phù hợp cho món buffet, món ăn kèm, đồ nhậu

---

## 📊 Verification Queries

### Xem orders chờ xác nhận
```sql
SELECT 
    o.OrderId,
    t.TableNumber,
    o.Status,
    o.TotalAmount,
    COUNT(od.OrderDetailId) AS ItemCount,
    o.CreatedAt
FROM Orders o
LEFT JOIN Reservations r ON o.ReservationId = r.ReservationId
LEFT JOIN ReservationTables rt ON r.ReservationId = rt.ReservationId
LEFT JOIN Tables t ON rt.TableId = t.TableId
LEFT JOIN OrderDetails od ON o.OrderId = od.OrderId
WHERE o.Status = 'waiting-confirmation'
GROUP BY o.OrderId, t.TableNumber, o.Status, o.TotalAmount, o.CreatedAt
ORDER BY o.CreatedAt DESC;
```

### Xem chi tiết items của order
```sql
SELECT 
    od.OrderDetailId,
    mi.Name AS ItemName,
    mi.BillingType,
    od.Quantity,
    od.QuantityUsed,
    od.UnitPrice,
    od.Status
FROM OrderDetails od
INNER JOIN MenuItems mi ON od.MenuItemId = mi.MenuItemId
WHERE od.OrderId = 1;
```

### Xem bàn đang bận
```sql
SELECT 
    t.TableNumber,
    t.Status,
    o.OrderId,
    o.Status AS OrderStatus,
    o.TotalAmount
FROM Tables t
LEFT JOIN ReservationTables rt ON t.TableId = rt.TableId
LEFT JOIN Reservations r ON rt.ReservationId = r.ReservationId
LEFT JOIN Orders o ON r.ReservationId = o.ReservationId
WHERE t.Status = 'Occupied';
```

---

## 🔧 Troubleshooting

### Lỗi: Table already exists
```
Msg 2627, Level 14, State 1, Line XXX
Violation of PRIMARY KEY constraint...
```

**Giải pháp:** Chạy cleanup trước:
```sql
DELETE FROM Transactions WHERE OrderId IN (SELECT OrderId FROM Orders WHERE Status = 'waiting-confirmation');
DELETE FROM Payments WHERE OrderId IN (SELECT OrderId FROM Orders WHERE Status = 'waiting-confirmation');
DELETE FROM OrderDetails WHERE OrderId IN (SELECT OrderId FROM Orders WHERE Status = 'waiting-confirmation');
DELETE FROM Orders WHERE Status = 'waiting-confirmation';
```

### Lỗi: Foreign Key constraint violation
```
The DELETE statement conflicted with the REFERENCE constraint...
```

**Giải pháp:** Xóa theo đúng thứ tự (script đã handle):
1. Transactions
2. Payments
3. OrderDetails
4. Orders

---

## 📝 Notes

### ConsumptionBased vs FixedPrice

- **FixedPrice** (BillingType = 0): Tính tiền theo số lượng đặt (Quantity)
  - Ví dụ: Phở, Cơm, Bún - bếp đã nấu thì phải tính 100%

- **ConsumptionBased** (BillingType = 1): Tính tiền theo số lượng thực tế dùng (QuantityUsed)
  - Ví dụ: Salad buffet, rau ăn kèm - chỉ tính phần khách ăn

### Order Status Flow

```
waiting-confirmation 
    ↓ (Customer confirms items)
Confirmed
    ↓ (Cashier processes payment)
Paid
```

### Table Status Flow

```
Available
    ↓ (Guest seated)
Occupied
    ↓ (Payment completed)
Available
```

---

## 🎓 Related Documentation

- **Payment Flow**: `Docs/PAYMENT_FLOW_README.md`
- **API Documentation**: `Docs/SHIFT_MANAGEMENT_API_SUMMARY.md`
- **Order Confirmation**: `ORDER_CONFIRMATION_FEATURE_SUMMARY.md`

---

## 📞 Support

Nếu gặp vấn đề:
1. Check database constraints
2. Check audit logs: `SELECT * FROM AuditLogs ORDER BY CreatedAt DESC`
3. Check application logs
4. Liên hệ dev team

---

**Created:** 2025-01-15  
**Version:** 1.0  
**Status:** ✅ Ready for Testing

