# 📋 MAIN WORKFLOW - RESTAURANT MANAGEMENT SYSTEM (RMS)

## Tóm tắt luồng từ đặt bàn đến in hóa đơn

---

## 🔄 LUỒNG CHÍNH (9 BƯỚC)

### **1. Khách hàng đặt bàn**
- **Actor:** Khách hàng
- **Service:** `ReservationService.CreateReservationAsync()`
- **Mô tả:**
  - Khách hàng điền form đặt bàn (CustomerName, Phone, Date, Time, NumberOfGuests)
  - Hệ thống tạo User/Customer nếu chưa có
  - Kiểm tra trùng đơn theo Phone + Date + TimeSlot
  - Tạo Reservation với Status = "Pending"
  - Tính DepositAmount = NumberOfGuests × DEPOSIT_PER_GUEST

---

### **2. Quản lý xếp bàn**
- **Actor:** Quản lý
- **Service:** `ReservationService.AssignTablesAsync()`
- **Mô tả:**
  - Quản lý xem danh sách đơn có Status = "Pending"
  - Chọn bàn phù hợp (Capacity >= NumberOfGuests)
  - Kiểm tra conflict (bàn đã được đặt trong slot này?)
  - Gán bàn vào ReservationTables
  - Cập nhật Reservation.Status = "Confirmed"
  - Gán StaffId (người xếp bàn)

---

### **3. Counter Staff xác nhận khách hàng đến**
- **Actor:** Counter Staff
- **Service:** `DashboardTableService.SeatGuestAsync()`
- **Mô tả:**
  - Counter Staff xem danh sách đơn có Status = "Confirmed"
  - Kiểm tra Reservation.Status = "Confirmed"
  - Kiểm tra đã có bàn được gán
  - Cập nhật Reservation.Status = "Guest Seated"
  - Ghi ArrivalAt = DateTime.Now
  - SignalR notify realtime (ReservationStatusChanged)

---

### **4. Phục vụ bàn gọi món + Khách hàng gọi món**
- **Actor:** Phục vụ / Khách hàng
- **Service:** `OrderTableService.CreateOrderAsync()`
- **Mô tả:**
  - Phục vụ/Khách chọn món từ Menu
  - Kiểm tra Reservation đang Active (Status = "Guest Seated")
  - Tạo Order mới với Status = "Pending"
  - Thêm OrderDetails cho từng món (MenuItem/Combo)
  - Thêm OrderComboItems nếu là Combo
  - Reserve inventory cho ConsumptionBased items
  - Lưu Order vào database

---

### **5. Bếp nấu món**
- **Actor:** Bếp
- **Service:** `KitchenDisplayService.UpdateItemStatusAsync()`
- **Mô tả:**
  - Kitchen Display hiển thị đơn mới
  - Kitchen nhận đơn và bắt đầu nấu
  - Kiểm tra đủ nguyên liệu (InventoryService)
  - Cập nhật OrderDetail.Status = "Cooking"
  - Ghi StartedAt = DateTime.Now
  - Nếu là Combo, cập nhật tất cả OrderComboItems
  - SignalR notify realtime
  - Nấu món...
  - Cập nhật OrderDetail.Status = "Ready"
  - Ghi ReadyAt = DateTime.Now
  - Consume inventory (QuantityReserved → QuantityUsed)
  - SignalR notify realtime

---

### **6. Phục vụ bê món**
- **Actor:** Phục vụ
- **Service:** `WaiterOrderTrackingService.MarkAsServedAsync()`
- **Mô tả:**
  - Waiter Order Tracking hiển thị món Ready
  - Kiểm tra món đã Ready
  - Cập nhật OrderDetail.Status = "Done"
  - Ghi ServedAt = DateTime.Now
  - SignalR notify realtime
  - Phục vụ món cho khách

---

