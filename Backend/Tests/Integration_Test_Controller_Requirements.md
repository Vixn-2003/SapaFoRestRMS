# Integration Test Requirements - Controller Documentation

This document provides descriptions and pre-conditions for integration testing of all controllers in the SapaFoRestRMS API.

---

## 1. AdminDashboardController

### Description
API Controller for Admin Dashboard that provides comprehensive system overview data including:
- KPI cards (total users, reservations, orders, revenue)
- User role distribution statistics
- Revenue and orders data for the last 7 days
- Warehouse alerts summary
- Top 5 active users and best-selling categories
- Recent system logs

**Endpoints:**
- `GET /api/admin/dashboard` - Get full dashboard data
- `GET /api/admin/dashboard/revenue-7days` - Get revenue data for last 7 days
- `GET /api/admin/dashboard/orders-7days` - Get orders data for last 7 days
- `GET /api/admin/dashboard/alerts` - Get warehouse alerts summary
- `GET /api/admin/dashboard/logs` - Get recent system logs

**Authorization:** `Admin, Owner` roles required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Admin` or `Owner` role exists
   - Multiple users with different roles (Admin, Manager, Staff, Customer) exist
   - At least 7 days of historical data:
     - Orders with various statuses (Confirmed, Completed, Cancelled)
     - Transactions with completed payments
     - Reservations (Pending, Confirmed, Guest Seated)
   - Warehouse inventory items with some low stock items
   - System logs entries (at least 10 recent logs)

2. **Authentication:**
   - Valid JWT token for Admin or Owner user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Users with different roles to test role distribution
   - Orders created over the last 7 days with different dates
   - Completed transactions with revenue data
   - Inventory items with stock levels below threshold
   - System log entries with timestamps

---

## 2. CounterStaffDashboardController

### Description
API Controller for Counter Staff Dashboard that provides operational overview for counter staff including:
- Today's reservations count
- Today's revenue
- Active orders count
- Pending payment orders
- Active tables count
- Completed transactions count
- Hourly revenue and orders charts

**Endpoints:**
- `GET /api/counter/dashboard` - Get full counter staff dashboard data

**Authorization:** `Owner, Manager, Staff` roles required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Owner`, `Manager`, or `Staff` role exists
   - Today's data:
     - Reservations with status "Confirmed" or "Guest Seated"
     - Orders with status "Confirmed" or "In Progress"
     - Transactions with status "Pending" or "Paid"
     - Tables with active status
   - Historical hourly data for charts (orders and revenue by hour)

2. **Authentication:**
   - Valid JWT token for Owner, Manager, or Staff user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Reservations created today
   - Orders created today with various statuses
   - Transactions with today's date
   - Tables with active reservations/orders

---

## 3. AuthController

### Description
API Controller for authentication and authorization that handles:
- User login (email/password)
- Token refresh
- Google OAuth login
- Phone OTP authentication (request and verify)
- Password reset (forgot password, reset password)
- Admin creating managers
- Manager creating staff (with verification code)

**Endpoints:**
- `POST /api/auth/login` - Login with email/password
- `POST /api/auth/refresh-token` - Refresh access token
- `POST /api/auth/google-login` - Login with Google OAuth
- `POST /api/auth/request-otp` - Request OTP for phone login
- `POST /api/auth/verify-otp` - Verify OTP and login
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password with code
- `POST /api/auth/admin/create-manager` - Admin creates manager
- `POST /api/auth/manager/create-staff/send-code` - Manager sends verification code
- `POST /api/auth/manager/create-staff` - Manager creates staff
- `POST /api/auth/logout` - Logout (client-side token discard)

**Authorization:** Most endpoints are `AllowAnonymous`, except:
- `create-manager` requires `Admin` role
- `create-staff` endpoints require `Manager` role
- `logout` requires authenticated user

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with each role (Admin, Manager, Staff, Customer, Owner)
   - Users with valid email addresses and hashed passwords
   - Users with phone numbers for OTP testing
   - OTP cache/storage mechanism configured
   - Email service configured for password reset

2. **Authentication:**
   - For protected endpoints: Valid JWT token with appropriate role
   - For anonymous endpoints: No authentication required

