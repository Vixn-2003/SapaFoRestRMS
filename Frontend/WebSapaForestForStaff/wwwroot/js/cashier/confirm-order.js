/**
 * ========================================
 * CASHIER CONFIRM ORDER PAGE SCRIPTS
 * ========================================
 * Handles order confirmation and payment flow
 */

// Global state
let currentTransactionId = null;
let orderConfig = {}; // Will be initialized from the view

/**
 * Initialize the confirm order page
 * @param {Object} config - Configuration object with order data
 */
function initConfirmOrderPage(config) {
    orderConfig = config;
    
    // Initialize event listeners
    initFormSubmitHandler();
    initTooltips();
    
    // Initialize undo confirm handler if applicable
    if (config.canUndoConfirm) {
        initUndoConfirmHandler(config.undoEndpoint, config.staffId);
    }
}

/**
 * Show toast notification
 */
function showToast(message, type = 'info') {
    const toastElement = document.getElementById('cashierToast');
    const toastBody = document.getElementById('cashierToastMessage');
    
    if (!toastElement || !toastBody) return;
    
    toastElement.classList.remove('text-bg-primary', 'text-bg-success', 'text-bg-danger', 'text-bg-warning');
    const typeClass = type === 'success'
        ? 'text-bg-success'
        : type === 'error'
            ? 'text-bg-danger'
            : type === 'warning'
                ? 'text-bg-warning'
                : 'text-bg-primary';
    toastElement.classList.add(typeClass);
    toastBody.textContent = message;
    bootstrap.Toast.getOrCreateInstance(toastElement, { delay: 3500 }).show();
}

/**
 * Validate quantity input
 */
function validateQuantityInput(input) {
    const max = parseInt(input.dataset.max);
    const value = parseInt(input.value) || 0;
    
    if (value < 0) {
        input.value = 0;
        input.classList.add('invalid');
    } else if (value > max) {
        input.value = max;
        input.classList.add('invalid');
        setTimeout(() => input.classList.remove('invalid'), 300);
    } else {
        input.classList.remove('invalid');
    }
}

/**
 * Update consumption item total
 */
function updateConsumptionTotal(itemId) {
    const row = document.querySelector(`tr[data-item-id="${itemId}"]`);
    if (!row) return;
    
    const input = row.querySelector('.consumption-quantity');
    const quantityUsed = parseInt(input.value) || 0;
    const unitPrice = parseFloat(row.dataset.unitPrice);
    const totalPrice = quantityUsed * unitPrice;
    
    const totalSpan = row.querySelector(`.item-total[data-item-id="${itemId}"]`);
    if (totalSpan) {
        totalSpan.textContent = totalPrice.toLocaleString('vi-VN') + ' ₫';
    }
    
    // Recalculate consumption subtotal
    updateConsumptionSubtotal();
}

/**
 * Update consumption subtotal
 */
function updateConsumptionSubtotal() {
    let total = 0;
    document.querySelectorAll('#consumptionItemsTable tr').forEach(row => {
        const itemId = row.dataset.itemId;
        if (!itemId) return;
        
        const input = row.querySelector('.consumption-quantity');
        const quantityUsed = parseInt(input.value) || 0;
        const unitPrice = parseFloat(row.dataset.unitPrice);
        
        total += quantityUsed * unitPrice;
    });
    
    const subtotalEl = document.getElementById('consumptionSubtotal');
    if (subtotalEl) {
        subtotalEl.textContent = total.toLocaleString('vi-VN') + ' ₫';
    }
}

/**
 * 2️⃣ CONFIRM ITEMS (Khách đã xác nhận)
 * Form validation + confirmation popup before submission
 */
