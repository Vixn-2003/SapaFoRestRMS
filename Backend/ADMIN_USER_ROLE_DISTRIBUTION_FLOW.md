# Phân tích luồng HTTP: Phân bố vai trò người dùng (Admin Dashboard)

## Tổng quan
Tài liệu này mô tả chi tiết luồng xử lý HTTP request cho tính năng **"Phân bố vai trò người dùng"** trong Admin Dashboard tại URL: `http://localhost:5054/Admin`

## Luồng xử lý

### 1. Frontend Request (Browser → Frontend Controller)

**URL:** `GET http://localhost:5054/Admin`

**Controller:** `Frontend/WebSapaForestForStaff/Controllers/AdminController.cs`

```csharp
[Authorize(Policy = "Admin")]
[HttpGet]
public async Task<IActionResult> Index()
{
    // Gọi API service để lấy dữ liệu dashboard
    var dashboard = await _adminDashboardApiService.GetDashboardAsync();
    
    // Tạo ViewModel và trả về View
    var viewModel = new AdminDashboardViewModel
    {
        Dashboard = dashboard,
        TotalUsers = dashboard.KpiCards.TotalUsers,
        // ...
    };
    
    return View(viewModel);
}
```

**Điểm kiểm tra:**
- ✅ Authorization: Yêu cầu Policy "Admin"
- ✅ Dependency Injection: `IAdminDashboardApiService`

---

### 2. Frontend API Service Call

**Service:** `Frontend/WebSapaForestForStaff/Services/Api/AdminDashboardApiService.cs`

```csharp
public async Task<AdminDashboardDto?> GetDashboardAsync()
{
    var token = GetToken(); // Lấy JWT token từ HttpContext
    var client = GetAuthenticatedClient(); // HttpClient với Authorization header
    
    // Gọi Backend API
    var response = await client.GetAsync($"{GetApiBaseUrl()}/admin/dashboard");
    
    if (!response.IsSuccessStatusCode)
        return null;
    
    // Deserialize JSON response
    var content = await response.Content.ReadAsStringAsync();
    var dashboard = JsonSerializer.Deserialize<AdminDashboardDto>(content);
    
    return dashboard;
}
```

**HTTP Request gửi đến Backend:**
```
GET /api/admin/dashboard
Headers:
  Authorization: Bearer <JWT_TOKEN>
  Content-Type: application/json
```

**Điểm kiểm tra:**
- ✅ Base URL: Lấy từ `IConfiguration` (thường là `http://localhost:5000` hoặc từ appsettings.json)
- ✅ Authentication: JWT token được tự động thêm vào header
- ✅ Error Handling: Trả về `null` nếu request thất bại

---

### 3. Backend API Controller

**Controller:** `Backend/SapaFoRestRMSAPI/Controllers/AdminDashboardController.cs`

```csharp
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default)
    {
        try
        {
            var dashboard = await _dashboardService.GetDashboardDataAsync(ct);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Lỗi khi lấy dữ liệu dashboard",
                error = ex.Message
            });
        }
    }
}
```

**Điểm kiểm tra:**
- ✅ Route: `/api/admin/dashboard`
- ✅ Authorization: Yêu cầu Role "Admin"
- ✅ Dependency Injection: `IAdminDashboardService`

---

### 4. Business Logic Layer (Service)

**Service:** `Backend/BusinessAccessLayer/Services/AdminDashboardService.cs`

