// ===== BIẾN TOÀN CỤC CHO BÁO CÁO =====
let customDateRange = { from: null, to: null };

// ===== XỬ LÝ CHỌN KHOẢNG THỜI GIAN =====
document.getElementById('reportPeriod')?.addEventListener('change', function() {
    if (this.value === 'custom') {
        openCustomDateModal();
    }
});

function openCustomDateModal() {
    document.getElementById('customDateModal').style.display = 'flex';
    
    // Set ngày mặc định (30 ngày qua đến hôm nay)
    const today = new Date();
    const past30Days = new Date();
    past30Days.setDate(today.getDate() - 30);
    
    document.getElementById('customDateFrom').value = past30Days.toISOString().split('T')[0];
    document.getElementById('customDateTo').value = today.toISOString().split('T')[0];
}

function closeCustomDateModal() {
    document.getElementById('customDateModal').style.display = 'none';
    document.getElementById('reportPeriod').value = 'today'; // Reset về mặc định
}

function applyCustomDate() {
    const dateFrom = document.getElementById('customDateFrom').value;
    const dateTo = document.getElementById('customDateTo').value;
    
    if (!dateFrom || !dateTo) {
        alert('Vui lòng chọn đầy đủ khoảng thời gian');
        return;
    }
    
    if (new Date(dateFrom) > new Date(dateTo)) {
        alert('Ngày bắt đầu phải nhỏ hơn ngày kết thúc');
        return;
    }
    
    customDateRange = { from: dateFrom, to: dateTo };
    closeCustomDateModal();
    
    // Hiển thị thông báo đã chọn
    showNotification(`Đã chọn: ${formatDate(dateFrom)} - ${formatDate(dateTo)}`);
}

// ===== HÀM LẤY KHOẢNG THỜI GIAN =====
function getDateRange(period) {
    const today = new Date();
    today.setHours(23, 59, 59, 999);
    
    let fromDate = new Date();
    let toDate = new Date(today);
    
    switch(period) {
        case 'today':
            fromDate.setHours(0, 0, 0, 0);
            break;
            
        case '7days':
            fromDate.setDate(today.getDate() - 7);
            fromDate.setHours(0, 0, 0, 0);
            break;
            
        case '30days':
            fromDate.setDate(today.getDate() - 30);
            fromDate.setHours(0, 0, 0, 0);
            break;
            
        case 'thisMonth':
            fromDate = new Date(today.getFullYear(), today.getMonth(), 1);
            fromDate.setHours(0, 0, 0, 0);
            break;
            
        case 'lastMonth':
            fromDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
            fromDate.setHours(0, 0, 0, 0);
            toDate = new Date(today.getFullYear(), today.getMonth(), 0);
            toDate.setHours(23, 59, 59, 999);
            break;
            
        case 'custom':
            if (customDateRange.from && customDateRange.to) {
                fromDate = new Date(customDateRange.from);
                toDate = new Date(customDateRange.to);
                toDate.setHours(23, 59, 59, 999);
            }
            break;
    }
    
    return { from: fromDate, to: toDate };
}

// ===== HÀM TẠO BÁO CÁO =====
async function generateReport() {
    const period = document.getElementById('reportPeriod').value;
    
    // Nếu chọn custom nhưng chưa áp dụng
    if (period === 'custom' && (!customDateRange.from || !customDateRange.to)) {
        openCustomDateModal();
        return;
    }
    
    const dateRange = getDateRange(period);
    
    // Lọc đơn hàng theo khoảng thời gian
    const filteredOrders = purchaseList.filter(order => {
        const orderDate = new Date(order.OrderDate || order.orderDate);
        return orderDate >= dateRange.from && orderDate <= dateRange.to;
    });
    
    if (filteredOrders.length === 0) {
        alert('Không có đơn hàng nào trong khoảng thời gian này');
        return;
    }
    
    // Hiển thị loading
    showLoading();
    
    try {
        // Gửi request đến API
        const response = await fetch('/MainImportInventory/GenerateReport', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                orders: filteredOrders,
                dateFrom: dateRange.from.toISOString(),
                dateTo: dateRange.to.toISOString(),
                periodText: getPeriodText(period)
            })
        });
        
        if (!response.ok) {
            throw new Error('Không thể tạo báo cáo');
        }
        
        // Tải file PDF
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `BaoCaoNhapHang_${formatDateForFilename(new Date())}.pdf`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        
        hideLoading();
        showNotification('Đã tải xuống báo cáo thành công!');
        
    } catch (error) {
        console.error('Error:', error);
        hideLoading();
        alert('Có lỗi xảy ra khi tạo báo cáo: ' + error.message);
    }
}

// ===== HELPER FUNCTIONS =====
function getPeriodText(period) {
    const texts = {
        'today': 'Hôm nay',
        '7days': '7 ngày qua',
        '30days': '30 ngày qua',
        'thisMonth': 'Tháng này',
        'lastMonth': 'Tháng trước',
        'custom': `${formatDate(customDateRange.from)} - ${formatDate(customDateRange.to)}`
    };
    return texts[period] || period;
}

function formatDateForFilename(date) {
    const d = new Date(date);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    const hour = String(d.getHours()).padStart(2, '0');
    const minute = String(d.getMinutes()).padStart(2, '0');
    return `${day}${month}${year}_${hour}${minute}`;
}

function showLoading() {
    const html = `
        <div class="loading-overlay active" id="loadingOverlay">
            <div class="loading-content">
                <div class="loading-spinner"></div>
                <div style="font-weight: 600; color: #1f2937; margin-bottom: 8px;">
                    Đang tạo báo cáo...
                </div>
                <div style="font-size: 14px; color: #6b7280;">
                    Vui lòng đợi trong giây lát
                </div>
            </div>
        </div>
    `;
    document.body.insertAdjacentHTML('beforeend', html);
}

function hideLoading() {
    const loading = document.getElementById('loadingOverlay');
    if (loading) loading.remove();
}

function showNotification(message) {
    const html = `
        <div style="position: fixed; top: 20px; right: 20px; background: #10b981; color: white;
                    padding: 16px 24px; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.15);
                    z-index: 10002; animation: slideIn 0.3s ease-out;" id="notification">
            <i class="fas fa-check-circle"></i> ${message}
        </div>
    `;
    document.body.insertAdjacentHTML('beforeend', html);
    
    setTimeout(() => {
        const notif = document.getElementById('notification');
        if (notif) notif.remove();
    }, 3000);
}