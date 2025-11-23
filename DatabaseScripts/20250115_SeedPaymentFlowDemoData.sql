-- ============================================
-- SEED DATA CHO PAYMENT FLOW DEMO
-- ============================================
-- Script này tạo dữ liệu mẫu để demo các tính năng:
-- 1. Cash Payment (underpaid, overpaid, exact)
-- 2. Split Bill
-- 3. Payment Status Polling
-- 4. Retry Logic
-- 5. Order Locking
-- ============================================

USE SapaFoRestRMS;
GO

-- ============================================
-- 1. Tạo Orders với các trạng thái khác nhau
-- ============================================

-- Order 1: Pending Payment - Để test Cash Payment (underpaid/overpaid)
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 1001)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, Subtotal, VatAmount, ServiceFee, TotalAmount, Status, CreatedAt, CreatedBy)
    VALUES (1001, NULL, 1, 'DineIn', 450000, 45000, 22500, 517500, 'PendingPayment', GETDATE(), 1);
END

-- Order 2: Pending Payment - Để test Split Bill
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 1002)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, Subtotal, VatAmount, ServiceFee, TotalAmount, Status, CreatedAt, CreatedBy)
    VALUES (1002, NULL, 2, 'DineIn', 720000, 72000, 36000, 828000, 'PendingPayment', GETDATE(), 1);
END

-- Order 3: Pending Payment - Để test Payment Status Polling
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 1003)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, Subtotal, VatAmount, ServiceFee, TotalAmount, Status, CreatedAt, CreatedBy)
    VALUES (1003, NULL, 3, 'DineIn', 360000, 36000, 18000, 414000, 'PendingPayment', GETDATE(), 1);
END

-- Order 4: PaymentProcessing - Để test Retry Logic
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 1004)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, Subtotal, VatAmount, ServiceFee, TotalAmount, Status, CreatedAt, CreatedBy)
    VALUES (1004, NULL, 4, 'DineIn', 280000, 28000, 14000, 322000, 'PaymentProcessing', GETDATE(), 1);
END

-- Order 5: PartiallyPaid - Để test Split Bill completion
IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = 1005)
BEGIN
    INSERT INTO Orders (OrderId, ReservationId, CustomerId, OrderType, Subtotal, VatAmount, ServiceFee, TotalAmount, Status, CreatedAt, CreatedBy)
    VALUES (1005, NULL, 5, 'DineIn', 600000, 60000, 30000, 690000, 'PartiallyPaid', GETDATE(), 1);
END

-- ============================================
-- 2. Tạo OrderDetails cho các orders
-- ============================================

-- Order 1001: 3 món
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 1001)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Notes, CreatedAt)
    VALUES 
        (1001, 1, 2, 150000, 300000, NULL, GETDATE()),
        (1001, 2, 1, 100000, 100000, NULL, GETDATE()),
        (1001, 3, 2, 25000, 50000, NULL, GETDATE());
END

-- Order 1002: 4 món (để split bill)
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 1002)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Notes, CreatedAt)
    VALUES 
        (1002, 1, 3, 150000, 450000, NULL, GETDATE()),
        (1002, 2, 2, 100000, 200000, NULL, GETDATE()),
        (1002, 3, 2, 25000, 50000, NULL, GETDATE()),
        (1002, 4, 1, 20000, 20000, NULL, GETDATE());
END

-- Order 1003: 2 món
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 1003)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Notes, CreatedAt)
    VALUES 
        (1003, 1, 2, 150000, 300000, NULL, GETDATE()),
        (1003, 3, 2, 25000, 50000, NULL, GETDATE());
END

-- Order 1004: 2 món (failed payment)
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 1004)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Notes, CreatedAt)
    VALUES 
        (1004, 2, 2, 100000, 200000, NULL, GETDATE()),
        (1004, 3, 3, 25000, 75000, NULL, GETDATE());
END

-- Order 1005: 3 món (partially paid)
IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderId = 1005)
BEGIN
    INSERT INTO OrderDetails (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Notes, CreatedAt)
    VALUES 
        (1005, 1, 3, 150000, 450000, NULL, GETDATE()),
        (1005, 2, 1, 100000, 100000, NULL, GETDATE()),
        (1005, 3, 2, 25000, 50000, NULL, GETDATE());
END

-- ============================================
-- 3. Tạo Transactions mẫu
-- ============================================

-- Transaction cho Order 1004 (Failed - để test retry)
IF NOT EXISTS (SELECT 1 FROM Transactions WHERE OrderId = 1004 AND Status = 'Failed')
BEGIN
    INSERT INTO Transactions (OrderId, TransactionCode, Amount, PaymentMethod, Status, CreatedAt, GatewayErrorCode, GatewayErrorMessage, RetryCount)
    VALUES (1004, 'TXN-FAILED-001', 322000, 'Card', 'Failed', GETDATE(), 'CARD_DECLINED', 'Thẻ không hợp lệ hoặc không đủ tiền', 0);
