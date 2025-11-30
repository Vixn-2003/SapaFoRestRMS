-- =============================================
-- Seed Data cho Payment Flow Testing
-- Tạo: 2025-01-15
-- Mục đích: Tạo dữ liệu test cho chức năng thanh toán (Payment Flow)
-- =============================================

USE SapaFoRestRMS;
GO

-- =============================================
-- 1. Kiểm tra và tạo dữ liệu cơ bản (nếu chưa có)
-- =============================================

-- Đảm bảo có Role (Staff, Manager, Owner)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Staff')
BEGIN
    INSERT INTO Roles (RoleName, Description, Status, CreatedAt)
    VALUES ('Staff', 'Nhân viên', 1, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Manager')
BEGIN
    INSERT INTO Roles (RoleName, Description, Status, CreatedAt)
    VALUES ('Manager', 'Quản lý', 1, GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Owner')
BEGIN
    INSERT INTO Roles (RoleName, Description, Status, CreatedAt)
    VALUES ('Owner', 'Chủ nhà hàng', 1, GETDATE());
END
GO

-- Lấy RoleId
DECLARE @StaffRoleId INT = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'Staff');
DECLARE @ManagerRoleId INT = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'Manager');
DECLARE @OwnerRoleId INT = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'Owner');
GO

-- =============================================
-- 2. Tạo Users (Staff) để phục vụ orders
-- =============================================

DECLARE @StaffRoleId INT = (SELECT TOP 1 RoleId FROM Roles WHERE RoleName = 'Staff');
DECLARE @StaffUserId1 INT;
DECLARE @StaffUserId2 INT;

-- Staff 1
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'staff1@restaurant.com')
BEGIN
    INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, Status, CreatedAt)
    VALUES ('Nguyễn Văn A', 'staff1@restaurant.com', '0901234567', 
            'SHA256_HASH_PLACEHOLDER', @StaffRoleId, 1, GETDATE());
    SET @StaffUserId1 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @StaffUserId1 = (SELECT UserId FROM Users WHERE Email = 'staff1@restaurant.com');
END

-- Staff 2
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'staff2@restaurant.com')
BEGIN
    INSERT INTO Users (FullName, Email, Phone, PasswordHash, RoleId, Status, CreatedAt)
    VALUES ('Trần Thị B', 'staff2@restaurant.com', '0907654321', 
            'SHA256_HASH_PLACEHOLDER', @StaffRoleId, 1, GETDATE());
    SET @StaffUserId2 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @StaffUserId2 = (SELECT UserId FROM Users WHERE Email = 'staff2@restaurant.com');
END
GO

-- =============================================
-- 3. Tạo Customers
-- =============================================

DECLARE @CustomerId1 INT;
DECLARE @CustomerId2 INT;
DECLARE @CustomerId3 INT;

-- Customer 1
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = 1001)
BEGIN
    INSERT INTO Customers (CustomerId, UserId, LoyaltyPoints, Notes)
    VALUES (1001, NULL, 500, 'Khách hàng VIP');
    SET @CustomerId1 = 1001;
END
ELSE
BEGIN
    SET @CustomerId1 = 1001;
END

-- Customer 2
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = 1002)
BEGIN
    INSERT INTO Customers (CustomerId, UserId, LoyaltyPoints, Notes)
    VALUES (1002, NULL, 200, 'Khách hàng thường');
    SET @CustomerId2 = 1002;
END
ELSE
BEGIN
    SET @CustomerId2 = 1002;
END

-- Customer 3
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = 1003)
BEGIN
    INSERT INTO Customers (CustomerId, UserId, LoyaltyPoints, Notes)
    VALUES (1003, NULL, 0, 'Khách hàng mới');
    SET @CustomerId3 = 1003;
END
ELSE
BEGIN
    SET @CustomerId3 = 1003;
END
GO

-- =============================================
-- 4. Tạo Area và Tables (nếu chưa có)
-- =============================================

DECLARE @AreaId INT;

IF NOT EXISTS (SELECT 1 FROM Areas WHERE AreaName = 'Khu vực chính')
BEGIN
    INSERT INTO Areas (AreaName, Description, Status)
    VALUES ('Khu vực chính', 'Khu vực phục vụ chính', 1);
    SET @AreaId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @AreaId = (SELECT TOP 1 AreaId FROM Areas WHERE AreaName = 'Khu vực chính');
