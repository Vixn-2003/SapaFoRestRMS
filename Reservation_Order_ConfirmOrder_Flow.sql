-- ===============================================
-- RESERVATION → ORDER → CONFIRM ORDER FLOW SCRIPT
-- ===============================================
-- This script demonstrates the complete flow from creating a reservation
-- to confirming an order with all business logic including TotalAmount saving
-- and post-payment actions triggering.

-- ===============================================
-- STEP 1: CREATE RESERVATION
-- ===============================================

-- API Call: POST /api/reservation/create
-- or POST /api/ReservationStaff/AddReservation
{
  "customerName": "Nguyễn Văn A",
  "phone": "0987654321",
  "reservationDate": "2025-12-20",
  "reservationTime": "19:00:00",
  "numberOfGuests": 4,
  "notes": "Khách VIP, yêu cầu bàn gần cửa sổ",
  "otpCode": "123456",
  "paymentMethod": "PAYOS"
}

-- Expected Response:
-- {
--   "reservationId": 123,
--   "status": "Confirmed",
--   "message": "Reservation created successfully"
-- }

-- Database Changes:
INSERT INTO Reservations (
    CustomerNameReservation, Phone, ReservationDate, ReservationTime,
    NumberOfGuests, Notes, Status, CreatedAt, OtpCode, PaymentMethod
) VALUES (
    'Nguyễn Văn A', '0987654321', '2025-12-20', '19:00:00',
    4, 'Khách VIP, yêu cầu bàn gần cửa sổ', 'Confirmed',
    GETUTCDATE(), '123456', 'PAYOS'
);

-- Assign tables to reservation (if auto-assigned)
INSERT INTO ReservationTables (ReservationId, TableId, AssignedAt)
SELECT 123, T.TableId, GETUTCDATE()
FROM Tables T
WHERE T.Status = 'Available'
  AND T.Capacity >= 4
  AND T.AreaId IN (SELECT AreaId FROM Areas WHERE Name = 'Tầng 1')
ORDER BY T.Capacity ASC; -- Smallest suitable table first

-- ===============================================
-- STEP 2: CUSTOMER ARRIVES - CREATE ORDER
-- ===============================================

-- When customer arrives and sits at the reserved table,
-- staff creates an order for the table.

-- API Call: POST /api/DashboardTable/SaveChanges
-- (This creates a new order for the table)
{
  "tableId": 15,
  "items": []  -- Empty initially, items will be added next
}

-- Expected Response:
-- {
--   "success": true,
--   "message": "Lưu thành công!",
--   "orderId": 456
-- }

-- Database Changes:
INSERT INTO Orders (
    ReservationId, CustomerId, Status, CreatedAt,
    ConfirmedAt, ConfirmedByStaffId, TotalAmount
) VALUES (
    123, NULL, 'WaitingConfirmation', GETUTCDATE(),
    NULL, NULL, NULL
);

-- Create OrderDetails (initially empty)
-- Items will be added in Step 3

-- ===============================================
-- STEP 3: ADD ITEMS TO ORDER
-- ===============================================

-- Customer orders food/drinks. Staff adds items to the order.

-- API Call: POST /api/DashboardTable/SaveChanges
-- (Add items to existing order)
{
  "tableId": 15,
  "items": [
    {
      "orderItemId": 0,  -- 0 for new items
      "menuItemId": 101, -- Phở bò
      "comboId": null,
      "quantity": 2,
      "note": "Ít rau",
      "action": "Add"
    },
    {
      "orderItemId": 0,  -- 0 for new items
      "menuItemId": 102, -- Bún riêu
      "comboId": null,
      "quantity": 1,
      "note": null,
      "action": "Add"
    },
    {
      "orderItemId": 0,  -- 0 for new items
      "menuItemId": null,
      "comboId": 201,    -- Combo Family Set
      "quantity": 1,
      "note": "Thêm nước ngọt",
      "action": "Add"
    }
  ]
}

-- Expected Response:
-- {
--   "success": true,
--   "message": "Lưu thành công!"
-- }

-- Database Changes:
-- Insert OrderDetails for each item
INSERT INTO OrderDetails (
    OrderId, MenuItemId, ComboId, Quantity, QuantityUsed,
    UnitPrice, Status, Notes, CreatedAt
) VALUES
(456, 101, NULL, 2, NULL, 45000, 'Pending', 'Ít rau', GETUTCDATE()),
(456, 102, NULL, 1, NULL, 35000, 'Pending', NULL, GETUTCDATE()),
(456, NULL, 201, 1, NULL, 120000, 'Pending', 'Thêm nước ngọt', GETUTCDATE());

