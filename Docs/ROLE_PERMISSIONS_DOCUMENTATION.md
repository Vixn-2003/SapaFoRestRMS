# 📋 TÀI LIỆU PHÂN QUYỀN HỆ THỐNG SAPAFORESTRMS

**Ngày tạo:** 2025-01-15  
**Phiên bản:** 2.0  
**Nguyên tắc:** Phân quyền rõ ràng, không đa quyền (no overlap)

---

## 🎯 NGUYÊN TẮC PHÂN QUYỀN

### ✅ **Quy tắc chung:**
- Mỗi role có trách nhiệm riêng biệt, không overlap
- Không có quyền "Admin,Manager" hay "AdminOrManager"
- Mỗi endpoint chỉ thuộc về 1 role duy nhất (trừ trường hợp đặc biệt)

---

## 👤 ADMIN - QUẢN TRỊ VIÊN HỆ THỐNG

### 📌 **Trách nhiệm:**
- Duy trì toàn bộ hệ thống web POS
- Tạo và quản lý tài khoản người dùng
- Quản lý vai trò (Roles) và quyền
- Cấu hình hệ thống
- Giám sát tính toàn vẹn của dữ liệu
- **KHÔNG** có quyền quản lý business

### ✅ **Quyền hạn:**

#### 1. User Management
- ✅ Tạo, sửa, xóa, xem tất cả users
- ✅ Phân quyền roles cho users
- ✅ Quản lý trạng thái users

#### 2. Role Management
- ✅ Xem danh sách roles
- ✅ Quản lý roles (nếu có endpoint)

#### 3. Position Management
- ✅ Tạo Position mới (có thể set BaseSalary ban đầu)
- ✅ Sửa, xóa Position
- ✅ Xem tất cả Position

#### 4. System Configuration
- ✅ Cấu hình hệ thống
- ✅ Quản lý system settings

### ❌ **KHÔNG có quyền:**
- ❌ Quản lý doanh thu/lợi nhuận
- ❌ Quản lý giá/khuyến mãi
- ❌ Quản lý kho
- ❌ Xác nhận yêu cầu của Manager
- ❌ Quản lý business operations

---

## 👑 OWNER - CHỦ SỞ HỮU / QUẢN LÝ KINH DOANH

### 📌 **Trách nhiệm:**
- Quản lý tổng thể hoạt động kinh doanh của nhà hàng
- Giám sát doanh thu – lợi nhuận
- Giám sát hiệu suất nhân viên
- Quản lý tình trạng kho
- Thiết lập chính sách giá/khuyến mãi
- **Xác nhận yêu cầu thay đổi của Manager**

### ✅ **Quyền hạn:**

#### 1. Business Management
- ✅ Xem báo cáo doanh thu/lợi nhuận
- ✅ Xem thống kê hiệu suất nhân viên
- ✅ Quản lý tình trạng kho
- ✅ Thiết lập giá món ăn
- ✅ Quản lý khuyến mãi/voucher
- ✅ Quản lý marketing campaigns

#### 2. Position & Salary Management
- ✅ Tạo Position mới (có thể set BaseSalary)
- ✅ Xem tất cả Position
- ✅ Xem và phê duyệt/từ chối yêu cầu thay đổi BaseSalary từ Manager
- ✅ Xem thống kê yêu cầu thay đổi lương

#### 3. Staff Management
- ✅ Xem thông tin nhân viên
- ✅ Xem hiệu suất nhân viên
- ✅ Quản lý payroll (nếu cần)

#### 4. Inventory Management
- ✅ Xem tình trạng kho
- ✅ Quản lý inventory

#### 5. Payment & Orders
- ✅ Xem tất cả đơn hàng
- ✅ Xem thống kê thanh toán
- ✅ Quản lý payment flow

### ❌ **KHÔNG có quyền:**
- ❌ Tạo/sửa/xóa users (chỉ Admin)
- ❌ Quản lý roles (chỉ Admin)
- ❌ Cấu hình hệ thống (chỉ Admin)

---

