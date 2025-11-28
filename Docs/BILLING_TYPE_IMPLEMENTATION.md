# Billing Type Implementation - Phân loại món theo cách tính tiền

## 📋 Tổng quan

Đã implement thành công hệ thống phân loại món ăn theo 2 loại hình tính tiền:

### **(A) ConsumptionBased - Món tiêu hao theo thực tế**
- Bia lon/chai, nước ngọt, khăn lạnh, khăn ướt, bia tươi
- **Tính tiền theo số lượng thực tế khách sử dụng (QuantityUsed)**
- Khách chỉ thanh toán những gì đã dùng

### **(B) KitchenPrepared - Món chế biến trong bếp**  
- Lẩu, steak, rau, cơm, món nóng/món nấu
- **Tính tiền theo số lượng đã đặt (Quantity) - 100%**
- Nếu bếp đã nấu thì phải thanh toán đủ

---

## ✅ Các thay đổi đã thực hiện

### 1. **Tạo Enum mới** ✓
**File:** `Backend/DomainAccessLayer/Enums/ItemBillingType.cs`

```csharp
public enum ItemBillingType
{
    ConsumptionBased = 1,  // Tính theo SL dùng
    KitchenPrepared = 2    // Tính theo SL đặt (100%)
}
```

---

### 2. **Cập nhật MenuItem Model** ✓
**File:** `Backend/DomainAccessLayer/Models/MenuItem.cs`

**Thêm property:**
```csharp
public ItemBillingType BillingType { get; set; } = ItemBillingType.KitchenPrepared;
```

**Mặc định:** `KitchenPrepared` (giữ nguyên logic cũ cho các món đã có)

---

### 3. **Cập nhật OrderDetail Model** ✓
**File:** `Backend/DomainAccessLayer/Models/OrderDetail.cs`

**Thêm property:**
```csharp
public int? QuantityUsed { get; set; }  // Nullable - chỉ set khi khách confirm
```

**Logic:**
- `Quantity`: Số lượng đặt ban đầu (KHÔNG thay đổi)
- `QuantityUsed`: Số lượng thực tế khách dùng (cập nhật khi confirm)

---

### 4. **Cập nhật DbContext Configuration** ✓
**File:** `Backend/DataAccessLayer/Dbcontext/SapaFoRestRmsContext.cs`

**MenuItem configuration:**
```csharp
entity.Property(e => e.BillingType)
    .HasDefaultValue(ItemBillingType.KitchenPrepared)
    .HasConversion<int>()  // Store as int in DB
    .IsRequired();
```

**OrderDetail configuration:**
```csharp
entity.Property(e => e.QuantityUsed)
    .HasDefaultValue(null)
    .IsRequired(false);
```

---

### 5. **FIX BUG trong ConfirmOrderAsync** ✓
**File:** `Backend/BusinessAccessLayer/Services/PaymentService.cs`

#### ❌ **Logic CŨ (SAI):**
```csharp
// GHI ĐÈ Quantity bằng QuantityUsed - WRONG!
detail.Quantity = confirmed.QuantityUsed;
```

#### ✅ **Logic MỚI (ĐÚNG):**
```csharp
if (confirmed.IsRemoved)
{
    detail.Quantity = 0;
    detail.QuantityUsed = 0;
    detail.Status = "Removed";
}
else
{
    // GIỮ NGUYÊN Quantity (SL đặt)
    // CHỈ cập nhật QuantityUsed (SL dùng)
    detail.QuantityUsed = confirmed.QuantityUsed;
    detail.Status = "Confirmed";
}
```

---

### 6. **Cập nhật Logic Tính Tiền** ✓
**Files:** 
- `Backend/BusinessAccessLayer/Services/PaymentService.cs`
- `Backend/BusinessAccessLayer/Services/ReceiptService.cs`

#### **Method: `CalculateOrderAmounts()`**

```csharp
foreach (var od in order.OrderDetails)
{
    if (od.Status == "Removed") continue;
    
    int billableQuantity;
    
    // ✅ LOGIC MỚI: Phân biệt 2 loại món
    if (od.MenuItem?.BillingType == ItemBillingType.ConsumptionBased)
    {
        // (A) Món tiêu hao: Tính theo SL dùng
        billableQuantity = od.QuantityUsed ?? od.Quantity;
    }
    else
    {
        // (B) Món bếp: LUÔN tính theo SL đặt (100%)
        billableQuantity = od.Quantity;
    }
    
    subtotal += od.UnitPrice * billableQuantity;
}
```

**Ưu điểm:**
- ✅ Backward compatible (fallback về Quantity nếu QuantityUsed = null)
- ✅ Món cũ vẫn hoạt động bình thường (mặc định KitchenPrepared)
- ✅ Logic rõ ràng, dễ maintain

---

### 7. **Database Migration** ✓
**File:** `Backend/DataAccessLayer/Migrations/20251126000000_AddBillingTypeAndQuantityUsed.cs`

**Thay đổi:**
- ➕ Thêm column `BillingType` (int) vào bảng `MenuItems` - Default: 2 (KitchenPrepared)
- ➕ Thêm column `QuantityUsed` (int, nullable) vào bảng `OrderDetails`

---

## 🚀 Cách sử dụng

### **Bước 1: Chạy Migration**
```bash
cd Backend/DataAccessLayer
dotnet ef database update
```

