-- Script để chỉ xóa Orders và OrderDetails (giữ lại MenuItems và MenuCategories)
-- Dùng script này nếu bạn chỉ muốn seed lại orders, không muốn xóa menu items

-- Xóa OrderDetails trước
DELETE FROM OrderDetails;

-- Xóa Orders
DELETE FROM Orders;

-- Reset identity seed
DBCC CHECKIDENT ('OrderDetails', RESEED, 0);
DBCC CHECKIDENT ('Orders', RESEED, 0);

PRINT 'Đã xóa tất cả orders và order details';
PRINT 'MenuItems và MenuCategories vẫn được giữ lại';
PRINT 'Bây giờ bạn có thể chạy lại ứng dụng để DataSeeder tự động seed lại orders mới';

