# Thêm Món Sau Khi Đã Xác Nhận Order
## SapaForest Restaurant Management System

---

**Ngày tạo:** 20/01/2025  
**Vấn đề:** Khách muốn thêm món sau khi đã xác nhận đơn hàng  
**Trạng thái:** ⚠️ Chưa được hỗ trợ trong hệ thống hiện tại

---

## 📋 Mục Lục

1. [Tình Huống Thực Tế](#1-tình-huống-thực-tế)
2. [Phân Tích Hiện Trạng](#2-phân-tích-hiện-trạng)
3. [Đề Xuất Giải Pháp](#3-đề-xuất-giải-pháp)
4. [Implementation Plan](#4-implementation-plan)
5. [Workflow Chi Tiết](#5-workflow-chi-tiết)
6. [Testing Scenarios](#6-testing-scenarios)

---

## 1. Tình Huống Thực Tế

### Scenario 1: Khách Nhớ Ra Muốn Gọi Thêm

**Timeline:**
```
T=46:   Thu ngân xác nhận đơn với khách
        - 2 Steak Sapa
        - 1 Trà sen
        Status: waiting-confirmation → confirmed ✅

T=47:   Thu ngân: "Anh/chị thanh toán thế nào ạ?"
        Khách: "Khoan, em gọi thêm 1 ly Trà nữa nhé."

T=48:   Thu ngân: "??? 🤔"
        → Hệ thống KHÔNG HỖ TRỢ thêm món khi status = "confirmed"
```

**Giải pháp hiện tại (workaround):**
1. ❌ **Cách 1:** Hoàn tác xác nhận → Thêm món → Xác nhận lại
   - **Vấn đề:** Mất thời gian, phức tạp, dễ nhầm lẫn
   - **Risk:** Nếu bếp đã bắt đầu làm món → Không thể hoàn tác

2. ❌ **Cách 2:** Tạo đơn mới cho món bổ sung
   - **Vấn đề:** 2 đơn riêng biệt, khó quản lý
   - **Risk:** Khách thanh toán chung → Phải merge 2 đơn thủ công

3. ❌ **Cách 3:** Thêm món vào đơn chính nhưng không qua hệ thống
   - **Vấn đề:** Mất tracking, báo cáo sai
   - **Risk:** Khách có thể tranh cãi về bill

---

### Scenario 2: Khách Muốn Thêm Món Khi Đang Chờ Thanh Toán

**Timeline:**
```
T=50:   Thu ngân: "Anh/chị thanh toán bằng tiền mặt hay chuyển khoản ạ?"
T=51:   Khách: "Chờ em chút, em gọi thêm 1 món Lẩu nữa."

        → Đơn đã confirmed, đang ở màn hình Payment
        → Phải quay lại, hoàn tác, thêm món, xác nhận lại, vào Payment lại
```

---

### Scenario 3: Bạn Bè Đến Sau, Gọi Thêm Món

**Timeline:**
```
T=46:   Khách A xác nhận đơn (2 người)
T=48:   Bạn B đến sau: "Mình gọi thêm 1 Steak nữa nhé!"

        → Đơn đã confirmed
        → Cần thêm món cho Bạn B
        → Nhưng hệ thống không cho phép
```

---

## 2. Phân Tích Hiện Trạng

### 2.1. Code Hiện Tại

**Backend: PaymentService.cs**

```csharp
public async Task<OrderDto> ConfirmOrderAsync(CustomerConfirmRequestDto request, ...)
{
    // CHỈ CẬP NHẬT số lượng các món ĐÃ CÓ
    foreach (var confirmed in request.Items)
    {
        var detail = order.OrderDetails.FirstOrDefault(d => d.OrderDetailId == confirmed.OrderDetailId);
        if (detail == null)
        {
            continue; // ❌ Bỏ qua nếu không tìm thấy
        }
        
        detail.Quantity = confirmed.QuantityUsed;
        detail.Status = "Confirmed";
    }
    
    order.Status = OrderStatusConstants.Confirmed;
    await _unitOfWork.SaveChangesAsync();
    
    // ❌ KHÔNG CÓ logic thêm món mới
}
```

**Frontend: ConfirmOrder.cshtml**

```razor
@* CHỈ HIỂN THị các món đã có trong order *@
@foreach (var item in Model.Items)
{
    <tr>
        <td>@item.Name</td>
        <td>@item.Quantity</td>
        <td>
            @if (canEdit)
            {
                <input type="number" name="QuantityUsed" value="@item.Quantity" />
            }
            else
            {
                @item.QuantityUsed
            }
        </td>
    </tr>
}

@* ❌ KHÔNG CÓ nút "Thêm món" *@
```

### 2.2. Các Ràng Buộc Hiện Tại

**Ràng buộc 1: Không thể Undo sau khi thanh toán**
```csharp
if (order.Payments != null && order.Payments.Any(p => p.PaymentDate.HasValue))
{
    throw new InvalidOperationException("Không thể hoàn tác vì đơn hàng đã bắt đầu thanh toán.");
}
```
→ Nếu đã nhấn "Thanh toán", không thể quay lại thêm món

**Ràng buộc 2: Không thể Undo nếu bếp đã chế biến**
```csharp
if (order.OrderDetails.Any(od => od.Status == "Cooking" || od.Status == "Served"))
{
    throw new InvalidOperationException("Không thể hoàn tác vì bếp đã bắt đầu chế biến món.");
}
```
→ Nếu món đã vào bếp, không thể hoàn tác để thêm món

---

## 3. Đề Xuất Giải Pháp

### 3.1. Solution Overview

**Giải pháp:** Cho phép **"Gọi Thêm Món"** khi Order status = `confirmed`

**Điều kiện:**
- ✅ Order.Status = `confirmed`
- ✅ Chưa có Payment transaction (chưa nhấn thanh toán)
- ✅ Món mới được thêm vào với Status = `Pending` hoặc `Confirmed`

**Workflow:**
```
[Order: confirmed]
    ↓
[Thêm món mới]
    ↓
[Gửi món mới cho bếp]
    ↓
[Cập nhật TotalAmount]
    ↓
[Xác nhận lại món mới với khách]
    ↓
[Tiếp tục thanh toán]
```

---

### 3.2. Business Rules

#### Rule 1: Thêm món CHI được khi chưa thanh toán
```csharp
if (order.Status == "paid" || order.Status == "partially-paid")
{
    throw new InvalidOperationException("Không thể thêm món sau khi đã thanh toán.");
}
```

#### Rule 2: Món mới phải được xác nhận lại
```
- Món cũ: Status = "Confirmed" (đã xác nhận)
- Món mới: Status = "Pending" → Cần xác nhận lại với khách
- Option: Tự động confirm luôn nếu khách đồng ý ngay
```

#### Rule 3: Cập nhật TotalAmount
```
Order.TotalAmount += (NewItems.Sum() * 1.15) // VAT + Service
```

#### Rule 4: Audit Log
```
OrderHistory:
  Action: "Add Items After Confirmation"
  Items: "1x Trà sen (45,000đ)"
  AddedBy: StaffId
  Reason: "Khách gọi thêm"
  Timestamp: DateTime.UtcNow
```

---

## 4. Implementation Plan

### 4.1. Backend Changes

#### Step 1: Create New DTO

**File:** `Backend/BusinessAccessLayer/DTOs/Payment/AddItemsRequestDto.cs`

```csharp
namespace BusinessAccessLayer.DTOs.Payment;

public class AddItemsRequestDto
{
    public int OrderId { get; set; }
    
    public List<AddItemDto> Items { get; set; } = new();
    
    public int StaffId { get; set; }
    
    public string Reason { get; set; } = string.Empty; // "Khách gọi thêm"
    
    public bool AutoConfirm { get; set; } = true; // Tự động confirm món mới
}

public class AddItemDto
{
    public int? MenuItemId { get; set; }
    
    public int? ComboId { get; set; }
    
    public int Quantity { get; set; }
    
    public string? Notes { get; set; }
}
```

---

#### Step 2: Add Service Method

**File:** `Backend/BusinessAccessLayer/Services/PaymentService.cs`

```csharp
/// <summary>
/// Thêm món vào Order đã xác nhận
/// </summary>
public async Task<OrderDto> AddItemsToConfirmedOrderAsync(
    AddItemsRequestDto request, 
    CancellationToken ct = default)
{
    // 1. Validate Order
    var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
    if (order == null)
    {
        throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
    }
    
    // 2. Check Order Status
    if (!string.Equals(order.Status, OrderStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Chỉ có thể thêm món khi đơn hàng đã xác nhận. Trạng thái hiện tại: {order.Status}"
        );
    }
    
    // 3. Check Payment Status
    if (order.Payments != null && order.Payments.Any(p => p.PaymentDate.HasValue))
    {
        throw new InvalidOperationException(
            "Không thể thêm món sau khi đã bắt đầu thanh toán."
        );
    }
    
    // 4. Add New Items
    var newOrderDetails = new List<OrderDetail>();
    
    foreach (var item in request.Items)
    {
        decimal unitPrice = 0m;
        
        if (item.MenuItemId.HasValue)
        {
            var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(item.MenuItemId.Value);
            if (menuItem == null || !menuItem.IsAvailable)
            {
                throw new InvalidOperationException($"Món với ID {item.MenuItemId} không khả dụng.");
            }
            unitPrice = menuItem.Price;
        }
        else if (item.ComboId.HasValue)
        {
            var combo = await _unitOfWork.Combos.GetByIdAsync(item.ComboId.Value);
            if (combo == null || !combo.IsAvailable)
            {
                throw new InvalidOperationException($"Combo với ID {item.ComboId} không khả dụng.");
            }
            unitPrice = combo.Price;
        }
        else
        {
            throw new ArgumentException("Phải cung cấp MenuItemId hoặc ComboId.");
        }
        
        var newDetail = new OrderDetail
        {
            OrderId = order.OrderId,
            MenuItemId = item.MenuItemId,
            ComboId = item.ComboId,
            Quantity = item.Quantity,
            UnitPrice = unitPrice,
            Status = request.AutoConfirm ? "Confirmed" : "Pending",
            Notes = item.Notes,
            CreatedAt = DateTime.UtcNow
        };
        
        newOrderDetails.Add(newDetail);
        order.OrderDetails.Add(newDetail);
    }
    
    // 5. Recalculate TotalAmount
    var subtotal = order.OrderDetails
        .Where(od => od.Status != "Removed")
        .Sum(od => od.UnitPrice * od.Quantity);
    
    var vat = subtotal * 0.1m;
    var serviceFee = subtotal * 0.05m;
    order.TotalAmount = subtotal + vat + serviceFee;
    
    // 6. Update Order
    await _unitOfWork.Payments.UpdateAsync(order);
    
    // 7. Create Audit Log
    var staffId = await ResolveStaffIdAsync(request.StaffId, ct);
    
    var itemsDescription = string.Join(", ", newOrderDetails.Select(od => 
        $"{od.Quantity}x {(od.MenuItemId.HasValue ? "MenuItem" : "Combo")} ID:{od.MenuItemId ?? od.ComboId} ({od.UnitPrice:N0}đ)"
    ));
    
    var history = new OrderHistory
    {
        OrderId = order.OrderId,
        Action = "Add Items After Confirmation",
        Reason = $"{request.Reason}. Món thêm: {itemsDescription}",
        StaffId = staffId,
        CreatedAt = DateTime.UtcNow
    };
    
    await _unitOfWork.Payments.AddOrderHistoryAsync(history);
    
    // 8. Save Changes
    await _unitOfWork.SaveChangesAsync();
    
    // 9. Return Updated Order
    var orderDto = _mapper.Map<OrderDto>(order);
    CalculateOrderAmounts(order, orderDto);
    PopulateOrderMetadata(order, orderDto);
    
    return orderDto;
}
```

---

#### Step 3: Add Controller Endpoint

**File:** `Backend/SapaFoRestRMSAPI/Controllers/PaymentController.cs`

```csharp
/// <summary>
/// Thêm món vào đơn hàng đã xác nhận
/// POST /api/payment/orders/{orderId}/add-items
/// </summary>
[HttpPost("orders/{orderId}/add-items")]
public async Task<IActionResult> AddItemsToConfirmedOrder(
    int orderId, 
    [FromBody] AddItemsRequestDto request, 
    CancellationToken ct = default)
{
    try
    {
        if (request == null)
        {
            return BadRequest(new { message = "Dữ liệu không hợp lệ" });
        }
        
        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest(new { message = "Vui lòng chọn ít nhất 1 món để thêm" });
        }
        
        request.OrderId = orderId;
        var result = await _paymentService.AddItemsToConfirmedOrderAsync(request, ct);
        
        return Ok(new 
        { 
            success = true,
            message = "Thêm món thành công",
            order = result 
        });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Lỗi khi thêm món", error = ex.Message });
    }
}
```

---

### 4.2. Frontend Changes

#### Step 1: Add "Thêm Món" Button in ConfirmOrder.cshtml

**File:** `Frontend/WebSapaForestForStaff/Views/CashierFlow/ConfirmOrder.cshtml`

```razor
@* Sau bảng danh sách món *@
</table>

@* Nút Thêm Món - Chỉ hiển thị khi status = "confirmed" *@
@if (isConfirmed && !isPaid)
{
    <div class="mt-3">
        <button type="button" 
                class="btn btn-outline-primary" 
                data-bs-toggle="modal" 
                data-bs-target="#addItemsModal">
            <i class="bi bi-plus-circle me-2"></i>Gọi Thêm Món
        </button>
    </div>
}
```

---

#### Step 2: Add Modal for Adding Items

```razor
@* Modal Thêm Món *@
@if (isConfirmed && !isPaid)
{
    <div class="modal fade" id="addItemsModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Gọi Thêm Món</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <form id="addItemsForm">
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label">Chọn món/combo:</label>
                            <select class="form-select" id="itemSelect">
                                <option value="">-- Chọn món --</option>
                                @* Fetch từ API MenuItems *@
                            </select>
                        </div>
                        
                        <div class="mb-3">
                            <label class="form-label">Số lượng:</label>
                            <input type="number" class="form-control" id="itemQuantity" min="1" value="1" />
                        </div>
                        
                        <div class="mb-3">
                            <label class="form-label">Ghi chú:</label>
                            <textarea class="form-control" id="itemNotes" rows="2"></textarea>
                        </div>
                        
                        <button type="button" class="btn btn-sm btn-secondary" onclick="addItemToList()">
                            + Thêm vào danh sách
                        </button>
                        
                        <div class="mt-3">
                            <h6>Món đã chọn:</h6>
                            <ul id="selectedItemsList" class="list-group">
                                @* Dynamically populated *@
                            </ul>
                        </div>
                        
                        <div class="mb-3 mt-3">
                            <label class="form-label">Lý do gọi thêm:</label>
                            <input type="text" class="form-control" id="addReason" 
                                   placeholder="Ví dụ: Khách muốn gọi thêm" required />
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
                        <button type="submit" class="btn btn-primary">
                            <i class="bi bi-check2 me-1"></i> Xác Nhận Thêm Món
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
}
```

---

#### Step 3: Add JavaScript Handler

```razor
@if (isConfirmed && !isPaid)
{
    <script>
        (function() {
            const form = document.getElementById('addItemsForm');
            if (!form) return;
            
            let selectedItems = [];
            
            // Load menu items
            async function loadMenuItems() {
                try {
                    const response = await fetch('/api/menu/items/available');
                    const items = await response.json();
                    
                    const select = document.getElementById('itemSelect');
                    items.forEach(item => {
                        const option = document.createElement('option');
                        option.value = `menu-${item.menuItemId}`;
                        option.textContent = `${item.name} - ${item.price.toLocaleString()}đ`;
                        option.dataset.price = item.price;
                        select.appendChild(option);
                    });
                } catch (error) {
                    console.error('Error loading menu items:', error);
                }
            }
            
            // Add item to list
            window.addItemToList = function() {
                const select = document.getElementById('itemSelect');
                const quantity = document.getElementById('itemQuantity').value;
                const notes = document.getElementById('itemNotes').value;
                
                if (!select.value) {
                    showToast('Vui lòng chọn món', 'warning');
                    return;
                }
                
                const option = select.options[select.selectedIndex];
                const itemId = select.value.split('-')[1];
                const itemType = select.value.split('-')[0]; // 'menu' or 'combo'
                
                selectedItems.push({
                    id: parseInt(itemId),
                    type: itemType,
                    name: option.textContent,
                    quantity: parseInt(quantity),
                    price: parseFloat(option.dataset.price),
                    notes: notes
                });
                
                updateSelectedItemsList();
                
                // Reset form
                select.value = '';
                document.getElementById('itemQuantity').value = 1;
                document.getElementById('itemNotes').value = '';
            };
            
            // Update selected items list
            function updateSelectedItemsList() {
                const list = document.getElementById('selectedItemsList');
                list.innerHTML = '';
                
                selectedItems.forEach((item, index) => {
                    const li = document.createElement('li');
                    li.className = 'list-group-item d-flex justify-content-between align-items-center';
                    li.innerHTML = `
                        <span>${item.name} x${item.quantity}</span>
                        <button type="button" class="btn btn-sm btn-danger" onclick="removeItem(${index})">Xóa</button>
                    `;
                    list.appendChild(li);
                });
            }
            
            // Remove item from list
            window.removeItem = function(index) {
                selectedItems.splice(index, 1);
                updateSelectedItemsList();
            };
            
            // Submit form
            form.addEventListener('submit', async (ev) => {
                ev.preventDefault();
                
                if (selectedItems.length === 0) {
                    showToast('Vui lòng chọn ít nhất 1 món', 'warning');
                    return;
                }
                
                const reason = document.getElementById('addReason').value.trim();
                if (!reason) {
                    showToast('Vui lòng nhập lý do gọi thêm', 'warning');
                    return;
                }
                
                const endpoint = '@Url.Content($"~/api/payment/orders/{Model.OrderId}/add-items")';
                const staffId = '@userIdClaim';
                
                try {
                    const response = await fetch(endpoint, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            orderId: @Model.OrderId,
                            staffId: parseInt(staffId, 10),
                            reason: reason,
                            autoConfirm: true,
                            items: selectedItems.map(item => ({
                                menuItemId: item.type === 'menu' ? item.id : null,
                                comboId: item.type === 'combo' ? item.id : null,
                                quantity: item.quantity,
                                notes: item.notes
                            }))
                        })
                    });
                    
                    const data = await response.json();
                    
                    if (response.ok) {
                        const modal = bootstrap.Modal.getInstance(document.getElementById('addItemsModal'));
                        modal.hide();
                        
                        showToast('Thêm món thành công!', 'success');
                        setTimeout(() => window.location.reload(), 1000);
                    } else {
                        showToast(data.message || 'Không thể thêm món', 'error');
                    }
                } catch (error) {
                    showToast('Có lỗi xảy ra. Vui lòng thử lại.', 'error');
                    console.error('Error:', error);
                }
            });
            
            // Load menu items on modal open
            const modal = document.getElementById('addItemsModal');
            modal.addEventListener('shown.bs.modal', loadMenuItems);
        })();
    </script>
}
```

---

## 5. Workflow Chi Tiết

### 5.1. Happy Path

```
1. Khách xác nhận đơn ban đầu
   → Order.Status = "confirmed"
   → Hiển thị nút "Gọi Thêm Món"

2. Thu ngân click "Gọi Thêm Món"
   → Modal mở ra

3. Thu ngân chọn món/combo từ dropdown
   → Nhập số lượng
   → Click "Thêm vào danh sách"
   → Có thể thêm nhiều món

4. Thu ngân nhập lý do: "Khách muốn gọi thêm"
   → Click "Xác Nhận Thêm Món"

5. Frontend gọi API:
   POST /api/payment/orders/{orderId}/add-items
   
6. Backend xử lý:
   → Validate Order status = "confirmed"
   → Validate chưa thanh toán
   → Thêm món mới vào OrderDetails
   → Cập nhật TotalAmount
   → Tạo OrderHistory log
   → Save changes

7. Frontend nhận response thành công:
   → Toast: "Thêm món thành công!"
   → Page reload
   → Hiển thị món mới trong danh sách

8. Món mới tự động có Status = "Confirmed" (nếu AutoConfirm = true)
   → Sẵn sàng thanh toán luôn

9. Thu ngân tiếp tục: "Anh/chị thanh toán ạ?"
   → Click "Thanh toán"
   → Tổng tiền đã bao gồm món mới
```

---

### 5.2. Edge Cases

#### Case 1: Thêm món sau khi đã nhấn "Thanh toán"

```
1. Order.Status = "confirmed"
2. Thu ngân click "Thanh toán"
3. Tạo Payment transaction (Status = "Pending")
4. Khách: "Khoan, em gọi thêm 1 món!"

→ Backend check: order.Payments.Any(p => p.PaymentDate.HasValue)
→ Throw: "Không thể thêm món sau khi đã bắt đầu thanh toán"

Giải pháp:
- Thu ngân phải Cancel payment session
- Quay lại ConfirmOrder
- Thêm món
- Thanh toán lại
```

#### Case 2: Món đã vào bếp (Status = "Cooking")

```
1. Order.Status = "confirmed"
2. Một số món đã có Status = "Cooking" (bếp đang làm)
3. Khách muốn thêm món mới

→ KHÔNG CẦN kiểm tra món cũ
→ Món mới được thêm vào với Status = "Pending" hoặc "Confirmed"
→ Món cũ vẫn giữ Status = "Cooking"
→ OK ✅
```

#### Case 3: Thêm món trùng với món đã có

```
Option 1: Tạo OrderDetail mới
- OrderDetail 1: MenuItem = Steak, Quantity = 2, Status = "Confirmed"
- OrderDetail 2: MenuItem = Steak, Quantity = 1, Status = "Confirmed" (món mới)

Option 2: Cộng dồn vào OrderDetail cũ
- OrderDetail 1: MenuItem = Steak, Quantity = 3, Status = "Confirmed"

→ Recommend: Option 1 (dễ tracking, audit)
```

---

## 6. Testing Scenarios

### Test 1: Thêm 1 món menu item

```
GIVEN: Order #1234, Status = "confirmed"
       Items: 2x Steak, 1x Trà sen
       TotalAmount: 983,250đ

WHEN: Thu ngân thêm 1x Lẩu cá hồi (320,000đ)

THEN: 
  - API trả về success
  - Order có thêm OrderDetail mới (Lẩu)
  - OrderDetail.Status = "Confirmed"
  - TotalAmount = 983,250 + (320,000 * 1.15) = 1,351,250đ
  - OrderHistory có record "Add Items After Confirmation"
```

### Test 2: Thêm combo

```
GIVEN: Order #1234, Status = "confirmed"

WHEN: Thu ngân thêm 1x Combo Steak Dinner (290,000đ)

THEN:
  - OrderDetail mới với ComboId, không có MenuItemId
  - TotalAmount cập nhật đúng
```

### Test 3: Thêm nhiều món cùng lúc

```
WHEN: Thu ngân thêm:
      - 1x Trà sen (45,000đ)
      - 2x Lẩu cá hồi (320,000đ)

THEN:
  - Tạo 2 OrderDetail mới
  - TotalAmount = old + ((45,000 + 640,000) * 1.15)
```

### Test 4: Lỗi - Thêm món sau khi thanh toán

```
GIVEN: Order #1234, Status = "confirmed"
       Payment transaction đã có PaymentDate

WHEN: API POST /add-items

THEN:
  - HTTP 400 Bad Request
  - Message: "Không thể thêm món sau khi đã bắt đầu thanh toán"
```

### Test 5: Lỗi - Order không phải "confirmed"

```
GIVEN: Order #1234, Status = "waiting-confirmation"

WHEN: API POST /add-items

THEN:
  - HTTP 400 Bad Request
  - Message: "Chỉ có thể thêm món khi đơn hàng đã xác nhận"
```

---

## 7. Alternative: Simple Approach

Nếu chưa muốn implement full feature, có thể dùng **workaround đơn giản**:

### Approach 1: Undo → Add → Confirm lại

**Pros:**
- Không cần code mới
- Sử dụng chức năng hiện có

**Cons:**
- Mất thời gian
- Phức tạp cho user
- Không audit được việc "thêm món"

**Workflow:**
```
1. Click "Hoàn tác xác nhận"
2. Nhập lý do: "Khách muốn gọi thêm món"
3. Status → "waiting-confirmation"
4. Waiter/Manager thêm món vào Order (qua màn hình khác)
5. Quay lại ConfirmOrder
6. Xác nhận lại với tất cả các món (cũ + mới)
7. Status → "confirmed"
8. Thanh toán
```

### Approach 2: Tạo Order phụ, merge sau

**Pros:**
- Không ảnh hưởng đến Order chính
- Món mới có thể vào bếp ngay

**Cons:**
- Khó quản lý 2 orders
- Khách thấy 2 bill riêng → confused
- Phải merge manually

---

## 8. Recommendation

### 🎯 Đề xuất: Implement Full Feature

**Lý do:**
1. ✅ **UX tốt hơn:** Thu ngân không cần hoàn tác, thêm, xác nhận lại
2. ✅ **Audit trail:** Có log rõ ràng món nào được thêm, lúc nào, ai thêm
3. ✅ **Chính xác:** TotalAmount tự động cập nhật
4. ✅ **Scalable:** Dễ mở rộng thêm tính năng (ví dụ: xóa món, sửa món)

**Priority:** ⭐⭐⭐⭐ (4/5) - Should have

**Effort:** ~2-3 ngày (1 Backend + 1-2 Frontend)

---

## 9. Summary

### Hiện trạng
- ❌ Không có chức năng thêm món sau khi xác nhận
- ⚠️ Phải dùng workaround (undo → add → confirm lại)
- ⚠️ Gây bất tiện cho cashier và khách

### Giải pháp đề xuất
- ✅ API mới: `POST /api/payment/orders/{id}/add-items`
- ✅ UI: Nút "Gọi Thêm Món" + Modal chọn món
- ✅ Business rule: Chỉ cho phép khi chưa thanh toán
- ✅ Audit log: Track rõ ràng

### Lợi ích
- 🎯 Cải thiện UX cho cashier
- 🎯 Tăng tính linh hoạt cho khách hàng
- 🎯 Audit trail đầy đủ
- 🎯 Dễ maintain và mở rộng

---

**Document này đã phân tích chi tiết vấn đề và đề xuất giải pháp đầy đủ.**

**Next Steps:**
1. Review và phê duyệt thiết kế
2. Implement backend API
3. Implement frontend UI
4. Testing
5. Deploy

---

**Created:** 20/01/2025  
**Status:** 📝 Proposal - Chờ phê duyệt  
**Priority:** ⭐⭐⭐⭐ (Should have)

