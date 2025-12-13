# Integration Test Cases - AdminDashboardController

## Test Environment
- **Base URL**: `https://localhost:7096/api`
- **Authorization**: Bearer Token (JWT) with **Admin** role required (Note: Only Admin role, not Owner)
- **Content-Type**: `application/json`

---

## Test Case Template

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|----------|-----------|--------|----------|-----------|--------|

---

## 1. GET DASHBOARD DATA (GET /api/admin/dashboard)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|----------|-----------|--------|----------|-----------|--------|
| **ADM-DASH-001** | Get dashboard data with valid Admin token | "1. Login as Admin user to get JWT token<br>2. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>3. Header: Authorization: Bearer {token}<br>4. Send request" | "Status: 200 OK<br>Response body contains:<br>- KpiCards object with metrics<br>- UserRoleDistribution object<br>- RevenueLast7Days array<br>- OrdersLast7Days array<br>- WarehouseAlerts object<br>- Top5ActiveUsers array<br>- Top5BestSellingCategories array<br>- RecentLogs array" | "1. Admin user exists in database<br>2. Valid JWT token with Admin role<br>3. Database contains:<br>   - Users with different roles<br>   - Orders from last 7 days<br>   - Transactions with revenue data<br>   - Inventory items<br>   - System logs" | | | | | | | | | | |
| **ADM-DASH-002** | Get dashboard data without authentication token | "1. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>2. Do not include Authorization header<br>3. Send request" | "Status: 401 Unauthorized<br>Response body contains error message about authentication" | "1. No authentication token provided" | | | | | | | | | |
| **ADM-DASH-003** | Get dashboard data with invalid token | "1. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>2. Header: Authorization: Bearer invalid_token_12345<br>3. Send request" | "Status: 401 Unauthorized<br>Response body contains error message about invalid token" | "1. Invalid or expired JWT token" | | | | | | | | | |
| **ADM-DASH-004** | Get dashboard data with non-Admin role token | "1. Login as Manager/Staff/Customer/Owner user to get JWT token<br>2. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>3. Header: Authorization: Bearer {nonAdminToken}<br>4. Send request" | "Status: 403 Forbidden<br>Response body contains error message about insufficient permissions<br>Note: Even Owner role should be rejected (only Admin allowed)" | "1. Valid JWT token with non-Admin role (Manager, Staff, Customer, Owner)" | | | | | | | | | |
| **ADM-DASH-005** | Get dashboard data with empty database | "1. Login as Admin user<br>2. Clear all test data from database<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- KpiCards with zero values<br>- Empty arrays for lists<br>- Default/empty objects for complex types" | "1. Admin user exists<br>2. Database is empty (no orders, users, transactions, etc.)" | | | | | | | | | |
| **ADM-DASH-006** | Get dashboard data with expired token | "1. Login as Admin user to get JWT token<br>2. Wait for token to expire (or manually set expired token)<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>4. Header: Authorization: Bearer {expiredToken}<br>5. Send request" | "Status: 401 Unauthorized<br>Response body contains error message about token expiration" | "1. Expired JWT token" | | | | | | | | | |

---

## 2. GET REVENUE 7 DAYS (GET /api/admin/dashboard/revenue-7days)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|----------|-----------|--------|----------|-----------|--------|
| **ADM-REV-001** | Get revenue data for last 7 days with valid data | "1. Login as Admin user<br>2. Open Postman → GET {{baseUrl}}/api/admin/dashboard/revenue-7days<br>3. Header: Authorization: Bearer {adminToken}<br>4. Send request" | "Status: 200 OK<br>Response body contains array of revenue points:<br>- Each item has Date and Revenue properties<br>- Array contains 7 items (one per day)<br>- Revenue values are decimal numbers<br>- Dates are in correct format" | "1. Admin user with valid token<br>2. Database contains transactions with CompletedAt dates spanning last 7 days<br>3. Transactions have status 'Paid' or 'Completed'" | | | | | | | | | |
| **ADM-REV-002** | Get revenue data with no transactions in last 7 days | "1. Login as Admin user<br>2. Ensure no transactions exist in last 7 days<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/revenue-7days<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains array with 7 items:<br>- All revenue values are 0<br>- Dates are correct for last 7 days" | "1. Admin user with valid token<br>2. No transactions in last 7 days (or all older than 7 days)" | | | | | | | | | |
| **ADM-REV-003** | Get revenue data without authentication | "1. Open Postman → GET {{baseUrl}}/api/admin/dashboard/revenue-7days<br>2. Do not include Authorization header<br>3. Send request" | "Status: 401 Unauthorized<br>Response body contains authentication error" | "1. No authentication token" | | | | | | | | | |
| **ADM-REV-004** | Get revenue data with non-Admin role | "1. Login as Manager user<br>2. Open Postman → GET {{baseUrl}}/api/admin/dashboard/revenue-7days<br>3. Header: Authorization: Bearer {managerToken}<br>4. Send request" | "Status: 403 Forbidden<br>Response body contains permission error" | "1. Valid token with non-Admin role" | | | | | | | | | |
| **ADM-REV-005** | Get revenue data with transactions across multiple days | "1. Login as Admin user<br>2. Create transactions with different dates in last 7 days<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/revenue-7days<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- 7 revenue points (one per day)<br>- Days with transactions show correct revenue<br>- Days without transactions show 0<br>- Total revenue matches sum of individual days" | "1. Admin user with valid token<br>2. Multiple transactions across different days in last 7 days<br>3. Transactions have varying amounts" | | | | | | | | | |

