# 📋 Service Registration Guide - Order Confirmation Feature

## 🔧 Backend API - Program.cs / Startup.cs

Thêm registration cho `IOrderConfirmationService`:

```csharp
// File: Backend/SapaFoRestRMSAPI/Program.cs

// Thêm vào phần Services registration
builder.Services.AddScoped<IOrderConfirmationService, OrderConfirmationService>();
```

**Vị trí trong code:**
```csharp
// ... other services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IOrderConfirmationService, OrderConfirmationService>(); // ← ADD THIS
```

---

## 🌐 Frontend - Program.cs

Đã có HttpClientFactory, không cần thêm gì!

```csharp
// Đã có sẵn:
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"] 
        ?? "https://localhost:7001/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

---

## 📦 Required NuGet Packages

### Backend API:
- ✅ AutoMapper.Extensions.Microsoft.DependencyInjection (Đã có)
- ✅ Microsoft.EntityFrameworkCore (Đã có)
- ✅ Swashbuckle.AspNetCore (Đã có)

### Frontend:
- ✅ System.Text.Json (Đã có)
- ✅ Microsoft.AspNetCore.Mvc (Đã có)

**→ Không cần cài thêm package nào!**

---

## 🗂️ Files Created

### Backend:
```
Backend/
├─ BusinessAccessLayer/
│   ├─ DTOs/OrderConfirmation/
│   │   ├─ OrderConfirmationDto.cs          ✅
│   │   ├─ OrderItemConfirmDto.cs           ✅
│   │   ├─ ConfirmOrderRequestDto.cs        ✅
│   │   └─ CancelItemRequestDto.cs          ✅
│   │
│   └─ Services/
│       ├─ Interfaces/
│       │   └─ IOrderConfirmationService.cs ✅
│       │
│       └─ OrderConfirmationService.cs      ✅
│
└─ SapaFoRestRMSAPI/
    └─ Controllers/
        └─ OrderConfirmationController.cs   ✅
```

### Frontend:
```
Frontend/
└─ WebSapaForestForStaff/
    ├─ Controllers/
    │   └─ OrderConfirmationController.cs   ✅
    │
    └─ Views/OrderConfirmation/
        └─ Index.cshtml                      ✅
```

---

## 🔗 API Endpoints Created

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/orderconfirmation/{orderId}` | Lấy thông tin đơn hàng |
| POST | `/api/orderconfirmation/confirm` | Xác nhận hóa đơn |
| POST | `/api/orderconfirmation/cancel-item` | Hủy món |
| GET | `/api/orderconfirmation/can-cancel/{orderDetailId}` | Kiểm tra có thể hủy không |

---

## 🧪 Testing URLs

### Development:
```
Frontend: https://localhost:7002/OrderConfirmation/Index?orderId=123
Backend API: https://localhost:7001/api/orderconfirmation/123
Swagger: https://localhost:7001/swagger/index.html
```

### Test Flow:
1. Create test order
2. Navigate to: `/OrderConfirmation/Index?orderId={id}`
3. Verify 2 sections display correctly
4. Test quantity input for consumption items
5. Test cancel button for kitchen items
6. Confirm order
7. Verify redirect to payment page

---

## ⚠️ Additional Setup Required

### 1. Add Repository Method (if not exists)

File: `DataAccessLayer/Repositories/PaymentRepository.cs` (or similar)

```csharp
public async Task<OrderDetail?> GetOrderDetailByIdAsync(int orderDetailId)
{
    return await _context.OrderDetails
        .Include(od => od.MenuItem)
        .Include(od => od.Combo)
        .Include(od => od.Order)
        .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);
}
```

Add to interface:
```csharp
public interface IPaymentRepository
{
    // ... existing methods
    Task<OrderDetail?> GetOrderDetailByIdAsync(int orderDetailId);
}
```

### 2. Configure Routes (if needed)

Frontend `_counterstaffLayout.cshtml` hoặc menu:
```html
<a href="/OrderConfirmation/Index?orderId=@Model.OrderId" class="nav-link">
    🧾 Xác nhận hóa đơn
</a>
```

---

## 🎨 CSS Classes Used

Custom classes trong View:
- `.confirmation-container`
- `.confirmation-card`
- `.kitchen-section` / `.consumption-section`
- `.badge-kitchen-*` (notstarted, cooking, done)
- `.bg-cancelable` / `.bg-non-cancelable`
- `.quantity-input`
- `.tooltip-icon`

**→ Tất cả đã được define trong View, không cần CSS file riêng!**

---

## 📱 Responsive Design

View đã có responsive design cho mobile:
- Tables scroll horizontally on small screens
- Buttons stack vertically
- Font sizes adjust
- Touch-friendly input sizes

---

## 🔒 Authorization

Controllers đã có `[Authorize]` attribute:
- Roles: Staff, Manager, Admin
- Requires JWT token in cookies

---

## ✅ Checklist Before Deploy

- [ ] Service registered in Program.cs
- [ ] Repository method `GetOrderDetailByIdAsync` added
- [ ] Test all API endpoints
- [ ] Test Frontend UI with real data
- [ ] Verify 2 sections display correctly
- [ ] Test realtime calculation
- [ ] Test cancel kitchen item
- [ ] Test confirm order flow
- [ ] Check responsive design on mobile
- [ ] Verify tooltips working
- [ ] Test authorization/authentication

---

## 🚀 Deployment Steps

1. **Backend:**
   ```bash
   cd Backend/SapaFoRestRMSAPI
   dotnet publish -c Release
   ```

2. **Frontend:**
   ```bash
   cd Frontend/WebSapaForestForStaff
   dotnet publish -c Release
   ```

3. **Database:**
   - No new migrations needed (uses existing schema)
   - Ensure `BillingType` column exists in `MenuItems`

---

**Created:** November 26, 2025  
**Status:** ✅ Complete  
**Files:** 10 files created  
**Time to implement:** ~2-3 hours