function initFormSubmitHandler() {
    const form = document.getElementById('confirmOrderForm');
    if (!form) return;

    form.addEventListener('submit', async (event) => {
        event.preventDefault(); // Always prevent default to show confirmation

        // Validate quantity inputs first
        const inputs = form.querySelectorAll('input[name$=".QuantityUsed"][data-max]');
        let isValid = true;
        inputs.forEach(input => {
            const max = parseInt(input.dataset.max, 10) || 0;
            const value = parseInt(input.value, 10) || 0;
            if (value > max || value < 0) {
                isValid = false;
                input.classList.add('is-invalid');
            } else {
                input.classList.remove('is-invalid');
            }
        });

        if (!isValid) {
            showToast('Số lượng khách sử dụng phải nằm trong khoảng hợp lệ.', 'warning');
            return;
        }

        // Show confirmation popup
        const confirmed = await showConfirmPopup(
            "🍽️ Xác nhận món trong hóa đơn",
            `<p class="mb-3">Bạn đã kiểm tra danh sách món và số lượng thực tế khách sử dụng chưa?</p>
            <div class="alert alert-warning d-flex align-items-start gap-2 mb-0">
                <i class="fa-solid fa-exclamation-triangle mt-1"></i>
                <div class="small">
                    <strong>Lưu ý:</strong> Sau khi xác nhận, bạn sẽ không thể chỉnh sửa số lượng món tiêu hao. 
                    Hãy đảm bảo đã kiểm tra kỹ với khách hàng.
                </div>
            </div>`,
            "Khách đã xác nhận",
            "Quay lại kiểm tra"
        );

        if (confirmed) {
            // Submit the form programmatically
            form.submit();
        }
    });
}

/**
 * 4️⃣ SELECT PAYMENT METHOD
 * Shows confirmation before opening payment method selection
 */
async function showPaymentMethodSelection() {
    const confirmed = await showConfirmPopup(
        "💳 Bắt đầu thanh toán",
        `<p class="mb-3">Bạn có muốn bắt đầu thanh toán cho đơn hàng <strong>#${orderConfig.orderCode}</strong> không?</p>
        <div class="alert alert-info d-flex align-items-start gap-2 mb-0">
            <i class="fa-solid fa-info-circle mt-1"></i>
            <div class="small">
                <strong>Tổng tiền:</strong> <span class="text-success fw-bold">${orderConfig.totalAmount.toLocaleString('vi-VN')} ₫</span><br>
                <strong>Bước tiếp theo:</strong> Chọn phương thức thanh toán (Tiền mặt / QR)
            </div>
        </div>`,
        "Bắt đầu thanh toán",
        "Hủy thao tác"
    );

    if (confirmed) {
        const modal = new bootstrap.Modal(document.getElementById('paymentMethodModal'));
        modal.show();
    }
}

/**
 * 5️⃣ CONFIRM CASH PAYMENT EXECUTION
 * Adds confirmation before processing cash payment
 */
