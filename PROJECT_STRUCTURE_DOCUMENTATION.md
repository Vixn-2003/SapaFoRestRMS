# SapaFoRestRMS - Tài liệu Cấu trúc Dự án

## 📋 Mục lục
1. [Tổng quan Dự án](#1-tổng-quan-dự-án)
2. [Kiến trúc Hệ thống](#2-kiến-trúc-hệ-thống)
3. [Backend Architecture](#3-backend-architecture)
4. [Frontend Architecture](#4-frontend-architecture)
5. [Danh sách Features/Modules](#5-danh-sách-featuresmodules)
6. [Chi tiết Cấu trúc từng Feature](#6-chi-tiết-cấu-trúc-từng-feature)
7. [Data Models](#7-data-models)
8. [API Endpoints](#8-api-endpoints)
9. [Real-time Communication](#9-real-time-communication)

---

## 1. Tổng quan Dự án

### 1.1 Giới thiệu
**SapaFoRestRMS** (Sapa Forest Restaurant Management System) là hệ thống quản lý nhà hàng toàn diện dành cho nhà hàng Sapa Rừng Xanh, giúp quản lý đặt bàn, thực đơn, khách hàng, nhân viên, kho và các hoạt động kinh doanh khác.

### 1.2 Công nghệ sử dụng
- **Backend**: ASP.NET Core Web API (.NET 8)
- **Frontend**: ASP.NET Core MVC (.NET 8)
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Real-time**: SignalR
- **Authentication**: JWT, Google OAuth, Phone OTP
- **Payment**: MoMo Integration
- **Cloud Storage**: Cloudinary
- **Dependency Injection**: Built-in ASP.NET Core DI

### 1.3 Vai trò người dùng
- **Customer**: Khách hàng (đặt bàn, order online)
- **Waiter**: Nhân viên phục vụ
- **Cashier/Counter Staff**: Nhân viên thu ngân
- **Kitchen Staff**: Nhân viên bếp
- **Manager**: Quản lý
- **Owner**: Chủ nhà hàng
- **Admin**: Quản trị viên hệ thống

---

## 2. Kiến trúc Hệ thống

### 2.1 Mô hình kiến trúc tổng thể
```
┌─────────────────────────────────────────────────────────────┐
│                    FRONTEND LAYER                            │
│  ┌──────────────────────┐  ┌──────────────────────────┐    │
│  │ WebSapaForestForStaff│  │ WebSapaFoRestForCustomer │    │
│  │   (Staff Portal)     │  │   (Customer Portal)      │    │
│  └──────────┬───────────┘  └───────────┬──────────────┘    │
└─────────────┼──────────────────────────┼───────────────────┘
              │                           │
              │        HTTP/HTTPS         │
              │        SignalR            │
              ▼                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    API LAYER                                 │
│              SapaFoRestRMSAPI (Controllers)                  │
│                   ┌───────────┐                              │
│                   │   Hubs    │ (SignalR)                    │
│                   └───────────┘                              │
└─────────────────────────┬───────────────────────────────────┘
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
┌─────────────────────────┐  ┌──────────────────┐
│  BUSINESS ACCESS LAYER  │  │  EXTERNAL APIS   │
│     (Services)          │  │  - MoMo Payment  │
│     (Mapping)           │  │  - Google Auth   │
│     (DTOs)              │  │  - Cloudinary    │
│     (Hubs)              │  │  - SMS/OTP       │
└──────────┬──────────────┘  └──────────────────┘
           │
           ▼
┌─────────────────────────┐
│  DATA ACCESS LAYER      │
│    (Repositories)       │
│    (Unit of Work)       │
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────────┐
│  DOMAIN ACCESS LAYER    │
│    (Models/Entities)    │
│    (Enums)              │
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────────┐
│      DATABASE           │
│    SQL Server           │
└─────────────────────────┘
```

### 2.2 Design Pattern sử dụng
- **Repository Pattern**: Truy xuất dữ liệu
- **Unit of Work Pattern**: Quản lý transactions
- **Service Layer Pattern**: Business logic
- **DTO Pattern**: Data Transfer Objects
- **Dependency Injection**: IoC Container
- **MVC Pattern**: Frontend
- **API Gateway Pattern**: API Layer

---

## 3. Backend Architecture

### 3.1 Cấu trúc Backend
```
Backend/
├── SapaFoRestRMSAPI/          # API Layer (Web API)
│   ├── Controllers/            # API Controllers (59 files)
│   ├── Hubs/                   # SignalR Hubs
│   ├── Services/               # API-specific services
│   └── Program.cs              # API Configuration
│
├── BusinessAccessLayer/        # Business Logic Layer
│   ├── Services/               # Business Services (130 files)
│   ├── DTOs/                   # Data Transfer Objects
│   ├── Mapping/                # AutoMapper Profiles
│   ├── Hubs/                   # SignalR Hub Logic
│   └── Constants/              # Business Constants
│
├── DataAccessLayer/            # Data Access Layer
│   ├── Repositories/           # Repository Interfaces & Implementations (107 files)
│   ├── UnitOfWork/             # Unit of Work Pattern
│   ├── Dbcontext/              # EF DbContext
│   └── Migrations/             # Database Migrations
│
└── DomainAccessLayer/          # Domain Layer
    ├── Models/                 # Entity Models (56 files)
    ├── Enums/                  # Enumerations (8 files)
    └── Common/                 # Common utilities
```

### 3.2 Các Layer và Trách nhiệm

#### 3.2.1 API Layer (SapaFoRestRMSAPI)
**Trách nhiệm:**
- Nhận HTTP requests từ clients
- Validation input
- Route requests đến Business Layer
- Trả về HTTP responses
- WebSocket communication (SignalR)

**Thành phần chính:**
- **Controllers**: 59 controllers xử lý các API endpoints
- **Hubs**: KitchenHub cho real-time updates
- **Program.cs**: Cấu hình DI, CORS, Authentication, SignalR

#### 3.2.2 Business Access Layer
**Trách nhiệm:**
- Xử lý business logic
- Validation business rules
- Data transformation (Entity ↔ DTO)
- External service integration
- Background jobs

**Thành phần chính:**
- **Services/** (130 services): Core business logic
- **DTOs/**: Data transfer objects cho communication giữa layers
- **Mapping/**: AutoMapper profiles
- **Hubs/**: SignalR hub logic (ReservationHub, RestaurantHub)

#### 3.2.3 Data Access Layer
**Trách nhiệm:**
- Database operations (CRUD)
- Query optimization
- Transaction management
- Database migrations

**Thành phần chính:**
- **Repositories/** (107 repositories): Generic + Specific repositories
- **UnitOfWork/**: Transaction management
- **DbContext/**: Entity Framework context
- **Migrations/**: Database schema changes

#### 3.2.4 Domain Access Layer
**Trách nhiệm:**
- Define domain entities
- Business enumerations
- Domain-level utilities

**Thành phần chính:**
- **Models/** (56 models): Database entities
- **Enums/** (8 enums): Domain enumerations

---

## 4. Frontend Architecture

### 4.1 Cấu trúc Frontend
```
Frontend/
├── WebSapaForestForStaff/      # Staff Portal (Internal)
│   ├── Controllers/             # MVC Controllers (44 files)
│   ├── Views/                   # Razor Views (133 files)
│   ├── Models/                  # View Models (66 files)
│   ├── DTOs/                    # Data Transfer Objects (156 files)
│   ├── Services/                # API Client Services (31 files)
│   ├── Hubs/                    # SignalR Client Hubs
│   ├── wwwroot/                 # Static files (JS, CSS, Images)
│   └── Program.cs               # App Configuration
│
├── WebSapaFoRestForCustomer/   # Customer Portal (Public)
│   ├── Controllers/             # MVC Controllers (8 files)
│   ├── Views/                   # Razor Views (16 files)
│   ├── Models/                  # View Models (8 files)
│   ├── DTOs/                    # Data Transfer Objects (11 files)
│   ├── Services/                # API Client Services
│   └── wwwroot/                 # Static files
│
└── ShiftManagementUI/           # Separate UI for Shift Management
    └── src/
```

### 4.2 Frontend Patterns
- **MVC Pattern**: Model-View-Controller
- **Service Pattern**: API communication
- **Repository Pattern**: Data fetching
- **SignalR Client**: Real-time updates
- **Responsive Design**: Bootstrap-based UI

---

## 5. Danh sách Features/Modules

### 5.1 Authentication & Authorization Module
**Actors**: All Users
**Components:**
- Controllers: `AuthController`
- Services: `AuthService`, `ExternalAuthService`, `PhoneAuthService`, `OtpService`
- Models: `User`, `Role`, `VerificationCode`

### 5.2 Staff Management Module
**Actors**: Manager, Owner, Admin
**Components:**
- Controllers: `StaffManagementController`, `StaffsController`, `StaffProfilesController`
- Services: `StaffManagementService`, `StaffProfileService`
- Models: `Staff`, `Position`, `Department`
- DTOs: `StaffDTO`, `StaffCreateDto`, `StaffUpdateDto`

### 5.3 Shift Management Module
**Actors**: Manager, Staff
**Components:**
- Controllers: `ShiftManagementController`, `ShiftController`, `ShiftAssignmentController`, `ShiftTemplateController`
- Services: `ShiftManagementService`, `ShiftService`, `ShiftAssignmentService`, `ShiftTemplateService`
- Models: `Shift`, `ShiftAssignment`, `ShiftTemplate`, `ShiftHistory`, `Attendance`
- DTOs: `ShiftDTO`, `ShiftTemplateDTO`, `ShiftAssignmentDto`

### 5.4 Table Management Module
**Actors**: Manager, Counter Staff, Waiter
**Components:**
- Controllers: `TableManagerController`, `AreaController`, `DashboardTableController`
- Services: `TableService`, `AreaService`, `DashboardTableService`
- Models: `Table`, `Area`
- DTOs: `TableDto`, `TableCreateDto`, `TableUpdateDto`, `AreaDto`

### 5.5 Reservation Module
**Actors**: Customer, Counter Staff, Manager
**Components:**
- Controllers: `ReservationController`, `ReservationStaffController`, `ReservationDepositController`
- Services: `ReservationService`, `ReservationDepositService`, `CapacityStatisticsService`
- Models: `Reservation`, `ReservationTable`, `ReservationDeposit`
- DTOs: `ReservationCreateDto`, `ReservationUpdateDto`, `ReservationDepositDto`
- Hubs: `ReservationHub`

### 5.6 Menu Management Module
**Actors**: Manager, Kitchen Staff
**Components:**
- Controllers: `ManagerMenuController`, `MenuItemController`, `ManagerCategoryController`
- Services: `ManagerMenuService`, `MenuItemService`, `ManagerCategoryService`
- Models: `MenuItem`, `MenuCategory`, `Recipe`, `Ingredient`
- DTOs: Menu-related DTOs

### 5.7 Combo Management Module
**Actors**: Manager
**Components:**
- Controllers: `ManagerComboController`, `CombosController`
- Services: `ManagerComboService`
- Models: `Combo`, `ComboItem`
- DTOs: `ComboDto`, `ComboCreateDto`, `ComboUpdateDto`

### 5.8 Order Management Module
**Actors**: Waiter, Counter Staff, Customer
**Components:**
- Controllers: `OrderTableController`, `CounterStaffOrderController`, `WaiterOrderTrackingController`
- Services: `OrderTableService`, `CounterStaffOrderService`, `WaiterOrderTrackingService`
- Models: `Order`, `OrderDetail`, `OrderComboItem`, `OrderHistory`, `OrderLock`
- DTOs: Order-related DTOs
- Hubs: `RestaurantHub`

### 5.9 Kitchen Display System (KDS) Module
**Actors**: Kitchen Staff
**Components:**
- Controllers: `KitchenDisplayController`
- Services: `KitchenDisplayService`
- Models: `KitchenTicket`, `KitchenTicketDetail`
- DTOs: `KitchenDisplayDTOs`
- Hubs: `KitchenHub`

### 5.10 Payment Module
**Actors**: Cashier, Counter Staff
**Components:**
- Controllers: `PaymentController`, `CounterTransactionController`, `CashierPaymentFlowController`
- Services: `PaymentService`, `MomoService`, `ReceiptService`, `CounterTransactionService`
- Models: `Payment`, `Transaction`
- DTOs: Payment-related DTOs (21 files)

### 5.11 Customer Management Module
**Actors**: Manager, Counter Staff
**Components:**
- Controllers: `CustomerManagementController`, `CustomerController`, `ManagerCustomerController`
- Services: `CustomerManagementService`, `CustomerVipService`
- Models: `Customer`
- DTOs: `CustomerManagementDto`, `CustomerCreateDto`, `CustomerUpdateDto`

### 5.12 Inventory Management Module
**Actors**: Manager, Warehouse Staff
**Components:**
- Controllers: `InventoryIngredientController`, `ImportIngredientController`, `ExportIngredientController`, `AuditInventoryController`, `WarehouseController`, `UnitController`
- Services: `InventoryIngredientService`, `InventoryAnalyticsService`, `WarehouseService`, `AuditService`, `StockTransactionService`, `UnitService`
- Models: `Ingredient`, `Warehouse`, `InventoryBatch`, `StockTransaction`, `AuditInventory`, `Unit`
- DTOs: Inventory-related DTOs (30 files)

### 5.13 Purchase Order Management Module
**Actors**: Manager, Warehouse Staff
**Components:**
- Controllers: `PurchaseOrderController`, `PurchaseOrderDetailController`, `SupplierController`
- Services: `PurchaseOrderService`, `PurchaseOrderDetailService`, `ManagerSupplierService`, `SupplierManagerService`
- Models: `PurchaseOrder`, `PurchaseOrderDetail`, `Supplier`
- DTOs: PurchaseOrder-related DTOs

### 5.14 Payroll Management Module
**Actors**: Owner, Manager
**Components:**
- Controllers: `PayrollController`, `SalaryChangeRequestController`
- Services: `PayrollService`, `SalaryChangeRequestService`
- Models: `Payroll`, `SalaryRule`, `SalaryChangeRequest`
- DTOs: `PayrollDTO`, `PayrollStatus`

### 5.15 Dashboard & Analytics Module
**Actors**: Owner, Manager
**Components:**
- Controllers: `OwnerDashboardController`, `OwnerRevenueController`, `CounterStaffDashboardController`, `StatisticsController`
- Services: `OwnerDashboardService`, `OwnerRevenueService`, `CounterStaffDashboardService`
- DTOs: `DashboardDataDto`

### 5.16 Warehouse Alerts Module
**Actors**: Owner, Manager
**Components:**
- Controllers: `OwnerWarehouseAlertController`
- Services: `OwnerWarehouseAlertService`, `ReorderLevelBackgroundJob`
- Models: `Ingredient` (with reorder levels)

### 5.17 Marketing Campaign Module
**Actors**: Manager, Owner
**Components:**
- Controllers: `MarketingCampaignsController`
- Services: `MarketingCampaignService`
- Models: `MarketingCampaign`, `Voucher`, `Event`
- DTOs: `MarketingCampaignDto`, `VoucherDto`, `EventDto`

### 5.18 Voucher Management Module
**Actors**: Manager, Cashier
**Components:**
- Controllers: `VoucherController`
- Services: `VoucherService`
- Models: `Voucher`
- DTOs: `VoucherCreateDto`, `VoucherUpdateDto`

### 5.19 Position Management Module
**Actors**: Manager, Owner
**Components:**
- Controllers: `PositionsController`
- Services: `PositionService`
- Models: `Position`, `Department`
- DTOs: Position-related DTOs (6 files)

### 5.20 User Management Module
**Actors**: Admin
**Components:**
- Controllers: `UsersController`
- Services: `UserService`, `UserManagementService`, `PasswordService`
- Models: `User`, `Role`
- DTOs: User-related DTOs (9 files)

### 5.21 Restaurant Configuration Module
**Actors**: Owner, Manager
**Components:**
- Controllers: `BrandBannerController`, `SystemLogoController`, `RestaurantIntroController`, `EventsController`
- Services: `BrandBannerService`, `SystemLogoService`, `RestaurantIntroService`, `EventService`, `CloudinaryService`
- Models: `BrandBanner`, `SystemLogo`, `RestaurantIntro`, `Event`
- DTOs: Configuration-related DTOs

### 5.22 User Profile Module
**Actors**: All authenticated users
**Components:**
- Controllers: `UserProfileController`
- Services: User-related services
- Models: `User`, `Staff`, `Customer`
- DTOs: `UserProfileDto`

### 5.23 Audit & Logging Module
**Actors**: Admin, Owner
**Components:**
- Services: `AuditLogService`
- Models: `AuditLog`

### 5.24 Day Type & Calendar Module
**Actors**: Manager
**Components:**
- Controllers: `DayTypeController`
- Services: `DayTypeService`
- Models: `DayType`, `DayCalendar`
- DTOs: `DayTypeDto`

---

## 6. Chi tiết Cấu trúc từng Feature

### 6.1 Authentication & Authorization Module

#### Class Diagram Components:
```
Controllers:
- AuthController (API)
  + Login(LoginDto)
  + Register(RegisterDto)
  + GoogleLogin(GoogleAuthDto)
  + PhoneLogin(PhoneAuthDto)
  + VerifyOtp(OtpDto)
  + RefreshToken(TokenDto)
  + Logout()

Services:
- AuthService
  + AuthenticateAsync(email, password)
  + RegisterAsync(registerDto)
  + GenerateJwtToken(user)
  + ValidateToken(token)
  
- ExternalAuthService
  + GoogleAuthenticateAsync(googleToken)
  
- PhoneAuthService
  + SendOtpAsync(phoneNumber)
  + VerifyOtpAsync(phoneNumber, otp)
  
- OtpService
  + GenerateOtp()
  + SendOtpSms(phoneNumber, otp)
  + ValidateOtp(phoneNumber, otp)

Repositories:
- IUserRepository
  + GetByEmailAsync(email)
  + GetByPhoneAsync(phone)
  + CreateAsync(user)
  + UpdateAsync(user)
  
- IVerificationCodeRepository
  + CreateAsync(code)
  + GetByPhoneAsync(phone)
  + DeleteAsync(id)

Models:
- User
  + UserId: int
  + Email: string
  + PasswordHash: string
  + PhoneNumber: string
  + RoleId: int
  + IsActive: bool
  + CreatedAt: DateTime
  
- Role
  + RoleId: int
  + RoleName: string
  + Description: string
  
- VerificationCode
  + Id: int
  + PhoneNumber: string
  + Code: string
  + ExpiresAt: DateTime
  + IsUsed: bool
```

#### Sequence Diagram Use Cases:
1. **UC-AUTH-01: User Login with Email/Password**
2. **UC-AUTH-02: User Login with Google**
3. **UC-AUTH-03: User Login with Phone OTP**
4. **UC-AUTH-04: User Registration**
5. **UC-AUTH-05: Token Refresh**
6. **UC-AUTH-06: User Logout**

---

### 6.2 Staff Management Module

#### Class Diagram Components:
```
Controllers:
- StaffManagementController (API)
  + GetAllStaff(filter, pagination)
  + GetStaffById(id)
  + CreateStaff(staffDto)
  + UpdateStaff(id, staffDto)
  + DeleteStaff(id)
  + GetStaffByDepartment(departmentId)
  + GetStaffByPosition(positionId)

Services:
- StaffManagementService
  + GetAllStaffAsync(filter, pagination)
  + GetStaffByIdAsync(id)
  + CreateStaffAsync(staffDto)
  + UpdateStaffAsync(id, staffDto)
  + DeleteStaffAsync(id)
  + AssignPositionAsync(staffId, positionId)
  + UpdateSalaryAsync(staffId, salaryDto)

- StaffProfileService
  + GetProfileAsync(staffId)
  + UpdateProfileAsync(staffId, profileDto)
  + UploadAvatarAsync(staffId, file)

Repositories:
- IStaffRepository
  + GetAllAsync(filter, pagination)
  + GetByIdAsync(id)
  + CreateAsync(staff)
  + UpdateAsync(staff)
  + DeleteAsync(id)
  + GetByDepartmentAsync(departmentId)
  + GetByPositionAsync(positionId)
  
- IPositionRepository
- IDepartmentRepository

Models:
- Staff
  + StaffId: int
  + UserId: int
  + FullName: string
  + PhoneNumber: string
  + Email: string
  + Address: string
  + DateOfBirth: DateTime
  + Gender: string
  + PositionId: int
  + DepartmentId: int
  + HireDate: DateTime
  + BaseSalary: decimal
  + Status: string
  + AvatarUrl: string
  
- Position
  + PositionId: int
  + PositionName: string
  + Description: string
  + BaseSalary: decimal
  
- Department
  + DepartmentId: int
  + DepartmentName: string
  + Description: string
```

#### Sequence Diagram Use Cases:
1. **UC-STAFF-01: View Staff List**
2. **UC-STAFF-02: Create New Staff**
3. **UC-STAFF-03: Update Staff Information**
4. **UC-STAFF-04: Delete Staff**
5. **UC-STAFF-05: Assign Position to Staff**
6. **UC-STAFF-06: Update Staff Salary**
7. **UC-STAFF-07: View Staff Profile**

---

### 6.3 Shift Management Module

#### Class Diagram Components:
```
Controllers:
- ShiftManagementController (API)
  + GetShifts(date, staffId)
  + GetShiftById(id)
  + CreateShift(shiftDto)
  + UpdateShift(id, shiftDto)
  + DeleteShift(id)
  + AssignStaff(shiftId, staffId)
  + UnassignStaff(assignmentId)
  + GetShiftTemplate()
  + CreateShiftTemplate(templateDto)

Services:
- ShiftManagementService
  + GetShiftsAsync(date, staffId)
  + CreateShiftAsync(shiftDto)
  + UpdateShiftAsync(id, shiftDto)
  + DeleteShiftAsync(id)
  + GetShiftCalendarAsync(startDate, endDate)
  
- ShiftAssignmentService
  + AssignStaffToShiftAsync(shiftId, staffId)
  + UnassignStaffFromShiftAsync(assignmentId)
  + GetStaffShiftsAsync(staffId, date)
  + CheckInAsync(assignmentId)
  + CheckOutAsync(assignmentId)
  
- ShiftTemplateService
  + GetTemplatesAsync()
  + CreateTemplateAsync(templateDto)
  + ApplyTemplateAsync(templateId, date)

Repositories:
- IShiftRepository
- IShiftAssignmentRepository
- IShiftTemplateRepository
- IShiftHistoryRepository
- IAttendanceRepository

Models:
- Shift
  + ShiftId: int
  + ShiftName: string
  + StartTime: TimeSpan
  + EndTime: TimeSpan
  + Date: DateTime
  + RequiredStaffCount: int
  + Status: ShiftStatus
  
- ShiftAssignment
  + AssignmentId: int
  + ShiftId: int
  + StaffId: int
  + AssignedAt: DateTime
  + CheckInTime: DateTime?
  + CheckOutTime: DateTime?
  + Status: string
  
- ShiftTemplate
  + TemplateId: int
  + TemplateName: string
  + ShiftPattern: string (JSON)
  + IsActive: bool
  
- ShiftHistory
  + HistoryId: int
  + ShiftId: int
  + StaffId: int
  + Action: string
  + Timestamp: DateTime
  
- Attendance
  + AttendanceId: int
  + StaffId: int
  + Date: DateTime
  + CheckInTime: DateTime
  + CheckOutTime: DateTime
  + WorkedHours: decimal
  + OvertimeHours: decimal
```

#### Sequence Diagram Use Cases:
1. **UC-SHIFT-01: View Shift Calendar**
2. **UC-SHIFT-02: Create New Shift**
3. **UC-SHIFT-03: Assign Staff to Shift**
4. **UC-SHIFT-04: Staff Check-in**
5. **UC-SHIFT-05: Staff Check-out**
6. **UC-SHIFT-06: Create Shift Template**
7. **UC-SHIFT-07: Apply Shift Template**
8. **UC-SHIFT-08: View Staff Attendance**

---

### 6.4 Table Management Module

#### Class Diagram Components:
```
Controllers:
- TableManagerController (API)
  + GetTables(areaId, status)
  + GetTableById(id)
  + CreateTable(tableDto)
  + UpdateTable(id, tableDto)
  + DeleteTable(id)
  + UpdateTableStatus(id, status)
  + GetTablesByArea(areaId)

- AreaController (API)
  + GetAreas()
  + CreateArea(areaDto)
  + UpdateArea(id, areaDto)
  + DeleteArea(id)

- DashboardTableController (API)
  + GetTableDashboard()
  + GetTableStatus()
  + GetAvailableTables(capacity, time)

Services:
- TableService
  + GetTablesAsync(filter)
  + CreateTableAsync(tableDto)
  + UpdateTableAsync(id, tableDto)
  + DeleteTableAsync(id)
  + UpdateTableStatusAsync(id, status)
  + AssignOrderAsync(tableId, orderId)
  
- AreaService
  + GetAreasAsync()
  + CreateAreaAsync(areaDto)
  + UpdateAreaAsync(id, areaDto)
  + DeleteAreaAsync(id)
  
- DashboardTableService
  + GetTableDashboardAsync()
  + GetTableOccupancyAsync()
  + GetAvailableTablesAsync(criteria)

Repositories:
- ITableRepository
  + GetAllAsync(filter)
  + GetByIdAsync(id)
  + CreateAsync(table)
  + UpdateAsync(table)
  + DeleteAsync(id)
  + GetByAreaAsync(areaId)
  + GetByStatusAsync(status)
  + GetAvailableTablesAsync(capacity, time)
  
- IAreaRepository
  + GetAllAsync()
  + GetByIdAsync(id)
  + CreateAsync(area)
  + UpdateAsync(area)
  + DeleteAsync(id)

Models:
- Table
  + TableId: int
  + TableNumber: string
  + AreaId: int
  + Capacity: int
  + Status: TableStatus (Available, Occupied, Reserved, Cleaning)
  + QrCode: string
  + IsActive: bool
  + CreatedAt: DateTime
  
- Area
  + AreaId: int
  + AreaName: string
  + Description: string
  + Floor: int
  + IsActive: bool
```

#### Sequence Diagram Use Cases:
1. **UC-TABLE-01: View All Tables**
2. **UC-TABLE-02: Create New Table**
3. **UC-TABLE-03: Update Table Status**
4. **UC-TABLE-04: Assign Order to Table**
5. **UC-TABLE-05: Check Table Availability**
6. **UC-TABLE-06: Manage Areas**
7. **UC-TABLE-07: View Table Dashboard**

---

### 6.5 Reservation Module

#### Class Diagram Components:
```
Controllers:
- ReservationController (API - Customer)
  + CreateReservation(reservationDto)
  + GetMyReservations()
  + GetReservationById(id)
  + CancelReservation(id)
  + UpdateReservation(id, reservationDto)

- ReservationStaffController (API - Staff)
  + GetAllReservations(filter)
  + GetReservationById(id)
  + ConfirmReservation(id)
  + RejectReservation(id, reason)
  + AssignTable(id, tableId)
  + CheckInReservation(id)
  + GetReservationCalendar(date)

- ReservationDepositController (API)
  + CreateDeposit(reservationId, depositDto)
  + ConfirmDeposit(depositId)
  + RefundDeposit(depositId)

Services:
- ReservationService
  + CreateReservationAsync(reservationDto)
  + GetReservationsAsync(filter)
  + GetReservationByIdAsync(id)
  + UpdateReservationAsync(id, reservationDto)
  + CancelReservationAsync(id)
  + ConfirmReservationAsync(id)
  + CheckInReservationAsync(id)
  + NotifyReservationAsync(id)
  
- ReservationDepositService
  + CreateDepositAsync(reservationId, depositDto)
  + ProcessDepositAsync(depositId, paymentDto)
  + RefundDepositAsync(depositId)
  
- CapacityStatisticsService
  + GetAvailableCapacityAsync(date, time)
  + GetReservationStatisticsAsync(date)

Repositories:
- IReservationRepository
  + GetAllAsync(filter, pagination)
  + GetByIdAsync(id)
  + CreateAsync(reservation)
  + UpdateAsync(reservation)
  + GetByCustomerAsync(customerId)
  + GetByDateAsync(date)
  + GetByStatusAsync(status)
  
- IReservationTableRepository
- IReservationDepositRepository

Models:
- Reservation
  + ReservationId: int
  + CustomerId: int
  + CustomerName: string
  + PhoneNumber: string
  + Email: string
  + ReservationDate: DateTime
  + ReservationTime: TimeSpan
  + NumberOfGuests: int
  + Status: string (Pending, Confirmed, CheckedIn, Completed, Cancelled)
  + SpecialRequests: string
  + DepositAmount: decimal
  + CreatedAt: DateTime
  
- ReservationTable
  + Id: int
  + ReservationId: int
  + TableId: int
  + AssignedAt: DateTime
  
- ReservationDeposit
  + DepositId: int
  + ReservationId: int
  + Amount: decimal
  + PaymentMethod: string
  + TransactionId: string
  + Status: string
  + PaidAt: DateTime

Hubs:
- ReservationHub (SignalR)
  + NotifyNewReservation(reservation)
  + NotifyReservationStatusChange(reservationId, status)
  + NotifyTableAssignment(reservationId, tableId)
```

#### Sequence Diagram Use Cases:
1. **UC-RES-01: Customer Create Reservation**
2. **UC-RES-02: Staff Confirm Reservation**
3. **UC-RES-03: Staff Assign Table to Reservation**
4. **UC-RES-04: Customer Check-in Reservation**
5. **UC-RES-05: Cancel Reservation**
6. **UC-RES-06: Process Deposit Payment**
7. **UC-RES-07: Refund Deposit**
8. **UC-RES-08: Real-time Reservation Notification**

---

### 6.6 Menu Management Module

#### Class Diagram Components:
```
Controllers:
- ManagerMenuController (API)
  + GetMenuItems(categoryId, filter)
  + GetMenuItemById(id)
  + CreateMenuItem(menuItemDto)
  + UpdateMenuItem(id, menuItemDto)
  + DeleteMenuItem(id)
  + UpdateMenuItemStatus(id, status)
  + UploadMenuImage(id, file)

- ManagerCategoryController (API)
  + GetCategories()
  + CreateCategory(categoryDto)
  + UpdateCategory(id, categoryDto)
  + DeleteCategory(id)

Services:
- ManagerMenuService
  + GetMenuItemsAsync(filter)
  + GetMenuItemByIdAsync(id)
  + CreateMenuItemAsync(menuItemDto)
  + UpdateMenuItemAsync(id, menuItemDto)
  + DeleteMenuItemAsync(id)
  + UpdateRecipeAsync(menuItemId, recipeDto)
  
- MenuItemService
  + GetAvailableMenuItemsAsync()
  + GetMenuItemDetailAsync(id)
  + CheckIngredientAvailabilityAsync(menuItemId)
  
- ManagerCategoryService
  + GetCategoriesAsync()
  + CreateCategoryAsync(categoryDto)
  + UpdateCategoryAsync(id, categoryDto)

Repositories:
- IMenuItemRepository
  + GetAllAsync(filter)
  + GetByIdAsync(id)
  + GetByCategoryAsync(categoryId)
  + CreateAsync(menuItem)
  + UpdateAsync(menuItem)
  + DeleteAsync(id)
  
- IMenuCategoryRepository
- IRecipeRepository
  + GetByMenuItemAsync(menuItemId)
  + CreateAsync(recipe)
  + UpdateAsync(recipe)

Models:
- MenuItem
  + MenuItemId: int
  + ItemName: string
  + Description: string
  + CategoryId: int
  + Price: decimal
  + ImageUrl: string
  + IsAvailable: bool
  + PreparationTime: int (minutes)
  + Calories: int
  + CreatedAt: DateTime
  
- MenuCategory
  + CategoryId: int
  + CategoryName: string
  + Description: string
  + DisplayOrder: int
  + IsActive: bool
  
- Recipe
  + RecipeId: int
  + MenuItemId: int
  + IngredientId: int
  + Quantity: decimal
  + UnitId: int
  
- Ingredient
  + IngredientId: int
  + IngredientName: string
  + UnitId: int
  + CurrentStock: decimal
  + MinimumStock: decimal
  + UnitPrice: decimal
```

#### Sequence Diagram Use Cases:
1. **UC-MENU-01: Create Menu Item**
2. **UC-MENU-02: Update Menu Item**
3. **UC-MENU-03: Delete Menu Item**
4. **UC-MENU-04: Update Menu Item Recipe**
5. **UC-MENU-05: Check Ingredient Availability**
6. **UC-MENU-06: Manage Menu Categories**
7. **UC-MENU-07: Upload Menu Item Image**

---

### 6.7 Order Management Module

#### Class Diagram Components:
```
Controllers:
- OrderTableController (API)
  + CreateOrder(orderDto)
  + GetOrderById(id)
  + UpdateOrder(id, orderDto)
  + AddOrderItem(orderId, itemDto)
  + RemoveOrderItem(orderId, itemId)
  + UpdateOrderStatus(orderId, status)
  + CancelOrder(orderId)

- CounterStaffOrderController (API)
  + GetAllOrders(filter)
  + GetOrdersByTable(tableId)
  + CreateWalkInOrder(orderDto)
  + ProcessOrder(orderId)

- WaiterOrderTrackingController (API)
  + GetMyOrders(waiterId)
  + GetOrdersByTable(tableId)
  + UpdateOrderItemStatus(itemId, status)
  + RequestAssistance(orderId, message)

Services:
- OrderTableService
  + CreateOrderAsync(orderDto)
  + GetOrderByIdAsync(id)
  + UpdateOrderAsync(id, orderDto)
  + AddOrderItemAsync(orderId, itemDto)
  + RemoveOrderItemAsync(orderId, itemId)
  + CalculateOrderTotalAsync(orderId)
  + UpdateOrderStatusAsync(orderId, status)
  
- CounterStaffOrderService
  + GetAllOrdersAsync(filter)
  + CreateWalkInOrderAsync(orderDto)
  + ProcessOrderAsync(orderId)
  
- WaiterOrderTrackingService
  + GetWaiterOrdersAsync(waiterId)
  + UpdateOrderItemStatusAsync(itemId, status)
  + SendAssistanceRequestAsync(orderId, message)

Repositories:
- IOrderRepository
  + GetAllAsync(filter, pagination)
  + GetByIdAsync(id)
  + GetByTableAsync(tableId)
  + GetByCustomerAsync(customerId)
  + CreateAsync(order)
  + UpdateAsync(order)
  
- IOrderDetailRepository
  + GetByOrderAsync(orderId)
  + CreateAsync(orderDetail)
  + UpdateAsync(orderDetail)
  + DeleteAsync(id)
  
- IOrderHistoryRepository
- IOrderLockRepository

Models:
- Order
  + OrderId: int
  + OrderNumber: string
  + TableId: int?
  + CustomerId: int?
  + WaiterId: int?
  + OrderType: string (DineIn, TakeOut, Delivery)
  + Status: string (Pending, Confirmed, Preparing, Ready, Served, Completed, Cancelled)
  + SubTotal: decimal
  + DiscountAmount: decimal
  + TaxAmount: decimal
  + TotalAmount: decimal
  + OrderTime: DateTime
  + CompletedAt: DateTime?
  + Notes: string
  
- OrderDetail
  + OrderDetailId: int
  + OrderId: int
  + MenuItemId: int?
  + ComboId: int?
  + Quantity: int
  + UnitPrice: decimal
  + Subtotal: decimal
  + Status: string
  + SpecialInstructions: string
  
- OrderComboItem
  + Id: int
  + OrderDetailId: int
  + ComboId: int
  + MenuItemId: int
  + Quantity: int
  
- OrderHistory
  + HistoryId: int
  + OrderId: int
  + Action: string
  + Timestamp: DateTime
  + UserId: int
  
- OrderLock
  + LockId: int
  + OrderId: int
  + UserId: int
  + LockedAt: DateTime

Hubs:
- RestaurantHub (SignalR)
  + NotifyNewOrder(orderId)
  + NotifyOrderStatusChange(orderId, status)
  + NotifyOrderItemUpdate(orderDetailId, status)
```

#### Sequence Diagram Use Cases:
1. **UC-ORDER-01: Customer Place Order**
2. **UC-ORDER-02: Waiter Create Table Order**
3. **UC-ORDER-03: Add Item to Order**
4. **UC-ORDER-04: Remove Item from Order**
5. **UC-ORDER-05: Update Order Status**
6. **UC-ORDER-06: Cancel Order**
7. **UC-ORDER-07: Calculate Order Total**
8. **UC-ORDER-08: Real-time Order Updates**

---

### 6.8 Kitchen Display System (KDS) Module

#### Class Diagram Components:
```
Controllers:
- KitchenDisplayController (API)
  + GetPendingOrders()
  + GetOrderById(orderId)
  + UpdateTicketStatus(ticketId, status)
  + UpdateTicketItemStatus(itemId, status)
  + GetKitchenStatistics()

Services:
- KitchenDisplayService
  + GetPendingOrdersAsync()
  + GetOrderDetailsAsync(orderId)
  + UpdateTicketStatusAsync(ticketId, status)
  + UpdateTicketItemStatusAsync(itemId, status)
  + GetKitchenStatisticsAsync()
  + NotifyOrderReadyAsync(orderId)

Repositories:
- IKitchenTicketRepository
  + GetPendingTicketsAsync()
  + GetByOrderAsync(orderId)
  + CreateAsync(ticket)
  + UpdateAsync(ticket)
  
- IKitchenTicketDetailRepository

Models:
- KitchenTicket
  + TicketId: int
  + OrderId: int
  + TicketNumber: string
  + TableNumber: string
  + Priority: int
  + Status: string (Pending, InProgress, Completed)
  + CreatedAt: DateTime
  + StartedAt: DateTime?
  + CompletedAt: DateTime?
  
- KitchenTicketDetail
  + DetailId: int
  + TicketId: int
  + MenuItemId: int
  + Quantity: int
  + Status: string
  + SpecialInstructions: string
  + StartedAt: DateTime?
  + CompletedAt: DateTime?

Hubs:
- KitchenHub (SignalR)
  + NotifyNewKitchenTicket(ticket)
  + NotifyTicketStatusChange(ticketId, status)
  + NotifyTicketItemReady(itemId)
```

#### Sequence Diagram Use Cases:
1. **UC-KDS-01: Display Pending Orders**
2. **UC-KDS-02: Start Preparing Order**
3. **UC-KDS-03: Mark Item as Completed**
4. **UC-KDS-04: Complete Ticket**
5. **UC-KDS-05: Real-time Kitchen Updates**

---

### 6.9 Payment Module

#### Class Diagram Components:
```
Controllers:
- PaymentController (API)
  + CreatePayment(paymentDto)
  + GetPaymentById(id)
  + GetPaymentsByOrder(orderId)
  + ProcessPayment(id)
  + RefundPayment(id, refundDto)
  + GetPaymentMethods()

- CounterTransactionController (API)
  + GetTransactions(filter)
  + GetTransactionById(id)
  + CreateTransaction(transactionDto)
  + VoidTransaction(id)

Services:
- PaymentService
  + CreatePaymentAsync(paymentDto)
  + ProcessPaymentAsync(id)
  + ProcessCashPaymentAsync(paymentDto)
  + ProcessCardPaymentAsync(paymentDto)
  + ProcessMomoPaymentAsync(paymentDto)
  + RefundPaymentAsync(id, refundDto)
  + SplitPaymentAsync(orderId, splitDto)
  
- MomoService
  + CreatePaymentRequestAsync(amount, orderId)
  + VerifyPaymentAsync(momoResponse)
  + RefundPaymentAsync(transactionId, amount)
  
- ReceiptService
  + GenerateReceiptAsync(paymentId)
  + PrintReceiptAsync(paymentId)
  + SendReceiptEmailAsync(paymentId, email)
  
- CounterTransactionService
  + GetTransactionsAsync(filter)
  + CreateTransactionAsync(transactionDto)
  + VoidTransactionAsync(id)

Repositories:
- IPaymentRepository
  + GetAllAsync(filter)
  + GetByIdAsync(id)
  + GetByOrderAsync(orderId)
  + CreateAsync(payment)
  + UpdateAsync(payment)
  
- ITransactionRepository
  + GetAllAsync(filter)
  + GetByIdAsync(id)
  + CreateAsync(transaction)
  + UpdateAsync(transaction)

Models:
- Payment
  + PaymentId: int
  + OrderId: int
  + PaymentNumber: string
  + TotalAmount: decimal
  + PaidAmount: decimal
  + ChangeAmount: decimal
  + PaymentMethod: PaymentMethod (Cash, Card, MoMo, Transfer)
  + Status: PaymentStatus (Pending, Completed, Failed, Refunded)
  + TransactionId: string
  + PaymentTime: DateTime
  + VoucherId: int?
  + DiscountAmount: decimal
  
- Transaction
  + TransactionId: int
  + PaymentId: int
  + Amount: decimal
  + TransactionType: string
  + TransactionNumber: string
  + Timestamp: DateTime
  + Status: string
  + UserId: int
```

#### Sequence Diagram Use Cases:
1. **UC-PAY-01: Process Cash Payment**
2. **UC-PAY-02: Process Card Payment**
3. **UC-PAY-03: Process MoMo Payment**
4. **UC-PAY-04: Apply Voucher to Payment**
5. **UC-PAY-05: Split Payment**
6. **UC-PAY-06: Refund Payment**
7. **UC-PAY-07: Generate Receipt**
8. **UC-PAY-08: Print Receipt**

---

### 6.10 Customer Management Module

#### Class Diagram Components:
```
Controllers:
- CustomerManagementController (API)
  + GetAllCustomers(filter, pagination)
  + GetCustomerById(id)
  + CreateCustomer(customerDto)
  + UpdateCustomer(id, customerDto)
  + DeleteCustomer(id)
  + UpgradeToVip(id)
  + DowngradeFromVip(id)
  + GetCustomerHistory(id)

Services:
- CustomerManagementService
  + GetAllCustomersAsync(filter, pagination)
  + GetCustomerByIdAsync(id)
  + CreateCustomerAsync(customerDto)
  + UpdateCustomerAsync(id, customerDto)
  + DeleteCustomerAsync(id)
  + GetCustomerHistoryAsync(id)
  
- CustomerVipService
  + UpgradeToVipAsync(customerId)
  + DowngradeFromVipAsync(customerId)
  + GetVipBenefitsAsync(customerId)
  + CalculateLoyaltyPointsAsync(customerId, orderId)

Repositories:
- ICustomerRepository
  + GetAllAsync(filter, pagination)
  + GetByIdAsync(id)
  + GetByPhoneAsync(phone)
  + GetByEmailAsync(email)
  + CreateAsync(customer)
  + UpdateAsync(customer)
  + DeleteAsync(id)

Models:
- Customer
  + CustomerId: int
  + UserId: int?
  + FullName: string
  + PhoneNumber: string
  + Email: string
  + DateOfBirth: DateTime?
  + Gender: string
  + Address: string
  + IsVip: bool
  + LoyaltyPoints: int
  + TotalSpent: decimal
  + VisitCount: int
  + LastVisitDate: DateTime?
  + RegisteredDate: DateTime
  + Notes: string
```

#### Sequence Diagram Use Cases:
1. **UC-CUST-01: View Customer List**
2. **UC-CUST-02: Create Customer Profile**
3. **UC-CUST-03: Update Customer Information**
4. **UC-CUST-04: Upgrade Customer to VIP**
5. **UC-CUST-05: View Customer History**
6. **UC-CUST-06: Calculate Loyalty Points**

---

### 6.11 Inventory Management Module

#### Class Diagram Components:
```
Controllers:
- InventoryIngredientController (API)
  + GetIngredients(filter)
  + GetIngredientById(id)
  + CreateIngredient(ingredientDto)
  + UpdateIngredient(id, ingredientDto)
  + DeleteIngredient(id)
  + UpdateStock(id, stockDto)
  + GetLowStockItems()

- ImportIngredientController (API)
  + GetImports(filter)
  + CreateImport(importDto)
  + GetImportById(id)
  + ApproveImport(id)

- ExportIngredientController (API)
  + GetExports(filter)
  + CreateExport(exportDto)
  + GetExportById(id)

- AuditInventoryController (API)
  + GetAudits(filter)
  + CreateAudit(auditDto)
  + ProcessAudit(id, auditDto)

- WarehouseController (API)
  + GetWarehouses()
  + CreateWarehouse(warehouseDto)
  + UpdateWarehouse(id, warehouseDto)

Services:
- InventoryIngredientService
  + GetIngredientsAsync(filter)
  + CreateIngredientAsync(ingredientDto)
  + UpdateIngredientAsync(id, ingredientDto)
  + UpdateStockAsync(id, quantity)
  + CheckStockAvailabilityAsync(ingredientId, quantity)
  
- InventoryAnalyticsService
  + GetLowStockItemsAsync()
  + GetInventoryValueAsync()
  + GetStockMovementAsync(period)
  
- WarehouseService
  + GetWarehousesAsync()
  + CreateWarehouseAsync(warehouseDto)
  + TransferStockAsync(fromWarehouse, toWarehouse, items)
  
- AuditService
  + CreateAuditAsync(auditDto)
  + ProcessAuditAsync(id, auditDto)
  + GetAuditHistoryAsync(filter)
  
- StockTransactionService
  + CreateTransactionAsync(transactionDto)
  + GetTransactionsAsync(filter)

Repositories:
- IIngredientRepository
- IWarehouseRepository
- IInventoryBatchRepository
- IStockTransactionRepository
- IAuditInventoryRepository

Models:
- Ingredient
  + IngredientId: int
  + IngredientName: string
  + CategoryId: int?
  + UnitId: int
  + CurrentStock: decimal
  + MinimumStock: decimal
  + MaximumStock: decimal
  + ReorderLevel: decimal
  + UnitPrice: decimal
  + LastRestockedDate: DateTime?
  
- Warehouse
  + WarehouseId: int
  + WarehouseName: string
  + Location: string
  + Capacity: decimal
  + IsActive: bool
  
- InventoryBatch
  + BatchId: int
  + IngredientId: int
  + WarehouseId: int
  + Quantity: decimal
  + UnitPrice: decimal
  + ManufactureDate: DateTime?
  + ExpiryDate: DateTime?
  + BatchNumber: string
  + SupplierId: int
  
- StockTransaction
  + TransactionId: int
  + IngredientId: int
  + TransactionType: string (Import, Export, Adjustment, Transfer)
  + Quantity: decimal
  + UnitPrice: decimal
  + TotalAmount: decimal
  + ReferenceId: int?
  + Timestamp: DateTime
  + UserId: int
  + Notes: string
  
- AuditInventory
  + AuditId: int
  + AuditDate: DateTime
  + WarehouseId: int
  + Status: string
  + ConductedBy: int
  + Notes: string
```

#### Sequence Diagram Use Cases:
1. **UC-INV-01: Create Ingredient**
2. **UC-INV-02: Import Ingredients**
3. **UC-INV-03: Export Ingredients**
4. **UC-INV-04: Update Stock Level**
5. **UC-INV-05: Check Low Stock Alerts**
6. **UC-INV-06: Conduct Inventory Audit**
7. **UC-INV-07: Transfer Stock Between Warehouses**
8. **UC-INV-08: View Inventory Analytics**

---

### 6.12 Purchase Order Management Module

#### Class Diagram Components:
```
Controllers:
- PurchaseOrderController (API)
  + GetPurchaseOrders(filter)
  + GetPurchaseOrderById(id)
  + CreatePurchaseOrder(poDto)
  + UpdatePurchaseOrder(id, poDto)
  + ApprovePurchaseOrder(id)
  + CancelPurchaseOrder(id)
  + ReceivePurchaseOrder(id, receiveDto)

- SupplierController (API)
  + GetSuppliers()
  + GetSupplierById(id)
  + CreateSupplier(supplierDto)
  + UpdateSupplier(id, supplierDto)
  + DeleteSupplier(id)

Services:
- PurchaseOrderService
  + GetPurchaseOrdersAsync(filter)
  + CreatePurchaseOrderAsync(poDto)
  + UpdatePurchaseOrderAsync(id, poDto)
  + ApprovePurchaseOrderAsync(id)
  + ReceivePurchaseOrderAsync(id, receiveDto)
  
- PurchaseOrderDetailService
  + AddDetailAsync(poId, detailDto)
  + UpdateDetailAsync(id, detailDto)
  + RemoveDetailAsync(id)
  
- ManagerSupplierService
  + GetSuppliersAsync()
  + CreateSupplierAsync(supplierDto)
  + UpdateSupplierAsync(id, supplierDto)
  + EvaluateSupplierAsync(id, evaluationDto)

Repositories:
- IPurchaseOrderRepository
- IPurchaseOrderDetailRepository
- ISupplierRepository

Models:
- PurchaseOrder
  + PurchaseOrderId: int
  + OrderNumber: string
  + SupplierId: int
  + OrderDate: DateTime
  + ExpectedDeliveryDate: DateTime
  + ActualDeliveryDate: DateTime?
  + Status: string (Draft, Pending, Approved, Received, Cancelled)
  + SubTotal: decimal
  + TaxAmount: decimal
  + TotalAmount: decimal
  + CreatedBy: int
  + ApprovedBy: int?
  + Notes: string
  
- PurchaseOrderDetail
  + DetailId: int
  + PurchaseOrderId: int
  + IngredientId: int
  + Quantity: decimal
  + UnitPrice: decimal
  + Subtotal: decimal
  + ReceivedQuantity: decimal
  + UnitId: int
  
- Supplier
  + SupplierId: int
  + SupplierName: string
  + ContactPerson: string
  + PhoneNumber: string
  + Email: string
  + Address: string
  + TaxCode: string
  + PaymentTerms: string
  + Rating: decimal
  + IsActive: bool
```

#### Sequence Diagram Use Cases:
1. **UC-PO-01: Create Purchase Order**
2. **UC-PO-02: Approve Purchase Order**
3. **UC-PO-03: Receive Purchase Order**
4. **UC-PO-04: Cancel Purchase Order**
5. **UC-PO-05: Manage Suppliers**

---

### 6.13 Payroll Management Module

#### Class Diagram Components:
```
Controllers:
- PayrollController (API)
  + GetPayrolls(filter)
  + GetPayrollById(id)
  + CalculatePayroll(period)
  + ApprovePayroll(id)
  + ProcessPayment(id)
  + GetPayrollByStaff(staffId, period)

- SalaryChangeRequestController (API)
  + GetRequests(filter)
  + CreateRequest(requestDto)
  + ApproveRequest(id)
  + RejectRequest(id, reason)

Services:
- PayrollService
  + CalculatePayrollAsync(period)
  + GetPayrollsAsync(filter)
  + ApprovePayrollAsync(id)
  + ProcessPaymentAsync(id)
  + CalculateOvertime(staffId, period)
  + CalculateBonus(staffId, period)
  
- SalaryChangeRequestService
  + CreateRequestAsync(requestDto)
  + ApproveRequestAsync(id)
  + RejectRequestAsync(id, reason)
  + GetRequestsAsync(filter)

Repositories:
- IPayrollRepository
- ISalaryRuleRepository
- ISalaryChangeRequestRepository
- IAttendanceRepository

Models:
- Payroll
  + PayrollId: int
  + StaffId: int
  + Period: string (YYYY-MM)
  + BaseSalary: decimal
  + OvertimeHours: decimal
  + OvertimePay: decimal
  + BonusAmount: decimal
  + DeductionAmount: decimal
  + TotalSalary: decimal
  + Status: string (Pending, Approved, Paid)
  + ApprovedBy: int?
  + PaymentDate: DateTime?
  + Notes: string
  
- SalaryRule
  + RuleId: int
  + RuleName: string
  + OvertimeRate: decimal
  + PositionId: int?
  + EffectiveDate: DateTime
  
- SalaryChangeRequest
  + RequestId: int
  + StaffId: int
  + CurrentSalary: decimal
  + RequestedSalary: decimal
  + Reason: string
  + Status: string
  + RequestedDate: DateTime
  + ReviewedBy: int?
  + ReviewedDate: DateTime?
```

#### Sequence Diagram Use Cases:
1. **UC-PAY-01: Calculate Monthly Payroll**
2. **UC-PAY-02: Approve Payroll**
3. **UC-PAY-03: Process Salary Payment**
4. **UC-PAY-04: Request Salary Change**
5. **UC-PAY-05: Approve/Reject Salary Change**

---

### 6.14 Dashboard & Analytics Module

#### Class Diagram Components:
```
Controllers:
- OwnerDashboardController (API)
  + GetDashboardOverview()
  + GetDailyStatistics(date)
  + GetMonthlyStatistics(month)
  
- OwnerRevenueController (API)
  + GetRevenueReport(startDate, endDate)
  + GetRevenueByPeriod(period)
  + GetRevenueByCategory()
  + GetTopSellingItems(limit)

- CounterStaffDashboardController (API)
  + GetTodayOrders()
  + GetTodayRevenue()
  + GetTableStatus()

Services:
- OwnerDashboardService
  + GetDashboardOverviewAsync()
  + GetDailyStatisticsAsync(date)
  + GetMonthlyStatisticsAsync(month)
  + GetYearlyStatisticsAsync(year)
  
- OwnerRevenueService
  + GetRevenueReportAsync(startDate, endDate)
  + GetRevenueByPeriodAsync(period)
  + GetRevenueByCategoryAsync()
  + GetTopSellingItemsAsync(limit)
  + GetCustomerInsightsAsync()
  
- CounterStaffDashboardService
  + GetTodayOrdersAsync()
  + GetTodayRevenueAsync()
  + GetTableStatusAsync()

Models:
- DashboardData (DTO)
  + TotalRevenue: decimal
  + TotalOrders: int
  + TotalCustomers: int
  + AverageOrderValue: decimal
  + TopSellingItems: List<BestSellerDto>
  + RevenueByPeriod: List<RevenueDto>
```

#### Sequence Diagram Use Cases:
1. **UC-DASH-01: View Owner Dashboard**
2. **UC-DASH-02: Generate Revenue Report**
3. **UC-DASH-03: View Top Selling Items**
4. **UC-DASH-04: View Counter Staff Dashboard**

---

## 7. Data Models

### 7.1 Core Entities

#### User Management
```
User
├── UserId (PK)
├── Email
├── PasswordHash
├── PhoneNumber
├── RoleId (FK -> Role)
├── IsActive
└── CreatedAt

Role
├── RoleId (PK)
├── RoleName
└── Description

Staff
├── StaffId (PK)
├── UserId (FK -> User)
├── FullName
├── PositionId (FK -> Position)
├── DepartmentId (FK -> Department)
├── BaseSalary
└── HireDate

Customer
├── CustomerId (PK)
├── UserId (FK -> User)
├── FullName
├── PhoneNumber
├── IsVip
└── LoyaltyPoints
```

#### Restaurant Operations
```
Table
├── TableId (PK)
├── TableNumber
├── AreaId (FK -> Area)
├── Capacity
└── Status

Area
├── AreaId (PK)
├── AreaName
└── Floor

Order
├── OrderId (PK)
├── TableId (FK -> Table)
├── CustomerId (FK -> Customer)
├── WaiterId (FK -> Staff)
├── Status
├── TotalAmount
└── OrderTime

OrderDetail
├── OrderDetailId (PK)
├── OrderId (FK -> Order)
├── MenuItemId (FK -> MenuItem)
├── Quantity
└── UnitPrice

MenuItem
├── MenuItemId (PK)
├── ItemName
├── CategoryId (FK -> MenuCategory)
├── Price
└── IsAvailable

Reservation
├── ReservationId (PK)
├── CustomerId (FK -> Customer)
├── ReservationDate
├── NumberOfGuests
└── Status
```

#### Inventory
```
Ingredient
├── IngredientId (PK)
├── IngredientName
├── UnitId (FK -> Unit)
├── CurrentStock
├── MinimumStock
└── UnitPrice

PurchaseOrder
├── PurchaseOrderId (PK)
├── SupplierId (FK -> Supplier)
├── OrderDate
├── Status
└── TotalAmount

PurchaseOrderDetail
├── DetailId (PK)
├── PurchaseOrderId (FK)
├── IngredientId (FK)
├── Quantity
└── UnitPrice

Warehouse
├── WarehouseId (PK)
├── WarehouseName
└── Location

StockTransaction
├── TransactionId (PK)
├── IngredientId (FK)
├── TransactionType
├── Quantity
└── Timestamp
```

#### HR & Payroll
```
Shift
├── ShiftId (PK)
├── ShiftName
├── StartTime
├── EndTime
└── Date

ShiftAssignment
├── AssignmentId (PK)
├── ShiftId (FK -> Shift)
├── StaffId (FK -> Staff)
└── Status

Attendance
├── AttendanceId (PK)
├── StaffId (FK -> Staff)
├── Date
├── CheckInTime
└── CheckOutTime

Payroll
├── PayrollId (PK)
├── StaffId (FK -> Staff)
├── Period
├── BaseSalary
├── TotalSalary
└── Status
```

#### Payment
```
Payment
├── PaymentId (PK)
├── OrderId (FK -> Order)
├── PaymentMethod
├── TotalAmount
├── Status
└── PaymentTime

Transaction
├── TransactionId (PK)
├── PaymentId (FK -> Payment)
├── Amount
└── Timestamp

Voucher
├── VoucherId (PK)
├── VoucherCode
├── DiscountType
├── DiscountValue
├── ValidFrom
└── ValidTo
```

### 7.2 Enumerations

```csharp
// TableStatus
public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    Cleaning
}

// PaymentMethod
public enum PaymentMethod
{
    Cash,
    Card,
    MoMo,
    BankTransfer
}

// PaymentStatus
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

// ShiftStatus
public enum ShiftStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

// Roles
public enum Roles
{
    Admin,
    Owner,
    Manager,
    Waiter,
    Cashier,
    KitchenStaff,
    Customer
}

// UnitType
public enum UnitType
{
    Kg,
    Gram,
    Liter,
    Piece,
    Box
}

// ItemBillingType
public enum ItemBillingType
{
    PerUnit,
    PerWeight,
    PerVolume
}
```

---

## 8. API Endpoints

### 8.1 Authentication Endpoints
```
POST   /api/auth/login
POST   /api/auth/register
POST   /api/auth/google-login
POST   /api/auth/phone-login
POST   /api/auth/verify-otp
POST   /api/auth/refresh-token
POST   /api/auth/logout
POST   /api/auth/forgot-password
POST   /api/auth/reset-password
```

### 8.2 Staff Management Endpoints
```
GET    /api/staffmanagement
GET    /api/staffmanagement/{id}
POST   /api/staffmanagement
PUT    /api/staffmanagement/{id}
DELETE /api/staffmanagement/{id}
GET    /api/staffmanagement/by-department/{departmentId}
GET    /api/staffmanagement/by-position/{positionId}
PUT    /api/staffmanagement/{id}/assign-position
PUT    /api/staffmanagement/{id}/salary
```

### 8.3 Shift Management Endpoints
```
GET    /api/shiftmanagement
GET    /api/shiftmanagement/{id}
POST   /api/shiftmanagement
PUT    /api/shiftmanagement/{id}
DELETE /api/shiftmanagement/{id}
POST   /api/shiftmanagement/{id}/assign-staff
DELETE /api/shiftmanagement/assignment/{assignmentId}
GET    /api/shiftmanagement/calendar
POST   /api/shiftmanagement/checkin
POST   /api/shiftmanagement/checkout
GET    /api/shifttemplate
POST   /api/shifttemplate
POST   /api/shifttemplate/{id}/apply
```

### 8.4 Table Management Endpoints
```
GET    /api/tablemanager
GET    /api/tablemanager/{id}
POST   /api/tablemanager
PUT    /api/tablemanager/{id}
DELETE /api/tablemanager/{id}
PUT    /api/tablemanager/{id}/status
GET    /api/tablemanager/by-area/{areaId}
GET    /api/tablemanager/available
GET    /api/area
POST   /api/area
PUT    /api/area/{id}
DELETE /api/area/{id}
GET    /api/dashboardtable
GET    /api/dashboardtable/status
```

### 8.5 Reservation Endpoints
```
POST   /api/reservation
GET    /api/reservation/my-reservations
GET    /api/reservation/{id}
PUT    /api/reservation/{id}
DELETE /api/reservation/{id}
GET    /api/reservationstaff
GET    /api/reservationstaff/{id}
POST   /api/reservationstaff/{id}/confirm
POST   /api/reservationstaff/{id}/reject
POST   /api/reservationstaff/{id}/assign-table
POST   /api/reservationstaff/{id}/checkin
GET    /api/reservationstaff/calendar
POST   /api/reservationdeposit
POST   /api/reservationdeposit/{id}/confirm
POST   /api/reservationdeposit/{id}/refund
```

### 8.6 Order Management Endpoints
```
POST   /api/ordertable
GET    /api/ordertable/{id}
PUT    /api/ordertable/{id}
POST   /api/ordertable/{orderId}/items
DELETE /api/ordertable/{orderId}/items/{itemId}
PUT    /api/ordertable/{id}/status
DELETE /api/ordertable/{id}
GET    /api/counterstafforder
POST   /api/counterstafforder/walkin
POST   /api/counterstafforder/{id}/process
GET    /api/waiterordertracking/my-orders
GET    /api/waiterordertracking/table/{tableId}
PUT    /api/waiterordertracking/items/{itemId}/status
POST   /api/waiterordertracking/{orderId}/assistance
```

### 8.7 Kitchen Display Endpoints
```
GET    /api/kitchendisplay/pending
GET    /api/kitchendisplay/{orderId}
PUT    /api/kitchendisplay/ticket/{ticketId}/status
PUT    /api/kitchendisplay/item/{itemId}/status
GET    /api/kitchendisplay/statistics
```

### 8.8 Payment Endpoints
```
POST   /api/payment
GET    /api/payment/{id}
GET    /api/payment/order/{orderId}
POST   /api/payment/{id}/process
POST   /api/payment/{id}/refund
GET    /api/payment/methods
POST   /api/payment/split
GET    /api/countertransaction
POST   /api/countertransaction
POST   /api/countertransaction/{id}/void
```

### 8.9 Inventory Endpoints
```
GET    /api/inventoryingredient
GET    /api/inventoryingredient/{id}
POST   /api/inventoryingredient
PUT    /api/inventoryingredient/{id}
DELETE /api/inventoryingredient/{id}
PUT    /api/inventoryingredient/{id}/stock
GET    /api/inventoryingredient/low-stock
GET    /api/importingredient
POST   /api/importingredient
POST   /api/importingredient/{id}/approve
GET    /api/exportingredient
POST   /api/exportingredient
GET    /api/auditinventory
POST   /api/auditinventory
POST   /api/auditinventory/{id}/process
GET    /api/warehouse
POST   /api/warehouse
PUT    /api/warehouse/{id}
```

### 8.10 Dashboard Endpoints
```
GET    /api/ownerdashboard/overview
GET    /api/ownerdashboard/daily/{date}
GET    /api/ownerdashboard/monthly/{month}
GET    /api/ownerrevenue/report
GET    /api/ownerrevenue/by-period
GET    /api/ownerrevenue/by-category
GET    /api/ownerrevenue/top-selling
GET    /api/counterstaffdashboard/today-orders
GET    /api/counterstaffdashboard/today-revenue
GET    /api/counterstaffdashboard/table-status
```

---

## 9. Real-time Communication

### 9.1 SignalR Hubs

#### RestaurantHub
**Purpose**: Real-time updates cho restaurant operations
**Clients**: Waiters, Counter Staff, Managers

**Methods:**
```csharp
// Server -> Client
- NotifyNewOrder(orderId)
- NotifyOrderStatusChange(orderId, status)
- NotifyOrderItemUpdate(orderDetailId, status)
- NotifyTableStatusChange(tableId, status)
- NotifyAssistanceRequest(orderId, message)

// Client -> Server
- JoinRestaurantGroup()
- LeaveRestaurantGroup()
```

#### KitchenHub
**Purpose**: Real-time updates cho kitchen display system
**Clients**: Kitchen Staff

**Methods:**
```csharp
// Server -> Client
- NotifyNewKitchenTicket(ticket)
- NotifyTicketStatusChange(ticketId, status)
- NotifyTicketItemReady(itemId)
- NotifyPriorityChange(ticketId, priority)

// Client -> Server
- JoinKitchenGroup()
- LeaveKitchenGroup()
```

#### ReservationHub
**Purpose**: Real-time updates cho reservation management
**Clients**: Counter Staff, Managers

**Methods:**
```csharp
// Server -> Client
- NotifyNewReservation(reservation)
- NotifyReservationStatusChange(reservationId, status)
- NotifyTableAssignment(reservationId, tableId)
- NotifyReservationReminder(reservationId)

// Client -> Server
- JoinReservationGroup()
- LeaveReservationGroup()
```

---

## 10. Tổng kết và Hướng dẫn sử dụng Document

### 10.1 Cách sử dụng Document này

#### Để tạo Class Diagram:
1. Chọn một Feature từ Section 5
2. Tham khảo chi tiết cấu trúc ở Section 6 (tương ứng với feature)
3. Sử dụng các thành phần:
   - Controllers (API endpoints)
   - Services (Business logic)
   - Repositories (Data access)
   - Models (Entities)
   - DTOs (Data transfer)
4. Thể hiện relationships:
   - Controllers → Services (dependency)
   - Services → Repositories (dependency)
   - Repositories → Models (data access)
   - Services → DTOs (data transformation)

#### Để tạo Sequence Diagram:
1. Chọn một Use Case từ Section 6 (mỗi feature có list use cases)
2. Xác định Actors liên quan
3. Trace flow từ:
   - Client/Frontend → Controller
   - Controller → Service
   - Service → Repository
   - Repository → Database
   - Response flow ngược lại
4. Thêm SignalR notifications nếu có (Section 9)

### 10.2 Ví dụ Use Case Flow

**Example: UC-ORDER-01: Customer Place Order**

**Actors**: Customer, System

**Main Flow:**
1. Customer submits order (Frontend)
2. OrderTableController receives request
3. OrderTableService validates order
4. MenuItemService checks item availability
5. InventoryService checks ingredient stock
6. OrderTableService creates order
7. OrderRepository saves to database
8. RestaurantHub broadcasts new order
9. KitchenHub notifies kitchen
10. System returns order confirmation

**Alternative Flow:**
- Item not available → Return error
- Insufficient stock → Return error

### 10.3 Naming Conventions

**Controllers**: `{Feature}Controller.cs`
**Services**: `{Feature}Service.cs` hoặc `I{Feature}Service.cs` (interface)
**Repositories**: `{Entity}Repository.cs` hoặc `I{Entity}Repository.cs`
**Models**: `{Entity}.cs`
**DTOs**: `{Entity}Dto.cs`, `{Entity}CreateDto.cs`, `{Entity}UpdateDto.cs`
**Hubs**: `{Feature}Hub.cs`

---

## 11. Technology Stack Details

### Backend Technologies
- **Framework**: .NET 8 (ASP.NET Core)
- **API**: RESTful Web API
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Authentication**: JWT Bearer Tokens
- **Real-time**: SignalR
- **Mapping**: AutoMapper
- **Validation**: FluentValidation
- **Logging**: Serilog/NLog
- **Testing**: xUnit
- **Cloud Storage**: Cloudinary
- **Payment Gateway**: MoMo

### Frontend Technologies
- **Framework**: ASP.NET Core MVC
- **View Engine**: Razor
- **CSS Framework**: Bootstrap 5
- **JavaScript**: jQuery, Vanilla JS
- **SignalR Client**: Microsoft.AspNetCore.SignalR.Client
- **Icons**: Font Awesome
- **Charts**: Chart.js (for dashboards)

### Development Tools
- **IDE**: Visual Studio 2022 / Visual Studio Code
- **Version Control**: Git
- **Package Manager**: NuGet
- **API Testing**: Postman, Swagger/OpenAPI
- **Database Tools**: SQL Server Management Studio (SSMS)

---

## 12. Project Statistics

### Backend
- **Controllers**: 59 files
- **Services**: 130 files
- **Repositories**: 107 files
- **Models**: 56 files
- **DTOs**: 100+ files
- **Enums**: 8 files

### Frontend (Staff Portal)
- **Controllers**: 44 files
- **Views**: 133 files
- **Models**: 66 files
- **DTOs**: 156 files
- **Services**: 31 files
- **JavaScript files**: 74+ files

### Frontend (Customer Portal)
- **Controllers**: 8 files
- **Views**: 16 files
- **Models**: 8 files
- **DTOs**: 11 files

---

**Document Version**: 1.0  
**Last Updated**: December 11, 2025  
**Project**: SapaFoRestRMS  
**Created for**: Class Diagram & Sequence Diagram Development

