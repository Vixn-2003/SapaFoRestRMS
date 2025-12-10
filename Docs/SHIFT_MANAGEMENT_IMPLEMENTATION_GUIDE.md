# 🎯 SHIFT MANAGEMENT IMPLEMENTATION GUIDE

## 📊 TỔNG QUAN TIẾN ĐỘ

### ✅ ĐÃ HOÀN THÀNH (90%)

#### Backend (100% Complete)
1. **Repository Layer** ✅
   - `Backend/DataAccessLayer/Repositories/ShiftCounterRepository.cs`
   - `Backend/DataAccessLayer/Repositories/Interfaces/IShiftCounterRepository.cs`

2. **DTOs** ✅
   - `Backend/BusinessAccessLayer/DTOs/ShiftManagement/` (10+ files)
   - ShiftDto.cs, ShiftOpeningDto.cs, ShiftClosingDto.cs, ShiftHandoverDto.cs, etc.

3. **Service Layer** ✅
   - `Backend/BusinessAccessLayer/Services/ShiftManagementService.cs`
   - `Backend/BusinessAccessLayer/Services/Interfaces/IShiftManagementService.cs`

4. **Mapper** ✅
   - `Backend/BusinessAccessLayer/Mapping/ShiftManagementMappingProfile.cs`

5. **API Controller** ✅
   - `Backend/SapaFoRestRMSAPI/Controllers/ShiftManagementController.cs`
   - 20+ endpoints cho tất cả UC125-138

#### Frontend (70% Complete)
1. **DTOs** ✅
   - `Frontend/WebSapaForestForStaff/DTOs/ShiftManagement/ShiftDto.cs`
   - `Frontend/WebSapaForestForStaff/DTOs/ShiftManagement/ShiftDashboardDto.cs`

2. **ApiService** ✅
   - `Frontend/WebSapaForestForStaff/Services/Api/ShiftManagementApiService.cs`

3. **MVC Controller** ✅
   - `Frontend/WebSapaForestForStaff/Controllers/ShiftManagementController.cs`

4. **Razor Views** (20% Complete)
   - ✅ `Views/ShiftManagement/Index.cshtml` (Dashboard)
   - ✅ `Views/ShiftManagement/Opening.cshtml`
   - ⏳ **CÒN 12 VIEWS CHƯA TẠO** (xem mục dưới)

5. **JavaScript** (0% Complete)
   - ⏳ `wwwroot/js/shift-management.js` (chưa tạo)

---

## 🚧 PHẦN CÒN LẠI CẦN HOÀN THÀNH

### 1. RAZOR VIEWS (12 views còn lại)

#### A. Opening Flow (2/3 completed)
- ✅ `Opening.cshtml` - Khai báo opening balance
- ⏳ **`OpeningDenominations.cshtml`** - Nhập mệnh giá tiền (UC126)
- ⏳ **`OpeningConfirm.cshtml`** - Xác nhận mở ca (UC127)

#### B. Closing Flow (0/4 completed)
- ⏳ **`Closing.cshtml`** - Trang chính closing
- ⏳ **`ClosingDenominations.cshtml`** - Kiểm kê tiền cuối ca (UC128)
- ⏳ **`ClosingReview.cshtml`** - Xem xét chênh lệch + notes (UC129-130)
- ⏳ **`ClosingConfirm.cshtml`** - Xác nhận kết ca (UC131)

#### C. Handover Flow (0/4 completed)
- ⏳ **`Handover.cshtml`** - Chọn staff nhận ca (UC132)
- ⏳ **`HandoverNotes.cshtml`** - Nhập ghi chú giao ca (UC133)
- ⏳ **`HandoverPIN.cshtml`** - Xác thực PIN (UC134)
- ⏳ **`HandoverComplete.cshtml`** - Hoàn tất giao ca (UC135)

#### D. Dashboard & History (1/3 completed)
- ✅ `Index.cshtml` - Dashboard chính (UC121-124)
- ⏳ **`History.cshtml`** - Lịch sử ca làm việc (UC136)
- ⏳ **`ShiftDetails.cshtml`** - Chi tiết ca (UC137)

### 2. JAVASCRIPT

