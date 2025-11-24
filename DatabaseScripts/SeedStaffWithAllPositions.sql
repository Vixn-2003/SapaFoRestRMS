-- =============================================
-- Seed Data: Staff với tất cả các Positions để testing
-- Tạo: 2025-01-15
-- Mục đích: Tạo staff accounts với từng position để test login redirect
-- =============================================

USE SapaFoRestRMS;
GO

-- =============================================
-- 1. Đảm bảo Roles và Positions tồn tại
-- =============================================

-- Ensure Staff Role exists
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Staff')
BEGIN
    INSERT INTO Roles (RoleName, Description, Status, CreatedAt)
    VALUES ('Staff', 'Nhân viên', 1, GETDATE());
END
GO

DECLARE @StaffRoleId INT = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'Staff');
GO

-- Ensure Positions exist
IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionName = 'Waiter/Waitress')
BEGIN
    INSERT INTO Positions (PositionName, Description, Status, BaseSalary)
    VALUES ('Waiter/Waitress', 'Front-of-house service staff', 0, 7000000);
END

IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionName = 'Cashier')
BEGIN
    INSERT INTO Positions (PositionName, Description, Status, BaseSalary)
    VALUES ('Cashier', 'Handles billing and payments', 0, 7500000);
END

IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionName = 'Kitchen Staff')
BEGIN
    INSERT INTO Positions (PositionName, Description, Status, BaseSalary)
    VALUES ('Kitchen Staff', 'Back-of-house food preparation', 0, 8000000);
END

IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionName = 'Inventory Staff')
BEGIN
    INSERT INTO Positions (PositionName, Description, Status, BaseSalary)
    VALUES ('Inventory Staff', 'Warehouse and stock management', 0, 7000000);
END
GO

-- Get Position IDs
DECLARE @CashierPositionId INT = (SELECT TOP 1 PositionId FROM Positions WHERE PositionName = 'Cashier');
DECLARE @WaiterPositionId INT = (SELECT TOP 1 PositionId FROM Positions WHERE PositionName = 'Waiter/Waitress');
DECLARE @KitchenPositionId INT = (SELECT TOP 1 PositionId FROM Positions WHERE PositionName = 'Kitchen Staff');
DECLARE @InventoryPositionId INT = (SELECT TOP 1 PositionId FROM Positions WHERE PositionName = 'Inventory Staff');
DECLARE @StaffRoleId INT = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'Staff');
GO

-- =============================================
-- 2. Hash Password Function (SHA256)
-- =============================================

-- Password: Staff@123
-- Hash: Use C# equivalent: Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("Staff@123")))
-- For SQL, we'll use a simple hash (in production, use proper hashing)
DECLARE @DefaultPassword NVARCHAR(100) = 'Staff@123';
-- Note: In production, use proper password hashing. This is for testing only.

-- =============================================
-- 3. Tạo Staff Accounts với từng Position
-- =============================================

-- Helper function to hash password (simplified for SQL - use proper hashing in production)
-- For testing, we'll use a simple approach
DECLARE @PasswordHash NVARCHAR(MAX);

-- =============================================
-- Staff 1: Cashier
-- =============================================
DECLARE @CashierUserId INT;
DECLARE @CashierStaffId INT;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'cashier@test.com' AND IsDeleted = 0)
BEGIN
    -- Create User
    INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, Status, CreatedAt, IsDeleted)
    VALUES ('Test Cashier', 'cashier@test.com', '0900002001', 
            -- Hash for "Staff@123" (SHA256 + Base64)
            -- Calculated: Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("Staff@123")))
            'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
            @StaffRoleId, 0, GETDATE(), 0);
    
    SET @CashierUserId = SCOPE_IDENTITY();
    
    -- Create Staff Profile
    INSERT INTO Staffs (UserId, HireDate, SalaryBase, Status)
    VALUES (@CashierUserId, CAST(GETDATE() AS DATE), 7500000, 0);
    
    SET @CashierStaffId = SCOPE_IDENTITY();
    
    -- Assign Cashier Position
    INSERT INTO StaffPositions (StaffId, PositionId)
    VALUES (@CashierStaffId, @CashierPositionId);
