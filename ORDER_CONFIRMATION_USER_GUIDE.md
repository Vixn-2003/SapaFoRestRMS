# 📖 User Guide - Màn Hình Xác Nhận Hóa Đơn

## 🎯 Tổng Quan

Màn hình **Xác Nhận Hóa Đơn** giúp thu ngân xác nhận chính xác các món khách đã sử dụng trước khi thanh toán, đặc biệt phân biệt rõ:

1. **Món chế biến trong bếp** - Phải thanh toán 100% nếu đã chế biến
2. **Món tiêu hao** (bia, nước) - Chỉ thanh toán số lượng thực tế sử dụng

---

## 👨‍💼 Quy Trình Sử Dụng

### **Bước 1: Mở Màn Hình Xác Nhận**

Từ danh sách đơn hàng, click vào đơn cần xác nhận:
- URL: `/OrderConfirmation/Index?orderId={id}`
- Hoặc: Click nút "Xác nhận" trên danh sách đơn

### **Bước 2: Kiểm Tra Thông Tin Đơn Hàng**

Màn hình hiển thị:
- **Header:** Mã đơn, số bàn, tên khách hàng
- **2 Sections riêng biệt:**
  - 👨‍🍳 Món chế biến trong bếp
  - 🍺 Món tính theo SL sử dụng

### **Bước 3: Xử Lý Món Chế Biến Trong Bếp**

#### 📗 **Trạng thái "Chưa chế biến"** (Nền xanh nhạt)
- ✅ **CÓ THỂ HỦY**
- Click nút ❌ "Hủy món"
- Nhập lý do hủy
- Món sẽ bị xóa khỏi hóa đơn

#### 🟠 **Trạng thái "Đang chế biến"** (Nền xám)
- ❌ **KHÔNG THỂ HỦY**
- Bếp đã bắt đầu nấu
- Phải thanh toán 100%

#### 🔵 **Trạng thái "Đã hoàn thành"** (Nền xám)
- ❌ **KHÔNG THỂ HỦY**
- Món đã hoàn thành
- Phải thanh toán 100%

### **Bước 4: Điều Chỉnh SL Món Tiêu Hao**

Với các món như bia, nước ngọt, khăn lạnh:

1. **Kiểm tra SL đặt:** Hiển thị ở cột "SL Đặt"
2. **Nhập SL dùng:** 
   - Click vào ô input "SL Dùng"
   - Nhập số lượng thực tế khách đã sử dụng
   - Số phải từ 0 đến SL đặt

3. **Xem thành tiền tự động:**
   - Thành tiền = SL dùng × Đơn giá
   - Cập nhật realtime khi thay đổi

#### Ví dụ:
```
Bia Tiger lon:
- SL đặt: 10 lon
- SL dùng: 7 lon (khách chỉ uống 7)
- Đơn giá: 25,000₫
- Thành tiền: 175,000₫ (chỉ tính 7 lon)
```

### **Bước 5: Xem Tổng Tiền**

Tại phần **Tổng Kết**:
- **Tạm tính:** Tổng tiền tất cả món
- **VAT (10%):** Tự động tính
- **Phí dịch vụ (5%):** Tự động tính
- **Giảm giá:** Nếu có
- **Tổng cộng thanh toán:** Số tiền cuối cùng

Tất cả các số liệu **TỰ ĐỘNG CẬP NHẬT** khi thay đổi SL dùng!

### **Bước 6: Xác Nhận Hóa Đơn**

Click nút **"✓ Xác Nhận Hóa Đơn"**:
- Hệ thống lưu lại:
  - SL dùng cho món tiêu hao
  - Trạng thái đơn hàng = "Confirmed"
  - Thời gian xác nhận
  - Nhân viên xác nhận
- Chuyển sang màn hình thanh toán

---

## 🎨 Giao Diện & Ý Nghĩa Màu Sắc

### **Nền Xanh Nhạt (#E8FBE8)**
- Món **CÓ THỂ HỦY**
- Bếp chưa bắt đầu chế biến
- Text: "✓ Có thể hủy món"

### **Nền Xám Nhạt (#F5F5F5)**
- Món **KHÔNG THỂ HỦY**
- Bếp đang hoặc đã chế biến
- Không có nút hủy

### **Badge Trạng Thái**
- 📗 **Xanh lá:** Chưa chế biến
- 🟠 **Cam:** Đang chế biến
- 🔵 **Xanh dương:** Đã hoàn thành
- ❌ **Đỏ:** Đã hủy

---

## 💡 Tooltips (Gợi Ý)

Các icon **?** màu xanh cung cấp hướng dẫn:

- **Món chế biến:** Giải thích quy tắc hủy món
- **Món tiêu hao:** Giải thích cách tính tiền
- **SL dùng:** Hướng dẫn nhập số lượng

Hover chuột lên icon để xem gợi ý!

---

## ⚠️ Các Tình Huống Thường Gặp

### 🔴 **Tình huống 1: Khách không dùng hết bia**
```
Khách đặt: 12 lon bia
Khách dùng: 8 lon

✅ Xử lý:
1. Ở section "Món tiêu hao"
2. Tìm dòng "Bia..."
3. Nhập SL dùng = 8
4. Thành tiền tự động tính cho 8 lon
5. Xác nhận hóa đơn
```

