# Phân tích Logic Tính Doanh Thu Hệ Thống

## 📊 Tổng Quan

Hệ thống tính doanh thu từ 2 nguồn chính:
1. **Transactions** - Giao dịch thanh toán đơn hàng
2. **ReservationDeposits** - Tiền đặt cọc từ đặt bàn

---

## 🔍 Logic Tính Doanh Thu Hiện Tại

### 1. Nguồn Doanh Thu từ Transactions

**Điều kiện:**
- `Transaction.Status = "Paid"`
- `Transaction.CompletedAt.HasValue = true`
- `Transaction.CompletedAt.Date` nằm trong khoảng thời gian cần tính

**Công thức:**
```csharp
Revenue = Sum(Transaction.Amount) 
WHERE Status = "Paid" AND CompletedAt IS NOT NULL
```

**Vị trí áp dụng:**
- `OwnerRevenueService.BuildSummary()` - Line 57
- `AdminDashboardRepository.GetTodayRevenueAsync()` - Line 128-131
- `CounterStaffDashboardRepository.GetTodayRevenueAsync()` - Line 47-51
- `OwnerDashboardService.GetKpiCardsAsync()` - Line 74-77

---

### 2. Nguồn Doanh Thu từ ReservationDeposits

**Điều kiện (KHÔNG NHẤT QUÁN):**

#### ❌ AdminDashboardRepository (Line 134-136):
```csharp
// Tính TẤT CẢ deposits, KHÔNG check Reservation.Status
var depositRevenue = await _context.ReservationDeposits
    .Where(d => d.DepositDate.Date >= todayStart && d.DepositDate.Date <= todayEnd)
    .SumAsync(d => (decimal?)d.Amount) ?? 0m;
```

#### ✅ CounterStaffDashboardRepository (Line 54-60):
```csharp
// CHỈ tính deposits từ Reservation có Status = "Completed"
var depositRevenue = await _context.ReservationDeposits
    .Include(d => d.Reservation)
    .Where(d => d.DepositDate.Date >= todayStart.Date && 
               d.DepositDate.Date <= todayEnd.Date &&
               d.Reservation != null &&
               d.Reservation.Status == "Completed")
    .SumAsync(d => (decimal?)d.Amount) ?? 0m;
```

#### ❌ OwnerDashboardService (Line 80-82):
```csharp
// Tính TẤT CẢ deposits, KHÔNG check Reservation.Status
var todayDepositRevenue = deposits
    .Where(d => DateOnly.FromDateTime(d.DepositDate) == today)
    .Sum(d => d.Amount);
```

**Vấn đề:** Logic không nhất quán giữa các service/repository.

---

## ⚠️ VẤN ĐỀ NGHIÊM TRỌNG: TÍNH TRÙNG DOANH THU

### 🔴 Vấn đề 1: Combined Payment - TÍNH TRÙNG

**Mô tả:**
Khi thanh toán kết hợp (Cash + QR), hệ thống tạo **2 transactions riêng biệt**:
- Transaction 1: `PaymentMethod = "Cash"`, `Amount = CashAmount`, `Status = "Paid"`
- Transaction 2: `PaymentMethod = "QRBankTransfer"`, `Amount = QrAmount`, `Status = "Paid"`

**Ví dụ:**
- Order: 1,000,000 VND
- Combined Payment: 500,000 VND (Cash) + 500,000 VND (QR)
- Tạo 2 transactions:
  - TXN-1: Amount = 500,000, PaymentMethod = "Cash", Status = "Paid"
  - TXN-2: Amount = 500,000, PaymentMethod = "QRBankTransfer", Status = "Paid"

**Khi tính revenue:**
```csharp
var totalRevenue = transactions.Sum(t => t.Amount);
// = 500,000 + 500,000 = 1,000,000 ✅ ĐÚNG (tổng)
```

**NHƯNG khi filter theo PaymentMethod:**
```csharp
// Filter Cash only
var cashRevenue = transactions
    .Where(t => t.PaymentMethod == "Cash")
    .Sum(t => t.Amount);
// = 500,000 ✅ ĐÚNG

// Filter QR only  
var qrRevenue = transactions
    .Where(t => t.PaymentMethod == "QRBankTransfer")
    .Sum(t => t.Amount);
// = 500,000 ✅ ĐÚNG

// Filter Combined (không có transaction nào có PaymentMethod = "Combined")
var combinedRevenue = transactions
    .Where(t => t.PaymentMethod == "Combined")
    .Sum(t => t.Amount);
// = 0 ❌ SAI - không có transaction nào có PaymentMethod = "Combined"
```

**Vị trí code:**
- `PaymentService.ProcessCombinedPaymentAsync()` - Line 1375-1411
- `OwnerRevenueService.BuildSummary()` - Line 71-73 (tìm "Combined" nhưng không có)

---

### 🔴 Vấn đề 2: Split Bill - TÍNH TRÙNG NGHIÊM TRỌNG

