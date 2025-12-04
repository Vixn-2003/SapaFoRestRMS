// Waiter Order Tracking JavaScript
const API_BASE = window.API_BASE_URL || 'https://localhost:7096/api';

// Request / Cancel Urgent (toggle)
function requestUrgent(orderDetailId, orderComboItemId, isUrgent) {
    if (isUrgent) {
        // Hủy làm gấp: mở popup xác nhận (Bootstrap modal), không dùng window.confirm
        document.getElementById('cancelUrgentOrderDetailId').value = orderDetailId;
        const comboInput = document.getElementById('cancelUrgentOrderComboItemId');
        if (comboInput) {
            comboInput.value = orderComboItemId && orderComboItemId > 0 ? orderComboItemId : '';
        }
        const modal = new bootstrap.Modal(document.getElementById('cancelUrgentModal'));
        modal.show();
        return;
    }

    // Làm gấp mới: mở modal nhập lý do
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
            showToast('Đã yêu cầu làm gấp thành công', 'success');
            bootstrap.Modal.getInstance(document.getElementById('urgentModal')).hide();
            location.reload();
        } else {
            showToast('Lỗi: ' + result.message, 'error');
        }
    } catch (error) {
        showToast('Lỗi kết nối: ' + error.message, 'error');
    }
}

// Gửi toggle urgent trực tiếp (dùng cho Hủy làm gấp)
async function submitUrgentToggle(orderDetailId, orderComboItemId, reason) {
    try {
        const response = await fetch(`${API_BASE}/WaiterOrderTracking/request-urgent`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: orderComboItemId && orderComboItemId > 0 ? orderComboItemId : null,
                waiterUserId: getCurrentUserId(),
                reason: reason || ''
            })
        });

        const result = await response.json();
        if (result.success) {
            showToast('Đã cập nhật trạng thái làm gấp', 'success');
            const currentFilter = document.querySelector('.filter-btn.active')?.getAttribute('data-filter') || 'all';
            sessionStorage.setItem('waiterOrderFilter', currentFilter);
            location.reload();
        } else {
            showToast('Lỗi: ' + result.message, 'error');
        }
    } catch (error) {
        showToast('Lỗi kết nối: ' + error.message, 'error');
    }
}

// Submit từ popup hủy làm gấp
async function submitCancelUrgent() {
    const orderDetailId = parseInt(document.getElementById('cancelUrgentOrderDetailId').value);
    const orderComboItemId = parseInt(document.getElementById('cancelUrgentOrderComboItemId').value || '0');

    await submitUrgentToggle(orderDetailId, orderComboItemId, '');
    const modalEl = document.getElementById('cancelUrgentModal');
    const modalInstance = bootstrap.Modal.getInstance(modalEl);
    if (modalInstance) {
        modalInstance.hide();
    }
}

// Toast helper (Bootstrap)
function showToast(message, type = 'success') {
    try {
        let container = document.getElementById('waiterToastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'waiterToastContainer';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = 9999;
            document.body.appendChild(container);
        }

        const toastEl = document.createElement('div');
        toastEl.className = `toast align-items-center text-white border-0`;
        toastEl.role = 'alert';
        toastEl.ariaLive = 'assertive';
        toastEl.ariaAtomic = 'true';

        const bgClass = type === 'error' ? 'bg-danger' : (type === 'warning' ? 'bg-warning text-dark' : 'bg-success');
        toastEl.classList.add(bgClass);

        toastEl.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

        container.appendChild(toastEl);
        const toast = new bootstrap.Toast(toastEl, { delay: 5000 });
        toast.show();

        toastEl.addEventListener('hidden.bs.toast', () => {
            if (toastEl.parentNode === container) {
                container.removeChild(toastEl);
            }
        });
    } catch (e) {
        // Fallback nếu Bootstrap Toast lỗi
        alert(message);
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
    // Find the exact item row to get its name
    let selector = `[data-item-id="${orderDetailId}"]`;
    if (orderComboItemId && orderComboItemId > 0) {
        selector += `[data-combo-item-id="${orderComboItemId}"]`;
    }
    const itemRow = document.querySelector(selector);
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

