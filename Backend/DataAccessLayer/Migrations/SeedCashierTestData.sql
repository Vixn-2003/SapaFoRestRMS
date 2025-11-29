-- ═══════════════════════════════════════════════════════════════════
-- SEED DATA: Test Cashier Payment Workflow
-- Purpose: Tạo dữ liệu test cho nhân viên thu ngân xử lý hóa đơn
-- Status: waiting-confirmation (chờ khách xác nhận số lượng món)
-- ═══════════════════════════════════════════════════════════════════

USE [SapaFoRestRMSDb] -- Thay đổi tên database nếu cần
GO

PRINT '═══════════════════════════════════════════════════════════════════'
PRINT 'Starting Cashier Test Data Seeding...'
PRINT '═══════════════════════════════════════════════════════════════════'
GO

-- ───────────────────────────────────────────────────────────────────
-- STEP 1: Cleanup existing test orders
-- ───────────────────────────────────────────────────────────────────
PRINT ''
PRINT 'STEP 1: Cleaning up existing test orders...'

-- Delete existing test orders (Status: waiting-confirmation, Confirmed, pending-payment)
DECLARE @TestOrderIds TABLE (OrderId INT);

INSERT INTO @TestOrderIds (OrderId)
SELECT OrderId FROM Orders 
WHERE Status IN ('waiting-confirmation', 'Confirmed', 'pending-payment')
  AND CreatedAt >= DATEADD(DAY, -7, GETDATE()); -- Only recent test data

-- Delete related data
DELETE FROM Transactions WHERE OrderId IN (SELECT OrderId FROM @TestOrderIds);
DELETE FROM Payments WHERE OrderId IN (SELECT OrderId FROM @TestOrderIds);
DELETE FROM OrderDetails WHERE OrderId IN (SELECT OrderId FROM @TestOrderIds);
DELETE FROM Orders WHERE OrderId IN (SELECT OrderId FROM @TestOrderIds);

PRINT '✅ Cleaned up existing test orders'
GO

-- ───────────────────────────────────────────────────────────────────
-- STEP 2: Ensure required entities exist (Area, Tables, Customer, Staff)
-- ───────────────────────────────────────────────────────────────────
PRINT ''
PRINT 'STEP 2: Ensuring required entities exist...'

-- Create test area if not exists
IF NOT EXISTS (SELECT 1 FROM Areas WHERE AreaName = 'Tầng 1 - Test')
BEGIN
    INSERT INTO Areas (AreaName, Floor, Description)
    VALUES ('Tầng 1 - Test', 1, 'Khu vực test cho thu ngân');
    PRINT '✅ Created test area: Tầng 1 - Test'
END

DECLARE @TestAreaId INT = (SELECT AreaId FROM Areas WHERE AreaName = 'Tầng 1 - Test');

-- Create test tables if not exist
IF NOT EXISTS (SELECT 1 FROM Tables WHERE TableNumber = 'T01')
BEGIN
    INSERT INTO Tables (TableNumber, Capacity, Status, AreaId)
    VALUES 
        ('T01', 4, 'Occupied', @TestAreaId),
        ('T02', 6, 'Occupied', @TestAreaId),
        ('T03', 2, 'Occupied', @TestAreaId),
        ('T04', 8, 'Occupied', @TestAreaId);
    PRINT '✅ Created test tables: T01, T02, T03, T04'
END

-- Ensure test customer exists
DECLARE @TestCustomerUserId INT;
DECLARE @TestCustomerId INT;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'customer.test@example.com')
BEGIN
    -- Get Customer RoleId
    DECLARE @CustomerRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Customer');
    
    IF @CustomerRoleId IS NULL
    BEGIN
        INSERT INTO Roles (RoleName) VALUES ('Customer');
        SET @CustomerRoleId = SCOPE_IDENTITY();
    END

    -- Create user
    INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, Status, CreatedAt, IsDeleted)
    VALUES ('Khách Hàng Test', 'customer.test@example.com', '0900123456', 
            CONVERT(VARCHAR(MAX), HASHBYTES('SHA2_256', 'Test@123'), 2), 
            @CustomerRoleId, 0, GETDATE(), 0);
    
    SET @TestCustomerUserId = SCOPE_IDENTITY();

    -- Create customer
    INSERT INTO Customers (UserId, LoyaltyPoints, Notes)
    VALUES (@TestCustomerUserId, 100, 'Test customer for cashier workflow');
    
    SET @TestCustomerId = SCOPE_IDENTITY();
    PRINT '✅ Created test customer: customer.test@example.com'