END

-- Transaction cho Order 1005 (Partially Paid - Split Bill)
IF NOT EXISTS (SELECT 1 FROM Transactions WHERE OrderId = 1005 AND PaymentMethod = 'Split')
BEGIN
    -- Parent transaction
    DECLARE @ParentTxnId INT;
    INSERT INTO Transactions (OrderId, TransactionCode, Amount, PaymentMethod, Status, CreatedAt, Notes)
    VALUES (1005, 'TXN-SPLIT-001', 690000, 'Split', 'PartiallyPaid', GETDATE(), 'Split bill thành 2 phần');
    SET @ParentTxnId = SCOPE_IDENTITY();

    -- Child transaction 1 (Paid)
    INSERT INTO Transactions (OrderId, ParentTransactionId, TransactionCode, Amount, AmountReceived, PaymentMethod, Status, CreatedAt, CompletedAt, IsManualConfirmed, ConfirmedByUserId)
    VALUES (1005, @ParentTxnId, 'TXN-SPLIT-001-1', 345000, 345000, 'Cash', 'Paid', GETDATE(), GETDATE(), 1, 1);

    -- Child transaction 2 (PaymentProcessing)
    INSERT INTO Transactions (OrderId, ParentTransactionId, TransactionCode, Amount, PaymentMethod, Status, CreatedAt)
    VALUES (1005, @ParentTxnId, 'TXN-SPLIT-001-2', 345000, 'QRBankTransfer', 'PaymentProcessing', GETDATE());
END

-- ============================================
-- 4. Tạo OrderLocks mẫu (để test locking)
-- ============================================

-- Lock cho Order 1003 (để test lock mechanism)
IF NOT EXISTS (SELECT 1 FROM OrderLocks WHERE OrderId = 1003)
BEGIN
    INSERT INTO OrderLocks (OrderId, LockedByUserId, SessionId, Reason, LockedAt, ExpiresAt)
    VALUES (1003, 1, 'SESSION-DEMO-001', 'Payment in progress - Demo', GETDATE(), DATEADD(MINUTE, 10, GETDATE()));
END

-- ============================================
-- 5. Tạo AuditLogs mẫu
-- ============================================

-- Audit logs cho các events
IF NOT EXISTS (SELECT 1 FROM AuditLogs WHERE EventType = 'payment_attempt' AND EntityId = 1004)
BEGIN
    INSERT INTO AuditLogs (UserId, EventType, EntityType, EntityId, Description, Metadata, CreatedAt)
    VALUES 
        (1, 'payment_attempt', 'Order', 1004, 'Payment attempt với Card - Failed', '{"PaymentMethod":"Card","Amount":322000,"Status":"Failed"}', GETDATE()),
        (1, 'payment_error', 'Order', 1004, 'Payment error: CARD_DECLINED', '{"ErrorCode":"CARD_DECLINED","ErrorMessage":"Thẻ không hợp lệ"}', GETDATE()),
        (1, 'order_locked', 'Order', 1003, 'Order locked để xử lý thanh toán', '{"SessionId":"SESSION-DEMO-001","Reason":"Payment in progress"}', GETDATE());
END

-- ============================================
-- 6. Summary
-- ============================================
PRINT '============================================';
PRINT 'SEED DATA ĐÃ ĐƯỢC TẠO THÀNH CÔNG!';
PRINT '============================================';
PRINT '';
PRINT 'Orders được tạo:';
PRINT '  - Order 1001: PendingPayment (517,500 VND) - Test Cash Payment';
PRINT '  - Order 1002: PendingPayment (828,000 VND) - Test Split Bill';
PRINT '  - Order 1003: PendingPayment (414,000 VND) - Test Status Polling (Đang bị lock)';
PRINT '  - Order 1004: PaymentProcessing (322,000 VND) - Test Retry Logic (Có failed transaction)';
PRINT '  - Order 1005: PartiallyPaid (690,000 VND) - Test Split Bill Completion';
PRINT '';
PRINT 'Test Cases:';
PRINT '  1. Cash Payment:';
PRINT '     - Order 1001: Test underpaid (nhập < 517,500)';
PRINT '     - Order 1001: Test overpaid (nhập > 517,500)';
PRINT '     - Order 1001: Test exact amount (nhập = 517,500)';
PRINT '';
PRINT '  2. Split Bill:';
PRINT '     - Order 1002: Chia thành 2-4 phần';
PRINT '     - Order 1005: Hoàn tất phần còn lại';
PRINT '';
PRINT '  3. Status Polling:';
PRINT '     - Order 1003: Poll status mỗi 5 giây';
PRINT '';
PRINT '  4. Retry Logic:';
PRINT '     - Order 1004: Retry failed payment';
PRINT '';
PRINT '  5. Order Locking:';
PRINT '     - Order 1003: Đang bị lock, không thể thêm món';
PRINT '';
PRINT '============================================';
GO