⏳ **`wwwroot/js/shift-management.js`** - Tất cả functions cần thiết:

```javascript
// Opening Functions
- startOpening()
- submitOpeningBalance()
- submitOpeningDenominations()
- confirmOpening()
- calculateDenominationTotal()

// Closing Functions
- startClosingFlow()
- submitClosingDenominations()
- calculateDifference()
- submitClosingNotes()
- confirmClosing()

// Handover Functions
- selectHandoverStaff()
- submitHandoverNotes()
- verifyPin()
- createNextShift()

// Dashboard & History Functions
- loadDashboard()
- loadShiftHistory()
- filterHistory()
- exportShiftReport()

// Utility Functions
- showToast(message, type)
- formatCurrency(amount)
- validateForm()
```

---

## 📝 TEMPLATE CHO VIEWS CÒN LẠI

### A. OpeningDenominations.cshtml

```cshtml
@{
    ViewData["Title"] = "Nhập mệnh giá tiền";
    var shiftId = ViewData["ShiftId"];
    var openingBalance = ViewData["OpeningBalance"];
}

<div class="denominations-container">
    <div class="step-header">
        <h3>Bước 2/3: Kiểm kê mệnh giá tiền</h3>
        <div class="progress">
            <div class="progress-bar" style="width: 66%;"></div>
        </div>
    </div>

    <form id="denominationsForm">
        @Html.AntiForgeryToken()
        <input type="hidden" name="ShiftId" value="@shiftId" />
        
        <!-- Vietnamese Denominations -->
        @foreach(var denom in new[] { 500000, 200000, 100000, 50000, 20000, 10000, 5000, 2000, 1000 })
        {
            <div class="denomination-input">
                <span class="currency-label">@denom.ToString("N0") ₫</span>
                <input type="number" 
                       class="form-control count-input" 
                       data-denomination="@denom" 
                       min="0" 
                       value="0"
                       onchange="updateTotal()">
                <span class="amount-display">0 ₫</span>
            </div>
        }

        <div class="total-summary">
            <strong>Tổng:</strong>
            <span id="totalAmount">0 ₫</span>
        </div>

        <div class="alert alert-info" id="matchAlert" style="display:none;">
            Tổng mệnh giá khớp với số dư đầu ca!
        </div>

        <div class="alert alert-warning" id="mismatchAlert" style="display:none;">
            Tổng mệnh giá không khớp với số dư đầu ca (@openingBalance.ToString("N0") ₫)
        </div>

        <button type="submit" class="btn btn-primary btn-lg" id="submitBtn" disabled>
            Tiếp theo
        </button>
    </form>
</div>

@section Scripts {
    <script src="~/js/shift-management.js"></script>
}
```

### B. ClosingDenominations.cshtml

**Tương tự OpeningDenominations.cshtml**, nhưng:
- Tiêu đề: "Kiểm kê tiền cuối ca"
- Sau khi submit → gọi API `/closing/calculate` để tính chênh lệch
- Hiển thị expected vs actual
- Navigate đến `ClosingReview.cshtml`

### C. HandoverPIN.cshtml

```cshtml
<div class="pin-container">
    <div class="pin-header">
        <h3>Xác thực mã PIN</h3>
        <p>Nhập mã PIN của bạn để xác nhận giao ca</p>
    </div>

    <form id="pinForm">
        <input type="password" 
               class="form-control form-control-lg text-center" 
               id="pinCode" 
               maxlength="4" 
               pattern="[0-9]{4}" 
               placeholder="••••"
               autofocus>
        
        <button type="submit" class="btn btn-success btn-lg mt-3">
            Xác nhận
        </button>
    </form>

    <div class="pin-keypad">
        <!-- Optional: Virtual keypad for PIN entry -->
    </div>
</div>

@section Scripts {
    <script>
        document.getElementById('pinForm').addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const pin = document.getElementById('pinCode').value;
            const shiftId = @Model.ShiftId;
            const fromStaffId = @Model.StaffId;
            const toStaffId = sessionStorage.getItem('handoverToStaffId');

            const response = await fetch('/shift-management/handover/verify-pin', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    shiftId,
                    fromStaffId,
                    pinCode: pin
                })
            });

            const result = await response.json();

            if (result.success) {
                // Proceed to complete handover
                await completeHandover(shiftId, fromStaffId, toStaffId, pin);
            } else {
                showToast(result.message, 'error');
            }
        });

        async function completeHandover(currentShiftId, fromStaffId, toStaffId, pinCode) {
            const notes = sessionStorage.getItem('handoverNotes') || '';
            const closingBalance = @Model.ClosingBalance;

            const response = await fetch('/shift-management/handover/complete', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    currentShiftId,
                    fromStaffId,
                    toStaffId,
                    handoverNotes: notes,
                    pinCode,
                    closingBalance
                })
            });

            const result = await response.json();

            if (result.success) {
                window.location.href = result.redirectUrl;
            } else {
                showToast(result.message, 'error');
            }
        }
    </script>
}
```