---

## 3. GET ORDERS 7 DAYS (GET /api/admin/dashboard/orders-7days)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|----------|-----------|--------|----------|-----------|--------|
| **ADM-ORD-001** | Get orders data for last 7 days with valid data | "1. Login as Admin user<br>2. Open Postman → GET {{baseUrl}}/api/admin/dashboard/orders-7days<br>3. Header: Authorization: Bearer {adminToken}<br>4. Send request" | "Status: 200 OK<br>Response body contains array of order points:<br>- Each item has Date and OrderCount properties<br>- Array contains 7 items (one per day)<br>- OrderCount values are integers<br>- Dates are in correct format" | "1. Admin user with valid token<br>2. Database contains orders with CreatedAt dates spanning last 7 days<br>3. Orders have various statuses" | | | | | | | | | |
| **ADM-ORD-002** | Get orders data with no orders in last 7 days | "1. Login as Admin user<br>2. Ensure no orders exist in last 7 days<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/orders-7days<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains array with 7 items:<br>- All OrderCount values are 0<br>- Dates are correct for last 7 days" | "1. Admin user with valid token<br>2. No orders in last 7 days" | | | | | | | | | |
| **ADM-ORD-003** | Get orders data without authentication | "1. Open Postman → GET {{baseUrl}}/api/admin/dashboard/orders-7days<br>2. Do not include Authorization header<br>3. Send request" | "Status: 401 Unauthorized<br>Response body contains authentication error" | "1. No authentication token" | | | | | | | | | |
| **ADM-ORD-004** | Get orders data with orders on specific days only | "1. Login as Admin user<br>2. Create orders only on days 1, 3, 5 of last 7 days<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/orders-7days<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- 7 order points (one per day)<br>- Days 1, 3, 5 show correct order counts<br>- Days 2, 4, 6, 7 show 0<br>- Total matches sum of individual days" | "1. Admin user with valid token<br>2. Orders exist only on specific days (1, 3, 5) in last 7 days" | | | | | | | | | |
| **ADM-ORD-005** | Get orders data with large number of orders | "1. Login as Admin user<br>2. Create 100+ orders distributed across last 7 days<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/orders-7days<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- 7 order points with correct counts<br>- OrderCount values match actual orders per day<br>- Response time is acceptable (< 2 seconds)" | "1. Admin user with valid token<br>2. Large number of orders (100+) in last 7 days" | | | | | | | | | |

---

## 4. GET WAREHOUSE ALERTS (GET /api/admin/dashboard/alerts)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|----------|-----------|--------|----------|-----------|--------|
| **ADM-ALT-001** | Get warehouse alerts with low stock items | "1. Login as Admin user<br>2. Create inventory items with stock below threshold<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/alerts<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- LowStockCount > 0<br>- AlertSummary object with alert details<br>- List of low stock items<br>- Correct alert counts" | "1. Admin user with valid token<br>2. Inventory items with stock quantity below reorder threshold<br>3. Items have valid stock levels" | | | | | | | | | |
| **ADM-ALT-002** | Get warehouse alerts with no alerts | "1. Login as Admin user<br>2. Ensure all inventory items have stock above threshold<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/alerts<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- LowStockCount = 0<br>- NearExpiryCount = 0<br>- ExpiredCount = 0<br>- Empty or minimal alert details" | "1. Admin user with valid token<br>2. All inventory items have adequate stock<br>3. No items near expiry or expired" | | | | | | | | | |
| **ADM-ALT-003** | Get warehouse alerts with near expiry items | "1. Login as Admin user<br>2. Create inventory items with expiry dates within warning period<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/alerts<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- NearExpiryCount > 0<br>- List of items near expiry<br>- Correct expiry dates and warnings" | "1. Admin user with valid token<br>2. Inventory items with expiry dates within 7-30 days<br>3. Items have valid expiry date fields" | | | | | | | | | |
| **ADM-ALT-004** | Get warehouse alerts with expired items | "1. Login as Admin user<br>2. Create inventory items with past expiry dates<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/alerts<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- ExpiredCount > 0<br>- List of expired items<br>- Correct expiry dates (past dates)" | "1. Admin user with valid token<br>2. Inventory items with expiry dates in the past<br>3. Items have valid expiry date fields" | | | | | | | | | |
| **ADM-ALT-005** | Get warehouse alerts without authentication | "1. Open Postman → GET {{baseUrl}}/api/admin/dashboard/alerts<br>2. Do not include Authorization header<br>3. Send request" | "Status: 401 Unauthorized<br>Response body contains authentication error" | "1. No authentication token" | | | | | | | | | |
| **ADM-ALT-006** | Get warehouse alerts with all alert types | "1. Login as Admin user<br>2. Create items with low stock, near expiry, and expired items<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/alerts<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains:<br>- LowStockCount > 0<br>- NearExpiryCount > 0<br>- ExpiredCount > 0<br>- Complete alert summary with all types" | "1. Admin user with valid token<br>2. Inventory items with all alert types:<br>   - Low stock items<br>   - Near expiry items<br>   - Expired items" | | | | | | | | | |