3. **Test Data Requirements:**
   - User with email: `test@example.com` and known password
   - User with phone number: `+84123456789` for OTP testing
   - Admin user for creating managers
   - Manager user for creating staff
   - Valid refresh tokens for refresh token testing
   - Google OAuth test credentials (if testing Google login)

4. **External Services:**
   - Email service configured (for password reset)
   - SMS/OTP service configured (for phone authentication)
   - Google OAuth credentials (for Google login)

---

## 4. CounterStaffOrderController

### Description
API Controller for Counter Staff Order Management that handles:
- Viewing list of orders with filtering options
- Filtering by status, date, table number, and search keyword

**Endpoints:**
- `GET /api/counter/orders` - Get orders list with filters

**Authorization:** `Owner, Manager, Staff` roles required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Owner`, `Manager`, or `Staff` role exists
   - Multiple orders with different:
     - Statuses (Confirmed, In Progress, Completed, Cancelled)
     - Dates (today, yesterday, last week)
     - Table numbers
     - Customer information

2. **Authentication:**
   - Valid JWT token for Owner, Manager, or Staff user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Orders with various statuses
   - Orders with different dates
   - Orders associated with different tables
   - Orders with customer information for search testing

---

## 5. CounterTransactionController

### Description
API Controller for Counter Transaction History that handles:
- Viewing transaction history with filtering and pagination
- Exporting transactions to Excel

**Endpoints:**
- `POST /api/counter/transactions/filter` - Get transaction history with filters
- `POST /api/counter/transactions/export-excel` - Export transactions to Excel

**Authorization:** `Owner, Manager, Staff` roles required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Owner`, `Manager`, or `Staff` role exists
   - Multiple transactions with different:
     - Payment methods (Cash, QR, Credit Card, etc.)
     - Statuses (Pending, Paid, Failed, Cancelled)
     - Dates (range of dates for filtering)
     - Amounts

2. **Authentication:**
   - Valid JWT token for Owner, Manager, or Staff user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Transactions with various payment methods
   - Transactions with different statuses
   - Transactions spanning multiple dates
   - Completed transactions with `CompletedAt` timestamps

4. **External Dependencies:**
   - EPPlus or similar library for Excel export functionality

---

## 6. CustomerController

### Description
API Controller for Customer operations that handles:
- OTP-based phone login for customers
- Customer profile management (view and update)
- Viewing customer orders

**Endpoints:**
- `POST /api/customer/send-otp-login` - Send OTP for phone login (Anonymous)
- `GET /api/customer/profile` - Get customer profile
- `PUT /api/customer/profile` - Update customer profile
- `GET /api/customer/orders` - Get customer orders

**Authorization:** `Customer` role required (except send-otp-login which is Anonymous)

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Customer` role exists
   - Customer record linked to user
   - Customer with valid phone number
   - Orders associated with customer
   - OTP cache/storage mechanism configured

2. **Authentication:**
   - For protected endpoints: Valid JWT token for Customer user
   - For send-otp-login: No authentication required
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Customer user with phone number
   - Customer profile with personal information
   - Orders associated with customer ID
   - OTP service configured for phone authentication

4. **External Services:**
   - SMS/OTP service configured

---

## 7. CustomerManagementController

### Description
API Controller for Customer Management that handles:
- Viewing list of customers with filtering and pagination
- Viewing customer details
- Updating customer VIP status
- Checking VIP criteria

**Endpoints:**
- `GET /api/customer-management` - Get customers list with filters
- `GET /api/customer-management/{id}` - Get customer detail
- `PUT /api/customer-management/{id}/vip` - Update VIP status
- `GET /api/customer-management/{id}/vip-criteria` - Check VIP criteria

**Authorization:** `Manager, Admin` roles required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Manager` or `Admin` role exists
   - Multiple customers with:
     - Different spending amounts
     - Different visit counts
     - Some with VIP status, some without
     - Various personal information (name, phone, email)
   - Orders and transactions associated with customers for spending calculation

2. **Authentication:**
   - Valid JWT token for Manager or Admin user
   - Token must include `userId` claim (for Manager ID tracking)

3. **Test Data Requirements:**
   - Customers with total spending data
   - Customers with visit count data
   - Customers with VIP status (true/false)
   - Customers with orders and transactions for spending calculation
   - Customer with enough spending to meet VIP criteria
   - Customer with insufficient spending for VIP criteria

---

## 8. OwnerDashboardController

