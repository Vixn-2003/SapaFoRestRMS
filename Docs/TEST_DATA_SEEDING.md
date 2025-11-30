# Test Data Seeding - Cashier Workflow

**Date:** 20/01/2025  
**Purpose:** Comprehensive test data for Cashier Payment Flow with various order statuses

---

## 📊 Overview

Seeder tạo **7 orders** với nhiều trạng thái khác nhau để test toàn bộ workflow:

| Order | Status | Description | Test Case |
|-------|--------|-------------|-----------|
| **Order 1** | `waiting-confirmation` | Menu items only | Customer Confirm flow |
| **Order 2** | `confirmed` | Combos + items | Payment flow |
| **Order 3** | `waiting-confirmation` | Simple order (2 items) | Customer Confirm (different table) |
| **Order 4** | `confirmed` | Combo only | Payment flow with combo |
| **Order 5** | `paid` | Already paid (today) | Receipt view, history |
| **Order 6** | `waiting-confirmation` | Many items (5+) | Edge case: bulk confirm |
| **Order 7** | `paid` | Old paid order (yesterday) | Historical data, reporting |

---

## 🎯 Chi Tiết Từng Order

### Order 1: Waiting Confirmation - Menu Items Only

**Basic Info:**
```
Status: "waiting-confirmation"
Table: B01
Created: 30 phút trước
Items: 3 menu items
```

**Order Details:**
```
- Item 1 (e.g., Steak): Quantity=2, Status="Pending"
- Item 2 (e.g., Tea): Quantity=1, Status="Pending"
- Item 3 (e.g., Hotpot): Quantity=1, Status="Pending"
```

**Use Cases:**
1. ✅ Test Customer Confirm flow từ đầu
2. ✅ Edit QuantityUsed
3. ✅ Add notes to items
4. ✅ Confirm → Status chuyển sang "confirmed"
5. ✅ After confirm: Test Undo Confirm
6. ✅ After confirm: Proceed to Payment

**Expected Behavior:**
- Hiển thị trong OrderSelection với badge "Chờ xác nhận" (màu vàng)
- Có thể edit Quantity và Notes
- Nút "Khách đã xác nhận" active
- Không có nút "Hoàn tác xác nhận" (chưa confirm)
- Không có nút "Thanh toán" (chưa confirm)

---

### Order 2: Confirmed - Combos + Individual Items

**Basic Info:**
```
Status: "confirmed"
ConfirmedAt: 5 phút trước
ConfirmedByStaffId: Cashier Staff
Table: B01
Created: 10 phút trước
Items: 2 combos + 2 individual items
```

**Order Details:**
```
- Combo 1 (Steak Dinner): Quantity=1, Status="Confirmed"
- Combo 2 (Family Hotpot): Quantity=1, Status="Confirmed"
- Item 4: Quantity=1, Status="Confirmed"
- Item 5: Quantity=2, Status="Confirmed"
```

**Use Cases:**
1. ✅ Test Undo Confirm flow
2. ✅ Test Payment flow (Cash/QR/Combined/Split)
3. ✅ Test combo display in payment screen
4. ✅ Verify totals calculation with combos

**Expected Behavior:**
- Badge "Đã xác nhận" (màu xanh)
- Có nút "Hoàn tác xác nhận" (với modal lý do)
- Có nút "Thanh toán" active
- Không thể edit Quantity/Notes (đã confirm)
- Clicking "Thanh toán" → Navigate to Payment.cshtml

---

### Order 3: Waiting Confirmation - Different Table

**Basic Info:**
```
Status: "waiting-confirmation"
Table: B02
Created: 20 phút trước
Items: 2 menu items
```

**Order Details:**
```
- Item 1: Quantity=1, Status="Pending"
- Item 2: Quantity=2, Status="Pending"
```

**Use Cases:**
1. ✅ Test multiple pending orders simultaneously
2. ✅ Verify table isolation (B01 vs B02)
3. ✅ Test simple order confirm
4. ✅ Test reservation on different table

**Expected Behavior:**
- Hiển thị cùng Order 1 trong OrderSelection
- Độc lập với Order 1 (table khác)
- Workflow giống Order 1

---

### Order 4: Confirmed - Combo Only

