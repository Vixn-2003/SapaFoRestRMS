# 🔒 CORS Configuration Guide

## 📖 Giới thiệu

Khi làm việc nhóm, mỗi developer sẽ có IP khác nhau trên mạng LAN/WiFi.  
Để tránh phải commit IP cá nhân lên Git, chúng ta sử dụng **`appsettings.Development.json`** để cấu hình riêng.

---

## 🚀 Hướng dẫn Setup (Cho Developer mới)

### **Bước 1: Copy file template**

```bash
# Trong thư mục Backend/SapaFoRestRMSAPI/
cp appsettings.Development.json.template appsettings.Development.json
```

Hoặc copy thủ công file `appsettings.Development.json.template` và đổi tên thành `appsettings.Development.json`

---

### **Bước 2: Lấy IP của máy bạn**

**Trên Windows:**
```powershell
ipconfig
```

**Trên Mac/Linux:**
```bash
ifconfig
# hoặc
ip addr show
```

Tìm **IPv4 Address** trong phần WiFi/Ethernet, ví dụ:
```
IPv4 Address: 192.168.1.10
```

---

### **Bước 3: Thêm IP vào `appsettings.Development.json`**

Mở file `appsettings.Development.json` và thêm IP của bạn vào mảng `AllowedOrigins`:

```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:5054",
      "http://localhost:5123",
      "http://localhost:5180",
      "https://localhost:7096",
      
      // ✅ THÊM IP CỦA BẠN Ở ĐÂY
      "http://192.168.1.10:5123",    // 👈 Thay 192.168.1.10 bằng IP của bạn
      "https://192.168.1.10:5123",
      "http://192.168.1.10:5054",
      "https://192.168.1.10:5054"
    ]
  }
}
```

**Lưu ý:**
- ✅ Thay `192.168.1.10` bằng IP thực của bạn
- ✅ Giữ nguyên các port: `5123`, `5054`, `5180`, `7096`
- ✅ Thêm cả HTTP và HTTPS để đảm bảo hoạt động trong mọi trường hợp

---

### **Bước 4: Restart Backend API**

Sau khi chỉnh sửa file, **bắt buộc phải restart backend**:

```bash
# Stop backend (Ctrl+C)
# Then restart:
dotnet run
```

Hoặc trong Visual Studio: Stop Debugging → Start Debugging (F5)

---

### **Bước 5: Kiểm tra Console Log**

Khi backend khởi động, bạn sẽ thấy danh sách CORS origins được load:

```
🔒 CORS Allowed Origins:
   ✅ http://localhost:5054
   ✅ http://localhost:5123
   ✅ http://192.168.1.10:5123  ← IP của bạn
   ✅ https://192.168.1.10:5123
   ...
```

---

## 📁 File Structure

```
Backend/SapaFoRestRMSAPI/
├── appsettings.json                          ← Base config (commit to Git)
├── appsettings.Development.json              ← Your personal config (DO NOT commit)
├── appsettings.Development.json.template     ← Template for team members (commit to Git)
└── CORS_SETUP_GUIDE.md                       ← This guide (commit to Git)
```

---

## ⚠️ QUAN TRỌNG: Gitignore

File `appsettings.Development.json` **KHÔNG ĐƯỢC commit lên Git** vì chứa IP cá nhân.

Đảm bảo file `.gitignore` có dòng sau:

```gitignore
# Development settings (contains personal IPs)
**/appsettings.Development.json
```

---

## 🔧 Troubleshooting

### **Lỗi: "CORS policy blocked"**

**Nguyên nhân:** IP của bạn chưa được thêm vào CORS hoặc backend chưa restart

**Giải pháp:**
1. Kiểm tra IP trong `appsettings.Development.json`
2. Restart backend API
3. Clear browser cache (Ctrl+Shift+Delete)

---

### **Lỗi: SignalR connection failed**

**Nguyên nhân:** SignalR Hub cũng cần CORS

**Giải pháp:**
- Đảm bảo `app.UseCors()` được gọi **TRƯỚC** `app.MapHub()`
- Trong `Program.cs`, thứ tự phải là:
  ```csharp
  app.UseCors(MyAllowSpecificOrigins);  // ← Phải ở đây
  app.UseAuthentication();
  app.UseAuthorization();
  app.MapHub<RestaurantHub>("/restaurantHub");  // ← Sau khi enable CORS
  ```

---

## 🎯 Best Practices

1. ✅ **Không commit IP cá nhân** lên Git
2. ✅ **Sử dụng template file** để team member dễ setup
3. ✅ **Log CORS origins** trong console để dễ debug
4. ✅ **Restart backend** sau mỗi lần thay đổi CORS

---

## 📞 Hỗ trợ

Nếu vẫn gặp lỗi CORS sau khi làm theo hướng dẫn, kiểm tra:
- [ ] Đã restart backend chưa?
- [ ] IP trong config có đúng không? (check bằng `ipconfig`)
- [ ] Port có đúng không? (frontend đang chạy port nào?)
- [ ] Console log có hiển thị IP của bạn không?

---

**Happy Coding! 🎉**