-- For combo items, also insert OrderComboItems
INSERT INTO OrderComboItems (
    OrderDetailId, MenuItemId, Quantity, UnitPrice, Status
) VALUES
(1001, 301, 2, 15000, 'Pending'),  -- Cơm chiên in Family Combo
(1001, 302, 4, 10000, 'Pending');  -- Nước ngọt in Family Combo

-- Reserve inventory for ConsumptionBased items
UPDATE IngredientBatches
SET ReservedQuantity = ReservedQuantity + requested_quantity
WHERE BatchId IN (
    SELECT TOP 1 BatchId
    FROM IngredientBatches IB
    WHERE IB.IngredientId = required_ingredient_id
      AND IB.ExpiryDate > GETUTCDATE()
      AND IB.AvailableQuantity >= requested_quantity
    ORDER BY IB.ExpiryDate ASC
);

-- ===============================================
-- STEP 4: KITCHEN PROCESSES ITEMS
-- ===============================================

-- Kitchen staff starts cooking items and updates their status

-- Items move through statuses: Pending → Cooking → Done → Served
UPDATE OrderDetails
SET Status = 'Cooking', UpdatedAt = GETUTCDATE()
WHERE OrderDetailId IN (1001, 1002, 1003);

-- Later, when items are ready:
UPDATE OrderDetails
SET Status = 'Done', UpdatedAt = GETUTCDATE()
WHERE OrderDetailId IN (1001, 1002, 1003);

-- ===============================================
-- STEP 5: CONFIRM ORDER (CUSTOMER QUANTITY CONFIRMATION)
-- ===============================================

-- Customer confirms the actual quantity consumed and pays

-- API Call: PUT /api/payment/orders/{orderId}/confirm
-- Body:
{
  "orderId": 456,
  "items": [
    {
      "orderDetailId": 1001,
      "quantityUsed": 2,  -- Customer ate all Phở bò
      "isRemoved": false
    },
    {
      "orderDetailId": 1002,
      "quantityUsed": 1,  -- Customer ate all Bún riêu
      "isRemoved": false
    },
    {
      "orderDetailId": 1003,
      "quantityUsed": 1,  -- Customer used the full combo
      "isRemoved": false
    }
  ]
}

-- Expected Response:
-- {
--   "orderId": 456,
--   "status": "Confirmed",
--   "totalAmount": 195000,
--   "subtotal": 195000,
--   "vatAmount": 19500,
--   "serviceFee": 9750,
--   "discountAmount": 0,
--   "items": [...]
-- }

-- ===============================================
-- BUSINESS LOGIC IN ConfirmOrderAsync():
-- ===============================================

-- 1. VALIDATION: Check order exists and has items
-- 2. QUANTITY CONFIRMATION: Update QuantityUsed for each item
-- 3. STATUS UPDATES: Set billable items to "Done"
-- 4. CALCULATE TOTALS: Compute Subtotal, VAT, Service Fee, Total
-- 5. SAVE TOTALAMOUNT: Store calculated total in Orders.TotalAmount
-- 6. ORDER STATUS: Change Status from "WaitingConfirmation" to "Confirmed"
-- 7. AUDIT LOGGING: Record ConfirmedAt, ConfirmedByStaffId, OrderHistory

-- Database Changes in ConfirmOrderAsync:
UPDATE OrderDetails
SET QuantityUsed = CASE
    WHEN OrderDetailId = 1001 THEN 2  -- Phở bò: 2 used
    WHEN OrderDetailId = 1002 THEN 1  -- Bún riêu: 1 used
    WHEN OrderDetailId = 1003 THEN 1  -- Combo: 1 used
END,
Status = 'Done',
UpdatedAt = GETUTCDATE()
WHERE OrderDetailId IN (1001, 1002, 1003);

-- Calculate and update Order totals
DECLARE @subtotal DECIMAL(18,2) = 195000;  -- (2*45000) + (1*35000) + (1*120000)
DECLARE @vatAmount DECIMAL(18,2) = @subtotal * 0.10;  -- 19500
DECLARE @serviceFee DECIMAL(18,2) = @subtotal * 0.05; -- 9750
DECLARE @totalAmount DECIMAL(18,2) = @subtotal + @vatAmount + @serviceFee; -- 214500