---

## 5. GET RECENT SYSTEM LOGS (GET /api/admin/dashboard/logs)

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|----------|-----------|--------|----------|-----------|--------|
| **ADM-LOG-001** | Get recent system logs with valid logs | "1. Login as Admin user<br>2. Ensure system has generated logs<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/logs<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains array of log entries:<br>- Maximum 10 log entries<br>- Each entry has Time, TimeFormatted, Username, Action properties<br>- Logs are ordered by time (most recent first)<br>- Valid timestamp formats" | "1. Admin user with valid token<br>2. System logs table contains at least 10 log entries<br>3. Logs have valid timestamps and user information" | | | | | | | | | |
| **ADM-LOG-002** | Get recent system logs with less than 10 logs | "1. Login as Admin user<br>2. Ensure system has less than 10 logs<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/logs<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains array:<br>- Number of entries matches available logs (< 10)<br>- All available logs are returned<br>- Logs are ordered by time (most recent first)" | "1. Admin user with valid token<br>2. System logs table contains less than 10 entries (e.g., 3-5 logs)" | | | | | | | | | |
| **ADM-LOG-003** | Get recent system logs with no logs | "1. Login as Admin user<br>2. Clear all system logs<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/logs<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains empty array: []" | "1. Admin user with valid token<br>2. System logs table is empty" | | | | | | | | | |
| **ADM-LOG-004** | Get recent system logs without authentication | "1. Open Postman → GET {{baseUrl}}/api/admin/dashboard/logs<br>2. Do not include Authorization header<br>3. Send request" | "Status: 401 Unauthorized<br>Response body contains authentication error" | "1. No authentication token" | | | | | | | | | |
| **ADM-LOG-005** | Get recent system logs with more than 10 logs | "1. Login as Admin user<br>2. Ensure system has 20+ log entries<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/logs<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Response body contains array:<br>- Exactly 10 log entries<br>- Most recent 10 logs are returned<br>- Logs are ordered by time (most recent first)<br>- Older logs are not included" | "1. Admin user with valid token<br>2. System logs table contains 20+ log entries<br>3. Logs have different timestamps" | | | | | | | | | |
| **ADM-LOG-006** | Get recent system logs with non-Admin role | "1. Login as Manager user<br>2. Open Postman → GET {{baseUrl}}/api/admin/dashboard/logs<br>3. Header: Authorization: Bearer {managerToken}<br>4. Send request" | "Status: 403 Forbidden<br>Response body contains permission error" | "1. Valid token with non-Admin role (Manager, Staff, Customer)" | | | | | | | | | |

---

## 6. PERFORMANCE AND LOAD TESTING

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|----------|-----------|--------|----------|-----------|--------|
| **ADM-PERF-001** | Dashboard endpoint response time with large dataset | "1. Login as Admin user<br>2. Create large dataset:<br>   - 1000+ users<br>   - 5000+ orders<br>   - 10000+ transactions<br>   - 500+ inventory items<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>4. Header: Authorization: Bearer {adminToken}<br>5. Measure response time<br>6. Send request" | "Status: 200 OK<br>Response time < 3 seconds<br>All data is correctly aggregated<br>No timeout errors" | "1. Admin user with valid token<br>2. Large dataset in database<br>3. Database indexes are properly configured" | | | | | | | | | |
| **ADM-PERF-002** | Concurrent requests to dashboard endpoint | "1. Login as Admin user<br>2. Send 10 concurrent requests to GET {{baseUrl}}/api/admin/dashboard<br>3. All requests use same Admin token<br>4. Monitor response times and success rate" | "All requests return Status: 200 OK<br>Response times are consistent<br>No deadlocks or database errors<br>All responses contain correct data" | "1. Admin user with valid token<br>2. Test tool supports concurrent requests (Postman Runner, JMeter, etc.)<br>3. Database can handle concurrent queries" | | | | | | | | | |