END
ELSE
BEGIN
    -- Update existing user
    SET @CashierUserId = (SELECT TOP 1 UserId FROM Users WHERE Email = 'cashier@test.com' AND IsDeleted = 0);
    UPDATE Users SET 
        PasswordHash = 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
        RoleId = @StaffRoleId
    WHERE UserId = @CashierUserId;
    
    -- Get or create Staff profile
    SET @CashierStaffId = (SELECT TOP 1 StaffId FROM Staffs WHERE UserId = @CashierUserId);
    IF @CashierStaffId IS NULL
    BEGIN
        INSERT INTO Staffs (UserId, HireDate, SalaryBase, Status)
        VALUES (@CashierUserId, CAST(GETDATE() AS DATE), 7500000, 0);
        SET @CashierStaffId = SCOPE_IDENTITY();
    END
    
    -- Ensure position is assigned
    IF NOT EXISTS (SELECT 1 FROM StaffPositions WHERE StaffId = @CashierStaffId AND PositionId = @CashierPositionId)
    BEGIN
        INSERT INTO StaffPositions (StaffId, PositionId)
        VALUES (@CashierStaffId, @CashierPositionId);
    END
END
GO

-- =============================================
-- Staff 2: Waiter/Waitress
-- =============================================
DECLARE @WaiterUserId INT;
DECLARE @WaiterStaffId INT;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'waiter@test.com' AND IsDeleted = 0)
BEGIN
    INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, Status, CreatedAt, IsDeleted)
    VALUES ('Test Waiter', 'waiter@test.com', '0900002002', 
            'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
            @StaffRoleId, 0, GETDATE(), 0);
    
    SET @WaiterUserId = SCOPE_IDENTITY();
    
    INSERT INTO Staffs (UserId, HireDate, SalaryBase, Status)
    VALUES (@WaiterUserId, CAST(GETDATE() AS DATE), 7000000, 0);
    
    SET @WaiterStaffId = SCOPE_IDENTITY();
    
    INSERT INTO StaffPositions (StaffId, PositionId)
    VALUES (@WaiterStaffId, @WaiterPositionId);
END
ELSE
BEGIN
    SET @WaiterUserId = (SELECT TOP 1 UserId FROM Users WHERE Email = 'waiter@test.com' AND IsDeleted = 0);
    UPDATE Users SET 
        PasswordHash = 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
        RoleId = @StaffRoleId
    WHERE UserId = @WaiterUserId;
    
    SET @WaiterStaffId = (SELECT TOP 1 StaffId FROM Staffs WHERE UserId = @WaiterUserId);
    IF @WaiterStaffId IS NULL
    BEGIN
        INSERT INTO Staffs (UserId, HireDate, SalaryBase, Status)
        VALUES (@WaiterUserId, CAST(GETDATE() AS DATE), 7000000, 0);
        SET @WaiterStaffId = SCOPE_IDENTITY();
    END
    
    IF NOT EXISTS (SELECT 1 FROM StaffPositions WHERE StaffId = @WaiterStaffId AND PositionId = @WaiterPositionId)
    BEGIN
        INSERT INTO StaffPositions (StaffId, PositionId)
        VALUES (@WaiterStaffId, @WaiterPositionId);
    END
END
GO

-- =============================================
-- Staff 3: Kitchen Staff
-- =============================================
DECLARE @KitchenUserId INT;
DECLARE @KitchenStaffId INT;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'kitchen@test.com' AND IsDeleted = 0)
BEGIN
    INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, Status, CreatedAt, IsDeleted)
    VALUES ('Test Kitchen Staff', 'kitchen@test.com', '0900002003', 
            'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
            @StaffRoleId, 0, GETDATE(), 0);
    
    SET @KitchenUserId = SCOPE_IDENTITY();
    
    INSERT INTO Staffs (UserId, HireDate, SalaryBase, Status)
    VALUES (@KitchenUserId, CAST(GETDATE() AS DATE), 8000000, 0);
    
    SET @KitchenStaffId = SCOPE_IDENTITY();
    
    INSERT INTO StaffPositions (StaffId, PositionId)
    VALUES (@KitchenStaffId, @KitchenPositionId);
