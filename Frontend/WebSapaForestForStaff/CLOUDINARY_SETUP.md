# 📸 Cloudinary Setup Guide - Staff Management

## 🎯 Tổng quan

Module Staff Management đã được tích hợp **Cloudinary Upload Widget** để upload ảnh đại diện nhân viên trực tiếp từ browser lên cloud storage.

---

## 🚀 Bước 1: Tạo Cloudinary Account (MIỄN PHÍ)

1. Truy cập: https://cloudinary.com/users/register/free
2. Đăng ký tài khoản miễn phí (Free plan: 25GB storage, 25GB bandwidth/tháng)
3. Xác nhận email và đăng nhập

---

## ⚙️ Bước 2: Lấy Cloud Name

Sau khi đăng nhập vào Cloudinary Dashboard:

1. Vào **Dashboard** (trang chủ)
2. Tìm phần **Account Details**
3. Copy **Cloud Name** (ví dụ: `dqn7os3pr`)

![Cloud Name Location](https://res.cloudinary.com/demo/image/upload/v1/docs/cloud_name.png)

---

## 🔐 Bước 3: Tạo Upload Preset (Unsigned)

### 3.1. Vào Settings
1. Click vào icon **Settings** (⚙️) ở góc phải trên
2. Chọn tab **Upload**
3. Scroll xuống phần **Upload presets**

### 3.2. Tạo Preset mới
1. Click **Add upload preset**
2. Điền thông tin:
   - **Preset name**: `staff_avatars_preset` (hoặc tên tùy chọn)
   - **Signing mode**: Chọn **Unsigned** ⚠️ QUAN TRỌNG!
   - **Folder**: `staff_avatars` (để tổ chức ảnh)
   - **Use filename or externally defined public ID**: Tắt (để Cloudinary tự generate)
   - **Unique filename**: Bật (tránh trùng lặp)
   
3. **Optional Settings** (khuyến nghị):
   - **Allowed formats**: `jpg, png, gif, jpeg, webp`
   - **Max file size**: `5000000` bytes (5MB)
   - **Max image dimensions**: Width `2000`, Height `2000`
   - **Transformation**: Thêm `c_fill,w_500,h_500,g_face` (auto crop khuôn mặt)

4. Click **Save**

### 3.3. Copy Upload Preset Name
Copy tên preset vừa tạo (ví dụ: `staff_avatars_preset` hoặc `ml_default`)

---

## 📝 Bước 4: Cập nhật Code

### 4.1. File: `Create.cshtml`

Tìm dòng (khoảng line 165):
```javascript
const CLOUDINARY_CLOUD_NAME = 'dqn7os3pr'; // Replace with your cloud name
const CLOUDINARY_UPLOAD_PRESET = 'ml_default'; // Replace with your upload preset
```

Thay thế:
```javascript
const CLOUDINARY_CLOUD_NAME = 'YOUR_CLOUD_NAME'; // ← Paste Cloud Name của bạn
const CLOUDINARY_UPLOAD_PRESET = 'staff_avatars_preset'; // ← Paste Upload Preset của bạn
```

### 4.2. File: `Edit.cshtml`

Tương tự, tìm và thay thế (khoảng line 169):
```javascript
const CLOUDINARY_CLOUD_NAME = 'YOUR_CLOUD_NAME';
const CLOUDINARY_UPLOAD_PRESET = 'staff_avatars_preset';
```

---

## ✅ Bước 5: Test Upload

1. Run application
2. Vào `/StaffManagement/Create`
3. Click **"Upload ảnh từ máy tính"**
4. Upload một ảnh test
5. Kiểm tra:
   - Preview hiển thị đúng ✅
   - URL được điền vào hidden input ✅
   - Submit form → ảnh được lưu trong DB ✅

---

## 🎨 Tính năng đã tích hợp

### ✨ Features
- ✅ **Upload từ máy tính** (drag & drop support)
- ✅ **Upload từ URL** (paste link ảnh)
- ✅ **Chụp ảnh từ camera** (trên mobile/desktop có webcam)
- ✅ **Crop ảnh** (tỷ lệ 1:1 - vuông)
- ✅ **Preview real-time** (xem ảnh trước khi submit)
- ✅ **Xóa ảnh** (nút "Xóa ảnh")
- ✅ **Validation** (max 5MB, chỉ JPG/PNG/GIF)
- ✅ **Ngôn ngữ tiếng Việt** (toàn bộ UI)

### 📦 Widget Config
```javascript
{
    cloudName: 'YOUR_CLOUD_NAME',
    uploadPreset: 'staff_avatars_preset',
    sources: ['local', 'url', 'camera'], // 3 nguồn upload
    multiple: false, // Chỉ 1 ảnh
    maxFileSize: 5000000, // 5MB
    clientAllowedFormats: ['jpg', 'png', 'gif', 'jpeg', 'webp'],
    cropping: true, // Bật crop
    croppingAspectRatio: 1, // Crop vuông
    folder: 'staff_avatars', // Lưu vào folder này trên Cloudinary
    language: 'vi' // Tiếng Việt
}
```

---

## 🔒 Security Best Practices

### 1. **Unsigned Upload Preset** (Đã setup)
- Client-side upload trực tiếp lên Cloudinary
- Không cần expose API Secret
- Giới hạn được config trong Preset

### 2. **Folder Organization**
Ảnh được tổ chức theo folder:
```
Cloudinary Root
└── staff_avatars/
    ├── abc123xyz.jpg (Staff ID 1)
    ├── def456uvw.png (Staff ID 2)
    └── ghi789rst.jpg (Staff ID 3)
```

### 3. **Transformation on Upload** (Optional)
Thêm vào Upload Preset để auto optimize:
```
Incoming Transformation:
- Quality: auto
- Format: auto (WebP cho browser hỗ trợ)
- Width: 500px, Height: 500px
- Crop: fill, Gravity: face
```

---

## 📊 Monitoring & Analytics

### Xem usage trên Cloudinary Dashboard:
1. **Dashboard** → **Media Library**: Xem tất cả ảnh đã upload
2. **Dashboard** → **Analytics**: Xem bandwidth, storage usage
3. **Dashboard** → **Reports**: Xem transformation stats

### Limits của Free Plan:
- ✅ **Storage**: 25GB
- ✅ **Bandwidth**: 25GB/month
- ✅ **Transformations**: 25,000/month
- ✅ **Admin API calls**: 500/hour

---

## 🐛 Troubleshooting

### 1. **"Upload Failed" error**
**Nguyên nhân**: Upload Preset bị sai hoặc là "Signed"
**Fix**: 
- Kiểm tra `CLOUDINARY_UPLOAD_PRESET` đúng tên chưa
- Đảm bảo Preset là **Unsigned**

### 2. **Widget không mở**
**Nguyên nhân**: CDN script chưa load
**Fix**: 
- Kiểm tra Internet connection
- Xem Console có error không
- Đảm bảo có script: `<script src="https://upload-widget.cloudinary.com/global/all.js"></script>`

### 3. **Preview không hiển thị**
**Nguyên nhân**: jQuery chưa load hoặc ID element sai
**Fix**:
- Kiểm tra jQuery đã include chưa
- Xem Console có error không
- Verify ID: `#avatarPreview`, `#avatarUrlInput`

### 4. **CORS Error**
**Nguyên nhân**: Cloudinary settings chặn domain
**Fix**:
- Vào Cloudinary Settings → Security
- Add domain vào **Allowed fetch domains** (nếu upload from URL)

---

## 🎓 Advanced Usage

### Custom Transformations
Thay đổi kích thước ảnh khi hiển thị:
```javascript
// Thay vì dùng URL gốc
const imageUrl = result.info.secure_url;

// Dùng URL có transformation
const imageUrl = result.info.secure_url.replace('/upload/', '/upload/w_500,h_500,c_fill,g_face/');
```

### Named Transformations
Tạo preset transformation trên Cloudinary:
1. Settings → Transformations → Named transformations
2. Tạo preset: `staff_avatar_thumb` = `w_500,h_500,c_fill,g_face,q_auto,f_auto`
3. Dùng: `cloudinary.url('staff_avatars/abc123.jpg', {transformation: 'staff_avatar_thumb'})`

---

## 📚 Resources

- **Cloudinary Docs**: https://cloudinary.com/documentation
- **Upload Widget Docs**: https://cloudinary.com/documentation/upload_widget
- **Upload Presets**: https://cloudinary.com/documentation/upload_presets
- **Transformations**: https://cloudinary.com/documentation/image_transformations

---

## ✅ Checklist

- [ ] Đã tạo Cloudinary account
- [ ] Đã lấy Cloud Name
- [ ] Đã tạo Unsigned Upload Preset
- [ ] Đã update constants trong `Create.cshtml`
- [ ] Đã update constants trong `Edit.cshtml`
- [ ] Đã test upload ảnh thành công
- [ ] Đã verify ảnh xuất hiện trong Cloudinary Media Library

---

🎉 **Done! Staff Management module đã sẵn sàng upload ảnh lên Cloudinary!**