**Basic Info:**
```
Status: "confirmed"
ConfirmedAt: 3 phút trước
ConfirmedByStaffId: Cashier Staff
Table: B01
Created: 15 phút trước
Items: 1 combo (quantity=2)
```

**Order Details:**
```
- Combo 1 (Steak Dinner): Quantity=2, Status="Confirmed"
```

**Use Cases:**
1. ✅ Test payment with combo only (no individual items)
2. ✅ Verify combo pricing
3. ✅ Test receipt generation with combos
4. ✅ Edge case: Order chỉ có combo

**Expected Behavior:**
- Badge "Đã xác nhận"
- Payment flow bình thường
- Receipt hiển thị combo details

---

### Order 5: Paid - Today

**Basic Info:**
```
Status: "paid"
ConfirmedAt: 1 giờ trước
ConfirmedByStaffId: Cashier Staff
Table: B02
Created: 2 giờ trước
Paid: 30 phút trước (Cash)
Items: 2 menu items
```

**Order Details:**
```
- Item 1: Quantity=1, Status="Served"
- Item 2: Quantity=1, Status="Served"
```

**Payment Record:**
```
PaymentMethod: "Cash"
PaymentDate: 30 phút trước
FinalAmount: (calculated)
```

**Use Cases:**
1. ✅ Test Receipt view
2. ✅ Test PDF download
3. ✅ Verify paid orders in OrderSelection
4. ✅ Test "processed" status filter
5. ✅ Cannot undo/edit paid orders

**Expected Behavior:**
- Badge "Đã thanh toán" (màu xanh đậm)
- Chỉ có nút "Xem hóa đơn" và "Tải PDF"
- Không có nút edit/undo/payment
- Hiển thị trong tab "processed" của OrderSelection

---

### Order 6: Waiting Confirmation - Many Items (Edge Case)

**Basic Info:**
```
Status: "waiting-confirmation"
Table: B01
Created: 5 phút trước
Items: 5 menu items (quantities 1-5)
```

**Order Details:**
```
- Item 1: Quantity=1, Status="Pending"
- Item 2: Quantity=2, Status="Pending"
- Item 3: Quantity=3, Status="Pending"
- Item 4: Quantity=4, Status="Pending"
- Item 5: Quantity=5, Status="Pending"
Total: 15 items across 5 menu items
```

**Use Cases:**
1. ✅ Test bulk item confirmation
2. ✅ Test UI scrolling with many items
3. ✅ Verify calculation with large quantities
4. ✅ Edge case: Order với nhiều món
5. ✅ Performance test with multiple items

**Expected Behavior:**
- UI responsive với nhiều items
- Có thể edit tất cả items
- Total calculation chính xác
- Confirm flow bình thường

---

### Order 7: Paid - Yesterday (Historical Data)

**Basic Info:**
```
Status: "paid"
ConfirmedAt: 1 ngày trước
ConfirmedByStaffId: Cashier Staff
Table: B01
Created: 1 ngày trước
Paid: 1 ngày trước (QR)
Items: 1 combo + 1 menu item
```

**Order Details:**
```
- Combo 2 (Family Hotpot): Quantity=1, Status="Served"
- Item 1: Quantity=3, Status="Served"
```

**Payment Record:**
```
PaymentMethod: "QR"
PaymentDate: 1 ngày trước
FinalAmount: (calculated)
```

**Use Cases:**
1. ✅ Test date filter in OrderSelection
2. ✅ Test historical data view
3. ✅ Verify old orders don't interfere with today's workflow
4. ✅ Test reporting/analytics
5. ✅ Receipt for old orders

**Expected Behavior:**
- Không hiển thị khi filter = "Today"
- Hiển thị khi chọn date = "Yesterday"
- Read-only (đã paid)
- Receipt vẫn download được

---

## 🔄 Status Distribution

### By Status
```
waiting-confirmation: 3 orders (Order 1, 3, 6)
confirmed: 2 orders (Order 2, 4)
paid: 2 orders (Order 5, 7)
```

### By Date
```
Today:
  - waiting-confirmation: 3 orders
  - confirmed: 2 orders
  - paid: 1 order (Order 5)

Yesterday:
  - paid: 1 order (Order 7)
```

### By Table
```
Table B01: Orders 1, 2, 4, 6, 7
Table B02: Orders 3, 5
```

---

## 🧪 Test Scenarios