### **Bước 2: Cấu hình món ăn**
Trong admin panel/database, set `BillingType` cho từng món:
- `1` = ConsumptionBased (bia, nước, khăn)
- `2` = KitchenPrepared (món nấu, mặc định)

### **Bước 3: Workflow thanh toán**

1. **Waiter nhập đơn:**
   - Khách đặt 10 ly bia → `Quantity = 10`, `QuantityUsed = null`

2. **Cashier xác nhận với khách:**
   - Khách chỉ dùng 7 ly → Update `QuantityUsed = 7`
   - `Quantity = 10` vẫn giữ nguyên (để audit)

3. **Hệ thống tính tiền:**
   - Nếu món là `ConsumptionBased` → Tính 7 ly (theo `QuantityUsed`)
   - Nếu món là `KitchenPrepared` → Tính 10 ly (theo `Quantity`)

---

## 📊 So sánh trước/sau

| Khía cạnh | Trước | Sau |
|-----------|-------|-----|
| **Phân loại món** | ❌ Không có | ✅ 2 loại: ConsumptionBased / KitchenPrepared |
| **Lưu SL đặt** | ⚠️ Bị ghi đè | ✅ Giữ nguyên trong `Quantity` |
| **Lưu SL dùng** | ❌ Không có | ✅ Có trong `QuantityUsed` |
| **Tính tiền** | ⚠️ Luôn theo Quantity | ✅ Theo BillingType |
| **Bug confirm** | ❌ Ghi đè Quantity | ✅ Đã fix |

---

## 🎯 Lợi ích

1. ✅ **Chính xác nghiệp vụ:** Phản ánh đúng cách tính tiền của nhà hàng
2. ✅ **Audit trail:** Giữ nguyên `Quantity` để biết khách đặt bao nhiêu
3. ✅ **Linh hoạt:** Dễ dàng thêm loại mới (VD: Partially-Consumed)
4. ✅ **Backward compatible:** Món cũ vẫn hoạt động bình thường
5. ✅ **Code rõ ràng:** Logic dễ hiểu, dễ maintain

---

## ⚠️ Lưu ý quan trọng

### **1. Data migration cho món hiện có**
Tất cả món hiện có sẽ mặc định là `KitchenPrepared` (=2). Cần review và update lại các món là ConsumptionBased:

```sql
-- Update các món là consumption-based
UPDATE MenuItems 
SET BillingType = 1 
WHERE Name IN ('Bia Saigon lon', 'Bia Tiger', 'Coca Cola', 'Pepsi', 'Khăn lạnh', 'Khăn ướt', 'Bia tươi');
```

### **2. Frontend cần update**
Frontend đã có `QuantityUsed` trong DTO và View (đã fix trước đó), nhưng có thể cần:
- Thêm UI để admin set `BillingType` cho món
- Hiển thị icon/badge để phân biệt 2 loại món

### **3. Testing scenarios**

#### Test Case 1: ConsumptionBased item
- Đặt 5 bia → Quantity=5, QuantityUsed=null
- Khách confirm dùng 3 → QuantityUsed=3
- **Kỳ vọng:** Tính tiền 3 bia

#### Test Case 2: KitchenPrepared item  
- Đặt 2 bát lẩu → Quantity=2, QuantityUsed=null
- Khách confirm → QuantityUsed=2
- **Kỳ vọng:** Tính tiền 2 bát (100%)

#### Test Case 3: Mixed order
- 5 bia (Consumption) + 2 lẩu (Kitchen)
- Khách dùng 3 bia
- **Kỳ vọng:** Tính 3 bia + 2 lẩu

---

## 🔧 Troubleshooting

### Lỗi: "MenuItem.BillingType not found"
- **Nguyên nhân:** Chưa chạy migration
- **Giải pháp:** `dotnet ef database update`

### Lỗi: "Column QuantityUsed does not exist"
- **Nguyên nhân:** Migration chưa apply
- **Giải pháp:** Chạy migration hoặc add column thủ công:
```sql
ALTER TABLE OrderDetails ADD QuantityUsed INT NULL;
```

### Tính tiền sai cho món ConsumptionBased
- **Kiểm tra:** `MenuItem.BillingType` = 1 chưa?
- **Kiểm tra:** `OrderDetail.QuantityUsed` có giá trị chưa?
- **Debug:** Set breakpoint tại `CalculateOrderAmounts()` line 414

---

## 📝 Tổng kết

**Đã hoàn thành:**
- [x] Tạo enum ItemBillingType
- [x] Thêm BillingType vào MenuItem
- [x] Thêm QuantityUsed vào OrderDetail  
- [x] Cập nhật DbContext configuration
- [x] Fix bug trong ConfirmOrderAsync
- [x] Cập nhật logic tính tiền
- [x] Tạo database migration

**Tác động:**
- ✅ Backend: Hoàn chỉnh
- ✅ Database: Migration ready
- ✅ Business Logic: Đã implement
- ⚠️ Frontend: Cần thêm UI quản lý BillingType (optional)

**Thời gian implement:** ~2 giờ  
**Files thay đổi:** 7 files  
**Lines of code:** ~150 lines

---

## 👨‍💻 Tác giả
Implemented by: AI Assistant  
Date: November 26, 2025  
Version: 1.0