END
ELSE
BEGIN
    SET @KitchenUserId = (SELECT TOP 1 UserId FROM Users WHERE Email = 'kitchen@test.com' AND IsDeleted = 0);
    UPDATE Users SET 
        PasswordHash = 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
        RoleId = @StaffRoleId
    WHERE UserId = @KitchenUserId;
    
    SET @KitchenStaffId = (SELECT TOP 1 StaffId FROM Staffs WHERE UserId = @KitchenUserId);
    IF @KitchenStaffId IS NULL
    BEGIN
        INSERT INTO Staffs (UserId, HireDate, SalaryBase, Status)
        VALUES (@KitchenUserId, CAST(GETDATE() AS DATE), 8000000, 0);
        SET @KitchenStaffId = SCOPE_IDENTITY();
    END
    
    IF NOT EXISTS (SELECT 1 FROM StaffPositions WHERE StaffId = @KitchenStaffId AND PositionId = @KitchenPositionId)
    BEGIN
        INSERT INTO StaffPositions (StaffId, PositionId)
        VALUES (@KitchenStaffId, @KitchenPositionId);
    END
END
GO

-- =============================================
-- Staff 4: Inventory Staff
-- =============================================
DECLARE @InventoryUserId INT;
DECLARE @InventoryStaffId INT;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'inventory@test.com' AND IsDeleted = 0)
BEGIN
    INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, Status, CreatedAt, IsDeleted)
    VALUES ('Test Inventory Staff', 'inventory@test.com', '0900002004', 
            'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
            @StaffRoleId, 0, GETDATE(), 0);
    
    SET @InventoryUserId = SCOPE_IDENTITY();
    
    INSERT INTO Staffs (UserId, HireDate, SalaryBase, Status)
    VALUES (@InventoryUserId, CAST(GETDATE() AS DATE), 7000000, 0);
    
    SET @InventoryStaffId = SCOPE_IDENTITY();
    
    INSERT INTO StaffPositions (StaffId, PositionId)
    VALUES (@InventoryStaffId, @InventoryPositionId);
END
ELSE
BEGIN
    SET @InventoryUserId = (SELECT TOP 1 UserId FROM Users WHERE Email = 'inventory@test.com' AND IsDeleted = 0);
    UPDATE Users SET 
        PasswordHash = 'XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=',
        RoleId = @StaffRoleId
    WHERE UserId = @InventoryUserId;
    
    SET @InventoryStaffId = (SELECT TOP 1 StaffId FROM Staffs WHERE UserId = @InventoryUserId);
    IF @InventoryStaffId IS NULL
    BEGIN
        INSERT INTO Staffs (UserId, HireDate, SalaryBase, Status)
        VALUES (@InventoryUserId, CAST(GETDATE() AS DATE), 7000000, 0);
        SET @InventoryStaffId = SCOPE_IDENTITY();
    END
    
    IF NOT EXISTS (SELECT 1 FROM StaffPositions WHERE StaffId = @InventoryStaffId AND PositionId = @InventoryPositionId)
    BEGIN
        INSERT INTO StaffPositions (StaffId, PositionId)
        VALUES (@InventoryStaffId, @InventoryPositionId);
    END
END
GO

-- =============================================
-- 4. Verify Data
-- =============================================
PRINT '========================================';
PRINT 'Staff Accounts Created:';
PRINT '========================================';

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

PRINT '';
PRINT 'Login Credentials:';
PRINT 'Email: cashier@test.com | Password: Staff@123 | Position: Cashier (redirects to OrderSelection)';
PRINT 'Email: waiter@test.com | Password: Staff@123 | Position: Waiter/Waitress';
PRINT 'Email: kitchen@test.com | Password: Staff@123 | Position: Kitchen Staff';
PRINT 'Email: inventory@test.com | Password: Staff@123 | Position: Inventory Staff';
PRINT '========================================';

