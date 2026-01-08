# Hướng dẫn Debug: Chart "Phân bố vai trò người dùng" bị trống

## Các bước kiểm tra

### 1. Kiểm tra Console Browser (F12)

Mở Developer Tools (F12) → Console tab và kiểm tra các log:

**Logs mong đợi:**
```
=== Admin Dashboard Data ===
Full dashboardData: { ... }
UserRoleDistribution: { RoleDistribution: { ... }, ... }
RoleDistribution: { "Admin": 2, "Manager": 5, ... }
Chart.js loaded: true
DOM Content Loaded - Initializing charts...
Checking User Role Distribution data...
Role Distribution Data: { "Admin": 2, "Manager": 5, ... }
Labels: ["Admin", "Manager", ...]
Data: [2, 5, ...]
User Role Chart initialized successfully
```

**Nếu thấy lỗi:**
- `Chart.js is not loaded!` → Chart.js CDN không load được
- `No user role data available` → Dictionary RoleDistribution rỗng hoặc tất cả giá trị = 0
- `Canvas element "userRoleChart" not found` → HTML element không tồn tại
- `UserRoleDistribution data is missing or invalid` → Dữ liệu không được truyền từ backend

### 2. Kiểm tra Network Request

**F12 → Network tab → Reload page**

Tìm request: `GET /api/admin/dashboard`

**Kiểm tra:**
- Status code: Phải là `200 OK`
- Response body: Phải có `userRoleDistribution.roleDistribution` với dữ liệu

**Ví dụ response đúng:**
```json
{
  "userRoleDistribution": {
    "roleDistribution": {
      "Admin": 2,
      "Manager": 5,
      "Staff": 15,
      "Cashier": 3,
      "Waiter": 8,
      "Kitchen": 6,
      "Owner": 1,
      "Customer": 120
    },
    "adminCount": 2,
    "managerCount": 5,
    ...
  }
}
```

**Nếu response rỗng:**
```json
{
  "userRoleDistribution": {
    "roleDistribution": {},
    "adminCount": 0,
    ...
  }
}
```
→ Vấn đề ở **Backend**: Database không có dữ liệu hoặc query không trả về kết quả

### 3. Kiểm tra Backend API

**Test trực tiếp API:**
```bash
GET http://localhost:5000/api/admin/dashboard
Headers:
  Authorization: Bearer <JWT_TOKEN>
```

**Hoặc dùng Postman/Thunder Client**

**Kiểm tra:**
- Response status: `200 OK`
- Response có `userRoleDistribution.roleDistribution` không?
- Dictionary có dữ liệu không?

### 4. Kiểm tra Database

**Query SQL để kiểm tra dữ liệu:**
```sql
-- Kiểm tra Users có RoleId không
SELECT 
    r.RoleName,
    COUNT(*) AS UserCount
FROM Users u
LEFT JOIN Roles r ON u.RoleId = r.RoleId
WHERE u.RoleId IS NOT NULL
GROUP BY r.RoleName;

-- Kiểm tra tổng số Users
SELECT COUNT(*) FROM Users;

-- Kiểm tra Roles
SELECT * FROM Roles;
```

**Nếu query trả về 0 rows:**
→ Database không có dữ liệu Users hoặc Users không có RoleId

### 5. Kiểm tra Code Backend

**File:** `Backend/DataAccessLayer/Repositories/AdminDashboardRepository.cs`

**Method:** `GetUsersByRoleAsync()`

**Kiểm tra:**
- Có exception nào không? (check Console/Logs)
- Return value có phải empty dictionary không?

**Thêm logging để debug:**
```csharp
public async Task<Dictionary<string, int>> GetUsersByRoleAsync()
{
    try
    {
        var userRoles = await _context.Users
            .Where(u => u.RoleId != null)
            .GroupBy(u => u.Role != null ? u.Role.RoleName : "Unknown")
            .Select(g => new { RoleName = g.Key ?? "Unknown", Count = g.Count() })
            .ToListAsync();
        
        // DEBUG: Log kết quả
        Console.WriteLine($"GetUsersByRoleAsync: Found {userRoles.Count} roles");
        foreach (var role in userRoles)
        {
            Console.WriteLine($"  - {role.RoleName}: {role.Count}");
        }
        
        return userRoles
            .GroupBy(x => x.RoleName)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in GetUsersByRoleAsync: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return new Dictionary<string, int>();
    }
}
```