END

-- Tạo bàn
IF NOT EXISTS (SELECT 1 FROM Tables WHERE TableNumber = 'Bàn 1')
BEGIN
    INSERT INTO Tables (TableNumber, Capacity, Status, AreaId)
    VALUES ('Bàn 1', 4, 'Available', @AreaId);
END

IF NOT EXISTS (SELECT 1 FROM Tables WHERE TableNumber = 'Bàn 2')
BEGIN
    INSERT INTO Tables (TableNumber, Capacity, Status, AreaId)
    VALUES ('Bàn 2', 6, 'Available', @AreaId);
END

IF NOT EXISTS (SELECT 1 FROM Tables WHERE TableNumber = 'Bàn 3')
BEGIN
    INSERT INTO Tables (TableNumber, Capacity, Status, AreaId)
    VALUES ('Bàn 3', 2, 'Available', @AreaId);
END

IF NOT EXISTS (SELECT 1 FROM Tables WHERE TableNumber = 'Bàn 5')
BEGIN
    INSERT INTO Tables (TableNumber, Capacity, Status, AreaId)
    VALUES ('Bàn 5', 8, 'Available', @AreaId);
END
GO

-- =============================================
-- 5. Tạo Reservations
-- =============================================

DECLARE @CustomerId1 INT = 1001;
DECLARE @CustomerId2 INT = 1002;
DECLARE @ReservationId1 INT;
DECLARE @ReservationId2 INT;
DECLARE @TableId1 INT = (SELECT TOP 1 TableId FROM Tables WHERE TableNumber = 'Bàn 1');
DECLARE @TableId2 INT = (SELECT TOP 1 TableId FROM Tables WHERE TableNumber = 'Bàn 2');

-- Reservation 1 (đã có order)
IF NOT EXISTS (SELECT 1 FROM Reservations WHERE ReservationId = 2001)
BEGIN
    INSERT INTO Reservations (ReservationId, CustomerId, CustomerNameReservation, ReservationDate, 
                              TimeSlot, ReservationTime, NumberOfGuests, Status, RequireDeposit, DepositPaid)
    VALUES (2001, @CustomerId1, 'Nguyễn Văn Khách', GETDATE(), 
            'Ca tối', GETDATE(), 4, 'Confirmed', 0, 0);
    SET @ReservationId1 = 2001;
    
    -- Link với bàn
    INSERT INTO ReservationTables (ReservationId, TableId)
    VALUES (2001, @TableId1);
END
ELSE
BEGIN
    SET @ReservationId1 = 2001;
END

-- Reservation 2
IF NOT EXISTS (SELECT 1 FROM Reservations WHERE ReservationId = 2002)
BEGIN
    INSERT INTO Reservations (ReservationId, CustomerId, CustomerNameReservation, ReservationDate, 
                              TimeSlot, ReservationTime, NumberOfGuests, Status, RequireDeposit, DepositPaid)
    VALUES (2002, @CustomerId2, 'Trần Thị Khách', DATEADD(DAY, 1, GETDATE()), 
            'Ca tối', DATEADD(DAY, 1, GETDATE()), 6, 'Confirmed', 0, 0);
    SET @ReservationId2 = 2002;
    
    -- Link với bàn
    INSERT INTO ReservationTables (ReservationId, TableId)
    VALUES (2002, @TableId2);
END
ELSE
BEGIN
    SET @ReservationId2 = 2002;
END
GO

-- =============================================
-- 6. Tạo MenuItems (món ăn) nếu chưa có
-- =============================================

DECLARE @MenuItemId1 INT;
DECLARE @MenuItemId2 INT;
DECLARE @MenuItemId3 INT;
DECLARE @MenuItemId4 INT;
DECLARE @MenuItemId5 INT;

-- Phở Bò
IF NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'Phở Bò')
BEGIN
    INSERT INTO MenuItems (Name, Description, Price, CourseType, IsAvailable, CategoryId)
    VALUES ('Phở Bò', 'Phở bò truyền thống', 85000, 'Main', 1, NULL);
    SET @MenuItemId1 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @MenuItemId1 = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Phở Bò');
END