### Description
API Controller for Owner Dashboard that provides business overview including:
- KPI cards (today revenue, monthly revenue, total orders, active customers, alerts)
- Revenue trend data (last 30 days)
- Top selling items
- Branch comparison data
- Alerts summary (low stock, near expiry)

**Endpoints:**
- `GET /api/owner/dashboard` - Get full owner dashboard data

**Authorization:** `Owner` role required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Owner` role exists
   - Revenue data:
     - Today's transactions with completed payments
     - Monthly transactions (current month)
     - Last 30 days of revenue data
   - Orders data:
     - Today's orders
     - Monthly orders
   - Customer data:
     - Active customers (customers with recent orders)
   - Inventory data:
     - Items with low stock levels
     - Items near expiry date
   - Menu items with sales data for top selling items

2. **Authentication:**
   - Valid JWT token for Owner user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Transactions with today's date and completed status
   - Transactions spanning the last 30 days
   - Orders with various statuses
   - Active customers (customers with orders in last 30 days)
   - Inventory items with stock levels and expiry dates
   - Menu items with order item associations for sales calculation

---

## 9. OwnerRevenueController

### Description
API Controller for Owner Revenue Management that handles:
- Viewing revenue data with filtering (date range, payment method, branch)
- Getting revenue summary (last 30 days)

**Endpoints:**
- `POST /api/owner/revenue/filter` - Get revenue data with filters
- `GET /api/owner/revenue/summary` - Get revenue summary (last 30 days)

**Authorization:** `Owner` role required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Owner` role exists
   - Multiple transactions with:
     - Different payment methods (Cash, QR, Credit Card, etc.)
     - Different dates (spanning at least 30 days)
     - Status "Paid" with `CompletedAt` timestamps
     - Various amounts
   - Branch data (if multi-branch support is implemented)

2. **Authentication:**
   - Valid JWT token for Owner user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Transactions with status "Paid" and `CompletedAt` values
   - Transactions with different payment methods
   - Transactions spanning date ranges for filtering
   - At least 30 days of historical transaction data

---

## 10. OwnerWarehouseAlertController

### Description
API Controller for Owner Warehouse Alert Management that handles:
- Viewing warehouse alerts (low stock, near expiry, expired items)
- Getting warehouse alert summary

**Endpoints:**
- `GET /api/owner/warehouse/alerts` - Get all warehouse alerts
- `GET /api/owner/warehouse/summary` - Get warehouse alert summary

**Authorization:** `Owner` role required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Owner` role exists
   - Inventory items with:
     - Low stock levels (below threshold)
     - Items near expiry date (within warning period)
     - Expired items (past expiry date)
     - Normal stock levels (for comparison)
   - Category data for category distribution

2. **Authentication:**
   - Valid JWT token for Owner user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Inventory items with stock quantity below reorder threshold
   - Inventory items with expiry dates in the near future
   - Inventory items with expired expiry dates
   - Items across different categories
   - Stock level data for chart generation

---

## 11. PasswordController

### Description
API Controller for Password Management that handles:
- Requesting password reset (forgot password)
- Verifying password reset code
- Requesting password change (authenticated users)
- Confirming password change with verification code

**Endpoints:**
- `POST /api/password/reset/request` - Request password reset (Anonymous)
- `POST /api/password/reset/verify` - Verify reset code and get new password (Anonymous)
- `POST /api/password/change/request` - Request password change (Authenticated)
- `POST /api/password/change/confirm` - Confirm password change with code (Authenticated)

**Authorization:** 
- Reset endpoints: `AllowAnonymous`
- Change endpoints: `Authorize` (authenticated users)

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - Users with valid email addresses
   - Users with existing passwords
   - Password reset code storage mechanism
   - Password change verification code storage

2. **Authentication:**
   - For reset endpoints: No authentication required
   - For change endpoints: Valid JWT token with `userId` claim

3. **Test Data Requirements:**
   - User with email: `test@example.com`
   - User with known password for change testing
   - Email service configured for sending reset codes
   - Verification code storage mechanism

4. **External Services:**
   - Email service configured for password reset emails

---

## 12. PaymentController

### Description
API Controller for Payment Processing that handles:
- Viewing pending orders for payment
- Getting order details for payment
- Processing cash payments
- Processing QR code payments
- Processing combined payments (cash + QR)
- Processing split bill payments
- Confirming payments
- Generating and printing receipts

**Endpoints:**
- `GET /api/payment/orders` - Get pending orders
- `GET /api/payment/orders/{id}/details` - Get order details
- `PUT /api/payment/orders/{orderId}/confirm` - Customer confirm order
- `POST /api/payment/cash` - Process cash payment
- `POST /api/payment/qr` - Process QR payment
- `POST /api/payment/combined` - Process combined payment
- `POST /api/payment/split-bill` - Process split bill
- `POST /api/payment/confirm` - Confirm payment
- `GET /api/payment/receipt/{transactionId}` - Generate receipt
- `POST /api/payment/start` - Start payment process

**Authorization:** `Owner, Manager, Staff` roles required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Owner`, `Manager`, or `Staff` role exists
   - Orders with status "Confirmed" or "In Progress"
   - Orders with order items and calculated totals
   - Customers associated with orders
   - Tables associated with orders
   - Vouchers/promotions (if applicable)
   - Payment gateway configuration (for QR payments)