async function selectCashPayment() {
    // Close payment method modal
    bootstrap.Modal.getInstance(document.getElementById('paymentMethodModal'))?.hide();
    
    // Prompt for cash amount
    const totalAmount = orderConfig.totalAmount;
    const cashGiven = prompt(`Tổng tiền: ${totalAmount.toLocaleString('vi-VN')} ₫\n\nNhập số tiền khách đưa:`);
    
    if (!cashGiven) return;
    
    const cashAmount = parseFloat(cashGiven.replace(/,/g, ''));
    if (isNaN(cashAmount) || cashAmount < totalAmount) {
        showToast('Số tiền không hợp lệ hoặc chưa đủ!', 'error');
        return;
    }

    // Calculate change
    const change = cashAmount - totalAmount;
    const changeInfo = change > 0 
        ? `<div class="mt-2 p-2 bg-success bg-opacity-10 border border-success rounded">
               <strong class="text-success">💵 Tiền thối lại:</strong> 
               <span class="fw-bold text-success">${change.toLocaleString('vi-VN')} ₫</span>
           </div>`
        : '<div class="mt-2 text-success"><i class="fa-solid fa-check-circle"></i> Khách đưa vừa đủ</div>';

    // Show confirmation popup
    const confirmed = await showConfirmPopup(
        "💳 Xác nhận thanh toán tiền mặt",
        `<p class="mb-3">Bạn có chắc muốn xác nhận thanh toán hóa đơn này không?</p>
        <div style="background: #f7fafc; padding: 12px; border-radius: 8px; margin-bottom: 12px;">
            <div class="d-flex justify-content-between mb-2">
                <span class="text-muted">Tổng tiền:</span>
                <span class="fw-bold">${totalAmount.toLocaleString('vi-VN')} ₫</span>
            </div>
            <div class="d-flex justify-content-between">
                <span class="text-muted">Khách đưa:</span>
                <span class="fw-bold text-primary">${cashAmount.toLocaleString('vi-VN')} ₫</span>
            </div>
        </div>
        ${changeInfo}`,
        "Xác nhận thanh toán",
        "Hủy"
    );

    if (!confirmed) return;
    
    try {
        const token = getCookie('jwtToken');
        
        // Start payment
        const startResponse = await fetch('/api/payment/start', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                orderId: orderConfig.orderId,
                paymentMethod: 'Cash'
            })
        });
        
        if (!startResponse.ok) throw new Error('Không thể khởi tạo thanh toán');
        
        const transaction = await startResponse.json();
        
        // Manual confirm
        const confirmResponse = await fetch('/api/payment/manual-confirm', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                orderId: orderConfig.orderId,
                transactionId: transaction.transactionId,
                cashGiven: cashAmount,
                notes: `Tiền mặt. Khách đưa: ${cashAmount.toLocaleString('vi-VN')} ₫`
            })
        });
        
        if (confirmResponse.ok) {
            showToast('✓ Thanh toán thành công!', 'success');
            
            // Show print receipt confirmation
            await promptPrintReceipt(orderConfig.orderId);
            
            window.location.reload();
        } else {
            const error = await confirmResponse.json();
            showToast('Lỗi: ' + (error.message || 'Không thể xác nhận thanh toán'), 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showToast('Lỗi: ' + error.message, 'error');
    }
}

/**
 * Select QR Payment
 */
async function selectQRPayment() {
    // Close payment method modal
    bootstrap.Modal.getInstance(document.getElementById('paymentMethodModal'))?.hide();
    
    try {
        const token = getCookie('jwtToken');
        
        // Start payment
        const startResponse = await fetch('/api/payment/start', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                orderId: orderConfig.orderId,
                paymentMethod: 'QRBankTransfer'
            })
        });
        
        if (!startResponse.ok) throw new Error('Không thể khởi tạo thanh toán');
        
        const transaction = await startResponse.json();
        currentTransactionId = transaction.transactionId;
        
        // Generate QR
        const qrResponse = await fetch(`/api/payment/vietqr/${orderConfig.orderId}?bankCode=VCB&account=0123456789`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!qrResponse.ok) throw new Error('Không thể tạo mã QR');
        
        const qrData = await qrResponse.json();
        
        // Display QR modal
        document.getElementById('qrImageDisplay').src = qrData.qrUrl;
        document.getElementById('qrTotalAmount').textContent = qrData.total.toLocaleString('vi-VN') + ' ₫';
        document.getElementById('qrDescription').textContent = qrData.description;
        
        const qrModal = new bootstrap.Modal(document.getElementById('qrPaymentModal'));
        qrModal.show();
        
    } catch (error) {
        console.error('Error:', error);
        showToast('Lỗi: ' + error.message, 'error');
    }
}

/**
 * 6️⃣ CONFIRM QR PAYMENT EXECUTION
 * Adds confirmation before confirming QR payment received
 */
