-- Migration: Normalize Order Status to kebab-case format
-- Date: 2025-01-20
-- Purpose: Fix inconsistent Order.Status values to match OrderStatusConstants

-- Update to standardized kebab-case format
UPDATE Orders
SET Status = 'waiting-confirmation'
WHERE Status IN ('Pending', 'pending', 'WaitingConfirmation', 'waiting-confirmation');

UPDATE Orders
SET Status = 'confirmed'
WHERE Status IN ('Confirmed', 'confirmed');

UPDATE Orders
SET Status = 'pending-payment'
WHERE Status IN ('PendingPayment', 'pending-payment', 'PaymentProcessing');

UPDATE Orders
SET Status = 'paid'
WHERE Status IN ('Paid', 'paid', 'Completed', 'completed');

UPDATE Orders
SET Status = 'partially-paid'
WHERE Status IN ('PartiallyPaid', 'partially-paid');

-- Verify the update
SELECT Status, COUNT(*) as Count
FROM Orders
GROUP BY Status
ORDER BY Status;

PRINT 'Order status normalization completed successfully.';