2. **Authentication:**
   - Valid JWT token for Owner, Manager, or Staff user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Order with status ready for payment
   - Order with order items and total amount
   - Customer associated with order
   - Valid payment amounts (matching order total)
   - Test payment gateway credentials (for QR testing)
   - Receipt template configuration

4. **External Services:**
   - Payment gateway service configured (for QR payments)
   - Receipt generation service

---

## 13. RolesController

### Description
API Controller for Role Management that handles:
- Viewing all roles
- Viewing role details

**Endpoints:**
- `GET /api/roles` - Get all roles
- `GET /api/roles/{id}` - Get role by ID

**Authorization:** `Admin` role required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Admin` role exists
   - Roles table populated with standard roles:
     - Owner (RoleId: 1)
     - Admin (RoleId: 2)
     - Manager (RoleId: 3)
     - Staff (RoleId: 4)
     - Customer (RoleId: 5)

2. **Authentication:**
   - Valid JWT token for Admin user
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - All standard roles exist in database
   - Role IDs are consistent with system expectations

---

## 14. SalaryChangeRequestController

### Description
API Controller for Salary Change Request Management that handles:
- Manager creating salary change requests for positions
- Owner viewing pending requests
- Owner approving/rejecting requests
- Manager viewing their own requests
- Owner viewing statistics

**Endpoints:**
- `POST /api/salarychangerequest` - Create request (Manager)
- `GET /api/salarychangerequest/pending` - Get pending requests (Owner)
- `GET /api/salarychangerequest` - Get all requests with status filter (Owner)
- `PUT /api/salarychangerequest/{id}/approve` - Approve request (Owner)
- `PUT /api/salarychangerequest/{id}/reject` - Reject request (Owner)
- `GET /api/salarychangerequest/my-requests` - Get my requests (Manager)
- `GET /api/salarychangerequest/statistics` - Get statistics (Owner)
- `GET /api/salarychangerequest/{id}` - Get request detail

**Authorization:** 
- Create/My Requests: `Manager` role
- Approve/Reject/View All/Statistics: `Owner` role
- Get Detail: `Manager, Owner` roles

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Manager` role exists
   - At least one user with `Owner` role exists
   - Positions table with positions that have base salaries
   - SalaryChangeRequest table (may be empty initially)
   - Position with current base salary for testing

2. **Authentication:**
   - Valid JWT token for Manager user (for creating requests)
   - Valid JWT token for Owner user (for approving/rejecting)
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Position with known base salary
   - Manager user to create requests
   - Owner user to approve/reject requests
   - Test requests with different statuses (Pending, Approved, Rejected)

---

## 15. StaffManagementController

### Description
API Controller for Staff Management that handles:
- Viewing list of staff with filtering and pagination
- Viewing staff details
- Creating new staff
- Updating staff information
- Deactivating staff
- Getting active positions for dropdown

**Endpoints:**
- `GET /api/StaffManagement` - Get staff list with filters
- `GET /api/StaffManagement/{id}` - Get staff detail
- `POST /api/StaffManagement` - Create staff
- `PUT /api/StaffManagement/{id}` - Update staff
- `PUT /api/StaffManagement/{id}/deactivate` - Deactivate staff
- `GET /api/StaffManagement/positions` - Get active positions