async function confirmQRPayment() {
    if (!currentTransactionId) {
        showToast('Không tìm thấy thông tin giao dịch', 'error');
        return;
    }

    // Show confirmation popup
    const confirmed = await showConfirmPopup(
        "💳 Xác nhận thanh toán QR",
        `<p class="mb-3">Bạn đã kiểm tra và xác nhận nhận được tiền chuyển khoản từ khách hàng chưa?</p>
        <div class="alert alert-warning d-flex align-items-start gap-2 mb-0">
            <i class="fa-solid fa-exclamation-triangle mt-1"></i>
            <div class="small">
                <strong>⚠️ Lưu ý quan trọng:</strong><br>
                Chỉ xác nhận khi bạn đã thấy tiền trong tài khoản ngân hàng. 
                Kiểm tra kỹ số tiền và nội dung chuyển khoản.
            </div>
        </div>`,
        "Đã nhận tiền",
        "Chưa nhận"
    );

    if (!confirmed) return;
    
    try {
        const token = getCookie('jwtToken');
        
        const response = await fetch('/api/payment/manual-confirm', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                orderId: orderConfig.orderId,
                transactionId: currentTransactionId,
                notes: 'QR Bank Transfer - Đã xác nhận nhận tiền'
            })
        });
        
        if (response.ok) {
            bootstrap.Modal.getInstance(document.getElementById('qrPaymentModal'))?.hide();
            showToast('✓ Thanh toán thành công!', 'success');
            
            // Show print receipt confirmation
            await promptPrintReceipt(orderConfig.orderId);
            
            setTimeout(() => window.location.reload(), 1000);
        } else {
            const error = await response.json();
            showToast('Lỗi: ' + (error.message || 'Không thể xác nhận thanh toán'), 'error');
        }
    } catch (error) {
        console.error('Error:', error);
        showToast('Lỗi: ' + error.message, 'error');
    }
}

/**
 * 7️⃣ CANCEL KITCHEN ITEM
 * Adds confirmation before cancelling a kitchen item
 */
async function cancelKitchenItem(orderDetailId) {
    // First, prompt for reason
    const reason = prompt('Nhập lý do hủy món (tối thiểu 10 ký tự):');
    if (!reason || reason.trim() === '') {
        showToast('Vui lòng nhập lý do hủy món', 'warning');
        return;
    }

    if (reason.trim().length < 10) {
        showToast('Lý do phải có ít nhất 10 ký tự', 'warning');
        return;
    }

    // Show confirmation popup with reason preview
    const confirmed = await showConfirmPopup(
        "⚠️ Xác nhận hủy món",
        `<p class="mb-3">Bạn có chắc muốn hủy món này không?</p>
        <div class="alert alert-danger d-flex align-items-start gap-2 mb-3">
            <i class="fa-solid fa-exclamation-circle mt-1"></i>
            <div class="small">
                <strong>⚠️ Cảnh báo:</strong><br>
                Món này sẽ bị xóa khỏi hóa đơn và <strong>không thể khôi phục</strong>. 
                Hành động này sẽ được ghi nhận trong hệ thống.
            </div>
        </div>
        <div style="background: #fff5f5; padding: 12px; border-radius: 8px; border-left: 4px solid #e53e3e;">
            <div class="small"><strong class="text-danger">Lý do hủy món:</strong></div>
            <div class="mt-2 fst-italic" style="color: #742a2a;">"${reason}"</div>
        </div>`,
        "Xác nhận hủy",
        "Giữ lại món"
    );

    if (!confirmed) return;
    
    try {
        const token = getCookie('jwtToken');
        const response = await fetch('/api/payment/cancel-item', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                reason: reason
            })
        });
        
        if (response.ok) {
            showToast('Hủy món thành công!', 'success');
            setTimeout(() => location.reload(), 1000);
        } else {
            const error = await response.json();
            showToast('Không thể hủy món: ' + (error.message || 'Lỗi không xác định'), 'error');
        }
    } catch (error) {
        console.error('Error cancelling item:', error);
        showToast('Có lỗi xảy ra khi hủy món', 'error');
    }
}

/**
 * 8️⃣ PRINT RECEIPT AFTER PAYMENT
 * Shows confirmation popup for printing receipt after successful payment
 */