```csharp
public async Task<AdminDashboardDto> GetDashboardDataAsync(CancellationToken ct = default)
{
    // 1. Lấy dữ liệu từ Repository
    var usersByRole = await SafeExecuteAsync(
        () => _dashboardRepository.GetUsersByRoleAsync(), 
        new Dictionary<string, int>()
    );
    
    // 2. Build User Role Distribution DTO
    var roleDistribution = new UserRoleDistributionDto
    {
        RoleDistribution = usersByRole, // Dictionary<string, int>
        AdminCount = usersByRole.ContainsKey("Admin") ? usersByRole["Admin"] : 0,
        ManagerCount = usersByRole.ContainsKey("Manager") ? usersByRole["Manager"] : 0,
        StaffCount = usersByRole.ContainsKey("Staff") ? usersByRole["Staff"] : 0,
        CashierCount = usersByRole.ContainsKey("Cashier") ? usersByRole["Cashier"] : 0,
        WaiterCount = usersByRole.ContainsKey("Waiter") ? usersByRole["Waiter"] : 0,
        KitchenCount = usersByRole.ContainsKey("Kitchen") ? usersByRole["Kitchen"] : 0,
        OwnerCount = usersByRole.ContainsKey("Owner") ? usersByRole["Owner"] : 0,
        CustomerCount = usersByRole.ContainsKey("Customer") ? usersByRole["Customer"] : 0
    };
    
    // 3. Build final Dashboard DTO
    var dashboard = new AdminDashboardDto
    {
        UserRoleDistribution = roleDistribution,
        // ... other data
    };
    
    return dashboard;
}
```

**Điểm kiểm tra:**
- ✅ Error Handling: Sử dụng `SafeExecuteAsync` với fallback values
- ✅ Data Transformation: Chuyển đổi từ Dictionary sang DTO có cấu trúc
- ✅ Dependency Injection: `IAdminDashboardRepository`

---

### 5. Data Access Layer (Repository)

**Repository:** `Backend/DataAccessLayer/Repositories/AdminDashboardRepository.cs`

```csharp
public async Task<Dictionary<string, int>> GetUsersByRoleAsync()
{
    try
    {
        // Query database: Group users by Role
        var userRoles = await _context.Users
            .Where(u => u.RoleId != null)
            .GroupBy(u => u.Role != null ? u.Role.RoleName : "Unknown")
            .Select(g => new { 
                RoleName = g.Key ?? "Unknown", 
                Count = g.Count() 
            })
            .ToListAsync();
        
        // Handle potential duplicate keys by summing counts
        return userRoles
            .GroupBy(x => x.RoleName)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in GetUsersByRoleAsync: {ex.Message}");
        return new Dictionary<string, int>(); // Fallback: empty dictionary
    }
}
```

**SQL Query được tạo ra (tương đương):**
```sql
SELECT 
    COALESCE(r.RoleName, 'Unknown') AS RoleName,
    COUNT(*) AS Count
FROM Users u
LEFT JOIN Roles r ON u.RoleId = r.RoleId
WHERE u.RoleId IS NOT NULL
GROUP BY r.RoleName
```

**Điểm kiểm tra:**
- ✅ Database Context: `SapaFoRestRmsContext`
- ✅ Entity Framework: Sử dụng LINQ to Entities
- ✅ Null Handling: Xử lý trường hợp Role = null
- ✅ Error Handling: Trả về empty dictionary nếu có lỗi

---

### 6. Database Schema

**Tables liên quan:**
- `Users` (Id, RoleId, Status, ...)
- `Roles` (Id, RoleName, ...)

**Relationship:**
- `Users.RoleId` → `Roles.Id` (Foreign Key)

---

### 7. Response Flow (Backend → Frontend)

**Backend Response (JSON):**
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
    "staffCount": 15,
    "cashierCount": 3,
    "waiterCount": 8,
    "kitchenCount": 6,
    "ownerCount": 1,
    "customerCount": 120
  },
  "kpiCards": { ... },
  "revenueLast7Days": [ ... ],
  // ... other dashboard data
}
```

**Frontend nhận response:**
- `AdminDashboardApiService` deserialize JSON → `AdminDashboardDto`
- `AdminController` truyền DTO vào `AdminDashboardViewModel`
- View nhận `Model.Dashboard`

---

### 8. Frontend View Rendering

**View:** `Frontend/WebSapaForestForStaff/Views/Admin/Index.cshtml`

**HTML Section:**
```html
<div class="card">
    <div class="card-header">
        <i class="fas fa-users-cog me-2"></i>Phân bố vai trò người dùng
    </div>
    <div class="card-body">
        <div class="chart-container">
            <canvas id="userRoleChart"></canvas>
        </div>
    </div>