-- Bánh Mì Thịt Nướng
IF NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'Bánh Mì Thịt Nướng')
BEGIN
    INSERT INTO MenuItems (Name, Description, Price, CourseType, IsAvailable, CategoryId)
    VALUES ('Bánh Mì Thịt Nướng', 'Bánh mì thịt nướng đặc biệt', 45000, 'Main', 1, NULL);
    SET @MenuItemId2 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @MenuItemId2 = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Bánh Mì Thịt Nướng');
END

-- Coca Cola
IF NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'Coca Cola')
BEGIN
    INSERT INTO MenuItems (Name, Description, Price, CourseType, IsAvailable, CategoryId)
    VALUES ('Coca Cola', 'Nước ngọt Coca Cola', 25000, 'Drink', 1, NULL);
    SET @MenuItemId3 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @MenuItemId3 = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Coca Cola');
END

-- Gỏi Cuốn
IF NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'Gỏi Cuốn')
BEGIN
    INSERT INTO MenuItems (Name, Description, Price, CourseType, IsAvailable, CategoryId)
    VALUES ('Gỏi Cuốn', 'Gỏi cuốn tôm thịt', 80000, 'Appetizer', 1, NULL);
    SET @MenuItemId4 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @MenuItemId4 = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Gỏi Cuốn');
END

-- Bún Chả
IF NOT EXISTS (SELECT 1 FROM MenuItems WHERE Name = 'Bún Chả')
BEGIN
    INSERT INTO MenuItems (Name, Description, Price, CourseType, IsAvailable, CategoryId)
    VALUES ('Bún Chả', 'Bún chả Hà Nội', 90000, 'Main', 1, NULL);
    SET @MenuItemId5 = SCOPE_IDENTITY();
END
ELSE
BEGIN
    SET @MenuItemId5 = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Bún Chả');
END
GO

-- =============================================
-- 7. Tạo Orders với các trạng thái khác nhau
-- =============================================

DECLARE @CustomerId1 INT = 1001;
DECLARE @CustomerId2 INT = 1002;
DECLARE @CustomerId3 INT = 1003;
DECLARE @ReservationId1 INT = 2001;
DECLARE @OrderId1 INT;
DECLARE @OrderId2 INT;
DECLARE @OrderId3 INT;
DECLARE @OrderId4 INT;
DECLARE @OrderId5 INT;

-- Tính TotalAmount theo logic: Subtotal + VAT (10%) + Service Fee (5%) - Discount
-- Order 1: Subtotal = 450000, VAT = 45000, Service Fee = 22500, Total = 517500
DECLARE @Order1Total DECIMAL = 517500;
-- Order 2: Subtotal = 360000, VAT = 36000, Service Fee = 18000, Total = 414000
DECLARE @Order2Total DECIMAL = 414000;
-- Order 3: Subtotal = 140000, VAT = 14000, Service Fee = 7000, Total = 161000
DECLARE @Order3Total DECIMAL = 161000;
-- Order 4: Subtotal = 225000, VAT = 22500, Service Fee = 11250, Total = 258750
DECLARE @Order4Total DECIMAL = 258750;
-- Order 5: Subtotal = 720000, VAT = 72000, Service Fee = 36000, Total = 828000
DECLARE @Order5Total DECIMAL = 828000;

-- Order 1: Pending Payment (chờ thanh toán) - có reservation
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 3001)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, TotalAmount, Status, CreatedAt)
    VALUES (3001, @ReservationId1, @CustomerId1, 'DineIn', @Order1Total, 'pending-payment', DATEADD(HOUR, -2, GETDATE()));
    SET @OrderId1 = 3001;
END
ELSE
BEGIN
    SET @OrderId1 = 3001;
END

-- Order 2: Pending Payment - không có reservation
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 3002)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, TotalAmount, Status, CreatedAt)
    VALUES (3002, NULL, @CustomerId2, 'DineIn', @Order2Total, 'pending-payment', DATEADD(HOUR, -1, GETDATE()));
    SET @OrderId2 = 3002;
END
ELSE
BEGIN
    SET @OrderId2 = 3002;
END

-- Order 3: Pending Payment - order nhỏ
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 3003)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, TotalAmount, Status, CreatedAt)
    VALUES (3003, NULL, @CustomerId3, 'Takeaway', @Order3Total, 'pending-payment', DATEADD(MINUTE, -30, GETDATE()));
    SET @OrderId3 = 3003;
END
ELSE
BEGIN
    SET @OrderId3 = 3003;
END