async function promptPrintReceipt(orderId) {
    const confirmed = await showConfirmPopup(
        "🖨️ In hóa đơn",
        `<div class="text-center mb-3">
            <div style="width: 80px; height: 80px; margin: 0 auto 16px; background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%); border-radius: 50%; display: flex; align-items: center; justify-content: center;">
                <i class="fa-solid fa-check-circle" style="font-size: 48px; color: #059669;"></i>
            </div>
            <h5 class="text-success fw-bold mb-2">🎉 Thanh toán thành công!</h5>
            <p class="text-muted mb-0">Bạn có muốn in hóa đơn cho khách hàng không?</p>
        </div>
        <div class="alert alert-info d-flex align-items-start gap-2 mb-0">
            <i class="fa-solid fa-info-circle mt-1"></i>
            <div class="small">
                Hóa đơn sẽ được mở trong tab mới. Bạn có thể in hoặc lưu file PDF.
            </div>
        </div>`,
        "In hóa đơn",
        "Bỏ qua"
    );

    if (confirmed) {
        // Open receipt in new tab
        window.open(`/api/payment/receipt/${orderId}`, '_blank');
    }
}

/**
 * 3️⃣ UNDO CONFIRMATION
 * Adds confirmation popup before undoing order confirmation
 */
function initUndoConfirmHandler(endpoint, staffId) {
    const form = document.getElementById('undoConfirmForm');
    if (!form) return;

    const modalEl = document.getElementById('undoConfirmModal');
    const undoModal = modalEl ? bootstrap.Modal.getOrCreateInstance(modalEl) : null;

    form.addEventListener('submit', async (ev) => {
        ev.preventDefault();
        const reason = (form.querySelector('textarea[name="Reason"]')?.value || '').trim();

        if (!reason) {
            showToast('Vui lòng nhập lý do hoàn tác.', 'warning');
            return;
        }

        if (reason.length < 10) {
            showToast('Lý do phải có ít nhất 10 ký tự.', 'warning');
            return;
        }

        // Show confirmation popup with reason preview
        const confirmed = await showConfirmPopup(
            "↩️ Hoàn tác xác nhận",
            `<p class="mb-3">Hoàn tác xác nhận sẽ đưa đơn hàng về trạng thái <strong>"Chờ xác nhận"</strong>.</p>
            <div class="alert alert-warning d-flex align-items-start gap-2 mb-3">
                <i class="fa-solid fa-exclamation-triangle mt-1"></i>
                <div class="small">
                    <strong>Lưu ý:</strong> Hành động này chỉ khả dụng khi bếp chưa bắt đầu chế biến món.
                </div>
            </div>
            <div style="background: #f7fafc; padding: 12px; border-radius: 8px; border-left: 4px solid #667eea;">
                <div class="small"><strong>Lý do hoàn tác:</strong></div>
                <div class="mt-2 fst-italic text-muted">"${reason}"</div>
            </div>`,
            "Xác nhận hoàn tác",
            "Hủy"
        );

        if (!confirmed) return;

        try {
            const response = await fetch(endpoint, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    staffId: parseInt(staffId, 10) || 0,
                    reason
                })
            });

            if (response.ok) {
                undoModal?.hide();
                showToast('Đã hoàn tác xác nhận thành công!', 'success');
                setTimeout(() => window.location.reload(), 900);
            } else {
                const data = await response.json().catch(() => null);
                showToast(data?.message || 'Không thể hoàn tác xác nhận.', 'error');
            }
        } catch {
            showToast('Có lỗi xảy ra. Vui lòng thử lại sau.', 'error');
        }
    });
}

/**
 * Utility: Get cookie
 */
function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}

/**
 * Initialize tooltips
 */
function initTooltips() {
    document.querySelectorAll('.tooltip-icon').forEach(el => {
        el.title = el.getAttribute('title');
    });
}

// Export functions to global scope for onclick handlers
window.initConfirmOrderPage = initConfirmOrderPage;
window.validateQuantityInput = validateQuantityInput;
window.updateConsumptionTotal = updateConsumptionTotal;
window.showPaymentMethodSelection = showPaymentMethodSelection;
window.selectCashPayment = selectCashPayment;
window.selectQRPayment = selectQRPayment;
window.confirmQRPayment = confirmQRPayment;
window.cancelKitchenItem = cancelKitchenItem;

