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
        showToast('Vui lòng chọn hoặc nhập lý do làm gấp', 'warning');
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
        // Fallback nếu Bootstrap Toast lỗi - sử dụng console thay vì alert
        console.error('Toast error:', e, 'Message:', message);
    }
}

// ===== POPUP HELPERS (thay cho alert/confirm) =====
function ensureWaiterPopupStyles() {
    if (document.getElementById('waiter-popup-styles')) {
        return;
    }
    const style = document.createElement('style');
    style.id = 'waiter-popup-styles';
    style.textContent = `
        .waiter-popup-overlay {
            position: fixed;
            inset: 0;
            background: rgba(0,0,0,0.4);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 10550;
            padding: 16px;
        }
        .waiter-popup {
            background: #fff;
            border-radius: 10px;
            box-shadow: 0 12px 30px rgba(0,0,0,0.18);
            max-width: 420px;
            width: 100%;
            overflow: hidden;
            animation: waiterPopupIn 0.2s ease;
        }
        .waiter-popup-header {
            display: flex;
            align-items: center;
            gap: 10px;
            padding: 14px 16px 10px;
            border-bottom: 1px solid #f1f1f1;
        }
        .waiter-popup-title {
            margin: 0;
            font-size: 18px;
            font-weight: 700;
            color: #333;
        }
        .waiter-popup-body {
            padding: 12px 16px 4px;
            color: #444;
            line-height: 1.5;
        }
        .waiter-popup-footer {
            padding: 14px 16px 16px;
            display: flex;
            justify-content: flex-end;
            gap: 10px;
        }
        .waiter-popup-btn {
            border: none;
            border-radius: 6px;
            padding: 8px 14px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.15s ease;
        }
        .waiter-popup-btn-cancel { background: #f1f1f1; color: #333; }
        .waiter-popup-btn-cancel:hover { background: #e6e6e6; }
        .waiter-popup-btn-confirm { background: #0d6efd; color: #fff; }
        .waiter-popup-btn-confirm.warning { background: #f59e0b; color: #fff; }
        .waiter-popup-btn-confirm.error { background: #dc3545; color: #fff; }
        .waiter-popup-btn-confirm:hover { opacity: 0.92; }
        .waiter-popup-badge {
            width: 36px;
            height: 36px;
            border-radius: 50%;
            display: grid;
            place-items: center;
            font-size: 18px;
            color: #fff;
        }
        .waiter-popup-badge.info { background: #0d6efd; }
        .waiter-popup-badge.success { background: #22c55e; }
        .waiter-popup-badge.warning { background: #f59e0b; }
        .waiter-popup-badge.error { background: #dc3545; }
        @keyframes waiterPopupIn {
            from { transform: translateY(8px); opacity: 0; }
            to { transform: translateY(0); opacity: 1; }
        }
    `;
    document.head.appendChild(style);
}

function showWaiterMessagePopup(message, { title = 'Thông báo', type = 'info', confirmText = 'Đóng' } = {}) {
    ensureWaiterPopupStyles();
    return new Promise(resolve => {
        const overlay = document.createElement('div');
        overlay.className = 'waiter-popup-overlay';
        overlay.innerHTML = `
            <div class="waiter-popup">
                <div class="waiter-popup-header">
                    <div class="waiter-popup-badge ${type}">${type === 'error' ? '!' : (type === 'warning' ? '!' : 'i')}</div>
                    <h5 class="waiter-popup-title">${title}</h5>
                </div>
                <div class="waiter-popup-body">${message}</div>
                <div class="waiter-popup-footer">
                    <button class="waiter-popup-btn waiter-popup-btn-confirm ${type}">${confirmText}</button>
                </div>
            </div>
        `;

        const close = () => {
            overlay.style.opacity = '0';
            setTimeout(() => {
                overlay.remove();
                resolve(true);
            }, 120);
        };

        overlay.querySelector('.waiter-popup-btn-confirm').addEventListener('click', close);
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                close();
            }
        });

        document.body.appendChild(overlay);
    });
}

function showWaiterConfirmPopup(message, { title = 'Xác nhận', confirmText = 'Đồng ý', cancelText = 'Hủy', type = 'warning' } = {}) {
    ensureWaiterPopupStyles();
    return new Promise(resolve => {
        const overlay = document.createElement('div');
        overlay.className = 'waiter-popup-overlay';
        overlay.innerHTML = `
            <div class="waiter-popup">
                <div class="waiter-popup-header">
                    <div class="waiter-popup-badge ${type}">${type === 'warning' ? '!' : '?'}</div>
                    <h5 class="waiter-popup-title">${title}</h5>
                </div>
                <div class="waiter-popup-body">${message}</div>
                <div class="waiter-popup-footer">
                    <button class="waiter-popup-btn waiter-popup-btn-cancel">${cancelText}</button>
                    <button class="waiter-popup-btn waiter-popup-btn-confirm ${type}">${confirmText}</button>
                </div>
            </div>
        `;

        const close = (result) => {
            overlay.style.opacity = '0';
            setTimeout(() => {
                overlay.remove();
                resolve(result);
            }, 120);
        };

        overlay.querySelector('.waiter-popup-btn-confirm').addEventListener('click', () => close(true));
        overlay.querySelector('.waiter-popup-btn-cancel').addEventListener('click', () => close(false));
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                close(false);
            }
        });

        document.body.appendChild(overlay);
    });
}