**Authorization:** `Manager, Admin` roles required

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Manager` or `Admin` role exists
   - Positions table with active positions
   - Users table with staff users
   - Staff table with staff records linked to users
   - Departments table (if department filtering is used)

2. **Authentication:**
   - Valid JWT token for Manager or Admin user
   - Token must include `userId` claim (for tracking who created/updated)

3. **Test Data Requirements:**
   - Active positions for staff assignment
   - Existing staff records for update/deactivate testing
   - Valid email addresses (unique) for new staff creation
   - Valid phone numbers for staff
   - Staff with different statuses (Active, Inactive)
   - Staff with different positions

---

## 16. StaffProfilesController

### Description
API Controller for Staff Profile Management that handles:
- Viewing all staff profiles
- Viewing staff profile details
- Updating staff profiles

**Endpoints:**
- `GET /api/StaffProfiles` - Get all staff profiles (Admin, Owner)
- `GET /api/StaffProfiles/{userId}` - Get staff profile by user ID (Admin, Owner)
- `PUT /api/StaffProfiles/{userId}` - Update staff profile (Admin)

**Authorization:** 
- View: `Admin, Owner` roles
- Update: `Admin` role only

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Admin` role exists
   - At least one user with `Owner` role exists
   - Users with `Staff` role
   - Staff records linked to users
   - Positions table with positions
   - Staff-Position relationships

2. **Authentication:**
   - Valid JWT token for Admin user (for all operations)
   - Valid JWT token for Owner user (for viewing only)
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Staff users with complete profile information
   - Staff users with associated positions
   - Staff users with different statuses
   - Valid data for profile updates (name, email, phone, status, positions)

---

## 17. UsersController

### Description
API Controller for User Management that handles:
- Viewing all users
- Searching users with filters
- Viewing user details
- Creating new users
- Updating users
- Deleting users
- Changing user status
- Resetting user passwords
- Viewing and updating own profile

**Endpoints:**
- `GET /api/users` - Get all users (Admin)
- `GET /api/users/search` - Search users (Admin)
- `GET /api/users/{id}` - Get user by ID (Admin)
- `GET /api/users/{id}/details` - Get user details (Admin)
- `POST /api/users` - Create user (Admin)
- `PUT /api/users/{id}` - Update user (Admin)
- `DELETE /api/users/{id}` - Delete user (Admin)
- `PATCH /api/users/{id}/status/{status}` - Change user status (Admin)
- `POST /api/users/{id}/reset-password` - Reset user password (Admin)
- `GET /api/users/profile` - Get own profile (Authenticated)
- `PUT /api/users/profile` - Update own profile (Authenticated)

**Authorization:** 
- Most operations: `Admin` role
- Profile endpoints: Any authenticated user

### Pre-Conditions for Integration Testing
1. **Database Setup:**
   - At least one user with `Admin` role exists
   - Multiple users with different roles
   - Users with different statuses (Active, Inactive)
   - Roles table with all standard roles
   - Valid email addresses (unique constraints)

2. **Authentication:**
   - Valid JWT token for Admin user (for admin operations)
   - Valid JWT token for any user (for profile operations)
   - Token must include `userId` claim

3. **Test Data Requirements:**
   - Users with various roles
   - Users with different statuses
   - Unique email addresses for new user creation
   - Valid passwords for password reset testing
   - User profile data (name, email, phone, etc.)

---

## General Pre-Conditions for All Integration Tests

1. **Test Database:**
   - Clean test database or test data isolation
   - Database migrations applied
   - Seed data for standard roles and configurations

2. **Test Environment:**
   - API server running and accessible
   - Database connection configured
   - JWT token generation configured
   - All required services registered in DI container

3. **Test Data Management:**
   - Test data setup before each test
   - Test data cleanup after each test (or use transactions)
   - Unique identifiers for test data to avoid conflicts

4. **Authentication Setup:**
   - JWT token generation utility
   - Test users with known credentials
   - Token expiration handling

5. **External Services Mocking:**
   - Email service mock (for password reset, OTP)
   - SMS/OTP service mock (for phone authentication)
   - Payment gateway mock (for payment processing)
   - File storage mock (for receipt generation)

6. **Test Utilities:**
   - HTTP client configured for API calls
   - Test data builders/factories
   - Assertion helpers for response validation
   - Database context for direct data verification