### 6. Kiểm tra Frontend Controller

**File:** `Frontend/WebSapaForestForStaff/Controllers/AdminController.cs`

**Kiểm tra:**
- `_adminDashboardApiService.GetDashboardAsync()` có trả về null không?
- ViewModel có được tạo đúng không?

**Thêm logging:**
```csharp
[HttpGet]
public async Task<IActionResult> Index()
{
    var dashboard = await _adminDashboardApiService.GetDashboardAsync();
    
    // DEBUG
    if (dashboard == null)
    {
        Console.WriteLine("AdminController: Dashboard is null!");
    }
    else
    {
        Console.WriteLine($"AdminController: Dashboard received");
        Console.WriteLine($"  - UserRoleDistribution: {dashboard.UserRoleDistribution != null}");
        Console.WriteLine($"  - RoleDistribution count: {dashboard.UserRoleDistribution?.RoleDistribution?.Count ?? 0}");
    }
    
    // ... rest of code
}
```

### 7. Kiểm tra HTML Element

**F12 → Elements tab → Tìm `userRoleChart`**

**Kiểm tra:**
- Element `<canvas id="userRoleChart">` có tồn tại không?
- Element có nằm trong DOM không? (không bị ẩn bởi `@if` condition)
- Element có được render sau khi JavaScript chạy không?

**Nếu element không tồn tại:**
→ Check xem có nằm trong `@if (Model.Dashboard != null)` block không?

### 8. Kiểm tra Chart.js

**Console:**
```javascript
typeof Chart  // Phải trả về "function"
```

**Nếu undefined:**
- CDN không load được
- Check network tab xem có lỗi 404 không
- Thử thay CDN URL khác

## Các nguyên nhân phổ biến

### 1. Dictionary RoleDistribution rỗng
**Nguyên nhân:** Database không có Users hoặc Users không có RoleId
**Giải pháp:** 
- Thêm dữ liệu Users vào database
- Đảm bảo Users có RoleId

### 2. JSON Serialization Issue
**Nguyên nhân:** Dictionary<string, int> không được serialize đúng
**Giải pháp:** 
- Check `System.Text.Json` serializer options
- Đảm bảo property names match (case-sensitive)

### 3. Chart.js không load
**Nguyên nhân:** CDN bị block hoặc network issue
**Giải pháp:**
- Check network tab
- Thử CDN khác hoặc download Chart.js local

### 4. JavaScript Error
**Nguyên nhân:** Syntax error hoặc undefined variable
**Giải pháp:**
- Check Console tab cho errors
- Fix syntax errors

### 5. Timing Issue
**Nguyên nhân:** Chart.js chưa load khi code chạy
**Giải pháp:**
- Đã có `DOMContentLoaded` event listener
- Check xem Chart.js có load trước không

## Quick Fix Checklist

- [ ] Check Browser Console (F12) - có errors không?
- [ ] Check Network tab - API request có thành công không?
- [ ] Check API response - có `roleDistribution` data không?
- [ ] Check Database - có Users với RoleId không?
- [ ] Check HTML - canvas element có tồn tại không?
- [ ] Check Chart.js - có load được không?

## Test Script

Chạy script này trong Browser Console sau khi page load:

```javascript
// Check data
console.log('Dashboard Data:', dashboardData);
console.log('UserRoleDistribution:', dashboardData?.UserRoleDistribution);
console.log('RoleDistribution:', dashboardData?.UserRoleDistribution?.RoleDistribution);

// Check element
const canvas = document.getElementById('userRoleChart');
console.log('Canvas element:', canvas);

// Check Chart.js
console.log('Chart.js:', typeof Chart);

// Check chart instance
console.log('Chart instance:', charts?.userRole);
```

## Next Steps

Sau khi thêm debug logging, reload page và:
1. Mở Browser Console (F12)
2. Xem các log messages
3. Xác định bước nào fail
4. Fix theo hướng dẫn trên