-- Order 4: Đã thanh toán (Paid)
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 3004)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, TotalAmount, Status, CreatedAt)
    VALUES (3004, NULL, @CustomerId1, 'DineIn', @Order4Total, 'Paid', DATEADD(DAY, -1, GETDATE()));
    SET @OrderId4 = 3004;
END
ELSE
BEGIN
    SET @OrderId4 = 3004;
END

-- Order 5: Pending Payment - order lớn
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 3005)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, TotalAmount, Status, CreatedAt)
    VALUES (3005, NULL, @CustomerId2, 'DineIn', @Order5Total, 'pending-payment', DATEADD(MINUTE, -15, GETDATE()));
    SET @OrderId5 = 3005;
END
ELSE
BEGIN
    SET @OrderId5 = 3005;
END
GO

-- =============================================
-- 8. Tạo OrderDetails cho các Orders
-- =============================================

DECLARE @MenuItemId1 INT = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Phở Bò');
DECLARE @MenuItemId2 INT = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Bánh Mì Thịt Nướng');
DECLARE @MenuItemId3 INT = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Coca Cola');
DECLARE @MenuItemId4 INT = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Gỏi Cuốn');
DECLARE @MenuItemId5 INT = (SELECT TOP 1 MenuItemId FROM MenuItems WHERE Name = 'Bún Chả');

-- OrderDetails cho Order 1 (3001)
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 3001)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status, CreatedAt)
    VALUES 
        (3001, @MenuItemId1, 2, 85000, 'Completed', DATEADD(HOUR, -2, GETDATE())), -- Phở Bò x2 = 170000
        (3001, @MenuItemId2, 1, 45000, 'Completed', DATEADD(HOUR, -2, GETDATE())), -- Bánh Mì x1 = 45000
        (3001, @MenuItemId3, 3, 25000, 'Completed', DATEADD(HOUR, -2, GETDATE())), -- Coca x3 = 75000
        (3001, @MenuItemId4, 2, 80000, 'Completed', DATEADD(HOUR, -2, GETDATE())); -- Gỏi Cuốn x2 = 160000
    -- Subtotal: 170000 + 45000 + 75000 + 160000 = 450000
    -- VAT (10%): 45000, Service Fee (5%): 22500
    -- Total: 450000 + 45000 + 22500 = 517500
END

-- OrderDetails cho Order 2 (3002)
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 3002)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status, CreatedAt)
    VALUES 
        (3002, @MenuItemId5, 2, 90000, 'Completed', DATEADD(HOUR, -1, GETDATE())), -- Bún Chả x2 = 180000
        (3002, @MenuItemId3, 4, 25000, 'Completed', DATEADD(HOUR, -1, GETDATE())), -- Coca x4 = 100000
        (3002, @MenuItemId4, 1, 80000, 'Completed', DATEADD(HOUR, -1, GETDATE())); -- Gỏi Cuốn x1 = 80000
    -- Subtotal: 180000 + 100000 + 80000 = 360000
    -- VAT (10%): 36000, Service Fee (5%): 18000
    -- Total: 360000 + 36000 + 18000 = 414000
END

-- OrderDetails cho Order 3 (3003)
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 3003)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status, CreatedAt)
    VALUES 
        (3003, @MenuItemId2, 2, 45000, 'Completed', DATEADD(MINUTE, -30, GETDATE())), -- Bánh Mì x2 = 90000
        (3003, @MenuItemId3, 2, 25000, 'Completed', DATEADD(MINUTE, -30, GETDATE())); -- Coca x2 = 50000
    -- Subtotal: 90000 + 50000 = 140000
    -- VAT (10%): 14000, Service Fee (5%): 7000
    -- Total: 140000 + 14000 + 7000 = 161000
END

-- OrderDetails cho Order 4 (3004) - Đã thanh toán
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 3004)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status, CreatedAt)
    VALUES 
        (3004, @MenuItemId1, 1, 85000, 'Completed', DATEADD(DAY, -1, GETDATE())), -- Phở Bò x1 = 85000
        (3004, @MenuItemId5, 1, 90000, 'Completed', DATEADD(DAY, -1, GETDATE())), -- Bún Chả x1 = 90000
        (3004, @MenuItemId3, 2, 25000, 'Completed', DATEADD(DAY, -1, GETDATE())); -- Coca x2 = 50000
    -- Subtotal: 85000 + 90000 + 50000 = 225000
    -- VAT (10%): 22500, Service Fee (5%): 11250
    -- Total: 225000 + 22500 + 11250 = 258750