</div>
```

**JavaScript Data Serialization:**
```javascript
// Line 450: Serialize C# model to JavaScript object
var dashboardData = @Html.Raw(Json.Serialize(Model.Dashboard ?? new AdminDashboardDto()));

// Line 560-610: Initialize Chart.js
if (dashboardData.UserRoleDistribution && dashboardData.UserRoleDistribution.RoleDistribution) {
    const userRoleCtx = document.getElementById('userRoleChart');
    const roleData = dashboardData.UserRoleDistribution.RoleDistribution;
    const labels = Object.keys(roleData);
    const data = Object.values(roleData);
    
    charts.userRole = new Chart(userRoleCtx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: [ ... ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom' },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return `${label}: ${value} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}
```

**Điểm kiểm tra:**
- ✅ Chart Library: Chart.js (CDN)
- ✅ Data Binding: Razor `@Html.Raw(Json.Serialize(...))`
- ✅ Chart Type: Doughnut chart
- ✅ Tooltip: Hiển thị số lượng và phần trăm

---

## Sơ đồ luồng dữ liệu

```
┌─────────────┐
│   Browser   │
│  (User)     │
└──────┬──────┘
       │ GET /Admin
       ▼
┌─────────────────────────┐
│ AdminController         │
│ (Frontend MVC)          │
│ - Authorize(Policy)     │
└──────┬──────────────────┘
       │ GetDashboardAsync()
       ▼
┌─────────────────────────┐
│ AdminDashboardApiService│
│ (Frontend API Client)   │
│ - Get JWT Token         │
│ - HTTP GET Request      │
└──────┬──────────────────┘
       │ GET /api/admin/dashboard
       │ Headers: Authorization: Bearer <token>
       ▼
┌─────────────────────────┐
│ AdminDashboardController│
│ (Backend API)           │
│ - Authorize(Roles)      │
└──────┬──────────────────┘
       │ GetDashboardDataAsync()
       ▼
┌─────────────────────────┐
│ AdminDashboardService   │
│ (Business Logic)        │
│ - Transform Data        │
│ - Build DTOs            │
└──────┬──────────────────┘
       │ GetUsersByRoleAsync()
       ▼
┌─────────────────────────┐
│ AdminDashboardRepository│
│ (Data Access)           │
│ - LINQ Query            │
└──────┬──────────────────┘
       │ EF Core Query
       ▼
┌─────────────────────────┐
│   Database              │
│   (SQL Server)          │
│   - Users Table         │
│   - Roles Table         │
└─────────────────────────┘
       │
       │ Dictionary<string, int>
       │ { "Admin": 2, "Manager": 5, ... }
       ▼
┌─────────────────────────┐
│   Response Flow         │
│   (Backward)            │
└─────────────────────────┘
       │
       │ JSON Response
       ▼
┌─────────────────────────┐
│   View Rendering        │
│   - Razor HTML          │
│   - Chart.js            │
└─────────────────────────┘
       │
       ▼
┌─────────────┐
│   Browser   │
│  (Display)  │
└─────────────┘
```

---

## Các điểm cần kiểm tra

### 1. Authentication & Authorization
- [ ] JWT token có hợp lệ không?
- [ ] User có Role "Admin" không?
- [ ] Policy "Admin" có được cấu hình đúng không?

### 2. API Configuration
- [ ] Base URL của Backend API đúng chưa?
- [ ] CORS đã được cấu hình chưa?
- [ ] HttpClient timeout đã được set chưa?

### 3. Database
- [ ] Connection string đúng chưa?
- [ ] Users table có dữ liệu không?
- [ ] Roles table có dữ liệu không?
- [ ] Foreign key relationship đúng chưa?

### 4. Data Processing
- [ ] `GetUsersByRoleAsync()` có trả về dữ liệu không?
- [ ] Dictionary có được serialize đúng không?
- [ ] Chart.js có nhận được dữ liệu không?

### 5. Error Handling
- [ ] Exception có được log không?
- [ ] Fallback values có hoạt động không?
- [ ] User có thấy error message không?

---

## Các vấn đề tiềm ẩn

### 1. Null Reference Exception
**Vị trí:** `AdminDashboardRepository.GetUsersByRoleAsync()`
```csharp
// Có thể lỗi nếu u.Role == null
.GroupBy(u => u.Role != null ? u.Role.RoleName : "Unknown")
```
**Giải pháp:** ✅ Đã xử lý với null check

### 2. Empty Dictionary
**Vị trí:** `AdminDashboardService.GetDashboardDataAsync()`
```csharp
// Nếu repository trả về empty dictionary
var usersByRole = await SafeExecuteAsync(...);
// RoleDistribution sẽ là empty
```
**Giải pháp:** ✅ Frontend có check `if (dashboardData.UserRoleDistribution && ...)`

### 3. Chart.js Not Initialized
**Vị trí:** `Views/Admin/Index.cshtml` (JavaScript)
```javascript
// Nếu canvas element không tồn tại
const userRoleCtx = document.getElementById('userRoleChart');
if (!userRoleCtx) return; // ✅ Đã có check
```

### 4. JSON Serialization Issues
**Vị trí:** Dictionary serialization
```csharp
// Dictionary<string, int> có thể serialize không đúng format
RoleDistribution = usersByRole
```
**Giải pháp:** ✅ System.Text.Json tự động serialize Dictionary thành JSON object

---

## Testing Checklist

### Unit Tests
- [ ] `AdminDashboardRepository.GetUsersByRoleAsync()` với dữ liệu hợp lệ
- [ ] `AdminDashboardRepository.GetUsersByRoleAsync()` với Users không có RoleId
- [ ] `AdminDashboardRepository.GetUsersByRoleAsync()` với empty Users table
- [ ] `AdminDashboardService.GetDashboardDataAsync()` với repository trả về empty dictionary

### Integration Tests
- [ ] HTTP GET `/Admin` với user có Role Admin
- [ ] HTTP GET `/Admin` với user không có Role Admin (should return 403)
- [ ] HTTP GET `/api/admin/dashboard` với valid JWT token
- [ ] HTTP GET `/api/admin/dashboard` với invalid JWT token (should return 401)

### End-to-End Tests
- [ ] Load page `/Admin` → Chart hiển thị đúng dữ liệu
- [ ] Load page `/Admin` → Tooltip hiển thị đúng phần trăm
- [ ] Load page `/Admin` → Responsive trên mobile

---

## Performance Considerations

### 1. Database Query Optimization
**Hiện tại:**
```csharp
var userRoles = await _context.Users
    .Where(u => u.RoleId != null)
    .GroupBy(u => u.Role != null ? u.Role.RoleName : "Unknown")
    .Select(g => new { RoleName = g.Key, Count = g.Count() })
    .ToListAsync();
```

**Cải thiện có thể:**
- Thêm index trên `Users.RoleId`
- Sử dụng `Include()` để eager load Role nếu cần
- Cache kết quả nếu dữ liệu không thay đổi thường xuyên

### 2. API Response Size
- Dashboard API trả về toàn bộ dữ liệu (KPI, Revenue, Orders, Alerts, ...)
- Có thể tách thành các endpoint riêng nếu response quá lớn

### 3. Frontend Rendering
- Chart.js initialization chỉ chạy một lần khi page load
- Không có real-time update (cần refresh page để cập nhật)

---

## Kết luận

Luồng HTTP cho tính năng **"Phân bố vai trò người dùng"** hoạt động qua các lớp:
1. **Frontend Controller** → Xử lý HTTP request
2. **Frontend API Service** → Gọi Backend API với JWT authentication
3. **Backend API Controller** → Validate authorization
4. **Business Service** → Transform và build DTOs
5. **Repository** → Query database với Entity Framework
6. **Database** → Trả về dữ liệu
7. **Response Flow** → JSON serialization
8. **View Rendering** → Razor + Chart.js visualization

Tất cả các lớp đều có error handling và fallback mechanisms để đảm bảo ứng dụng không crash khi có lỗi.