END
ELSE
BEGIN
    SET @TestCustomerUserId = (SELECT UserId FROM Users WHERE Email = 'customer.test@example.com');
    SET @TestCustomerId = (SELECT CustomerId FROM Customers WHERE UserId = @TestCustomerUserId);
END

-- Ensure cashier staff exists
DECLARE @CashierStaffId INT;
DECLARE @CashierUserId INT;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'cashier@test.com')
BEGIN
    -- Get Staff RoleId
    DECLARE @StaffRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Staff');
    
    IF @StaffRoleId IS NULL
    BEGIN
        INSERT INTO Roles (RoleName) VALUES ('Staff');
        SET @StaffRoleId = SCOPE_IDENTITY();
    END

    -- Create user
    INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, Status, CreatedAt, IsDeleted)
    VALUES ('Thu Ngân Test', 'cashier@test.com', '0900999888', 
            CONVERT(VARCHAR(MAX), HASHBYTES('SHA2_256', 'Staff@123'), 2), 
            @StaffRoleId, 0, GETDATE(), 0);
    
    SET @CashierUserId = SCOPE_IDENTITY();

    -- Create staff
    INSERT INTO Staffs (UserId, HireDate)
    VALUES (@CashierUserId, GETDATE());
    
    SET @CashierStaffId = SCOPE_IDENTITY();
    PRINT '✅ Created cashier staff: cashier@test.com'
END
ELSE
BEGIN
    SET @CashierUserId = (SELECT UserId FROM Users WHERE Email = 'cashier@test.com');
    SET @CashierStaffId = (SELECT TOP 1 StaffId FROM Staffs WHERE UserId = @CashierUserId);
END

PRINT '✅ All required entities are ready'
GO

-- ───────────────────────────────────────────────────────────────────
-- STEP 3: Create test menu items
-- ───────────────────────────────────────────────────────────────────
PRINT ''
PRINT 'STEP 3: Creating test menu items...'

-- Ensure menu items exist
IF NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'Phở Bò Đặc Biệt')
BEGIN
    INSERT INTO MenuItems (Name, Price, Description, CourseType, IsAvailable, BillingType)
    VALUES 
        ('Phở Bò Đặc Biệt', 85000, 'Phở bò truyền thống Việt Nam', 'MainCourse', 1, 0),
        ('Cơm Gà Xối Mỡ', 95000, 'Cơm gà Hải Nam đặc trưng', 'MainCourse', 1, 0),
        ('Bún Chả Hà Nội', 75000, 'Bún chả thơm ngon', 'MainCourse', 1, 0),
        ('Bánh Mì Pate', 35000, 'Bánh mì pate trứng', 'Appetizer', 1, 0),
        ('Gỏi Cuốn Tôm Thịt', 65000, 'Gỏi cuốn tươi ngon', 'Appetizer', 1, 0),
        ('Trà Đào Cam Sả', 45000, 'Trà hoa quả giải khát', 'Beverage', 1, 0),
        ('Cafe Sữa Đá', 35000, 'Cafe sữa truyền thống', 'Beverage', 1, 0),
        ('Nước Cam Ép', 40000, 'Nước cam tươi ép', 'Beverage', 1, 0),
        ('Coca Cola', 25000, 'Nước ngọt có ga', 'Beverage', 1, 0),
        ('Salad Rau Trộn', 55000, 'Salad rau tươi', 'SideDish', 1, 1); -- ConsumptionBased
    
    PRINT '✅ Created 10 test menu items'
END
GO

-- ───────────────────────────────────────────────────────────────────
-- STEP 4: Create Reservations for tables
-- ───────────────────────────────────────────────────────────────────
PRINT ''
PRINT 'STEP 4: Creating test reservations...'

DECLARE @TestCustomerId INT = (SELECT CustomerId FROM Customers WHERE UserId = (SELECT UserId FROM Users WHERE Email = 'customer.test@example.com'));
DECLARE @CashierUserId INT = (SELECT UserId FROM Users WHERE Email = 'cashier@test.com');