---

## 7. ERROR HANDLING AND EDGE CASES

| Test Case ID | Test Case Description | Test Case Procedure | Expected Results | Pre-conditions | Round 1 | Test date | Tester | Round 2 | Test date | Tester | Round 3 | Test date | Tester |
|--------------|----------------------|---------------------|------------------|----------------|---------|-----------|--------|----------|-----------|--------|----------|-----------|--------|
| **ADM-ERR-001** | Dashboard endpoint with database connection error | "1. Login as Admin user<br>2. Simulate database connection failure<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 500 Internal Server Error<br>Response body contains:<br>- success: false<br>- message: Error message<br>- error: Database connection error details" | "1. Admin user with valid token<br>2. Database connection can be interrupted (test environment)" | | | | | | | | | |
| **ADM-ERR-002** | Dashboard endpoint with malformed token | "1. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>2. Header: Authorization: Bearer malformed.token.here<br>3. Send request" | "Status: 401 Unauthorized<br>Response body contains authentication error" | "1. Malformed JWT token (invalid format)" | | | | | | | | | |
| **ADM-ERR-003** | Dashboard endpoint with missing Bearer prefix | "1. Login as Admin user<br>2. Open Postman → GET {{baseUrl}}/api/admin/dashboard<br>3. Header: Authorization: {token} (without Bearer prefix)<br>4. Send request" | "Status: 401 Unauthorized<br>Response body contains authentication error" | "1. Valid token but missing 'Bearer ' prefix in Authorization header" | | | | | | | | | |
| **ADM-ERR-004** | Revenue endpoint with date boundary (exactly 7 days ago) | "1. Login as Admin user<br>2. Create transaction exactly 7 days ago (at 00:00:00)<br>3. Create transaction 7 days and 1 second ago<br>4. Open Postman → GET {{baseUrl}}/api/admin/dashboard/revenue-7days<br>5. Header: Authorization: Bearer {adminToken}<br>6. Send request" | "Status: 200 OK<br>Response includes transaction from exactly 7 days ago<br>Response excludes transaction older than 7 days<br>Date boundaries are correctly handled" | "1. Admin user with valid token<br>2. Transactions with precise timestamps at 7-day boundary" | | | | | | | | | |
| **ADM-ERR-005** | Orders endpoint with timezone considerations | "1. Login as Admin user<br>2. Create orders at different times (UTC vs local time)<br>3. Open Postman → GET {{baseUrl}}/api/admin/dashboard/orders-7days<br>4. Header: Authorization: Bearer {adminToken}<br>5. Send request" | "Status: 200 OK<br>Orders are correctly grouped by date<br>Timezone handling is consistent<br>No duplicate or missing orders" | "1. Admin user with valid token<br>2. Orders created with different timezone considerations<br>3. Server timezone is configured" | | | | | | | | | |

---

## Test Execution Notes

### Prerequisites for All Tests:
1. **Database Setup:**
   - Test database is isolated from production
   - Database migrations are applied
   - Seed data is available for standard roles

2. **Authentication Setup:**
   - Admin user credentials are known
   - JWT token generation is working
   - Token expiration is configured correctly

3. **Test Data:**
   - Test data can be created and cleaned up
   - Test data does not interfere with other tests
   - Test data is reset between test runs

### Test Execution Guidelines:
1. Execute tests in order (authentication tests first)
2. Clean up test data after each test run
3. Document any deviations from expected results
4. Note any performance issues or timeouts
5. Verify database state after each test

### Common Test Data Setup:
```sql
-- Example test data setup (adjust as needed)
-- Admin user
INSERT INTO Users (Email, PasswordHash, RoleId, Status, FullName) 
VALUES ('admin@test.com', 'hashed_password', 2, 1, 'Test Admin');

-- Test orders (last 7 days)
-- Test transactions
-- Test inventory items
-- Test system logs
```

### Authentication Token Format:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Test Results Summary Template

| Test Category | Total Tests | Passed | Failed | Blocked | Not Executed |
|--------------|-------------|--------|--------|---------|--------------|
| GET Dashboard Data | 6 | | | | |
| GET Revenue 7 Days | 5 | | | | |
| GET Orders 7 Days | 5 | | | | |
| GET Warehouse Alerts | 6 | | | | |
| GET Recent Logs | 6 | | | | |
| Performance Tests | 2 | | | | |
| Error Handling | 5 | | | | |
| **TOTAL** | **35** | | | | |

---

## Notes:
- All dates should be in ISO 8601 format (YYYY-MM-DD)
- All timestamps should include timezone information
- Response times should be measured and documented
- Any deviations from expected results should be documented with screenshots or logs
- Test data should be cleaned up after test execution to avoid test pollution