### **7. Phục vụ xác nhận hóa đơn để thanh toán**
- **Actor:** Phục vụ
- **Service:** `PaymentService.ConfirmOrderAsync()`
- **Mô tả:**
  - Waiter xem OrderDetail và xác nhận
  - Xác nhận QuantityUsed cho từng món
  - Cập nhật OrderDetail.Status = "Done" (cho các món billable)
  - Tính toán tổng tiền:
    - Subtotal = Σ(UnitPrice × QuantityUsed)
    - VAT = Subtotal × 10%
    - ServiceFee = Subtotal × 5%
    - Discount (nếu có)
    - TotalAmount = Subtotal + VAT + ServiceFee - Discount
  - Lưu TotalAmount vào Orders.TotalAmount
  - Cập nhật Order.Status = "Confirmed"
  - Ghi ConfirmedAt, ConfirmedByStaffId
  - Ghi OrderHistory với action "Order Confirmation"

---

### **8. Thu ngân thanh toán hóa đơn**
- **Actor:** Thu ngân
- **Service:** `PaymentService.ProcessCashPaymentAsync()`
- **Mô tả:**
  - Thu ngân chọn đơn có Status = "Confirmed"
  - Kiểm tra Order.Status = "Confirmed"
  - Thu ngân chọn phương thức thanh toán (Cash/QR/Combined)
  - Tạo Transaction:
    - TransactionCode
    - PaymentMethod
    - Amount = TotalAmount
    - AmountReceived
    - RefundAmount (nếu có)
  - Cập nhật Order.Status = "Paid"
  - Ghi Transaction vào database
  - Trigger post-payment actions:
    - VIP Status Update
    - Loyalty Points +1
    - Inventory Deduction (consume reserved)
    - Revenue Recording
  - Giải phóng bàn (ReleaseTablesAndCompleteReservationAsync)
  - Cập nhật Reservation.Status = "Completed"
  - Xóa ReservationTables
  - Ghi OrderHistory với action "PaymentCompleted"

---

### **9. Thu ngân in hóa đơn**
- **Actor:** Thu ngân
- **Service:** `ReceiptService.GenerateReceiptPdfAsync()`
- **Mô tả:**
  - Thu ngân vào màn hình Receipt
  - Kiểm tra Order.Status = "Paid"
  - Tính toán lại tổng tiền (giống ConfirmOrder)
  - Lấy thông tin Transaction
  - Lấy thông tin Customer và Table
  - Generate PDF bằng QuestPDF:
    - Header: Restaurant info
    - Content: Order info, Items table, Totals
    - Footer: Thank you message
  - Lưu PDF tại /receipts/{orderCode}.pdf
  - Upload lên Cloudinary (nếu có)
  - Trả về PDF file cho thu ngân
  - Thu ngân in hoặc tải PDF

---

## 📊 TRẠNG THÁI CHÍNH

### **Reservation Status:**
- `Pending` → `Confirmed` → `Guest Seated` → `Completed`

### **Order Status:**
- `Pending` → `WaitingConfirmation` → `Confirmed` → `Paid`

### **OrderDetail Status:**
- `Pending` → `Cooking` → `Ready` → `Done`

---

## 🔑 CÁC SERVICE CHÍNH

1. **ReservationService** - Quản lý đặt bàn và xếp bàn
2. **DashboardTableService** - Quản lý bàn và xác nhận khách đến
3. **OrderTableService** - Tạo đơn hàng và thêm món
4. **KitchenDisplayService** - Quản lý trạng thái nấu món
5. **WaiterOrderTrackingService** - Quản lý phục vụ món
6. **PaymentService** - Xác nhận đơn và thanh toán
7. **ReceiptService** - Tạo hóa đơn PDF

---

## 📁 FILE PLANTUML

- `Main_Workflow_RMS.puml` - Phiên bản tóm tắt
- `Main_Workflow_RMS_Detailed.puml` - Phiên bản chi tiết với các điều kiện và xử lý lỗi

---

## ✅ KẾT LUẬN

Luồng chính của hệ thống RMS bao gồm 9 bước từ khi khách hàng đặt bàn đến khi thu ngân in hóa đơn. Mỗi bước đều có validation, error handling và realtime notification qua SignalR để đảm bảo tính nhất quán và trải nghiệm người dùng tốt.