-- Table T01 - Reservation 1
DECLARE @Reservation1Id INT;
INSERT INTO Reservations (CustomerId, CustomerNameReservation, StaffId, ReservationDate, TimeSlot, ReservationTime, NumberOfGuests, Status, ArrivalAt, RequireDeposit, DepositPaid)
VALUES (@TestCustomerId, 'Khách Hàng Test', @CashierUserId, CAST(GETDATE() AS DATE), 'Ca trưa', DATEADD(MINUTE, -45, GETDATE()), 4, 'Guest Seated', DATEADD(MINUTE, -45, GETDATE()), 0, 0);
SET @Reservation1Id = SCOPE_IDENTITY();

INSERT INTO ReservationTables (ReservationId, TableId)
VALUES (@Reservation1Id, (SELECT TableId FROM Tables WHERE TableNumber = 'T01'));

-- Table T02 - Reservation 2
DECLARE @Reservation2Id INT;
INSERT INTO Reservations (CustomerId, CustomerNameReservation, StaffId, ReservationDate, TimeSlot, ReservationTime, NumberOfGuests, Status, ArrivalAt, RequireDeposit, DepositPaid)
VALUES (@TestCustomerId, 'Khách Hàng VIP', @CashierUserId, CAST(GETDATE() AS DATE), 'Ca trưa', DATEADD(MINUTE, -60, GETDATE()), 6, 'Guest Seated', DATEADD(MINUTE, -60, GETDATE()), 0, 0);
SET @Reservation2Id = SCOPE_IDENTITY();

INSERT INTO ReservationTables (ReservationId, TableId)
VALUES (@Reservation2Id, (SELECT TableId FROM Tables WHERE TableNumber = 'T02'));

-- Table T03 - Reservation 3
DECLARE @Reservation3Id INT;
INSERT INTO Reservations (CustomerId, CustomerNameReservation, StaffId, ReservationDate, TimeSlot, ReservationTime, NumberOfGuests, Status, ArrivalAt, RequireDeposit, DepositPaid)
VALUES (@TestCustomerId, 'Nguyễn Văn A', @CashierUserId, CAST(GETDATE() AS DATE), 'Ca trưa', DATEADD(MINUTE, -30, GETDATE()), 2, 'Guest Seated', DATEADD(MINUTE, -30, GETDATE()), 0, 0);
SET @Reservation3Id = SCOPE_IDENTITY();

INSERT INTO ReservationTables (ReservationId, TableId)
VALUES (@Reservation3Id, (SELECT TableId FROM Tables WHERE TableNumber = 'T03'));

-- Table T04 - Reservation 4
DECLARE @Reservation4Id INT;
INSERT INTO Reservations (CustomerId, CustomerNameReservation, StaffId, ReservationDate, TimeSlot, ReservationTime, NumberOfGuests, Status, ArrivalAt, RequireDeposit, DepositPaid)
VALUES (@TestCustomerId, 'Trần Thị B', @CashierUserId, CAST(GETDATE() AS DATE), 'Ca trưa', DATEADD(MINUTE, -90, GETDATE()), 8, 'Guest Seated', DATEADD(MINUTE, -90, GETDATE()), 0, 0);
SET @Reservation4Id = SCOPE_IDENTITY();

INSERT INTO ReservationTables (ReservationId, TableId)
VALUES (@Reservation4Id, (SELECT TableId FROM Tables WHERE TableNumber = 'T04'));

PRINT '✅ Created 4 test reservations'
GO

-- ───────────────────────────────────────────────────────────────────
-- STEP 5: Create Orders with status "waiting-confirmation"
-- ───────────────────────────────────────────────────────────────────
PRINT ''
PRINT 'STEP 5: Creating test orders with status waiting-confirmation...'

DECLARE @TestCustomerId INT = (SELECT CustomerId FROM Customers WHERE UserId = (SELECT UserId FROM Users WHERE Email = 'customer.test@example.com'));
DECLARE @Reservation1Id INT = (SELECT ReservationId FROM Reservations WHERE CustomerNameReservation = 'Khách Hàng Test');
DECLARE @Reservation2Id INT = (SELECT ReservationId FROM Reservations WHERE CustomerNameReservation = 'Khách Hàng VIP');
DECLARE @Reservation3Id INT = (SELECT ReservationId FROM Reservations WHERE CustomerNameReservation = 'Nguyễn Văn A');
DECLARE @Reservation4Id INT = (SELECT ReservationId FROM Reservations WHERE CustomerNameReservation = 'Trần Thị B');

-- ORDER 1: Bàn T01 - 4 món ăn chính + đồ uống (waiting-confirmation)
DECLARE @Order1Id INT;
INSERT INTO Orders (ReservationId, CustomerId, OrderType, Status, CreatedAt, TotalAmount)
VALUES (@Reservation1Id, @TestCustomerId, 'DineIn', 'waiting-confirmation', DATEADD(MINUTE, -40, GETDATE()), 0);
SET @Order1Id = SCOPE_IDENTITY();