### Scenario 1: Full Workflow - Order 1
```
1. Open OrderSelection → See Order 1 (waiting-confirmation)
2. Click Order 1 → ConfirmOrder.cshtml
3. Edit QuantityUsed for items
4. Click "Khách đã xác nhận"
5. ✅ Status → "confirmed"
6. See "Hoàn tác xác nhận" button appear
7. Click "Thanh toán" → Payment.cshtml
8. Select payment method (Cash)
9. Confirm payment
10. ✅ Status → "paid"
11. View Receipt → Receipt.cshtml
12. Download PDF
```

---

### Scenario 2: Undo Confirm - Order 2
```
1. Open Order 2 (confirmed)
2. Click "Hoàn tác xác nhận"
3. Enter reason in modal
4. Submit
5. ✅ Status → "waiting-confirmation"
6. ✅ ConfirmedAt → NULL
7. ✅ OrderHistory record created
8. Items status → "Pending"
```

---

### Scenario 3: Payment with Combo - Order 4
```
1. Open Order 4 (confirmed, combo only)
2. Click "Thanh toán"
3. Verify combo displayed correctly
4. Verify pricing (combo price, not individual items)
5. Select QR payment
6. Generate QR code
7. Confirm payment
8. ✅ Status → "paid"
9. View receipt → Verify combo details
```

---

### Scenario 4: View Historical Data - Order 7
```
1. Open OrderSelection
2. Change date to "Yesterday"
3. ✅ See Order 7 in processed list
4. Click Order 7
5. ✅ See paid order details
6. ✅ All edit buttons disabled
7. Download receipt
8. ✅ Receipt generates successfully
```

---

### Scenario 5: Multiple Pending Orders - Orders 1, 3, 6
```
1. Open OrderSelection (Today)
2. ✅ See 3 waiting-confirmation orders
3. Confirm Order 1
4. ✅ Order 3 and 6 still pending
5. Navigate back to OrderSelection
6. ✅ Order 1 now in confirmed section
7. ✅ Orders 3, 6 still in pending
```

---

### Scenario 6: Edge Case - Bulk Items (Order 6)
```
1. Open Order 6 (5 items, 15 total quantity)
2. ✅ UI displays all items correctly
3. Edit QuantityUsed for all items
4. ✅ Totals recalculate correctly
5. Confirm order
6. ✅ All 5 items status → "Confirmed"
7. Proceed to payment
8. ✅ Payment amount correct
```

---

## 📋 Data Integrity Checks

### Before Seeding
```sql
-- Count existing test orders
SELECT Status, COUNT(*) 
FROM Orders 
WHERE Status IN ('waiting-confirmation', 'confirmed', 'pending-payment')
GROUP BY Status;
```

### After Seeding
```sql
-- Verify all orders created
SELECT 
    o.OrderId,
    o.Status,
    o.TotalAmount,
    o.ConfirmedAt,
    COUNT(od.OrderDetailId) as ItemCount,
    CASE WHEN p.PaymentId IS NOT NULL THEN 'Has Payment' ELSE 'No Payment' END as PaymentStatus
FROM Orders o
LEFT JOIN OrderDetails od ON o.OrderId = od.OrderId
LEFT JOIN Payments p ON o.OrderId = p.OrderId
WHERE o.CreatedAt >= CAST(GETDATE() AS DATE)
GROUP BY o.OrderId, o.Status, o.TotalAmount, o.ConfirmedAt, p.PaymentId
ORDER BY o.OrderId;
```

**Expected Result:**
```
OrderId | Status               | ItemCount | PaymentStatus
--------|---------------------|-----------|---------------
1       | waiting-confirmation | 3         | No Payment
2       | confirmed           | 4         | No Payment
3       | waiting-confirmation | 2         | No Payment
4       | confirmed           | 1         | No Payment
5       | paid                | 2         | Has Payment
6       | waiting-confirmation | 5         | No Payment
7       | paid                | 2         | Has Payment
```

---

## 🔍 Validation Queries

### Query 1: Check Order Statuses
```sql
SELECT Status, COUNT(*) as Count
FROM Orders
WHERE CreatedAt >= CAST(GETDATE() AS DATE) - 1
GROUP BY Status
ORDER BY Status;
```

**Expected:**
```
Status               | Count
---------------------|------
confirmed           | 2
paid                | 2
waiting-confirmation| 3
```

---