// Cancel Item
async function cancelItem(orderDetailId, orderComboItemId) {
    const confirmed = await showWaiterConfirmPopup(
        'Bạn có chắc chắn muốn hủy món này? Món đang chờ hoặc đang nấu sẽ không tính tiền sau khi hủy.',
        { title: 'Xác nhận hủy món' }
    );
    if (!confirmed) {
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
        await showWaiterMessagePopup('Vui lòng chọn hoặc nhập lý do hủy', { title: 'Thiếu lý do', type: 'warning' });
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
            await showWaiterMessagePopup('Đã hủy món thành công', { title: 'Thành công', type: 'success' });
            bootstrap.Modal.getInstance(document.getElementById('cancelModal')).hide();
            location.reload();
        } else {
            await showWaiterMessagePopup('Lỗi: ' + result.message, { title: 'Không thể hủy món', type: 'error' });
        }
    } catch (error) {
        await showWaiterMessagePopup('Lỗi kết nối: ' + error.message, { title: 'Không thể hủy món', type: 'error' });
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
        showToast('Không tìm thấy món ăn', 'error');
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
    const confirmed = await showWaiterConfirmPopup(
        `Xác nhận đã lấy ${quantity} món và phục vụ khách?`,
        { title: 'Xác nhận lấy món', confirmText: 'Xác nhận', cancelText: 'Hủy' }
    );
    if (!confirmed) {
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
            showToast('Lỗi: ' + result.message, 'error');
        }
    } catch (error) {
        showToast('Lỗi kết nối: ' + error.message, 'error');
    }
}

// Submit pickup request
async function submitPickupRequest() {
    const orderDetailId = parseInt(document.getElementById('pickupOrderDetailId').value);
    const quantity = parseInt(document.getElementById('pickupQuantity').value);
    const maxQuantity = parseInt(document.getElementById('pickupQuantity').max);
    
    if (!quantity || quantity <= 0 || quantity > maxQuantity) {
        showToast(`Số lượng phải từ 1 đến ${maxQuantity}`, 'warning');
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
            showToast('Lỗi: ' + result.message, 'error');
        }
    } catch (error) {
        showToast('Lỗi kết nối: ' + error.message, 'error');
    }
}

// Update consumption quantity (inline edit)
async function updateConsumptionQuantity(orderDetailId, orderComboItemId, quantity) {
    if (!quantity || quantity <= 0) {
        showToast('Số lượng phải lớn hơn 0', 'warning');
        return;
    }
    
    // ✅ Kiểm tra xem có phải món trong combo không
    const comboItemId = orderComboItemId || 0;
    if (comboItemId > 0) {
        // Món trong combo: KHÔNG cho phép cập nhật số lượng
        showToast('Món tiêu hao trong combo không được phép thay đổi số lượng. Chỉ được phép xác nhận với số lượng hiện tại.', 'warning');
        // Reset về giá trị cũ
        const quantityInput = document.getElementById(`quantity_${orderDetailId}_${comboItemId}`);
        if (quantityInput) {
            let selector = `[data-item-id="${orderDetailId}"]`;
            selector += `[data-combo-item-id="${comboItemId}"]`;
            const itemRow = document.querySelector(selector);
            if (itemRow) {
                const itemData = itemRow.getAttribute('data-quantity-used') || itemRow.getAttribute('data-quantity') || '1';
                quantityInput.value = itemData;
            }
        }
        return;
    }
    
    // ✅ Kiểm tra BillingType trước khi gọi API (chỉ cho món lẻ)
    // Tìm item row để lấy BillingType
    let selector = `[data-item-id="${orderDetailId}"]`;
    const itemRow = document.querySelector(selector);
    if (!itemRow) {
        // Nếu không tìm thấy item row, thử tìm bằng cách khác
        // Vẫn cho phép gọi API, backend sẽ kiểm tra lại
    } else {
        const billingType = itemRow.getAttribute('data-billing-type');
        
        // Chỉ cho phép cập nhật nếu BillingType = 1 (ConsumptionBased)
        // billingType có thể là '1', '', null, hoặc undefined
        if (billingType !== '1') {
            showToast('Chỉ có thể cập nhật số lượng cho món tiêu hao (nước, khăn ướt, v.v.)', 'warning');
            // Reset về giá trị cũ
            const quantityInput = document.getElementById(`quantity_${orderDetailId}_${comboItemId}`);
            if (quantityInput) {
                const itemData = itemRow.getAttribute('data-quantity-used') || itemRow.getAttribute('data-quantity') || '1';
                quantityInput.value = itemData;
            }
            return;
        }
    }
    
    try {
        const response = await fetch(`${API_BASE}/WaiterOrderTracking/update-quantity`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: orderComboItemId && orderComboItemId > 0 ? orderComboItemId : null,
                quantity: parseInt(quantity)
            })
        });

        const result = await response.json();
        if (result.success) {
            // Toast notification (if showToast exists, otherwise use alert)
            showToast('Đã cập nhật số lượng thành công', 'success');
        } else {
            showToast('Lỗi: ' + result.message, 'error');
            // Reset về giá trị cũ nếu có lỗi
            const quantityInput = document.getElementById(`quantity_${orderDetailId}_${orderComboItemId || 0}`);
            if (quantityInput && itemRow) {
                const itemData = itemRow.getAttribute('data-quantity-used') || itemRow.getAttribute('data-quantity') || '1';
                quantityInput.value = itemData;
            }
        }
    } catch (error) {
        console.error('Error updating quantity:', error);
        showToast('Lỗi kết nối: ' + error.message, 'error');
        // Reset về giá trị cũ nếu có lỗi
        const quantityInput = document.getElementById(`quantity_${orderDetailId}_${orderComboItemId || 0}`);
        if (quantityInput && itemRow) {
            const itemData = itemRow.getAttribute('data-quantity-used') || itemRow.getAttribute('data-quantity') || '1';
            quantityInput.value = itemData;
        }
    }
}