-- OrderDetails cho Order 1
INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status)
SELECT @Order1Id, MenuItemId, Quantity, Price, 'Pending'
FROM (
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Phở Bò Đặc Biệt') AS MenuItemId, 2 AS Quantity, (SELECT Price FROM MenuItems WHERE Name = 'Phở Bò Đặc Biệt') AS Price
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Bún Chả Hà Nội'), 2, (SELECT Price FROM MenuItems WHERE Name = 'Bún Chả Hà Nội')
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Trà Đào Cam Sả'), 3, (SELECT Price FROM MenuItems WHERE Name = 'Trà Đào Cam Sả')
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Coca Cola'), 1, (SELECT Price FROM MenuItems WHERE Name = 'Coca Cola')
) AS OrderItems;

PRINT '✅ Created Order 1: Bàn T01 (4 khách, 8 món)'

-- ORDER 2: Bàn T02 - Order lớn (waiting-confirmation)
DECLARE @Order2Id INT;
INSERT INTO Orders (ReservationId, CustomerId, OrderType, Status, CreatedAt, TotalAmount)
VALUES (@Reservation2Id, @TestCustomerId, 'DineIn', 'waiting-confirmation', DATEADD(MINUTE, -55, GETDATE()), 0);
SET @Order2Id = SCOPE_IDENTITY();

-- OrderDetails cho Order 2
INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status)
SELECT @Order2Id, MenuItemId, Quantity, Price, 'Pending'
FROM (
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Cơm Gà Xối Mỡ') AS MenuItemId, 4 AS Quantity, (SELECT Price FROM MenuItems WHERE Name = 'Cơm Gà Xối Mỡ') AS Price
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Phở Bò Đặc Biệt'), 2, (SELECT Price FROM MenuItems WHERE Name = 'Phở Bò Đặc Biệt')
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Gỏi Cuốn Tôm Thịt'), 2, (SELECT Price FROM MenuItems WHERE Name = 'Gỏi Cuốn Tôm Thịt')
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Cafe Sữa Đá'), 4, (SELECT Price FROM MenuItems WHERE Name = 'Cafe Sữa Đá')
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Nước Cam Ép'), 2, (SELECT Price FROM MenuItems WHERE Name = 'Nước Cam Ép')
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Salad Rau Trộn'), 3, (SELECT Price FROM MenuItems WHERE Name = 'Salad Rau Trộn')
) AS OrderItems;

PRINT '✅ Created Order 2: Bàn T02 (6 khách, 17 món)'

-- ORDER 3: Bàn T03 - Order nhỏ (waiting-confirmation)
DECLARE @Order3Id INT;
INSERT INTO Orders (ReservationId, CustomerId, OrderType, Status, CreatedAt, TotalAmount)
VALUES (@Reservation3Id, @TestCustomerId, 'DineIn', 'waiting-confirmation', DATEADD(MINUTE, -25, GETDATE()), 0);
SET @Order3Id = SCOPE_IDENTITY();

-- OrderDetails cho Order 3
INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status)
SELECT @Order3Id, MenuItemId, Quantity, Price, 'Pending'
FROM (
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Bánh Mì Pate') AS MenuItemId, 2 AS Quantity, (SELECT Price FROM MenuItems WHERE Name = 'Bánh Mì Pate') AS Price
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Cafe Sữa Đá'), 2, (SELECT Price FROM MenuItems WHERE Name = 'Cafe Sữa Đá')
) AS OrderItems;

PRINT '✅ Created Order 3: Bàn T03 (2 khách, 4 món)'

-- ORDER 4: Bàn T04 - Order có món ConsumptionBased (waiting-confirmation)
DECLARE @Order4Id INT;
INSERT INTO Orders (ReservationId, CustomerId, OrderType, Status, CreatedAt, TotalAmount)
VALUES (@Reservation4Id, @TestCustomerId, 'DineIn', 'waiting-confirmation', DATEADD(MINUTE, -85, GETDATE()), 0);
SET @Order4Id = SCOPE_IDENTITY();

