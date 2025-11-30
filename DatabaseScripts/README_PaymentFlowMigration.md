# Payment Flow Migration Guide

## 📋 Overview

Migration này thêm các cột mới vào `Transactions` table và tạo 2 bảng mới: `AuditLogs` và `OrderLocks` để hỗ trợ comprehensive payment flow.

## 🗄️ Database Changes

### 1. Transactions Table Extensions
Thêm các cột mới:
- `AmountReceived` (DECIMAL(18,2)) - Số tiền khách đưa (cho Cash payment)
- `RefundAmount` (DECIMAL(18,2)) - Tiền thối lại (cho Cash payment)
- `GatewayErrorCode` (NVARCHAR(50)) - Mã lỗi từ gateway
- `GatewayErrorMessage` (NVARCHAR(500)) - Thông báo lỗi từ gateway
- `RetryCount` (INT, DEFAULT 0) - Số lần retry
- `LastRetryAt` (DATETIME) - Thời gian retry cuối cùng
- `ParentTransactionId` (INT) - ID transaction cha (cho Split Bill)
- `IsManualConfirmed` (BIT, DEFAULT 0) - Đã xác nhận thủ công
- `ConfirmedByUserId` (INT) - User ID xác nhận

### 2. AuditLogs Table (Mới)
- `AuditLogId` (INT, IDENTITY) - Primary Key
- `EventType` (NVARCHAR(100)) - Loại event
- `EntityType` (NVARCHAR(50)) - Loại entity
- `EntityId` (INT) - ID của entity
- `Description` (NVARCHAR(1000)) - Mô tả
- `Metadata` (NVARCHAR(MAX)) - Dữ liệu JSON
- `UserId` (INT) - User thực hiện
- `IpAddress` (NVARCHAR(50)) - IP address
- `CreatedAt` (DATETIME) - Thời gian tạo

### 3. OrderLocks Table (Mới)
- `OrderLockId` (INT, IDENTITY) - Primary Key
- `OrderId` (INT) - Order bị lock
- `LockedByUserId` (INT) - User lock order
- `SessionId` (NVARCHAR(100)) - Session ID
- `Reason` (NVARCHAR(500)) - Lý do lock
- `LockedAt` (DATETIME) - Thời gian lock
- `ExpiresAt` (DATETIME) - Thời gian hết hạn

## 🚀 Cách Chạy Migration

### Bước 1: Backup Database
```sql
BACKUP DATABASE SapaFoRestRMS 
TO DISK = 'C:\Backup\SapaFoRestRMS_BeforePaymentFlowMigration.bak';
```

### Bước 2: Chạy Migration Script
```sql
-- Mở SQL Server Management Studio (SSMS)
-- Kết nối đến database SapaFoRestRMS
-- Mở file: Backend/DatabaseScripts/20250115_PaymentFlowMigration.sql
-- Execute (F5)
```

### Bước 3: Verify Migration
```sql
-- Kiểm tra các cột mới trong Transactions
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Transactions'
AND COLUMN_NAME IN ('AmountReceived', 'RefundAmount', 'GatewayErrorCode', 
                    'GatewayErrorMessage', 'RetryCount', 'LastRetryAt', 
                    'ParentTransactionId', 'IsManualConfirmed', 'ConfirmedByUserId');

-- Kiểm tra bảng AuditLogs
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs';

-- Kiểm tra bảng OrderLocks
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrderLocks';

-- Kiểm tra Foreign Keys
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTableName
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fc ON fk.object_id = fc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) IN ('Transactions', 'AuditLogs', 'OrderLocks');
```

## ⚠️ Lưu Ý

1. **Script có thể chạy nhiều lần**: Script sử dụng `IF NOT EXISTS` để tránh duplicate
2. **Foreign Keys**: Các foreign keys sẽ được tạo tự động
3. **Indexes**: Các indexes sẽ được tạo để tối ưu performance
4. **Default Values**: Các cột có default values sẽ được set tự động

## 🔄 Rollback (Nếu Cần)

Nếu cần rollback migration:

```sql
-- Xóa Foreign Keys
ALTER TABLE Transactions DROP CONSTRAINT FK__Transactions__ParentTransactionId;
ALTER TABLE Transactions DROP CONSTRAINT FK__Transactions__ConfirmedByUserId;

-- Xóa các cột mới trong Transactions
ALTER TABLE Transactions DROP COLUMN AmountReceived;
ALTER TABLE Transactions DROP COLUMN RefundAmount;
ALTER TABLE Transactions DROP COLUMN GatewayErrorCode;
ALTER TABLE Transactions DROP COLUMN GatewayErrorMessage;
ALTER TABLE Transactions DROP COLUMN RetryCount;
ALTER TABLE Transactions DROP COLUMN LastRetryAt;
ALTER TABLE Transactions DROP COLUMN ParentTransactionId;
ALTER TABLE Transactions DROP COLUMN IsManualConfirmed;
ALTER TABLE Transactions DROP COLUMN ConfirmedByUserId;

-- Xóa các bảng mới
DROP TABLE IF EXISTS OrderLocks;
DROP TABLE IF EXISTS AuditLogs;
```

## ✅ Sau Khi Migration

1. **Update Application**: Đảm bảo application đã được update với:
   - Models mới (Transaction, AuditLog, OrderLock)
   - DbContext configuration
   - Service registration (IAuditLogService)

2. **Test**: Test các chức năng payment flow với seed data

## 📝 Related Files

- `Backend/DomainAccessLayer/Models/Transaction.cs` - Transaction model với các fields mới
- `Backend/DomainAccessLayer/Models/AuditLog.cs` - AuditLog model
- `Backend/DomainAccessLayer/Models/OrderLock.cs` - OrderLock model
- `Backend/DataAccessLayer/Dbcontext/SapaFoRestRmsContext.cs` - DbContext configuration
- `Backend/SapaFoRestRMSAPI/Program.cs` - Service registration

