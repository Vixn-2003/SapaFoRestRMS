# EF Core Migration - Payment Flow Extensions

## 📋 Overview

Migration này được tạo bằng **EF Core Code First** để thêm các cột mới vào `Transactions` table và tạo 2 bảng mới: `AuditLogs` và `OrderLocks`.

## 🗄️ Migration File

**File**: `Backend/DataAccessLayer/Migrations/20251113044342_AddPaymentFlowExtensions.cs`

## 🚀 Cách Sử Dụng Migration

### Bước 1: Review Migration
Kiểm tra file migration trước khi apply:
```bash
cd Backend/DataAccessLayer
# Xem file migration
cat Migrations/20251113044342_AddPaymentFlowExtensions.cs
```

### Bước 2: Apply Migration
```bash
# Từ thư mục DataAccessLayer
dotnet ef database update --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext
```

Hoặc từ Package Manager Console trong Visual Studio:
```powershell
Update-Database -Project DataAccessLayer -StartupProject SapaFoRestRMSAPI
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
```

## 🔄 Rollback Migration (Nếu Cần)

### Rollback về migration trước đó:
```bash
dotnet ef database update <PreviousMigrationName> --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext
```

### Xóa migration (chưa apply):
```bash
dotnet ef migrations remove --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext
```

## 📝 Database Changes

### 1. Transactions Table Extensions
- `AmountReceived` (DECIMAL(18,2), NULL)
- `RefundAmount` (DECIMAL(18,2), NULL)
- `GatewayErrorCode` (NVARCHAR(50), NULL)
- `GatewayErrorMessage` (NVARCHAR(500), NULL)
- `RetryCount` (INT, NOT NULL, DEFAULT 0)
- `LastRetryAt` (DATETIME, NULL)
- `ParentTransactionId` (INT, NULL) - FK to Transactions
- `IsManualConfirmed` (BIT, NOT NULL, DEFAULT 0)
- `ConfirmedByUserId` (INT, NULL) - FK to Users

### 2. AuditLogs Table (Mới)
- `AuditLogId` (INT, IDENTITY, PK)
- `EventType` (NVARCHAR(100), NOT NULL)
- `EntityType` (NVARCHAR(50), NOT NULL)
- `EntityId` (INT, NOT NULL)
- `Description` (NVARCHAR(1000), NULL)
- `Metadata` (NVARCHAR(MAX), NULL)
- `UserId` (INT, NULL) - FK to Users
- `IpAddress` (NVARCHAR(50), NULL)
- `CreatedAt` (DATETIME, NOT NULL, DEFAULT GETDATE())

**Indexes:**
- `IX_AuditLogs_EventType`
- `IX_AuditLogs_EntityType_EntityId`
- `IX_AuditLogs_CreatedAt`
- `IX_AuditLogs_UserId`

### 3. OrderLocks Table (Mới)
- `OrderLockId` (INT, IDENTITY, PK)
- `OrderId` (INT, NOT NULL) - FK to Orders
- `LockedByUserId` (INT, NOT NULL) - FK to Users
- `SessionId` (NVARCHAR(100), NULL)
- `Reason` (NVARCHAR(500), NOT NULL, DEFAULT 'Payment in progress')
- `LockedAt` (DATETIME, NOT NULL, DEFAULT GETDATE())
- `ExpiresAt` (DATETIME, NOT NULL)

**Indexes:**
- `IX_OrderLocks_OrderId`
- `IX_OrderLocks_ExpiresAt`

## 👥 Làm Việc Nhóm

### Khi Pull Code Mới:
1. **Pull code từ Git**
2. **Apply migration**:
   ```bash
   cd Backend/DataAccessLayer
   dotnet ef database update --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext
   ```

### Khi Tạo Migration Mới:
1. **Tạo migration**:
   ```bash
   cd Backend/DataAccessLayer
   dotnet ef migrations add <MigrationName> --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext
   ```
2. **Commit migration files** vào Git:
   - `Migrations/<timestamp>_<MigrationName>.cs`
   - `Migrations/<timestamp>_<MigrationName>.Designer.cs`
   - `Migrations/SapaFoRestRmsContextModelSnapshot.cs`

### ⚠️ Lưu Ý Quan Trọng:
- **KHÔNG** commit file `*.sql` migration thủ công
- **LUÔN** commit EF Core migration files (`.cs` files)
- **KHÔNG** edit migration files đã được commit (tạo migration mới thay vì sửa)
- **LUÔN** test migration trên local trước khi commit

## 🔍 Kiểm Tra Migration Status

```bash
# Xem danh sách migrations
dotnet ef migrations list --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext

# Xem migration script (SQL)
dotnet ef migrations script --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext

# Xem migration script từ một migration cụ thể
dotnet ef migrations script <FromMigration> <ToMigration> --startup-project ../SapaFoRestRMSAPI --context SapaFoRestRmsContext
```

## 📚 Related Files

- `Backend/DataAccessLayer/Migrations/20251113044342_AddPaymentFlowExtensions.cs` - Migration file
- `Backend/DataAccessLayer/Migrations/20251113044342_AddPaymentFlowExtensions.Designer.cs` - Designer file
- `Backend/DataAccessLayer/Migrations/SapaFoRestRmsContextModelSnapshot.cs` - Model snapshot
- `Backend/DataAccessLayer/Dbcontext/SapaFoRestRmsContext.cs` - DbContext configuration

## ✅ Checklist Trước Khi Commit

- [ ] Migration đã được test trên local database
- [ ] Migration files đã được add vào Git
- [ ] ModelSnapshot đã được update
- [ ] Không có conflict với migrations khác
- [ ] Code build thành công

