# 📚 SHIFT MANAGEMENT API - IMPLEMENTATION SUMMARY

**Ngày tạo:** 2025-01-15  
**Feature:** Quản lý Ca làm việc (Shift Management)  
**Architecture:** Layered Architecture (4 layers)

---

## 📋 MỤC LỤC

1. [Tổng quan](#1-tổng-quan)
2. [Các Layer đã implement](#2-các-layer-đã-implement)
3. [API Endpoints](#3-api-endpoints)
4. [Database Migration](#4-database-migration)
5. [Cách sử dụng](#5-cách-sử-dụng)
6. [Testing](#6-testing)

---

## 1. TỔNG QUAN

### Mục đích
Hệ thống quản lý ca làm việc cho thu ngân, bao gồm:
- Mở ca làm việc (khai báo tiền đầu ca)
- Kết ca làm việc (đối chiếu tiền cuối ca)
- Giao ca cho nhân viên khác
- Theo dõi doanh thu và thống kê theo ca

### Kiến trúc
```
Frontend (Razor View)
    ↓
Controller (ShiftManagementController)
    ↓
Service (ShiftManagementService)
    ↓
Repository (ShiftRepository)
    ↓
Database (SQL Server)
```

---

## 2. CÁC LAYER ĐÃ IMPLEMENT

### ✅ Layer 1: DOMAIN LAYER

**File:** `Backend/DomainAccessLayer/Models/Shift.cs`

**Extended Fields:**
```csharp
public decimal? OpeningBalance { get; set; }
public decimal? ClosingBalance { get; set; }
public string? OpeningDenominations { get; set; } // JSON
public string? ClosingDenominations { get; set; } // JSON
public string? Status { get; set; } // "Open", "Closed", "Handover"
public decimal? Difference { get; set; }
public string? Notes { get; set; }
public int? HandoverToStaffId { get; set; }
public string? HandoverNotes { get; set; }
public DateTime? HandoverTime { get; set; }
public string? PinCode { get; set; } // Encrypted
```

**Enum:** `Backend/DomainAccessLayer/Enums/ShiftStatus.cs`
```csharp
public enum ShiftStatus
{
    Open = 1,
    Closed = 2,
    Handover = 3
}
```

---

### ✅ Layer 2: DATA ACCESS LAYER

#### Repositories

**Interface:** `Backend/DataAccessLayer/Repositories/Interfaces/IShiftRepository.cs`

**Implementation:** `Backend/DataAccessLayer/Repositories/ShiftRepository.cs`

**Methods:**
- `GetCurrentOpenShiftAsync()` - Lấy ca đang mở
- `GetShiftsByDateAndStaffAsync()` - Lấy ca theo ngày & staff
- `GetShiftWithDetailsAsync()` - Lấy chi tiết ca
- `GetAllOpenShiftsAsync()` - Lấy tất cả ca đang mở
- `GetShiftHistoryAsync()` - Lấy lịch sử ca
- `HasOpenShiftAsync()` - Kiểm tra có ca đang mở không
- `GetShiftRevenueAsync()` - Tính doanh thu ca
- `GetShiftOrderCountAsync()` - Đếm số đơn hàng trong ca

#### Unit of Work

**Updated:** `Backend/DataAccessLayer/UnitOfWork/Interfaces/IUnitOfWork.cs`
```csharp
IShiftRepository Shifts { get; }
```

**Updated:** `Backend/DataAccessLayer/UnitOfWork/UnitOfWork.cs`
```csharp
public IShiftRepository Shifts => _shifts ??= new ShiftRepository(_context);
```

---

### ✅ Layer 3: BUSINESS ACCESS LAYER

#### DTOs

**File:** `Backend/BusinessAccessLayer/DTOs/ShiftManagement/ShiftDto.cs`

**Main DTOs:**
```csharp
- ShiftDto
- OpenShiftRequestDto
- CloseShiftRequestDto
- HandoverShiftRequestDto
- ShiftDashboardDto
- ShiftResponseDto
```

#### Service

**Interface:** `Backend/BusinessAccessLayer/Services/Interfaces/IShiftManagementService.cs`

**Implementation:** `Backend/BusinessAccessLayer/Services/ShiftManagementService.cs`

**Methods:**
```csharp
Task<ShiftDashboardDto?> GetCurrentShiftAsync(int staffId, CancellationToken ct);
Task<ShiftResponseDto> OpenShiftAsync(OpenShiftRequestDto request, CancellationToken ct);
Task<ShiftResponseDto> CloseShiftAsync(CloseShiftRequestDto request, CancellationToken ct);
Task<ShiftResponseDto> HandoverShiftAsync(HandoverShiftRequestDto request, CancellationToken ct);
Task<List<ShiftDto>> GetShiftHistoryAsync(int staffId, DateOnly fromDate, DateOnly toDate, CancellationToken ct);
Task<ShiftDto?> GetShiftDetailsAsync(int shiftId, CancellationToken ct);
Task<bool> HasOpenShiftAsync(int staffId, CancellationToken ct);
```

**Features:**
- ✅ Business validation
- ✅ Audit logging
- ✅ Automatic calculations (revenue, orders)
- ✅ PIN code encryption (TODO: implement proper hashing)

---

### ✅ Layer 4: API LAYER

**Controller:** `Backend/SapaFoRestRMSAPI/Controllers/ShiftManagementController.cs`

**Base Route:** `/api/ShiftManagement`

**Authorization:** `[Authorize]` - Requires authentication

---

## 3. API ENDPOINTS

### 1. GET `/api/ShiftManagement/current`
**Description:** Lấy ca làm việc hiện tại

**Request:**
- Header: `Authorization: Bearer {token}`

**Response:**
```json
{
  "shiftId": 1,
  "id": "CA20241127-001",
  "cashier": "Nguyễn Văn A",
  "startTime": "07:59",
  "currentTime": "08:15",
  "startDate": "22/11/2024",
  "openingBalance": 500000,
  "systemCash": 53723400,
  "systemCard": 3200000,
  "systemQR": 2150000,
  "totalRevenue": 53723400,
  "totalOrders": 3,
  "pendingOrders": 0,
  "status": "Open"
}
```

---

### 2. POST `/api/ShiftManagement/open`
**Description:** Mở ca làm việc mới

**Request Body:**
```json
{
  "staffId": 1,
  "openingBalance": 500000,
  "denominations": {
    "500": 0,
    "1000": 0,
    "5000": 20,
    "10000": 40,
    "20000": 0,
    "50000": 0,
    "100000": 0,
    "200000": 0,
    "500000": 0
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "Mở ca làm việc thành công",
  "data": { ... }
}
```

**Business Rules:**
- ❌ Không cho phép mở ca nếu đã có ca đang mở
- ✅ Tự động tính tổng số dư từ denominations
- ✅ Log audit event

---

### 3. POST `/api/ShiftManagement/close`
**Description:** Kết ca làm việc

**Request Body:**
```json
{
  "shiftId": 1,
  "closingBalance": 54000000,
  "denominations": {
    "500": 0,
    "1000": 0,
    "5000": 30,
    "10000": 50,
    "20000": 10,
    "50000": 20,
    "100000": 50,
    "200000": 0,
    "500000": 0
  },
  "difference": 276600,
  "notes": "Chênh lệch do làm tròn tiền lẻ"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Kết ca làm việc thành công",
  "data": { ... }
}
```

**Business Rules:**
- ❌ Chỉ có thể kết ca đang "Open"
- ✅ So sánh số dư thực tế vs hệ thống
- ✅ Log audit event với chênh lệch

---

### 4. POST `/api/ShiftManagement/handover`
**Description:** Giao ca cho nhân viên khác

**Request Body:**
```json
{
  "shiftId": 1,
  "handoverToStaffId": 2,
  "notes": "Có 2 đơn đang chờ khách quay lại",
  "pinCode": "123456"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Giao ca thành công",
  "data": { ... }
}
```

**Business Rules:**
- ❌ Chỉ có thể giao ca đang "Open"
- ❌ Nhân viên tiếp nhận không được có ca đang mở
- ✅ Tự động tạo ca mới cho nhân viên tiếp nhận
- ✅ Số dư đầu ca mới = Số dư ca cũ
- ✅ PIN code được mã hóa

---

### 5. GET `/api/ShiftManagement/history`
**Description:** Lấy lịch sử ca làm việc

**Query Parameters:**
- `fromDate` (optional): yyyy-MM-dd
- `toDate` (optional): yyyy-MM-dd

**Response:**
```json
[
  {
    "shiftId": 1,
    "staffId": 1,
    "staffName": "Nguyễn Văn A",
    "startTime": "2024-11-27T07:59:00",
    "endTime": "2024-11-27T16:00:00",
    "openingBalance": 500000,
    "closingBalance": 54000000,
    "status": "Closed",
    "totalRevenue": 53723400,
    "totalOrders": 3
  }
]
```

---

### 6. GET `/api/ShiftManagement/{shiftId}`
**Description:** Lấy chi tiết ca làm việc

**Response:**
```json
{
  "shiftId": 1,
  "staffId": 1,
  "staffName": "Nguyễn Văn A",
  "startTime": "2024-11-27T07:59:00",
  "endTime": "2024-11-27T16:00:00",
  "openingBalance": 500000,
  "closingBalance": 54000000,
  "difference": 276600,
  "notes": "Chênh lệch do làm tròn",
  "status": "Closed",
  "totalRevenue": 53723400,
  "totalOrders": 3
}
```

---

### 7. GET `/api/ShiftManagement/has-open-shift`
**Description:** Kiểm tra xem có ca đang mở không

**Response:**
```json
{
  "hasOpenShift": true
}
```

---

## 4. DATABASE MIGRATION

### SQL Migration Script

**File:** `Backend/DataAccessLayer/Migrations/AddShiftManagementFields.sql`

**Cách chạy:**

```bash
# Option 1: Chạy SQL script trực tiếp
sqlcmd -S localhost -d SapaFoRestRMSDb -i AddShiftManagementFields.sql

# Option 2: Mở SQL Server Management Studio và chạy script
```

**Columns được thêm:**
```sql
- OpeningBalance (DECIMAL(18,2))
- ClosingBalance (DECIMAL(18,2))
- OpeningDenominations (NVARCHAR(MAX))
- ClosingDenominations (NVARCHAR(MAX))
- Status (NVARCHAR(20), DEFAULT 'Open')
- Difference (DECIMAL(18,2))
- Notes (NVARCHAR(500))
- HandoverToStaffId (INT, FK to Staffs)
- HandoverNotes (NVARCHAR(500))
- HandoverTime (DATETIME)
- PinCode (NVARCHAR(255))
```

**Foreign Keys:**
```sql
FK_Shifts_Staff_HandoverTo: HandoverToStaffId → Staffs.StaffId
```

---

## 5. CÁCH SỬ DỤNG

### Frontend Integration

**URL:** `http://localhost:5054/CashierFlow/ShiftManagement`

**Menu:** Đã được thêm vào `_counterstaffLayout.cshtml`

### Flow

```
1. Mở ca (Open Shift)
   ↓
2. Làm việc (Processing Orders & Payments)
   ↓
3a. Kết ca (Close Shift)
    hoặc
3b. Giao ca (Handover Shift)
```

### Example Usage (C# - Frontend)

```csharp
// 1. Kiểm tra có ca đang mở không
var hasOpen = await ShiftAPI.HasOpenShiftAsync();

if (!hasOpen)
{
    // 2. Mở ca mới
    var openRequest = new OpenShiftRequestDto
    {
        StaffId = currentStaffId,
        OpeningBalance = 500000,
        Denominations = new Dictionary<int, int>
        {
            { 5000, 20 },
            { 10000, 40 }
        }
    };
    
    var result = await ShiftAPI.OpenShiftAsync(openRequest);
}

// 3. Lấy thông tin ca hiện tại
var currentShift = await ShiftAPI.GetCurrentShiftAsync();

// 4. Kết ca
var closeRequest = new CloseShiftRequestDto
{
    ShiftId = currentShift.ShiftId,
    ClosingBalance = 54000000,
    Denominations = calculatedDenominations,
    Difference = 276600,
    Notes = "Chênh lệch do làm tròn"
};

var closeResult = await ShiftAPI.CloseShiftAsync(closeRequest);
```

### Example Usage (JavaScript - AJAX)

```javascript
// Mở ca
const openShift = async () => {
    const response = await fetch('/api/ShiftManagement/open', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
            staffId: 1,
            openingBalance: 500000,
            denominations: { 5000: 20, 10000: 40 }
        })
    });
    
    const result = await response.json();
    console.log(result);
};

// Lấy ca hiện tại
const getCurrentShift = async () => {
    const response = await fetch('/api/ShiftManagement/current', {
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    
    const shift = await response.json();
    console.log(shift);
};
```

---

## 6. TESTING

### Manual Testing Steps

#### Test 1: Mở ca thành công
```bash
POST /api/ShiftManagement/open
Body: { "staffId": 1, "openingBalance": 500000, "denominations": {...} }
Expected: 200 OK, success: true
```

#### Test 2: Không cho phép mở ca trùng
```bash
POST /api/ShiftManagement/open (lần 2)
Expected: 400 Bad Request, message: "Bạn đang có ca làm việc đang mở"
```

#### Test 3: Lấy ca hiện tại
```bash
GET /api/ShiftManagement/current
Expected: 200 OK, shift data
```

#### Test 4: Kết ca thành công
```bash
POST /api/ShiftManagement/close
Body: { "shiftId": 1, "closingBalance": 54000000, "difference": 276600, ... }
Expected: 200 OK, success: true
```

#### Test 5: Giao ca thành công
```bash
POST /api/ShiftManagement/handover
Body: { "shiftId": 1, "handoverToStaffId": 2, "pinCode": "123456", ... }
Expected: 200 OK, success: true, ca mới tự động được tạo
```

### Postman Collection

**Import URL:** (Sẽ cung cấp sau)

### Unit Tests (TODO)

```csharp
// File: Tests/ShiftManagementServiceTests.cs
[Fact]
public async Task OpenShift_WithValidData_ReturnsSuccess() { }

[Fact]
public async Task OpenShift_WhenAlreadyHasOpenShift_ReturnsFailed() { }

[Fact]
public async Task CloseShift_WithValidData_ReturnsSuccess() { }

[Fact]
public async Task HandoverShift_CreatesNewShiftForNextStaff() { }
```

---

## 7. TODO / IMPROVEMENTS

### Security
- [ ] Implement proper PIN code hashing (BCrypt/PBKDF2)
- [ ] Add rate limiting for API endpoints
- [ ] Implement CSRF protection

### Features
- [ ] Email notification khi kết ca
- [ ] Export PDF báo cáo ca
- [ ] Dashboard thống kê theo ca
- [ ] Multi-currency support

### Performance
- [ ] Cache current shift data
- [ ] Optimize revenue calculation query
- [ ] Add indexes on Status, StartTime columns

### Testing
- [ ] Unit tests cho Service layer
- [ ] Integration tests cho API
- [ ] E2E tests cho Frontend

---

## 8. REFERENCES

- **Architecture Doc:** `Docs/DESIGN_PATTERNS_DOCUMENTATION.md`
- **Payment Flow:** `Docs/PAYMENT_FLOW_README.md`
- **API Standards:** `Docs/ARCHITECTURE_AUDIT_REPORT.md`

---

## 9. SUPPORT

Nếu có vấn đề, vui lòng:
1. Check logs trong `Backend/SapaFoRestRMSAPI/Logs/`
2. Check audit logs trong database table `AuditLogs`
3. Liên hệ team dev

---

**Created by:** AI Assistant  
**Last Updated:** 2025-01-15  
**Version:** 1.0

