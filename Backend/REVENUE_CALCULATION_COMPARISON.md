# So Sánh Logic Tính Doanh Thu: Admin vs CounterStaff vs Owner

## 📊 Tổng Quan So Sánh

| Tiêu chí | AdminDashboardRepository | CounterStaffDashboardRepository | OwnerDashboardService | OwnerRevenueService |
|----------|--------------------------|--------------------------------|----------------------|---------------------|
| **Transaction Filter** | ✅ Đúng | ✅ Đúng | ⚠️ Có fallback CreatedAt | ✅ Đúng |
| **Deposit Filter** | ❌ Tính TẤT CẢ | ✅ Chỉ Completed | ❌ Tính TẤT CẢ | ❌ Không tính |
| **Split Bill** | ❌ Tính cả parent | ❌ Tính cả parent | ❌ Tính cả parent | ❌ Tính cả parent |
| **Combined Payment** | ✅ Đúng (tổng 2 transactions) | ✅ Đúng (tổng 2 transactions) | ✅ Đúng (tổng 2 transactions) | ⚠️ Tìm "Combined" nhưng không có |

---

## 🔍 Chi Tiết Từng Service

### 1. AdminDashboardRepository.GetTodayRevenueAsync()

**File:** `Backend/DataAccessLayer/Repositories/AdminDashboardRepository.cs` (Line 121-139)

**Logic:**
```csharp
// Transactions: ✅ ĐÚNG
var transactionRevenue = await _context.Transactions
    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
    .Where(t => t.CompletedAt.Value.Date >= todayStart && t.CompletedAt.Value.Date <= todayEnd)
    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

// Deposits: ❌ SAI - Tính TẤT CẢ deposits, không check Reservation.Status
var depositRevenue = await _context.ReservationDeposits
    .Where(d => d.DepositDate.Date >= todayStart && d.DepositDate.Date <= todayEnd)
    .SumAsync(d => (decimal?)d.Amount) ?? 0m;
```

**Vấn đề:**
- ❌ Tính cả deposits từ reservations bị hủy
- ❌ Không loại bỏ Split Bill parent transactions

---

### 2. CounterStaffDashboardRepository.GetTodayRevenueAsync()

**File:** `Backend/DataAccessLayer/Repositories/CounterStaffDashboardRepository.cs` (Line 40-63)

**Logic:**
```csharp
// Transactions: ✅ ĐÚNG
var transactionRevenue = await _context.Transactions
    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
    .Where(t => t.CompletedAt.Value.Date >= todayStart.Date && 
                t.CompletedAt.Value.Date <= todayEnd.Date)
    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

// Deposits: ✅ ĐÚNG - Chỉ tính từ Reservation có Status = "Completed"
var depositRevenue = await _context.ReservationDeposits
    .Include(d => d.Reservation)
    .Where(d => d.DepositDate.Date >= todayStart.Date && 
               d.DepositDate.Date <= todayEnd.Date &&
               d.Reservation != null &&
               d.Reservation.Status == "Completed")
    .SumAsync(d => (decimal?)d.Amount) ?? 0m;
```

**Vấn đề:**
- ✅ Logic deposit ĐÚNG (chỉ tính từ Completed reservations)
- ❌ Không loại bỏ Split Bill parent transactions

**Kết luận:** ✅ **LOGIC ĐÚNG NHẤT** (chỉ thiếu filter Split Bill)

---

### 3. OwnerDashboardService.GetKpiCardsAsync()

**File:** `Backend/BusinessAccessLayer/Services/OwnerDashboardService.cs` (Line 66-133)

**Logic:**
```csharp
// Transactions: ⚠️ CÓ FALLBACK CreatedAt (có thể sai nếu CompletedAt null)
var todayTransactionRevenue = transactions
    .Where(t => t.Status == "Paid" && (t.CompletedAt.HasValue || t.CreatedAt != default))
    .Where(t => DateOnly.FromDateTime(t.CompletedAt ?? t.CreatedAt) == today)
    .Sum(t => t.Amount);

// Deposits: ❌ SAI - Tính TẤT CẢ deposits, không check Reservation.Status
var todayDepositRevenue = deposits
    .Where(d => DateOnly.FromDateTime(d.DepositDate) == today)
    .Sum(d => d.Amount);
```

**Vấn đề:**
- ⚠️ Fallback về CreatedAt nếu CompletedAt null (có thể không chính xác)
- ❌ Tính cả deposits từ reservations bị hủy
- ❌ Không loại bỏ Split Bill parent transactions

---

### 4. OwnerRevenueService.BuildSummary()

**File:** `Backend/BusinessAccessLayer/Services/OwnerRevenueService.cs` (Line 55-84)

**Logic:**
```csharp
// Transactions: ✅ ĐÚNG (nhưng không filter Split Bill)
var totalRevenue = transactions.Sum(t => t.Amount);

// Combined Payment: ⚠️ Tìm "Combined" nhưng không có transaction nào có PaymentMethod = "Combined"
var combinedRevenue = transactions
    .Where(t => t.PaymentMethod.Equals("Combined", StringComparison.OrdinalIgnoreCase))
    .Sum(t => t.Amount);
// → Luôn = 0 vì Combined Payment tạo 2 transactions riêng (Cash + QR)
```

**Vấn đề:**
- ❌ Không loại bỏ Split Bill parent transactions
- ⚠️ Combined Revenue luôn = 0 (vì không có transaction với PaymentMethod = "Combined")
- ❌ Không tính deposits

