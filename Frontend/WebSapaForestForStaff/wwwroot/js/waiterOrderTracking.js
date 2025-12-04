// Waiter Order Tracking JavaScript
const API_BASE = window.API_BASE_URL || 'https://localhost:7096/api';

// Request Urgent
function requestUrgent(orderDetailId, orderComboItemId) {
    document.getElementById('urgentOrderDetailId').value = orderDetailId;
    const comboInput = document.getElementById('urgentOrderComboItemId');
    if (comboInput) {
        comboInput.value = orderComboItemId && orderComboItemId > 0 ? orderComboItemId : '';
    }
    const modal = new bootstrap.Modal(document.getElementById('urgentModal'));
    modal.show();
}

async function submitUrgentRequest() {
    const orderDetailId = parseInt(document.getElementById('urgentOrderDetailId').value);
    const reason = document.getElementById('urgentReason').value;
    const reasonOther = document.getElementById('urgentReasonOtherText').value;
    const finalReason = reason === 'Khác' ? reasonOther : reason;

    if (!finalReason) {
        alert('Vui lòng chọn hoặc nhập lý do làm gấp');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/WaiterOrderTracking/request-urgent`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: (document.getElementById('urgentOrderComboItemId')?.value || '') || null,
                waiterUserId: getCurrentUserId(), // TODO: Get from session
                reason: finalReason
            })
        });

        const result = await response.json();
        if (result.success) {
            alert('Đã yêu cầu làm gấp thành công');
            bootstrap.Modal.getInstance(document.getElementById('urgentModal')).hide();
            location.reload();
        } else {
            alert('Lỗi: ' + result.message);
        }
    } catch (error) {
        alert('Lỗi kết nối: ' + error.message);
    }
}

// Cancel Item
function cancelItem(orderDetailId, orderComboItemId) {
    if (!confirm('Bạn có chắc chắn muốn hủy món này? Món chưa được nấu, sẽ không tính tiền.')) {
        return;
    }
    document.getElementById('cancelOrderDetailId').value = orderDetailId;
    const comboInput = document.getElementById('cancelOrderComboItemId');
    if (comboInput) {
        comboInput.value = orderComboItemId && orderComboItemId > 0 ? orderComboItemId : '';
    }
    const modal = new bootstrap.Modal(document.getElementById('cancelModal'));
    modal.show();
}

async function submitCancelRequest() {
    const orderDetailId = parseInt(document.getElementById('cancelOrderDetailId').value);
    const reason = document.getElementById('cancelReason').value;
    const reasonOther = document.getElementById('cancelReasonOtherText').value;
    const finalReason = reason === 'Khác' ? reasonOther : reason;

    if (!finalReason) {
        alert('Vui lòng chọn hoặc nhập lý do hủy');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/WaiterOrderTracking/cancel`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: (document.getElementById('cancelOrderComboItemId')?.value || '') || null,
                waiterUserId: getCurrentUserId(), // TODO: Get from session
                reason: finalReason
            })
        });

        const result = await response.json();
        if (result.success) {
            alert('Đã hủy món thành công');
            bootstrap.Modal.getInstance(document.getElementById('cancelModal')).hide();
            location.reload();
        } else {
            alert('Lỗi: ' + result.message);
        }
    } catch (error) {
        alert('Lỗi kết nối: ' + error.message);
    }
}


// Handle Served (Pick up item) - Show modal to select quantity (for non-split items)
function handleServed(orderDetailId, orderComboItemId, maxQuantity) {
    // Find the item to get its name
    const itemRow = document.querySelector(`[data-item-id="${orderDetailId}"]`);
    if (!itemRow) {
        alert('Không tìm thấy món ăn');
        return;
    }
    
    const itemName = itemRow.querySelector('.item-name span:not(.item-quantity):not(.badge-urgent)')?.textContent?.trim() || 'Món ăn';
    
    // Set modal values
    document.getElementById('pickupOrderDetailId').value = orderDetailId;
    const comboInput = document.getElementById('pickupOrderComboItemId');
    if (comboInput) {
        comboInput.value = orderComboItemId && orderComboItemId > 0 ? orderComboItemId : '';
    }
    document.getElementById('pickupMenuItemName').value = itemName;
    document.getElementById('pickupQuantity').value = 1;
    document.getElementById('pickupQuantity').max = maxQuantity;
    document.getElementById('pickupMaxQuantity').textContent = maxQuantity;
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('pickupQuantityModal'));
    modal.show();
}

// Handle Served Direct (for split items - no popup needed, take all)
async function handleServedDirect(orderDetailId, orderComboItemId, quantity) {
    if (!confirm(`Xác nhận đã lấy ${quantity} món và phục vụ khách?`)) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/WaiterOrderTracking/mark-as-served`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: orderComboItemId && orderComboItemId > 0 ? orderComboItemId : null,
                waiterUserId: getCurrentUserId(), // TODO: Get from session
                quantity: quantity // Lấy hết số lượng
            })
        });

        const result = await response.json();
        if (result.success) {
            // Reload trang để cập nhật UI
            const currentFilter = document.querySelector('.filter-btn.active')?.getAttribute('data-filter') || 'all';
            sessionStorage.setItem('waiterOrderFilter', currentFilter);
            location.reload();
        } else {
            alert('Lỗi: ' + result.message);
        }
    } catch (error) {
        alert('Lỗi kết nối: ' + error.message);
    }
}

// Submit pickup request
async function submitPickupRequest() {
    const orderDetailId = parseInt(document.getElementById('pickupOrderDetailId').value);
    const quantity = parseInt(document.getElementById('pickupQuantity').value);
    const maxQuantity = parseInt(document.getElementById('pickupQuantity').max);
    
    if (!quantity || quantity <= 0 || quantity > maxQuantity) {
        alert(`Số lượng phải từ 1 đến ${maxQuantity}`);
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/WaiterOrderTracking/mark-as-served`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: (document.getElementById('pickupOrderComboItemId')?.value || '') || null,
                waiterUserId: getCurrentUserId(), // TODO: Get from session
                quantity: quantity
            })
        });

        const result = await response.json();
        if (result.success) {
            // Close modal
            bootstrap.Modal.getInstance(document.getElementById('pickupQuantityModal')).hide();
            
            // Reload trang để cập nhật UI (ẩn nút LẤY MÓN, hiển thị trạng thái HOÀN THÀNH)
            // Lưu filter hiện tại vào sessionStorage để giữ nguyên sau khi reload
            const currentFilter = document.querySelector('.filter-btn.active')?.getAttribute('data-filter') || 'all';
            sessionStorage.setItem('waiterOrderFilter', currentFilter);
            location.reload();
        } else {
            alert('Lỗi: ' + result.message);
        }
    } catch (error) {
        alert('Lỗi kết nối: ' + error.message);
    }
}

// Helper function
function getCurrentUserId() {
    // TODO: Get from session/cookie
    return 1; // Temporary
}