-- OrderDetails cho Order 4
INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status, QuantityUsed)
SELECT @Order4Id, MenuItemId, Quantity, Price, 'Pending', NULL
FROM (
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Phở Bò Đặc Biệt') AS MenuItemId, 5 AS Quantity, (SELECT Price FROM MenuItems WHERE Name = 'Phở Bò Đặc Biệt') AS Price
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Cơm Gà Xối Mỡ'), 3, (SELECT Price FROM MenuItems WHERE Name = 'Cơm Gà Xối Mỡ')
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Salad Rau Trộn'), 8, (SELECT Price FROM MenuItems WHERE Name = 'Salad Rau Trộn') -- ConsumptionBased item
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Trà Đào Cam Sả'), 6, (SELECT Price FROM MenuItems WHERE Name = 'Trà Đào Cam Sả')
    UNION ALL
    SELECT (SELECT MenuItemId FROM MenuItems WHERE Name = 'Nước Cam Ép'), 2, (SELECT Price FROM MenuItems WHERE Name = 'Nước Cam Ép')
) AS OrderItems;

PRINT '✅ Created Order 4: Bàn T04 (8 khách, 24 món, có món tính theo tiêu hao)'

GO

-- ───────────────────────────────────────────────────────────────────
-- STEP 6: Update TotalAmount for all orders
-- ───────────────────────────────────────────────────────────────────
PRINT ''
PRINT 'STEP 6: Calculating TotalAmount for orders...'

UPDATE Orders
SET TotalAmount = (
    SELECT SUM(od.UnitPrice * od.Quantity)
    FROM OrderDetails od
    WHERE od.OrderId = Orders.OrderId
)
WHERE Status = 'waiting-confirmation';

PRINT '✅ Updated TotalAmount for all orders'
GO

-- ───────────────────────────────────────────────────────────────────
-- VERIFICATION
-- ───────────────────────────────────────────────────────────────────
PRINT ''
PRINT '═══════════════════════════════════════════════════════════════════'
PRINT 'VERIFICATION: Checking created data...'
PRINT '═══════════════════════════════════════════════════════════════════'

-- Show created orders
SELECT 
    o.OrderId,
    o.OrderType,
    ISNULL(t.TableNumber, 'N/A') AS TableNumber,
    o.Status,
    o.TotalAmount,
    COUNT(od.OrderDetailId) AS ItemCount,
    o.CreatedAt,
    c.CustomerNameReservation AS CustomerName
FROM Orders o
LEFT JOIN Reservations c ON o.ReservationId = c.ReservationId
LEFT JOIN ReservationTables rt ON c.ReservationId = rt.ReservationId
LEFT JOIN Tables t ON rt.TableId = t.TableId
LEFT JOIN OrderDetails od ON o.OrderId = od.OrderId
WHERE o.Status = 'waiting-confirmation'
  AND o.CreatedAt >= DATEADD(HOUR, -2, GETDATE())
GROUP BY o.OrderId, o.OrderType, t.TableNumber, o.Status, o.TotalAmount, o.CreatedAt, c.CustomerNameReservation
ORDER BY o.CreatedAt DESC;

PRINT ''
PRINT '═══════════════════════════════════════════════════════════════════'
PRINT '✅ CASHIER TEST DATA SEEDING COMPLETED SUCCESSFULLY!'
PRINT '═══════════════════════════════════════════════════════════════════'
PRINT ''
PRINT '📊 Summary:'
PRINT '   - 4 Orders created with status: waiting-confirmation'
PRINT '   - 4 Tables assigned (T01, T02, T03, T04)'
PRINT '   - 4 Reservations created'
PRINT '   - Total items: 53 món ăn'
PRINT ''
PRINT '🧪 Test Scenarios:'
PRINT '   1. Order 1 (T01): Standard order - 4 người, 8 món'
PRINT '   2. Order 2 (T02): Large order - 6 người, 17 món'
PRINT '   3. Order 3 (T03): Small order - 2 người, 4 món'
PRINT '   4. Order 4 (T04): Mixed billing - có món ConsumptionBased'
PRINT ''
PRINT '🔑 Login Credentials:'
PRINT '   Cashier: cashier@test.com / Staff@123'
PRINT '   Customer: customer.test@example.com / Test@123'
PRINT ''
PRINT '🌐 API Endpoints to Test:'
PRINT '   GET  /api/Payment/orders?status=waiting-confirmation'
PRINT '   GET  /api/Payment/orders/{orderId}/details'
PRINT '   PUT  /api/Payment/orders/{orderId}/confirm'
PRINT '   POST /api/Payment/orders/{orderId}/process'
PRINT ''
GO