END

-- OrderDetails cho Order 5 (3005)
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 3005)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Status, CreatedAt)
    VALUES 
        (3005, @MenuItemId1, 3, 85000, 'Completed', DATEADD(MINUTE, -15, GETDATE())), -- Phở Bò x3 = 255000
        (3005, @MenuItemId5, 2, 90000, 'Completed', DATEADD(MINUTE, -15, GETDATE())), -- Bún Chả x2 = 180000
        (3005, @MenuItemId4, 2, 80000, 'Completed', DATEADD(MINUTE, -15, GETDATE())), -- Gỏi Cuốn x2 = 160000
        (3005, @MenuItemId3, 5, 25000, 'Completed', DATEADD(MINUTE, -15, GETDATE())); -- Coca x5 = 125000
    -- Subtotal: 255000 + 180000 + 160000 + 125000 = 720000
    -- VAT (10%): 72000, Service Fee (5%): 36000
    -- Total: 720000 + 72000 + 36000 = 828000
END
GO

-- =============================================
-- 9. Tạo Transactions cho Order đã thanh toán
-- =============================================

-- Transaction cho Order 4 (đã thanh toán bằng Cash)
IF NOT EXISTS (SELECT 1 FROM Transactions WHERE OrderId = 3004)
BEGIN
    INSERT INTO Transactions (OrderId, TransactionCode, Amount, PaymentMethod, Status, CreatedAt, CompletedAt, Notes)
    VALUES (3004, 'TXN-' + CAST(GETDATE() AS VARCHAR) + '-3004', 258750, 'Cash', 'Success', 
            DATEADD(DAY, -1, GETDATE()), DATEADD(DAY, -1, GETDATE()), 'Thanh toán tiền mặt');
END
GO

-- =============================================
-- 10. Tạo Payments cho Order đã thanh toán
-- =============================================

-- Payment cho Order 4
IF NOT EXISTS (SELECT 1 FROM Payments WHERE OrderId = 3004)
BEGIN
    DECLARE @Subtotal4 DECIMAL = 225000; -- Tổng giá món
    DECLARE @VatAmount4 DECIMAL = @Subtotal4 * 0.1; -- VAT 10% = 22500
    DECLARE @ServiceFee4 DECIMAL = @Subtotal4 * 0.05; -- Service Fee 5% = 11250
    DECLARE @FinalAmount4 DECIMAL = @Subtotal4 + @VatAmount4 + @ServiceFee4; -- 258750
    
    INSERT INTO Payments (OrderId, PaymentMethod, Subtotal, DiscountAmount, Vatpercent, Vatamount, FinalAmount, PaymentDate)
    VALUES (3004, 'Cash', @Subtotal4, 0, 10, @VatAmount4, @FinalAmount4, DATEADD(DAY, -1, GETDATE()));
END
GO

-- =============================================
-- 11. Summary - Hiển thị dữ liệu đã tạo
-- =============================================

PRINT '========================================';
PRINT 'Seed Data cho Payment Flow đã hoàn tất!';
PRINT '========================================';
PRINT '';
PRINT 'Orders chờ thanh toán (pending-payment):';
SELECT OrderId, OrderType, TotalAmount, Status, CreatedAt, 
       CASE WHEN ReservationId IS NOT NULL THEN 'Có đặt bàn' ELSE 'Không đặt bàn' END AS HasReservation
FROM Orders 
WHERE Status = 'pending-payment'
ORDER BY CreatedAt DESC;
PRINT '';
PRINT 'Orders đã thanh toán (Paid):';
SELECT OrderId, OrderType, TotalAmount, Status, CreatedAt
FROM Orders 
WHERE Status = 'Paid'
ORDER BY CreatedAt DESC;
PRINT '';
PRINT 'Tổng số Orders: ' + CAST((SELECT COUNT(*) FROM Orders) AS VARCHAR);
PRINT 'Tổng số OrderDetails: ' + CAST((SELECT COUNT(*) FROM OrderDetails) AS VARCHAR);
PRINT 'Tổng số Transactions: ' + CAST((SELECT COUNT(*) FROM Transactions) AS VARCHAR);
PRINT '';
PRINT 'Để test VietQR Payment, sử dụng các OrderId: 3001, 3002, 3003, 3005';
PRINT '========================================';