// ✅ Mở modal xác nhận số lượng cho ConsumptionBased items
function openConfirmConsumptionQuantityModal(orderDetailId, orderComboItemId, menuItemName, currentQuantity, orderQuantity, quantityUsed) {
    // Set modal values
    document.getElementById('confirmConsumptionOrderDetailId').value = orderDetailId;
    const comboInput = document.getElementById('confirmConsumptionOrderComboItemId');
    if (comboInput) {
        comboInput.value = orderComboItemId && orderComboItemId > 0 ? orderComboItemId : '';
    }
    document.getElementById('confirmConsumptionMenuItemName').value = menuItemName || 'Món ăn';
    document.getElementById('confirmConsumptionQuantity').value = currentQuantity || orderQuantity || 1;
    document.getElementById('confirmConsumptionOrderQuantity').textContent = orderQuantity || 1;
    document.getElementById('confirmConsumptionQuantityUsed').textContent = quantityUsed || 0;
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('confirmConsumptionQuantityModal'));
    modal.show();
}

// Submit confirm consumption quantity
async function submitConfirmConsumptionQuantity() {
    const orderDetailId = parseInt(document.getElementById('confirmConsumptionOrderDetailId').value);
    const orderComboItemId = document.getElementById('confirmConsumptionOrderComboItemId').value;
    const quantity = parseInt(document.getElementById('confirmConsumptionQuantity').value);
    
    if (!quantity || quantity <= 0) {
        showToast('Số lượng phải lớn hơn 0', 'warning');
        return;
    }
    
    if (!orderDetailId || isNaN(orderDetailId)) {
        showToast('Lỗi: Không tìm thấy ID món ăn', 'error');
        return;
    }
    
    try {
        // ✅ QUAN TRỌNG: Cập nhật số lượng trước (nếu chưa được cập nhật)
        // Đảm bảo Quantity và QuantityUsed được cập nhật trước khi xác nhận
        const updateResponse = await fetch(`${API_BASE}/WaiterOrderTracking/update-quantity`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: orderComboItemId && orderComboItemId > 0 ? parseInt(orderComboItemId) : null,
                quantity: quantity
            })
        });

        const updateResult = await updateResponse.json();
        if (!updateResult.success) {
            // Vẫn tiếp tục xác nhận
        }

        // Sau đó xác nhận và chuyển sang Done
        const response = await fetch(`${API_BASE}/WaiterOrderTracking/confirm-consumption-quantity`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: orderComboItemId && orderComboItemId > 0 ? parseInt(orderComboItemId) : null,
                quantity: quantity
            })
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const result = await response.json();
        
        if (result.success) {
            // Close modal
            const modalElement = document.getElementById('confirmConsumptionQuantityModal');
            if (modalElement) {
                const modalInstance = bootstrap.Modal.getInstance(modalElement);
                if (modalInstance) {
                    modalInstance.hide();
                }
            }
            
            // Toast notification
            showToast('Đã xác nhận số lượng thành công', 'success');
            
            // Reload trang để cập nhật UI
            const currentFilter = document.querySelector('.filter-btn.active')?.getAttribute('data-filter') || 'all';
            sessionStorage.setItem('waiterOrderFilter', currentFilter);
            location.reload();
        } else {
            showToast('Lỗi: ' + (result.message || 'Không thể xác nhận số lượng'), 'error');
        }
    } catch (error) {
        console.error('Error confirming consumption quantity:', error);
        showToast('Lỗi kết nối: ' + error.message, 'error');
    }
}

// Helper function
function getCurrentUserId() {
    // TODO: Get from session/cookie
    return 1; // Temporary
}