### Query 2: Check OrderDetail Statuses Alignment
```sql
SELECT 
    o.OrderId,
    o.Status as OrderStatus,
    od.Status as ItemStatus,
    COUNT(*) as ItemCount
FROM Orders o
INNER JOIN OrderDetails od ON o.OrderId = od.OrderId
WHERE o.CreatedAt >= CAST(GETDATE() AS DATE) - 1
GROUP BY o.OrderId, o.Status, od.Status
ORDER BY o.OrderId, od.Status;
```

**Expected:**
```
OrderId | OrderStatus          | ItemStatus | ItemCount
--------|---------------------|------------|----------
1       | waiting-confirmation | Pending    | 3
2       | confirmed           | Confirmed  | 4
3       | waiting-confirmation | Pending    | 2
4       | confirmed           | Confirmed  | 1
5       | paid                | Served     | 2
6       | waiting-confirmation | Pending    | 5
7       | paid                | Served     | 2
```

---

### Query 3: Check Payment Records
```sql
SELECT 
    o.OrderId,
    o.Status,
    p.PaymentMethod,
    p.PaymentDate,
    p.FinalAmount
FROM Orders o
LEFT JOIN Payments p ON o.OrderId = p.OrderId
WHERE o.CreatedAt >= CAST(GETDATE() AS DATE) - 1
ORDER BY o.OrderId;
```

**Expected:**
- Orders 1-4, 6: No payment records (NULL)
- Orders 5, 7: Have payment records with PaymentDate

---

## 🎓 Best Practices Demonstrated

### 1. Status Consistency
```csharp
// ✅ GOOD: Using constants
order.Status = OrderStatusConstants.WaitingConfirmation;
```

### 2. OrderDetail Status Alignment
```
Order: "waiting-confirmation" → Items: "Pending"
Order: "confirmed"           → Items: "Confirmed"
Order: "paid"                → Items: "Served"
```

### 3. Timestamp Management
```csharp
order.CreatedAt = DateTime.UtcNow.AddMinutes(-30);
order.ConfirmedAt = DateTime.UtcNow.AddMinutes(-5);
payment.PaymentDate = DateTime.UtcNow.AddMinutes(-30);
```

### 4. Payment Records
```
Only create Payment when Order.Status = "paid"
DO NOT create Payment for pending/confirmed orders
```

### 5. Diverse Test Data
```
- Simple orders (2-3 items)
- Complex orders (combos + items)
- Edge cases (many items, combos only)
- Historical data (different dates)
- Multiple tables
```

---

## 🚀 Running the Seeder

### Command
```powershell
dotnet run --project Backend/SapaFoRestRMSAPI
```

### Console Output
```
[14:30:00] === Starting cashier workflow seeding ===
[14:30:01] Cleaning 0 existing pending orders...
ℹ️ No existing pending orders to clean.
✅ Created sample area and table (B01).
✅ Created Order 1 (menu items only).
✅ Created Order 2 (combos + items).
✅ Created table B02 for Order 3.
✅ Created Order 3 (waiting-confirmation).
✅ Created Order 4 (confirmed with combo).
✅ Created Order 5 (paid).
ℹ️ Payment record created for Order 5 (already paid).
✅ Created Order 6 (waiting-confirmation, many items).
✅ Created Order 7 (old paid order).
ℹ️ Payment record created for Order 7 (old paid order).
🎯 Cashier workflow seeding finished. Orders created: 7.
📊 Order Status Summary:
   - waiting-confirmation: Orders 1, 3, 6 (3 orders)
   - confirmed: Orders 2, 4 (2 orders)
   - paid: Orders 5, 7 (2 orders)
   Total: 7 orders created for comprehensive testing.
```

---

## 🔗 Related Documents

- [ORDER_STATUS_WORKFLOW.md](./ORDER_STATUS_WORKFLOW.md) - Status transitions
- [ORDER_STATUS_FIX.md](./ORDER_STATUS_FIX.md) - Status standardization
- [DATASEEDER_FIX.md](./DATASEEDER_FIX.md) - Seeder improvements
- [DEBUG_UNDO_CONFIRM_ERROR.md](./DEBUG_UNDO_CONFIRM_ERROR.md) - Debug guide

---

**Created:** 20/01/2025  
**Last Updated:** 20/01/2025  
**Status:** ✅ Ready for Testing

