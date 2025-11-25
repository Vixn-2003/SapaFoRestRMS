-- =============================================
-- Migration: Payment Flow Extensions
-- Tạo: 2025-01-15
-- Mục đích: Thêm các cột mới vào Transactions, tạo AuditLogs và OrderLocks tables
-- =============================================

USE SapaFoRestRMS;
GO

-- =============================================
-- 1. Thêm các cột mới vào Transactions table
-- =============================================

-- Kiểm tra và thêm cột AmountReceived
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'AmountReceived')
BEGIN
    ALTER TABLE Transactions
    ADD AmountReceived DECIMAL(18, 2) NULL;
    PRINT 'Added column AmountReceived to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column AmountReceived already exists in Transactions table';
END
GO

-- Kiểm tra và thêm cột RefundAmount
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'RefundAmount')
BEGIN
    ALTER TABLE Transactions
    ADD RefundAmount DECIMAL(18, 2) NULL;
    PRINT 'Added column RefundAmount to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column RefundAmount already exists in Transactions table';
END
GO

-- Kiểm tra và thêm cột GatewayErrorCode
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'GatewayErrorCode')
BEGIN
    ALTER TABLE Transactions
    ADD GatewayErrorCode NVARCHAR(50) NULL;
    PRINT 'Added column GatewayErrorCode to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column GatewayErrorCode already exists in Transactions table';
END
GO

-- Kiểm tra và thêm cột GatewayErrorMessage
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'GatewayErrorMessage')
BEGIN
    ALTER TABLE Transactions
    ADD GatewayErrorMessage NVARCHAR(500) NULL;
    PRINT 'Added column GatewayErrorMessage to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column GatewayErrorMessage already exists in Transactions table';
END
GO

-- Kiểm tra và thêm cột RetryCount
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'RetryCount')
BEGIN
    ALTER TABLE Transactions
    ADD RetryCount INT NOT NULL DEFAULT 0;
    PRINT 'Added column RetryCount to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column RetryCount already exists in Transactions table';
END
GO

-- Kiểm tra và thêm cột LastRetryAt
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'LastRetryAt')
BEGIN
    ALTER TABLE Transactions
    ADD LastRetryAt DATETIME NULL;
    PRINT 'Added column LastRetryAt to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column LastRetryAt already exists in Transactions table';
END
GO

-- Kiểm tra và thêm cột ParentTransactionId
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'ParentTransactionId')
BEGIN
    ALTER TABLE Transactions
    ADD ParentTransactionId INT NULL;
    PRINT 'Added column ParentTransactionId to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column ParentTransactionId already exists in Transactions table';
END
GO

-- Kiểm tra và thêm cột IsManualConfirmed
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'IsManualConfirmed')
BEGIN
    ALTER TABLE Transactions
    ADD IsManualConfirmed BIT NOT NULL DEFAULT 0;
    PRINT 'Added column IsManualConfirmed to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column IsManualConfirmed already exists in Transactions table';
END
GO

-- Kiểm tra và thêm cột ConfirmedByUserId
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'ConfirmedByUserId')
BEGIN
    ALTER TABLE Transactions
    ADD ConfirmedByUserId INT NULL;
    PRINT 'Added column ConfirmedByUserId to Transactions table';
END
ELSE
BEGIN
    PRINT 'Column ConfirmedByUserId already exists in Transactions table';
END
GO

-- Thêm Foreign Key cho ParentTransactionId
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__Transactions__ParentTransactionId')
BEGIN
    ALTER TABLE Transactions
    ADD CONSTRAINT FK__Transactions__ParentTransactionId
    FOREIGN KEY (ParentTransactionId) REFERENCES Transactions(TransactionId)
    ON DELETE NO ACTION;
    PRINT 'Added Foreign Key FK__Transactions__ParentTransactionId';
END
ELSE
BEGIN
    PRINT 'Foreign Key FK__Transactions__ParentTransactionId already exists';
END
GO

-- Thêm Foreign Key cho ConfirmedByUserId
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK__Transactions__ConfirmedByUserId')
BEGIN
    ALTER TABLE Transactions
    ADD CONSTRAINT FK__Transactions__ConfirmedByUserId
    FOREIGN KEY (ConfirmedByUserId) REFERENCES Users(UserId)
    ON DELETE NO ACTION;
    PRINT 'Added Foreign Key FK__Transactions__ConfirmedByUserId';
END
ELSE
BEGIN
    PRINT 'Foreign Key FK__Transactions__ConfirmedByUserId already exists';
END
GO

-- =============================================
-- 2. Tạo AuditLogs table
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        AuditLogId INT IDENTITY(1,1) PRIMARY KEY,
        EventType NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId INT NOT NULL,
        Description NVARCHAR(1000) NULL,
        Metadata NVARCHAR(MAX) NULL,
        UserId INT NULL,
        IpAddress NVARCHAR(50) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK__AuditLogs__UserId FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION
    );
    
    -- Indexes
    CREATE INDEX IX_AuditLogs_EventType ON AuditLogs(EventType);
    CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
    CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);
    CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
    
    PRINT 'Created AuditLogs table';
END
ELSE
BEGIN
    PRINT 'AuditLogs table already exists';
END
GO

-- =============================================
-- 3. Tạo OrderLocks table
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrderLocks')
BEGIN
    CREATE TABLE OrderLocks (
        OrderLockId INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        LockedByUserId INT NOT NULL,
        SessionId NVARCHAR(100) NULL,
        Reason NVARCHAR(500) NOT NULL DEFAULT 'Payment in progress',
        LockedAt DATETIME NOT NULL DEFAULT GETDATE(),
        ExpiresAt DATETIME NOT NULL,
        
        CONSTRAINT FK__OrderLocks__OrderId FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
        CONSTRAINT FK__OrderLocks__LockedByUserId FOREIGN KEY (LockedByUserId) REFERENCES Users(UserId) ON DELETE NO ACTION
    );
    
    -- Indexes
    CREATE INDEX IX_OrderLocks_OrderId ON OrderLocks(OrderId);
    CREATE INDEX IX_OrderLocks_ExpiresAt ON OrderLocks(ExpiresAt);
    CREATE UNIQUE INDEX IX_OrderLocks_OrderId_Active ON OrderLocks(OrderId) WHERE ExpiresAt > GETDATE();
    
    PRINT 'Created OrderLocks table';
END
ELSE
BEGIN
    PRINT 'OrderLocks table already exists';
END
GO

-- =============================================
-- 4. Summary
-- =============================================

PRINT '========================================';
PRINT 'Payment Flow Migration completed!';
PRINT '========================================';
PRINT '';
PRINT 'Changes:';
PRINT '1. Extended Transactions table with new columns';
PRINT '2. Created AuditLogs table';
PRINT '3. Created OrderLocks table';
PRINT '';
PRINT 'To verify, run:';
PRINT 'SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = ''Transactions'';';
PRINT 'SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN (''AuditLogs'', ''OrderLocks'');';
PRINT '========================================';