### 🔴 **Tình huống 2: Khách muốn hủy món chưa nấu**
```
Món: Lẩu Thái (trạng thái: Chưa chế biến)
Khách: "Em ơi, cho anh hủy lẩu đi"

✅ Xử lý:
1. Ở section "Món chế biến"
2. Tìm dòng "Lẩu Thái" (nền xanh nhạt)
3. Click nút ❌ "Hủy món"
4. Nhập lý do: "Khách yêu cầu hủy"
5. Confirm → Món bị xóa khỏi hóa đơn
```

### 🔴 **Tình huống 3: Khách muốn hủy món đang nấu**
```
Món: Steak (trạng thái: Đang chế biến)
Khách: "Em ơi, anh không ăn steak được"

❌ KHÔNG THỂ HỦY!

✅ Giải thích:
"Xin lỗi quý khách, món đang được bếp chế biến rồi ạ.
Quý khách vui lòng thanh toán món này.
Nếu không dùng được, nhà hàng sẽ đóng gói mang về cho quý khách."
```

### 🔴 **Tình huống 4: Nhập SL dùng sai**
```
SL đặt: 5
Nhập SL dùng: 8 ❌

→ Input tự động điều chỉnh về 5
→ Highlight đỏ báo lỗi
→ Không thể nhập quá SL đặt
```

---

## 🚨 Lỗi Thường Gặp & Cách Fix

### **Lỗi: "Không thể hủy món"**
- **Nguyên nhân:** Món đang/đã chế biến
- **Giải pháp:** Giải thích cho khách, không thể hủy

### **Lỗi: "Số lượng không hợp lệ"**
- **Nguyên nhân:** SL dùng > SL đặt hoặc < 0
- **Giải pháp:** Nhập lại số trong khoảng 0 → SL đặt

### **Lỗi: "Không tìm thấy đơn hàng"**
- **Nguyên nhân:** OrderId sai hoặc đơn đã bị xóa
- **Giải pháp:** Quay lại danh sách, chọn đơn khác

---

## 📊 Ví Dụ Thực Tế

### **Đơn Hàng Mẫu:**

```
Bàn B01 - Khách: Anh Tuấn

👨‍🍳 MÓN CHẾ BIẾN:
1. Lẩu cá hồi          × 2    [🔵 Đã hoàn thành]   580,000₫ → 1,160,000₫
2. Steak bò Úc         × 1    [🟠 Đang chế biến]   450,000₫ → 450,000₫
3. Rau tổng hợp        × 1    [📗 Chưa chế biến]   120,000₫ → 120,000₫ ❌ Có thể hủy
   
Tổng món bếp: 1,730,000₫

🍺 MÓN TIÊU HAO:
1. Bia Tiger lon       × 12   [SL dùng: 8 ↓]       25,000₫ → 200,000₫
2. Khăn lạnh          × 10   [SL dùng: 10]        5,000₫ → 50,000₫
3. Nước ngọt          × 6    [SL dùng: 4 ↓]       20,000₫ → 80,000₫

Tổng món tiêu hao: 330,000₫

💰 TỔNG KẾT:
Tạm tính:              2,060,000₫
VAT (10%):              206,000₫
Phí dịch vụ (5%):       103,000₫
─────────────────────────────────
TỔNG THANH TOÁN:     2,369,000₫
```

**Giải thích:**
- Lẩu & Steak: Đã nấu → Thanh toán đủ
- Rau: Chưa nấu → Có thể hủy nếu khách muốn
- Bia: Khách chỉ uống 8/12 → Chỉ tính 8 lon
- Nước ngọt: Khách dùng 4/6 → Chỉ tính 4 chai

---

## 🎓 Tips Cho Thu Ngân

### ✅ **Best Practices:**
1. **Luôn hỏi khách trước khi xác nhận:**
   - "Anh/chị đã sử dụng bao nhiêu lon bia ạ?"
   - "Khăn lạnh anh/chị dùng hết chưa ạ?"

2. **Kiểm tra kỹ SL dùng:**
   - So với SL trên bàn
   - Hỏi waiter nếu không chắc

3. **Giải thích rõ cho khách:**
   - Món nào tính theo SL dùng
   - Món nào phải thanh toán đủ
   - Lý do không thể hủy món đang nấu

4. **Double-check trước khi confirm:**
   - Tất cả SL dùng đã đúng
   - Không có món nào cần hủy
   - Tổng tiền hợp lý

### ❌ **Tránh:**
- Không hỏi khách về SL dùng
- Nhập SL dùng = SL đặt mà không kiểm tra
- Hủy món khi không được phép
- Xác nhận khi còn nghi ngờ

---

## 📞 Hỗ Trợ

Nếu gặp vấn đề:
1. Kiểm tra lại hướng dẫn
2. Hỏi đồng nghiệp/quản lý
3. Gọi IT support: ext. 123

---

## 📝 Checklist Thu Ngân

Trước khi xác nhận, đảm bảo:
- [ ] Đã hỏi khách về SL món tiêu hao
- [ ] Đã nhập đúng SL dùng
- [ ] Đã kiểm tra món có thể hủy (nếu cần)
- [ ] Tổng tiền hợp lý
- [ ] Khách đồng ý với số tiền
- [ ] Click "Xác nhận hóa đơn"

---

**Phiên bản:** 1.0  
**Ngày cập nhật:** 26/11/2025  
**Người tạo:** Tech Team  
**Áp dụng:** Sapa Forest Restaurant

