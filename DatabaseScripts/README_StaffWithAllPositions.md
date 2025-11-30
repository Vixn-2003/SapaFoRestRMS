# Seed Data: Staff với tất cả các Positions

## 📋 Mô tả

Script này tạo các staff accounts với từng position khác nhau để testing login redirect và position-based routing.

## 🎯 Dữ liệu được tạo

### Staff Accounts:

1. **Cashier** (cashier@test.com)
   - Position: Cashier
   - Password: `Staff@123`
   - **Expected Redirect**: `/Payment/OrderSelection` (vì có position Cashier)

2. **Waiter/Waitress** (waiter@test.com)
   - Position: Waiter/Waitress
   - Password: `Staff@123`
   - **Expected Redirect**: `/TableManage/Index` (default staff redirect)

3. **Kitchen Staff** (kitchen@test.com)
   - Position: Kitchen Staff
   - Password: `Staff@123`
   - **Expected Redirect**: `/TableManage/Index` (default staff redirect)

4. **Inventory Staff** (inventory@test.com)
   - Position: Inventory Staff
   - Password: `Staff@123`
   - **Expected Redirect**: `/TableManage/Index` (default staff redirect)

## 🚀 Cách sử dụng

### Option 1: Sử dụng C# DataSeeder (Recommended)

Method `SeedStaffWithAllPositionsAsync` sẽ tự động được gọi khi ứng dụng khởi động (trong `Program.cs`).

**Chạy ứng dụng:**
```bash
cd Backend/SapaFoRestRMSAPI
dotnet run
```

Method sẽ tự động:
- Tạo positions nếu chưa có
- Tạo staff accounts với từng position
- Assign positions cho staff

### Option 2: Sử dụng SQL Script

**Bước 1:** Mở SQL Server Management Studio (SSMS)

**Bước 2:** Kết nối đến database `SapaFoRestRMS`

**Bước 3:** Mở và chạy file:
```
Backend/DatabaseScripts/SeedStaffWithAllPositions.sql
```

**Bước 4:** Verify data:
```sql
SELECT 
    u.UserId,
    u.FullName,
    u.Email,
    u.Phone,
    r.RoleName,
    s.StaffId,
    STRING_AGG(p.PositionName, ', ') AS Positions
FROM Users u
INNER JOIN Roles r ON u.RoleId = r.RoleId
INNER JOIN Staffs s ON u.UserId = s.UserId
LEFT JOIN StaffPositions sp ON s.StaffId = sp.StaffId
LEFT JOIN Positions p ON sp.PositionId = p.PositionId
WHERE u.Email IN ('cashier@test.com', 'waiter@test.com', 'kitchen@test.com', 'inventory@test.com')
    AND u.IsDeleted = 0
GROUP BY u.UserId, u.FullName, u.Email, u.Phone, r.RoleName, s.StaffId
ORDER BY u.Email;
```

## 🧪 Testing Login Redirect

### Test Case 1: Cashier Login
1. Login với: `cashier@test.com` / `Staff@123`
2. **Expected**: Redirect đến `/Payment/OrderSelection`
3. **Reason**: Staff có position "Cashier" được redirect đến OrderSelection page

### Test Case 2: Waiter Login
1. Login với: `waiter@test.com` / `Staff@123`
2. **Expected**: Redirect đến `/TableManage/Index`
3. **Reason**: Staff không có position "Cashier" nên redirect mặc định

### Test Case 3: Kitchen Staff Login
1. Login với: `kitchen@test.com` / `Staff@123`
2. **Expected**: Redirect đến `/TableManage/Index`
3. **Reason**: Staff không có position "Cashier" nên redirect mặc định

### Test Case 4: Inventory Staff Login
1. Login với: `inventory@test.com` / `Staff@123`
2. **Expected**: Redirect đến `/TableManage/Index`
3. **Reason**: Staff không có position "Cashier" nên redirect mặc định

## 📝 Lưu ý

1. **Password**: Tất cả staff accounts đều dùng password `Staff@123` (cho testing)
2. **Idempotent**: Script có thể chạy nhiều lần mà không tạo duplicate data
3. **Password Hash**: Password được hash bằng SHA256 + Base64
4. **Positions**: Script sẽ tự động tạo positions nếu chưa tồn tại

## 🔍 Verify trong Database

### Check Staff và Positions:
```sql
SELECT 
    u.Email,
    u.FullName,
    p.PositionName
FROM Users u
INNER JOIN Staffs s ON u.UserId = s.UserId
INNER JOIN StaffPositions sp ON s.StaffId = sp.StaffId
INNER JOIN Positions p ON sp.PositionId = p.PositionId
WHERE u.Email LIKE '%@test.com'
    AND u.IsDeleted = 0
ORDER BY u.Email, p.PositionName;
```

### Check Login Response có Positions:
Khi login với `cashier@test.com`, API response sẽ có:
```json
{
  "userId": 123,
  "email": "cashier@test.com",
  "roleId": 4,
  "roleName": "Staff",
  "positions": ["Cashier"],
  "token": "..."
}
```

## 🐛 Troubleshooting

### Staff không có position sau khi seed
- Kiểm tra xem positions đã được tạo chưa: `SELECT * FROM Positions`
- Kiểm tra StaffPositions table: `SELECT * FROM StaffPositions`
- Đảm bảo `SeedPositionsAsync` được gọi trước `SeedStaffWithAllPositionsAsync`

### Login không redirect đúng
- Kiểm tra positions trong LoginResponse từ API
- Kiểm tra claims có chứa "Positions" claim không
- Verify position name là "Cashier" (case-insensitive)

### Password không đúng
- Password hash: SHA256("Staff@123") + Base64
- Có thể reset password bằng cách chạy lại script

