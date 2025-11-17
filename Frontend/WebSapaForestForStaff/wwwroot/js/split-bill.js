/**
 * Split Bill JavaScript
 */

(function () {
    'use strict';

    let currentOrderId = null;
    let currentOrderData = null;
    let splitParts = [];

    function getApiBaseUrl() {
        return window.API_BASE_URL || (typeof apiBaseUrl !== 'undefined' ? apiBaseUrl : 'https://localhost:7000/api');
    }

    function getToken() {
        return localStorage.getItem('jwtToken') || sessionStorage.getItem('jwtToken');
    }

    function formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    }

    function showToast(message, type = 'info') {
        if (typeof toastr !== 'undefined') {
            toastr[type === 'error' ? 'error' : type === 'success' ? 'success' : 'info'](message);
        } else {
            alert(message);
        }
    }

    window.openSplitBill = function (orderId, orderData) {
        currentOrderId = orderId;
        currentOrderData = orderData;

        // Populate modal
        document.getElementById('splitOrderCode').textContent = orderData.orderCode || `ORD-${orderId}`;
        document.getElementById('splitTableNumber').textContent = orderData.tableNumber || '-';
        document.getElementById('splitTotalAmount').textContent = formatCurrency(orderData.total || 0);

        // Reset
        splitParts = [];
        document.getElementById('numberOfParts').value = 2;
        document.getElementById('splitNotes').value = '';
        document.getElementById('splitTotalValidation').classList.add('d-none');

        // Initialize with equal split
        changeSplitType();

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('splitBillModal'));
        modal.show();
    };

    window.changeSplitType = function () {
        const splitType = document.querySelector('input[name="splitType"]:checked').value;
        const equalSection = document.getElementById('equalSplitSection');
        const partsList = document.getElementById('splitPartsList');

        if (splitType === 'equal') {
            equalSection.classList.remove('d-none');
            calculateEqualSplit();
        } else {
            equalSection.classList.add('d-none');
            renderCustomSplit();
        }
    };

    window.calculateEqualSplit = function () {
        const numberOfParts = parseInt(document.getElementById('numberOfParts').value) || 2;
        const totalAmount = currentOrderData?.total || 0;
        const amountPerPart = Math.floor(totalAmount / numberOfParts);
        const remainder = totalAmount - (amountPerPart * numberOfParts);

        splitParts = [];
        for (let i = 0; i < numberOfParts; i++) {
            splitParts.push({
                partNumber: i + 1,
                amount: i === 0 ? amountPerPart + remainder : amountPerPart, // Add remainder to first part
                paymentMethod: 'Cash',
                amountReceived: null,
                notes: ''
            });
        }

        renderSplitParts();
        validateSplitTotal();
    };

    window.renderCustomSplit = function () {
        if (splitParts.length === 0) {
            // Initialize with 2 equal parts
            const totalAmount = currentOrderData?.total || 0;
            splitParts = [
                { partNumber: 1, amount: totalAmount / 2, paymentMethod: 'Cash', amountReceived: null, notes: '' },
                { partNumber: 2, amount: totalAmount / 2, paymentMethod: 'Cash', amountReceived: null, notes: '' }
            ];
        }
        renderSplitParts();
        validateSplitTotal();
    };

    function renderSplitParts() {
        const container = document.getElementById('splitPartsList');
        const totalAmount = currentOrderData?.total || 0;
        const splitType = document.querySelector('input[name="splitType"]:checked')?.value || 'equal';
        const isCustomSplit = splitType === 'custom';

        container.innerHTML = splitParts.map((part, index) => `
            <div class="card mb-2">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <h6 class="mb-0">
                            <i class="bi bi-${part.partNumber}-circle me-2"></i>Phần ${part.partNumber}
                        </h6>
                        ${!isCustomSplit ? `<span class="badge bg-primary">${formatCurrency(part.amount)}</span>` : ''}
                    </div>
                    
                    ${isCustomSplit ? `
                        <div class="mb-2">
                            <label class="form-label small">
                                <strong>Số tiền thanh toán</strong>
                            </label>
                            <div class="input-group input-group-sm">
                                <input type="number" 
                                       class="form-control" 
                                       id="splitPartAmount_${index}"
                                       value="${part.amount.toFixed(0)}"
                                       min="0"
                                       step="1000"
                                       placeholder="Nhập số tiền..."
                                       onchange="updateSplitPartAmount(${index}, this)"
                                       oninput="updateSplitPartAmountInput(${index}, this)">
                                <span class="input-group-text">₫</span>
                            </div>
                            <small class="text-muted">Số tiền cần thanh toán cho phần này</small>
                        </div>
                    ` : ''}
                    
                    <div class="row g-2">
                        <div class="col-md-6">
                            <label class="form-label small">Phương thức thanh toán</label>
                            <select class="form-select form-select-sm" 
                                    onchange="updateSplitPart(${index}, 'paymentMethod', this.value)">
                                <option value="Cash" ${part.paymentMethod === 'Cash' ? 'selected' : ''}>Tiền mặt</option>
                                <option value="QRBankTransfer" ${part.paymentMethod === 'QRBankTransfer' ? 'selected' : ''}>Chuyển khoản QR</option>
                                <!-- Removed: Card and EWallet options - Simplified to Cash and QR only -->
                            </select>
                        </div>
                        ${part.paymentMethod === 'Cash' ? `
                            <div class="col-md-6">
                                <label class="form-label small">Số tiền nhận được</label>
                                <div class="input-group input-group-sm">
                                    <input type="number" 
                                           class="form-control" 
                                           value="${part.amountReceived || ''}"
                                           min="0"
                                           step="1000"
                                           placeholder="Nhập số tiền..."
                                           onchange="updateSplitPart(${index}, 'amountReceived', parseFloat(this.value) || null)"
                                           oninput="updateSplitPart(${index}, 'amountReceived', parseFloat(this.value) || null)">
                                    <span class="input-group-text">₫</span>
                                </div>
                            </div>
                        ` : ''}
                    </div>
                    ${part.paymentMethod === 'Cash' && part.amountReceived && part.amountReceived > part.amount ? `
                        <div class="alert alert-info mt-2 mb-0 py-1">
                            <small>Tiền thối: ${formatCurrency(part.amountReceived - part.amount)}</small>
                        </div>
                    ` : ''}
                    <div class="mt-2">
                        <input type="text" 
                               class="form-control form-control-sm" 
                               placeholder="Ghi chú (tùy chọn)"
                               value="${part.notes}"
                               onchange="updateSplitPart(${index}, 'notes', this.value)">
                    </div>
                    ${isCustomSplit ? `
                        <div class="mt-2 text-end">
                            <button type="button" 
                                    class="btn btn-outline-danger btn-sm" 
                                    onclick="removeSplitPart(${index})"
                                    ${splitParts.length <= 2 ? 'disabled' : ''}
                                    title="${splitParts.length <= 2 ? 'Phải có ít nhất 2 phần' : 'Xóa phần này'}">
                                <i class="bi bi-trash me-1"></i>Xóa
                            </button>
                        </div>
                    ` : ''}
                </div>
            </div>
        `).join('');

        // Add "Add Part" button if custom split
        if (isCustomSplit) {
            container.innerHTML += `
                <button type="button" class="btn btn-outline-primary btn-sm w-100" onclick="addSplitPart()">
                    <i class="bi bi-plus-circle me-1"></i>Thêm phần
                </button>
            `;
        }
    }

    window.updateSplitPart = function (index, field, value) {
        if (splitParts[index]) {
            splitParts[index][field] = value;
            // Only re-render if not updating amount (amount updates handled separately)
            if (field !== 'amount') {
                renderSplitParts();
            }
            validateSplitTotal();
        }
    };

    // Update split part amount on input (real-time validation, no re-render)
    window.updateSplitPartAmountInput = function (index, inputElement) {
        if (splitParts[index] && inputElement) {
            const newAmount = parseFloat(inputElement.value) || 0;
            if (newAmount >= 0) {
                splitParts[index].amount = newAmount;
                // Only validate, don't re-render to avoid losing focus
                validateSplitTotal();
            }
        }
    };

    // Update split part amount on change (with re-render)
    window.updateSplitPartAmount = function (index, inputElement) {
        if (splitParts[index] && inputElement) {
            const newAmount = parseFloat(inputElement.value) || 0;
            if (newAmount >= 0) {
                splitParts[index].amount = newAmount;
                validateSplitTotal();
                // Re-render to update UI (after user finishes editing)
                renderSplitParts();
            }
        }
    };

    // Remove split part (for custom split)
    window.removeSplitPart = function (index) {
        if (splitParts.length <= 2) {
            showToast('Phải có ít nhất 2 phần thanh toán!', 'warning');
            return;
        }
        
        if (confirm('Bạn có chắc muốn xóa phần này không?')) {
            splitParts.splice(index, 1);
            // Renumber parts
            splitParts.forEach((part, idx) => {
                part.partNumber = idx + 1;
            });
            renderSplitParts();
            validateSplitTotal();
        }
    };

    window.addSplitPart = function () {
        const totalAmount = currentOrderData?.total || 0;
        const currentTotal = splitParts.reduce((sum, p) => sum + p.amount, 0);
        const remaining = totalAmount - currentTotal;

        if (remaining <= 0) {
            showToast('Tổng các phần đã đủ. Vui lòng điều chỉnh số tiền các phần.', 'warning');
            return;
        }

        splitParts.push({
            partNumber: splitParts.length + 1,
            amount: remaining,
            paymentMethod: 'Cash',
            amountReceived: null,
            notes: ''
        });

        renderSplitParts();
        validateSplitTotal();
    };

    window.increaseParts = function () {
        const input = document.getElementById('numberOfParts');
        const current = parseInt(input.value) || 2;
        if (current < 10) {
            input.value = current + 1;
            calculateEqualSplit();
        }
    };

    window.decreaseParts = function () {
        const input = document.getElementById('numberOfParts');
        const current = parseInt(input.value) || 2;
        if (current > 2) {
            input.value = current - 1;
            calculateEqualSplit();
        }
    };

    function validateSplitTotal() {
        const totalAmount = currentOrderData?.total || 0;
        const partsTotal = splitParts.reduce((sum, p) => sum + p.amount, 0);
        const validation = document.getElementById('splitTotalValidation');
        const confirmBtn = document.getElementById('confirmSplitBillBtn');

        if (Math.abs(partsTotal - totalAmount) > 0.01) {
            validation.classList.remove('d-none');
            document.getElementById('splitPartsTotal').textContent = formatCurrency(partsTotal);
            document.getElementById('splitBillTotal').textContent = formatCurrency(totalAmount);
            confirmBtn.disabled = true;
        } else {
            validation.classList.add('d-none');
            confirmBtn.disabled = false;
        }
    }

    window.confirmSplitBill = async function () {
        const notes = document.getElementById('splitNotes').value;
        const confirmBtn = document.getElementById('confirmSplitBillBtn');
        const originalText = confirmBtn.innerHTML;
        confirmBtn.disabled = true;
        confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang xử lý...';

        try {
            const response = await fetch(`${getApiBaseUrl()}/payment/split-bill`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${getToken()}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    orderId: currentOrderId,
                    parts: splitParts.map(p => ({
                        paymentMethod: p.paymentMethod,
                        amount: p.amount,
                        amountReceived: p.amountReceived,
                        notes: p.notes
                    })),
                    notes: notes
                })
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Chia hóa đơn thất bại');
            }

            const transactions = await response.json();
            showToast(`✅ Đã chia hóa đơn thành ${transactions.length} phần thanh toán!`, 'success');

            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('splitBillModal'));
            modal.hide();

            // Reload page
            setTimeout(() => {
                window.location.reload();
            }, 1500);
        } catch (error) {
            console.error('Error processing split bill:', error);
            showToast(error.message || 'Lỗi khi chia hóa đơn. Vui lòng thử lại.', 'error');
        } finally {
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = originalText;
        }
    };

    // Export
    window.SplitBill = {
        openSplitBill,
        changeSplitType,
        calculateEqualSplit,
        renderCustomSplit,
        updateSplitPart,
        addSplitPart,
        increaseParts,
        decreaseParts,
        confirmSplitBill
    };
})();