UPDATE Orders
SET Status = 'Confirmed',
    ConfirmedAt = GETUTCDATE(),
    ConfirmedByStaffId = 789,  -- Staff who confirmed
    TotalAmount = @totalAmount,
    UpdatedAt = GETUTCDATE()
WHERE OrderId = 456;

-- Insert OrderHistory for audit trail
INSERT INTO OrderHistories (
    OrderId, Action, Reason, StaffId, CreatedAt
) VALUES (
    456,
    'Order Confirmation',
    'Confirmed by staff. Total amount: 214500 VND',
    789,
    GETUTCDATE()
);

-- ===============================================
-- STEP 6: PAYMENT PROCESSING
-- ===============================================

-- Customer pays for the confirmed order
-- (This can be Cash, QR, or Split Bill - all trigger post-payment actions when fully paid)

-- API Call: POST /api/payment/process
-- or POST /api/payment/cash
-- or POST /api/payment/split-bill

-- When payment is completed (Status = "Paid"):
-- ✅ Trigger Post-Payment Actions:
--   - VIP Status Update
--   - Loyalty Points +1
--   - Inventory Deduction (consume reserved batches)
--   - Revenue Recording
--   - Receipt Generation
--   - Table Release & Reservation Completion

-- ===============================================
-- COMPLETE FLOW SUMMARY:
-- ===============================================

-- 1. RESERVATION CREATED
--    └── Reservation.Status = "Confirmed"
--    └── Tables assigned via ReservationTables

-- 2. ORDER CREATED (when customer arrives)
--    └── Order.Status = "WaitingConfirmation"
--    └── OrderDetails created (empty initially)

-- 3. ITEMS ADDED TO ORDER
--    └── OrderDetails populated
--    └── OrderComboItems for combos
--    └── Inventory reserved for ConsumptionBased items

-- 4. KITCHEN PROCESSING
--    └── Item statuses: Pending → Cooking → Done

-- 5. ORDER CONFIRMATION (customer quantity confirmation)
--    └── Order.Status = "Confirmed"
--    └── Order.ConfirmedAt = current time
--    └── Order.ConfirmedByStaffId = staff ID
--    └── Order.TotalAmount = calculated total ⭐NEW⭐
--    └── OrderHistory logged

-- 6. PAYMENT PROCESSING
--    └── Order.Status = "Paid"
--    └── Post-payment actions triggered
--    └── Tables released, Reservation completed

-- ===============================================
-- KEY BUSINESS RULES:
-- ===============================================

-- ✅ TotalAmount Calculation:
--    Subtotal = Σ(UnitPrice × QuantityUsed) for billable items only
--    VAT = Subtotal × 10%
--    ServiceFee = Subtotal × 5%
--    TotalAmount = (Subtotal + VAT + ServiceFee - Discount) rounded to 1000VND

-- ✅ Billable Items: Only items with status "Cooking", "Done", "Ready", "Served"
-- ✅ Pending items are NOT included in total calculation
-- ✅ QuantityUsed drives final billing (for ConsumptionBased items)

-- ✅ Audit Trail: Every major action is logged in OrderHistories
-- ✅ Data Integrity: TotalAmount saved ensures payment accuracy

-- ===============================================
-- TESTING THE FLOW:
-- ===============================================

-- 1. Create reservation via API
-- 2. Create order by adding items to table
-- 3. Update item statuses to "Done" (simulate kitchen)
-- 4. Confirm order and verify TotalAmount is saved
-- 5. Process payment and verify post-payment actions trigger

-- ===============================================
-- MONITORING QUERIES:
-- ===============================================

-- Check order status progression:
SELECT OrderId, Status, ConfirmedAt, ConfirmedByStaffId, TotalAmount, PaidAt
FROM Orders
WHERE OrderId = 456;

-- Check order history:
SELECT Action, Reason, CreatedAt, Staff.FullName as StaffName
FROM OrderHistories OH
JOIN Staff ON OH.StaffId = Staff.StaffId
WHERE OH.OrderId = 456
ORDER BY OH.CreatedAt DESC;

-- Check transaction details:
SELECT TransactionCode, Amount, PaymentMethod, Status, CreatedAt
FROM Transactions
WHERE OrderId = 456;