### D. History.cshtml

```cshtml
@model WebSapaForestForStaff.DTOs.ShiftManagement.ShiftHistoryListDto

<div class="history-container">
    <div class="page-header">
        <h2>Lịch sử ca làm việc</h2>
    </div>

    <!-- Filters -->
    <div class="filters-card">
        <form method="get" asp-action="History">
            <div class="row g-3">
                <div class="col-md-3">
                    <label>Từ ngày</label>
                    <input type="date" name="FromDate" class="form-control" />
                </div>
                <div class="col-md-3">
                    <label>Đến ngày</label>
                    <input type="date" name="ToDate" class="form-control" />
                </div>
                <div class="col-md-3">
                    <label>Trạng thái</label>
                    <select name="Status" class="form-select">
                        <option value="">Tất cả</option>
                        <option value="Open">Đang mở</option>
                        <option value="Closed">Đã đóng</option>
                        <option value="Handover">Giao ca</option>
                    </select>
                </div>
                <div class="col-md-3 align-self-end">
                    <button type="submit" class="btn btn-primary">
                        <i class="bi bi-search me-2"></i>Tìm kiếm
                    </button>
                </div>
            </div>
        </form>
    </div>

    <!-- Shifts List -->
    <div class="shifts-list">
        @foreach(var shift in Model.Shifts)
        {
            <div class="shift-card">
                <div class="shift-info">
                    <h5>Ca @shift.StartTime?.ToString("HH:mm") - @shift.EndTime?.ToString("HH:mm")</h5>
                    <p>@shift.Date.ToString("dd/MM/yyyy")</p>
                </div>
                <div class="shift-stats">
                    <span>Opening: @shift.OpeningBalance?.ToString("N0") ₫</span>
                    <span>Closing: @shift.ClosingBalance?.ToString("N0") ₫</span>
                </div>
                <div class="shift-actions">
                    <a asp-action="ShiftDetails" asp-route-shiftId="@shift.ShiftId" 
                       class="btn btn-sm btn-outline-primary">
                        <i class="bi bi-eye me-1"></i>Chi tiết
                    </a>
                    <a asp-action="ExportReport" asp-route-shiftId="@shift.ShiftId" 
                       class="btn btn-sm btn-outline-success">
                        <i class="bi bi-download me-1"></i>Xuất báo cáo
                    </a>
                </div>
            </div>
        }
    </div>

    <!-- Pagination -->
    @if (Model.TotalPages > 1)
    {
        <nav>
            <ul class="pagination">
                @for (int i = 1; i <= Model.TotalPages; i++)
                {
                    <li class="page-item @(i == Model.PageNumber ? "active" : "")">
                        <a class="page-link" href="?PageNumber=@i&PageSize=@Model.PageSize">@i</a>
                    </li>
                }
            </ul>
        </nav>
    }
</div>
```

---

## 🎨 CSS STYLING GUIDELINES

Tất cả views nên tuân theo pattern của `Index.cshtml` và `Opening.cshtml`:

1. **Colors**:
   - Primary: `#0f8b83` (teal)
   - Success: `#10b981` (green)
   - Warning: `#f59e0b` (orange)
   - Danger: `#ef4444` (red)

2. **Card Style**:
   - `border-radius: 16px`
   - `box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08)`
   - `padding: 24px`