## 👔 MANAGER - QUẢN LÝ VẬN HÀNH

### 📌 **Trách nhiệm:**
- Quản lý hoạt động hàng ngày của nhà hàng
- Tạo yêu cầu thay đổi (cần Owner xác nhận)
- Xem thông tin cần thiết để vận hành

### ✅ **Quyền hạn:**

#### 1. Daily Operations
- ✅ Xem danh sách đơn hàng
- ✅ Xem chi tiết đơn hàng
- ✅ Xử lý thanh toán
- ✅ Quản lý reservation
- ✅ Quản lý bàn

#### 2. Request Management
- ✅ Tạo yêu cầu thay đổi BaseSalary (chờ Owner approve)
- ✅ Xem yêu cầu của mình

#### 3. View Only
- ✅ Xem danh sách Position (không thể sửa BaseSalary trực tiếp)
- ✅ Xem menu items
- ✅ Xem voucher/khuyến mãi (không thể tạo/sửa)

### ❌ **KHÔNG có quyền:**
- ❌ Tạo/sửa/xóa users
- ❌ Quản lý roles
- ❌ Trực tiếp update BaseSalary (phải tạo request)
- ❌ Tạo/sửa voucher/khuyến mãi
- ❌ Xem thống kê doanh thu (chỉ Owner)
- ❌ Phê duyệt yêu cầu (chỉ Owner)

---

## 📊 BẢNG PHÂN QUYỀN CHI TIẾT

| Chức năng | Admin | Owner | Manager | Staff |
|-----------|-------|-------|---------|-------|
| **User Management** |
| Tạo User | ✅ | ❌ | ❌ | ❌ |
| Sửa User | ✅ | ❌ | ❌ | ❌ |
| Xóa User | ✅ | ❌ | ❌ | ❌ |
| Xem Users | ✅ | ✅ | ❌ | ❌ |
| **Role Management** |
| Quản lý Roles | ✅ | ❌ | ❌ | ❌ |
| **Position Management** |
| Tạo Position | ✅ | ✅ | ❌ | ❌ |
| Sửa Position | ✅ | ✅ | ❌ | ❌ |
| Xóa Position | ✅ | ✅ | ❌ | ❌ |
| Xem Position | ✅ | ✅ | ✅ | ❌ |
| **Salary Management** |
| Tạo yêu cầu thay đổi lương | ❌ | ❌ | ✅ | ❌ |
| Phê duyệt yêu cầu | ❌ | ✅ | ❌ | ❌ |
| **Business Management** |
| Xem doanh thu/lợi nhuận | ❌ | ✅ | ❌ | ❌ |
| Quản lý giá/khuyến mãi | ❌ | ✅ | ❌ | ❌ |
| Quản lý kho | ❌ | ✅ | ❌ | ❌ |
| **Daily Operations** |
| Xử lý thanh toán | ❌ | ✅ | ✅ | ✅ |
| Quản lý đơn hàng | ❌ | ✅ | ✅ | ✅ |
| Quản lý reservation | ❌ | ✅ | ✅ | ❌ |

---

## 🔒 AUTHORIZATION RULES

### Rule 1: Admin Only
- User CRUD operations
- Role management
- System configuration

### Rule 2: Owner Only
- Business statistics (revenue, profit)
- Price/promotion management
- Inventory management
- Approve Manager requests

### Rule 3: Manager Only
- Create salary change requests
- View own requests

### Rule 4: Owner + Admin
- Position management (both can create/edit)

### Rule 5: Owner + Manager + Staff
- Payment processing
- Order management

---

## 📝 NOTES

1. **Không có overlap không cần thiết**: Mỗi endpoint chỉ thuộc về role phù hợp nhất
2. **Manager requests**: Tất cả yêu cầu thay đổi từ Manager phải được Owner xác nhận
3. **Business vs System**: Admin quản lý system, Owner quản lý business
4. **Separation of Concerns**: Rõ ràng giữa technical (Admin) và business (Owner)

---

**Cập nhật lần cuối:** 2025-01-15