---

## 🔴 VẤN ĐỀ NGHIÊM TRỌNG: Split Bill - TÍNH TRÙNG

**Tất cả services đều mắc lỗi này!**

Khi Split Bill, hệ thống tạo:
- **1 parent transaction** với `PaymentMethod = "Split"`, `Amount = TotalAmount`, `Status = "Paid"`
- **N child transactions** với `ParentTransactionId != null`, `Amount = PartAmount`, `Status = "Paid"`

**Ví dụ:**
- Order: 1,000,000 VND
- Split thành 2 phần: 500,000 + 500,000
- Transactions:
  - Parent: Amount = 1,000,000, PaymentMethod = "Split", Status = "Paid"
  - Child 1: Amount = 500,000, ParentTransactionId = parent.Id, Status = "Paid"
  - Child 2: Amount = 500,000, ParentTransactionId = parent.Id, Status = "Paid"

**Khi tính revenue:**
```csharp
var revenue = transactions
    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
    .Sum(t => t.Amount);
// = 1,000,000 (Parent) + 500,000 (Child 1) + 500,000 (Child 2)
// = 2,000,000 ❌ SAI - TRÙNG 2 LẦN!
```

**Giải pháp:** Loại bỏ parent transactions khi tính revenue:
```csharp
var revenue = transactions
    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
    .Where(t => t.ParentTransactionId == null) // Chỉ tính transactions không phải child
    .Where(t => t.PaymentMethod != "Split") // Loại bỏ parent transaction
    .Sum(t => t.Amount);
```

---

## ✅ KẾT LUẬN: Bên Nào Đúng?

### 🏆 CounterStaffDashboardRepository - ĐÚNG NHẤT

**Lý do:**
1. ✅ Filter transactions đúng (Status = "Paid", CompletedAt.HasValue)
2. ✅ Filter deposits đúng (chỉ tính từ Reservation.Status = "Completed")
3. ❌ Thiếu filter Split Bill parent transactions

**Điểm số:** 8/10 (thiếu filter Split Bill)

---

### ❌ AdminDashboardRepository - SAI

**Lý do:**
1. ✅ Filter transactions đúng
2. ❌ Tính TẤT CẢ deposits (bao gồm cả reservations bị hủy)
3. ❌ Không loại bỏ Split Bill parent transactions

**Điểm số:** 5/10

---

### ❌ OwnerDashboardService - SAI

**Lý do:**
1. ⚠️ Fallback về CreatedAt (có thể không chính xác)
2. ❌ Tính TẤT CẢ deposits (bao gồm cả reservations bị hủy)
3. ❌ Không loại bỏ Split Bill parent transactions

**Điểm số:** 4/10

---

### ❌ OwnerRevenueService - SAI

**Lý do:**
1. ✅ Filter transactions đúng
2. ❌ Không tính deposits
3. ❌ Không loại bỏ Split Bill parent transactions
4. ⚠️ Combined Revenue luôn = 0 (logic sai)

**Điểm số:** 4/10

---

## 📋 Đề Xuất Sửa Lỗi

### 1. Thống nhất logic tính Transaction Revenue

**Tất cả services cần:**
```csharp
var transactionRevenue = await _context.Transactions
    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
    .Where(t => t.ParentTransactionId == null) // ✅ Loại bỏ child transactions
    .Where(t => t.PaymentMethod != "Split") // ✅ Loại bỏ parent Split transactions
    .Where(t => t.CompletedAt.Value.Date >= startDate && t.CompletedAt.Value.Date <= endDate)
    .SumAsync(t => (decimal?)t.Amount) ?? 0m;
```

### 2. Thống nhất logic tính Deposit Revenue

**Tất cả services cần:**
```csharp
var depositRevenue = await _context.ReservationDeposits
    .Include(d => d.Reservation)
    .Where(d => d.DepositDate.Date >= startDate && d.DepositDate.Date <= endDate)
    .Where(d => d.Reservation != null && d.Reservation.Status == "Completed") // ✅ Chỉ tính từ Completed
    .SumAsync(d => (decimal?)d.Amount) ?? 0m;
```

### 3. Sửa Combined Payment Logic

**OwnerRevenueService cần:**
```csharp
// Tìm orders có cả Cash và QR transactions trong cùng ngày
var combinedOrders = transactions
    .Where(t => t.ParentTransactionId == null && t.PaymentMethod != "Split")
    .GroupBy(t => t.OrderId)
    .Where(g => g.Any(t => t.PaymentMethod == "Cash") && 
                g.Any(t => t.PaymentMethod == "QRBankTransfer" || 
                          t.PaymentMethod == "QR" || 
                          t.PaymentMethod == "VietQR"))
    .SelectMany(g => g)
    .Sum(t => t.Amount);
```

---

## 📝 Tóm Tắt

| Service | Điểm | Vấn đề chính |
|---------|------|--------------|
| **CounterStaff** | 8/10 | ✅ Đúng nhất, chỉ thiếu filter Split Bill |
| **Admin** | 5/10 | ❌ Tính tất cả deposits |
| **Owner Dashboard** | 4/10 | ❌ Tính tất cả deposits + fallback CreatedAt |
| **Owner Revenue** | 4/10 | ❌ Không tính deposits + Combined logic sai |

**Kết luận:** CounterStaff có logic đúng nhất, nhưng tất cả đều cần sửa Split Bill filter.