**Mô tả:**
Khi chia hóa đơn, hệ thống tạo:
- **1 parent transaction** với `Amount = TotalAmount`, `Status = "Paid"` hoặc `"PartiallyPaid"`
- **N child transactions** với `Amount = PartAmount`, `Status = "Paid"` (nếu đã thanh toán)

**Ví dụ:**
- Order: 1,000,000 VND
- Split thành 2 phần: 500,000 VND + 500,000 VND
- Tạo transactions:
  - Parent: Amount = 1,000,000, Status = "Paid"
  - Child 1: Amount = 500,000, Status = "Paid"
  - Child 2: Amount = 500,000, Status = "Paid"

**Khi tính revenue:**
```csharp
var totalRevenue = transactions
    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
    .Sum(t => t.Amount);
// = 1,000,000 (Parent) + 500,000 (Child 1) + 500,000 (Child 2)
// = 2,000,000 ❌ SAI - TRÙNG 2 LẦN!
```

**Vị trí code:**
- `PaymentService.ProcessSplitBillAsync()` - Line 1722-1770
- Tất cả revenue calculation đều tính cả parent + child → TRÙNG

---

### 🟡 Vấn đề 3: Deposit Revenue - Logic Không Nhất Quán

**Mô tả:**
Có 3 cách tính deposit revenue khác nhau:

1. **AdminDashboardRepository**: Tính TẤT CẢ deposits (không check Reservation.Status)
2. **CounterStaffDashboardRepository**: Chỉ tính deposits từ Reservation.Status = "Completed"
3. **OwnerDashboardService**: Tính TẤT CẢ deposits (không check Reservation.Status)

**Vấn đề:**
- Nếu reservation bị hủy, deposit vẫn được tính vào revenue (theo AdminDashboardRepository)
- Logic không nhất quán giữa các service

---

## 📋 Tóm Tắt Vấn Đề

| Vấn đề | Mức độ | Mô tả | Vị trí |
|--------|--------|-------|--------|
| **Combined Payment** | 🟡 Trung bình | Không có transaction với PaymentMethod = "Combined", chỉ có Cash + QR riêng | `OwnerRevenueService.BuildSummary()` Line 71-73 |
| **Split Bill - Trùng** | 🔴 Nghiêm trọng | Tính cả parent + child → trùng 2 lần | Tất cả revenue calculation |
| **Deposit Logic** | 🟡 Trung bình | Logic không nhất quán giữa các service | AdminDashboardRepository vs CounterStaffDashboardRepository |

---

## ✅ Giải Pháp Đề Xuất

### 1. Sửa Split Bill - Loại bỏ parent transaction khỏi revenue

**Cách 1: Chỉ tính child transactions**
```csharp
var revenue = transactions
    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
    .Where(t => t.ParentTransactionId == null) // Chỉ tính transactions không phải child
    .Sum(t => t.Amount);
```

**Cách 2: Loại trừ parent transaction có PaymentMethod = "Split"**
```csharp
var revenue = transactions
    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
    .Where(t => t.PaymentMethod != "Split") // Loại bỏ parent transaction
    .Sum(t => t.Amount);
```

### 2. Sửa Combined Payment - Thêm transaction tổng hoặc filter đúng

**Cách 1: Tạo transaction tổng với PaymentMethod = "Combined"**
```csharp
// Tạo transaction tổng (không tính vào revenue, chỉ để tracking)
var combinedTransaction = new Transaction
{
    PaymentMethod = "Combined",
    Status = "Paid",
    Amount = totalAmount,
    // ... other fields
};
```

**Cách 2: Filter đúng khi tính revenue theo payment method**
```csharp
// Khi filter "Combined", tìm orders có cả Cash và QR transactions
var combinedOrders = transactions
    .GroupBy(t => t.OrderId)
    .Where(g => g.Any(t => t.PaymentMethod == "Cash") && 
                g.Any(t => t.PaymentMethod == "QRBankTransfer"))
    .SelectMany(g => g)
    .Sum(t => t.Amount);
```

### 3. Thống nhất logic tính Deposit Revenue

**Đề xuất:** Chỉ tính deposits từ Reservation có Status = "Completed"
```csharp
var depositRevenue = await _context.ReservationDeposits
    .Include(d => d.Reservation)
    .Where(d => d.DepositDate.Date >= startDate && 
               d.DepositDate.Date <= endDate &&
               d.Reservation != null &&
               d.Reservation.Status == "Completed")
    .SumAsync(d => (decimal?)d.Amount) ?? 0m;
```

---

## 📝 Ghi Chú

- Revenue được tính từ `Transaction.Amount`, không phải `Order.TotalAmount`
- Combined Payment tạo 2 transactions riêng, không có transaction tổng
- Split Bill tạo parent + child transactions, cần loại bỏ parent khi tính revenue
- Deposit revenue logic không nhất quán giữa các service