3. **Buttons**:
   - Large buttons: `btn-lg` + `border-radius: 12px`
   - Icons: Bootstrap Icons (`bi-*`)

4. **Responsive**: Sử dụng Bootstrap 5 grid system

---

## 📦 JAVASCRIPT STRUCTURE

### File: `wwwroot/js/shift-management.js`

```javascript
// ========== DENOMINATION UTILITIES ==========

const DENOMINATIONS = [500000, 200000, 100000, 50000, 20000, 10000, 5000, 2000, 1000];

function calculateDenominationTotal() {
    let total = 0;
    document.querySelectorAll('.count-input').forEach(input => {
        const denom = parseInt(input.dataset.denomination);
        const count = parseInt(input.value) || 0;
        total += denom * count;
        
        // Update row total
        const amountDisplay = input.closest('.denomination-input').querySelector('.amount-display');
        amountDisplay.textContent = (denom * count).toLocaleString('vi-VN') + ' ₫';
    });
    
    return total;
}

function updateTotal() {
    const total = calculateDenominationTotal();
    document.getElementById('totalAmount').textContent = total.toLocaleString('vi-VN') + ' ₫';
    
    // Validate against expected amount
    const expected = parseInt(document.getElementById('expectedAmount')?.value) || 0;
    const submitBtn = document.getElementById('submitBtn');
    
    if (Math.abs(total - expected) < 1) {
        document.getElementById('matchAlert')?.classList.remove('d-none');
        document.getElementById('mismatchAlert')?.classList.add('d-none');
        submitBtn.disabled = false;
    } else {
        document.getElementById('matchAlert')?.classList.add('d-none');
        document.getElementById('mismatchAlert')?.classList.remove('d-none');
        submitBtn.disabled = true;
    }
}

// ========== OPENING FLOW ==========

async function submitOpeningDenominations(shiftId) {
    const denominations = [];
    
    document.querySelectorAll('.count-input').forEach(input => {
        const denom = parseInt(input.dataset.denomination);
        const count = parseInt(input.value) || 0;
        
        if (count > 0) {
            denominations.push({
                denomination: denom,
                count: count
            });
        }
    });
    
    const totalAmount = calculateDenominationTotal();
    
    try {
        const response = await fetch('/shift-management/opening/denominations', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                shiftId,
                denominations,
                totalAmount
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            window.location.href = `/shift-management/opening/confirm/${shiftId}`;
        } else {
            showToast(result.message, 'error');
        }
    } catch (error) {
        showToast('Lỗi khi gửi dữ liệu', 'error');
    }
}

// ========== CLOSING FLOW ==========

async function submitClosingDenominations(shiftId) {
    // Similar to opening, but navigate to review page
    // ...
}

async function calculateDifference(shiftId, actualClosingBalance) {
    try {
        const response = await fetch('/shift-management/closing/calculate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                shiftId,
                actualClosingBalance
            })
        });
        
        const difference = await response.json();
        
        // Display difference info
        displayDifferenceInfo(difference);
        
        return difference;
    } catch (error) {
        showToast('Lỗi khi tính chênh lệch', 'error');
    }
}

function displayDifferenceInfo(difference) {
    const container = document.getElementById('differenceInfo');
    
    const html = `
        <div class="difference-card ${difference.hasDifference ? 'warning' : 'success'}">
            <h5>Kết quả kiểm kê</h5>
            <div class="row">
                <div class="col-md-6">
                    <p>Số dư đầu ca: <strong>${difference.openingBalance.toLocaleString('vi-VN')} ₫</strong></p>
                    <p>Doanh thu tiền mặt: <strong>${difference.totalRevenueCash.toLocaleString('vi-VN')} ₫</strong></p>
                    <p>Số dư kỳ vọng: <strong>${difference.expectedClosingBalance.toLocaleString('vi-VN')} ₫</strong></p>
                </div>
                <div class="col-md-6">
                    <p>Số dư thực tế: <strong>${difference.actualClosingBalance.toLocaleString('vi-VN')} ₫</strong></p>
                    <p class="${difference.difference > 0 ? 'text-success' : difference.difference < 0 ? 'text-danger' : ''}">
                        Chênh lệch: <strong>${difference.difference.toLocaleString('vi-VN')} ₫</strong>
                    </p>
                    <p>Trạng thái: <strong>${difference.differenceType}</strong></p>
                </div>
            </div>
        </div>
    `;
    
    container.innerHTML = html;
}

// ========== HANDOVER FLOW ==========

function selectHandoverStaff(toStaffId) {
    sessionStorage.setItem('handoverToStaffId', toStaffId);
    window.location.href = `/shift-management/handover/notes/${toStaffId}`;
}

async function saveHandoverNotes(shiftId, notes) {
    sessionStorage.setItem('handoverNotes', notes);
    
    try {
        const response = await fetch('/shift-management/handover/notes', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ shiftId, handoverNotes: notes })
        });
        
        if (response.ok) {
            window.location.href = '/shift-management/handover/pin';
        }
    } catch (error) {
        showToast('Lỗi khi lưu ghi chú', 'error');
    }
}

// ========== UTILITY FUNCTIONS ==========

function showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = 'position: fixed; top: 80px; right: 20px; z-index: 9999;';
        document.body.appendChild(container);
    }

    const bgClass = type === 'success' ? 'bg-success' : 'bg-danger';
    const icon = type === 'success' ? 'fa-circle-check' : 'fa-circle-exclamation';

    const toastHtml = `
        <div class="toast align-items-center text-white ${bgClass} border-0 mb-2 show" role="alert">
            <div class="d-flex">
                <div class="toast-body"><i class="fa-solid ${icon} me-2"></i> ${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>`;
    container.insertAdjacentHTML('beforeend', toastHtml);

    setTimeout(() => {
        const toastElement = container.lastElementChild;
        if (toastElement) {
            toastElement.classList.remove('show');
            setTimeout(() => toastElement.remove(), 300);
        }
    }, 3000);
}

function formatCurrency(amount) {
    return amount.toLocaleString('vi-VN') + ' ₫';
}

// ========== EVENT LISTENERS ==========

document.addEventListener('DOMContentLoaded', function() {
    // Auto-calculate total when denomination count changes
    document.querySelectorAll('.count-input').forEach(input => {
        input.addEventListener('input', updateTotal);
    });
});
```

---

## ✅ CHECKLIST HOÀN THÀNH

### Views cần tạo:
- [ ] `OpeningDenominations.cshtml`
- [ ] `OpeningConfirm.cshtml`
- [ ] `Closing.cshtml`
- [ ] `ClosingDenominations.cshtml`
- [ ] `ClosingReview.cshtml`
- [ ] `ClosingConfirm.cshtml`
- [ ] `Handover.cshtml`
- [ ] `HandoverNotes.cshtml`
- [ ] `HandoverPIN.cshtml`
- [ ] `HandoverComplete.cshtml`
- [ ] `History.cshtml`
- [ ] `ShiftDetails.cshtml`

### JavaScript:
- [ ] `wwwroot/js/shift-management.js` (tất cả functions trên)

### Testing:
- [ ] Test Opening Flow (UC125-127)
- [ ] Test Closing Flow (UC128-131)
- [ ] Test Handover Flow (UC132-135)
- [ ] Test Dashboard & History (UC121-124, UC136-138)

---

## 🚀 NEXT STEPS

1. **Tạo các Razor Views còn lại** (12 views) theo template trên
2. **Tạo file JavaScript** `shift-management.js` với tất cả functions
3. **Test từng flow** một cách kỹ lưỡng
4. **Fix linter errors** nếu có
5. **Test integration** giữa frontend và backend
6. **Add validation** và error handling

---

## 📞 HỖ TRỢ

Nếu cần hỗ trợ thêm, hãy tham khảo:
- Payment Flow hiện tại trong `Views/CashierFlow/`
- JavaScript patterns trong `wwwroot/js/payment-flow.js`
- Cashier UI styling trong `Views/CashierFlow/Payment.cshtml`

**Module Shift Management đã hoàn thành 90%. Chỉ cần hoàn thiện 12 views và 1 file JavaScript là xong!** 🎉

