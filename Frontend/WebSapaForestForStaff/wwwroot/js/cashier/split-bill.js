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

    function showToast(message, type = 'error') {
        const errorContainer = document.getElementById('modal-error-message');
        const errorText = document.getElementById('modal-error-text');

        if (!errorContainer || !errorText) {
            // Fallback to global toast container if modal error elements not found
            if (typeof toastr !== 'undefined') {
                toastr[type === 'error' ? 'error' : type === 'success' ? 'success' : 'info'](message);
            } else {
                alert(message);
            }
            return;
        }

        // Set error message text
        errorText.textContent = message;

        // Show error container
        errorContainer.classList.remove('d-none');

        // Update alert styling based on type
        const alert = errorContainer.querySelector('.alert');
        if (alert) {
            // Remove all alert classes first
            alert.classList.remove('alert-danger', 'alert-success', 'alert-warning', 'alert-info');

            // Add appropriate class based on type
            const alertClass = type === 'error' ? 'alert-danger' :
                              type === 'success' ? 'alert-success' :
                              type === 'warning' ? 'alert-warning' : 'alert-info';
            alert.classList.add(alertClass);
        }

        // Auto hide after 8 seconds (longer than toast for better UX)
        setTimeout(() => {
            hideErrorMessage();
        }, 8000);
    }

    function hideErrorMessage() {
        const errorContainer = document.getElementById('modal-error-message');
        if (errorContainer) {
            errorContainer.classList.add('d-none');
        }
    }

    // Split bill specific error handling
    function showSplitPartError(partIndex, field, message) {
        const errorDiv = document.getElementById(`${field}Error_${partIndex}`);
        const input = document.getElementById(`${field === 'cash' ? 'cashReceived' : 'splitPartAmount'}_${partIndex}`);
        const inputGroup = input ? input.closest('.input-group') : null;

        if (errorDiv) {
            errorDiv.textContent = message;
            errorDiv.classList.add('show');
        }

        if (input) {
            input.classList.add('is-invalid');
        }

        if (inputGroup) {
            inputGroup.classList.add('is-invalid');
        }
    }

    function clearSplitPartError(partIndex, field) {
        const errorDiv = document.getElementById(`${field}Error_${partIndex}`);
        const input = document.getElementById(`${field === 'cash' ? 'cashReceived' : 'splitPartAmount'}_${partIndex}`);
        const inputGroup = input ? input.closest('.input-group') : null;

        if (errorDiv) {
            errorDiv.textContent = '';
            errorDiv.classList.remove('show');
        }

        if (input) {
            input.classList.remove('is-invalid');
        }

        if (inputGroup) {
            inputGroup.classList.remove('is-invalid');
        }
    }

    function clearAllSplitPartErrors() {
        splitParts.forEach((_, index) => {
            clearSplitPartError(index, 'cash');
            clearSplitPartError(index, 'amount');
        });
    }

    // QR Code generation functions (adapted from Payment.cshtml)
    function generateVietQrUrl(bank, account, amount, addInfo) {
        return `https://img.vietqr.io/image/${bank}-${account}-compact.png`
            + `?amount=${amount}`
            + `&addInfo=${encodeURIComponent(addInfo)}`;
    }

    window.showSplitPartQR = function (partIndex) {
        const qrPreview = document.getElementById(`qrPreview_${partIndex}`);
        const generateBtn = document.getElementById(`qrGenerateBtn_${partIndex}`);

        if (qrPreview && generateBtn) {
            qrPreview.classList.remove('d-none');
            generateBtn.style.display = 'none';
            generateSplitPartQR(partIndex);
        }
    };

    window.hideSplitPartQR = function (partIndex) {
        const qrPreview = document.getElementById(`qrPreview_${partIndex}`);
        const generateBtn = document.getElementById(`qrGenerateBtn_${partIndex}`);

        if (qrPreview && generateBtn) {
            qrPreview.classList.add('d-none');
            generateBtn.style.display = 'block';
        }
    };

    window.generateSplitPartQR = function (partIndex) {
        const part = splitParts[partIndex];
        if (!part || part.paymentMethod !== 'QRBankTransfer') return;

        const loadingDiv = document.getElementById(`qrLoading_${partIndex}`);
        const contentDiv = document.getElementById(`qrContent_${partIndex}`);
        const generateBtn = document.querySelector(`#qrPreview_${partIndex} .btn-outline-primary`);

        // Show loading
        loadingDiv.classList.remove('d-none');
        contentDiv.classList.add('d-none');
        if (generateBtn) generateBtn.disabled = true;

        setTimeout(() => {
            // Hide loading, show content
            loadingDiv.classList.add('d-none');
            contentDiv.classList.remove('d-none');
            if (generateBtn) generateBtn.disabled = false;

            // Generate QR details
            const qrAmount = part.amount;
            const orderCode = currentOrderData?.orderCode || `ORD-${currentOrderId}`;
            const transactionCode = `TXN-SPLIT-${Date.now()}-${partIndex}`;
            const bank = "MB";
            const account = "0397604824";
            const addInfo = `RMS#${orderCode}-P${part.partNumber}`;

            const qrUrl = generateVietQrUrl(bank, account, qrAmount, addInfo);

            // Update UI elements
            document.getElementById(`qrAmount_${partIndex}`).textContent = `Số tiền: ${formatCurrency(qrAmount)}`;
            document.getElementById(`qrDescription_${partIndex}`).textContent = addInfo;
            document.getElementById(`qrTransactionCode_${partIndex}`).textContent = transactionCode;
            document.getElementById(`qrImage_${partIndex}`).src = qrUrl;
        }, 800); // Slightly longer delay for better UX
    };

    window.openSplitBill = function (orderId, orderData) {
        currentOrderId = orderId;
        currentOrderData = orderData;

        // ✅ FIX: Tính lại Total nếu Total = 0 hoặc không có
        // Công thức: Total = Subtotal + Vat + ServiceFee - Discount - DepositAmount (nếu DepositPaid = true)
        let totalAmount = orderData.total || 0;
        if (totalAmount === 0 || !totalAmount) {
            const subtotal = orderData.subtotal || 0;
            const vat = orderData.vat || 0;
            const serviceFee = orderData.serviceFee || 0;
            const discount = orderData.discount || 0;
            const depositAmount = (orderData.depositPaid && orderData.depositAmount) ? orderData.depositAmount : 0;
            
            // Tính totalBeforeDeposit
            const totalBeforeDeposit = subtotal + vat + serviceFee - discount;
            
            // Nếu deposit > totalBeforeDeposit thì total = 0, ngược lại = totalBeforeDeposit - depositAmount
            if (depositAmount > 0 && depositAmount > totalBeforeDeposit) {
                totalAmount = 0;
            } else {
                totalAmount = totalBeforeDeposit - depositAmount;
            }
            
            // Cập nhật lại orderData để dùng cho các tính toán sau
            currentOrderData.total = totalAmount;
        }

        // Populate modal
        document.getElementById('splitOrderCode').textContent = orderData.orderCode || `ORD-${orderId}`;
        document.getElementById('splitTableNumber').textContent = orderData.tableNumber || '-';
        document.getElementById('splitTotalAmount').textContent = formatCurrency(totalAmount);

        // Reset
        splitParts = [];
        document.getElementById('numberOfParts').value = 2;
        document.getElementById('splitNotes').value = '';
        document.getElementById('splitTotalValidation').classList.add('d-none');

        // Clear any existing modal error message
        const errorContainer = document.getElementById('modal-error-message');
        if (errorContainer) {
            errorContainer.classList.add('d-none');
        }

        // Clear all split part errors
        clearAllSplitPartErrors();

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
                            <div class="split-part-error" id="amountError_${index}"></div>
                            <small class="text-muted">Số tiền cần thanh toán cho phần này</small>
                        </div>
                    ` : ''}
                    
                    <div class="row g-2">
                        <div class="col-md-6">
                            <label class="form-label small">Phương thức thanh toán</label>
                            <select class="form-select form-select-sm" 
                                    id="splitPaymentMethod_${index}"
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
                                           id="cashReceived_${index}"
                                           value="${part.amountReceived || ''}"
                                           min="0"
                                           step="1000"
                                           placeholder="Nhập số tiền..."
                                           onchange="updateAmountReceivedChange(${index}, this)"
                                           oninput="updateAmountReceivedInput(${index}, this)">
                                    <span class="input-group-text">₫</span>
                                </div>
                                <div class="split-part-error" id="cashError_${index}"></div>
                            </div>
                        ` : ''}
                    </div>
                    ${part.paymentMethod === 'Cash' && part.amountReceived && part.amountReceived > part.amount ? `
                        <div class="alert alert-info mt-2 mb-0 py-1">
                            <small>Tiền thối: ${formatCurrency(part.amountReceived - part.amount)}</small>
                        </div>
                    ` : ''}

                    ${part.paymentMethod === 'QRBankTransfer' ? `
                        <div class="qr-preview-section mt-2 d-none" id="qrPreview_${index}">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <small class="text-muted fw-bold">Mã QR thanh toán</small>
                                <button type="button" class="btn btn-sm btn-outline-danger" onclick="hideSplitPartQR(${index})">
                                    <i class="bi bi-x me-1"></i>Ẩn QR
                                </button>
                            </div>
                            <div class="qr-loading text-center d-none" id="qrLoading_${index}">
                                <div class="spinner-border spinner-border-sm text-primary" role="status">
                                    <span class="visually-hidden">Đang tạo...</span>
                                </div>
                                <small class="text-muted d-block mt-1">Đang tạo mã QR...</small>
                            </div>
                            <div class="qr-content d-none" id="qrContent_${index}">
                                <div class="text-center">
                                    <img id="qrImage_${index}" class="img-fluid mb-2" style="max-width: 150px; max-height: 150px;" alt="QR Code">
                                    <div class="qr-info">
                                        <small class="d-block fw-bold text-primary" id="qrAmount_${index}"></small>
                                        <small class="d-block text-muted" id="qrDescription_${index}"></small>
                                        <small class="d-block text-muted font-monospace" id="qrTransactionCode_${index}"></small>
                                        <button type="button" class="btn btn-sm btn-outline-primary mt-2" onclick="generateSplitPartQR(${index})">
                                            <i class="bi bi-arrow-clockwise me-1"></i>Tạo lại QR
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="mt-2" id="qrGenerateBtn_${index}" ${part.paymentMethod === 'QRBankTransfer' ? '' : 'style="display:none;"'}>
                            <button type="button" class="btn btn-sm btn-outline-primary w-100" onclick="showSplitPartQR(${index})">
                                <i class="bi bi-qr-code me-1"></i>Hiển thị mã QR thanh toán
                            </button>
                        </div>
                    ` : ''}

                    <div class="mt-2">
                        <input type="text"
                               class="form-control form-control-sm"
                               id="splitNotes_${index}"
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

    window.updateSplitPart = function (index, field, value, shouldRender = true) {
        if (splitParts[index]) {
            const oldPaymentMethod = splitParts[index].paymentMethod;
            splitParts[index][field] = value;

            // Special handling for payment method changes
            if (field === 'paymentMethod') {
                // Show/hide QR generate button
                const qrGenerateBtn = document.getElementById(`qrGenerateBtn_${index}`);
                if (qrGenerateBtn) {
                    qrGenerateBtn.style.display = value === 'QRBankTransfer' ? 'block' : 'none';
                }

                // Hide QR preview when switching away from QR
                if (value !== 'QRBankTransfer' && oldPaymentMethod === 'QRBankTransfer') {
                    hideSplitPartQR(index);
                }

                // Clear cash received when switching away from Cash
                if (value !== 'Cash' && oldPaymentMethod === 'Cash') {
                    splitParts[index].amountReceived = null;
                    clearSplitPartError(index, 'cash');
                }
            }

            // Update QR when amount changes for QR payment method
            if (field === 'amount' && splitParts[index].paymentMethod === 'QRBankTransfer') {
                // Only regenerate if QR is already visible
                const qrContent = document.getElementById(`qrContent_${index}`);
                if (qrContent && !qrContent.classList.contains('d-none')) {
                    setTimeout(() => generateSplitPartQR(index), 200);
                }
            }

            // Only re-render if explicitly requested and not updating amount/amountReceived during input
            if (shouldRender && field !== 'amount' && field !== 'amountReceived') {
                renderSplitParts();
            }
            validateSplitTotal();
        }
    };
    
    // Update amountReceived on input (real-time validation, no re-render)
    window.updateAmountReceivedInput = function (index, inputElement) {
        if (splitParts[index] && inputElement) {
            const newAmount = parseFloat(inputElement.value) || null;
            splitParts[index].amountReceived = newAmount;
            // Clear error when user starts typing
            clearSplitPartError(index, 'cash');
            // Only validate, don't re-render to avoid losing focus
            validateSplitTotal();
        }
    };
    
    // Update amountReceived on change (with re-render)
    window.updateAmountReceivedChange = function (index, inputElement) {
        if (splitParts[index] && inputElement) {
            const newAmount = parseFloat(inputElement.value) || null;
            splitParts[index].amountReceived = newAmount;
            // Clear error when user changes value
            clearSplitPartError(index, 'cash');
            validateSplitTotal();
            // Re-render to update UI (after user finishes editing)
            renderSplitParts();
        }
    };

    // Update split part amount on input (real-time validation, no re-render)
    window.updateSplitPartAmountInput = function (index, inputElement) {
        if (splitParts[index] && inputElement) {
            const newAmount = parseFloat(inputElement.value) || 0;
            if (newAmount >= 0) {
                splitParts[index].amount = newAmount;
                // Clear error when user starts typing
                clearSplitPartError(index, 'amount');
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
                // Clear error when user changes value
                clearSplitPartError(index, 'amount');
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

    window.confirmSplitBill = function () {
        const notes = document.getElementById('splitNotes').value;
        const confirmBtn = document.getElementById('confirmSplitBillBtn');
        
        try {
            // ✅ FIX: Validate splitParts có data không
            if (!splitParts || splitParts.length === 0) {
                showToast('Chưa có phần nào được chia. Vui lòng chọn cách chia hóa đơn.', 'error');
                return;
            }

            // ✅ FIX: Validate currentOrderData có total không
            const totalAmount = currentOrderData?.total || 0;
            if (totalAmount <= 0) {
                showToast('Tổng hóa đơn không hợp lệ. Vui lòng tải lại trang.', 'error');
                return;
            }

            // Clear all previous errors
            clearAllSplitPartErrors();

            // Validate từng phần: với tiền mặt cần nhập số tiền khách đưa >= số tiền phải trả
            let hasErrors = false;
            let firstErrorIndex = -1;

            splitParts.forEach((part, index) => {
                // ✅ FIX: Validate amount > 0
                if (!part.amount || part.amount <= 0) {
                    showSplitPartError(index, 'amount', 'Số tiền phải lớn hơn 0');
                    hasErrors = true;
                    if (firstErrorIndex === -1) firstErrorIndex = index;
                }

                if (part.paymentMethod === 'Cash') {
                    if (!part.amountReceived) {
                        showSplitPartError(index, 'cash', 'Vui lòng nhập số tiền khách đưa');
                        hasErrors = true;
                        if (firstErrorIndex === -1) firstErrorIndex = index;
                    } else if (part.amountReceived < part.amount) {
                        showSplitPartError(index, 'cash', `Số tiền khách đưa (${formatCurrency(part.amountReceived)}) nhỏ hơn số tiền cần thu (${formatCurrency(part.amount)})`);
                        hasErrors = true;
                        if (firstErrorIndex === -1) firstErrorIndex = index;
                    }
                }
            });

            if (hasErrors) {
                // Focus vào input đầu tiên có lỗi
                if (firstErrorIndex !== -1) {
                    const errorInput = document.getElementById(`cashReceived_${firstErrorIndex}`) || 
                                      document.getElementById(`splitPartAmount_${firstErrorIndex}`);
                    if (errorInput) {
                        errorInput.focus();
                        errorInput.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                }
                return;
            }

            // ✅ FIX: Đọc lại data từ DOM để đảm bảo sync (quan trọng cho custom split)
            const syncPartsFromDOM = () => {
                const partsListContainer = document.getElementById('splitPartsList');
                if (!partsListContainer) return;

                const partCards = partsListContainer.querySelectorAll('.card');
                partCards.forEach((card, index) => {
                    if (!splitParts[index]) return;

                    // Đọc payment method từ select trong card này
                    const paymentMethodSelect = card.querySelector(`#splitPaymentMethod_${index}`) || 
                                                card.querySelector('select[onchange*="paymentMethod"]');
                    if (paymentMethodSelect) {
                        splitParts[index].paymentMethod = paymentMethodSelect.value;
                    }

                    // Đọc amount từ input (nếu là custom split)
                    const amountInput = card.querySelector(`#splitPartAmount_${index}`);
                    if (amountInput) {
                        const amount = parseFloat(amountInput.value) || 0;
                        if (amount > 0) {
                            splitParts[index].amount = amount;
                        }
                    }

                    // Đọc amountReceived từ input (nếu là Cash)
                    const cashReceivedInput = card.querySelector(`#cashReceived_${index}`);
                    if (cashReceivedInput) {
                        const amountReceived = parseFloat(cashReceivedInput.value) || null;
                        splitParts[index].amountReceived = amountReceived;
                    }

                    // Đọc notes từ input trong card này
                    const notesInput = card.querySelector(`#splitNotes_${index}`) || 
                                      card.querySelector('input[onchange*="notes"]');
                    if (notesInput) {
                        splitParts[index].notes = notesInput.value || '';
                    }
                });
            };

            // Sync data từ DOM trước khi submit
            syncPartsFromDOM();

            // ✅ FIX: Validate tổng các phần = tổng hóa đơn (double check)
            const partsTotal = splitParts.reduce((sum, p) => sum + (p.amount || 0), 0);
            if (Math.abs(partsTotal - totalAmount) > 0.01) {
                showToast(`Tổng các phần (${formatCurrency(partsTotal)}) không khớp với tổng hóa đơn (${formatCurrency(totalAmount)}). Vui lòng kiểm tra lại.`, 'error');
                return;
            }

            // Disable button
            confirmBtn.disabled = true;
            confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang xử lý...';

            // Prepare data for form submission
            const partsData = splitParts.map(p => ({
                paymentMethod: p.paymentMethod,
                amount: p.amount,
                amountReceived: p.amountReceived,
                notes: p.notes || ''
            }));

            // ✅ FIX: Debug log để kiểm tra data
            console.log('[SplitBill] Submitting:', {
                orderId: currentOrderId,
                totalAmount: totalAmount,
                partsTotal: partsTotal,
                partsCount: partsData.length,
                partsData: partsData
            });

            // Fill form and submit
            document.getElementById('splitBillOrderId').value = currentOrderId;
            document.getElementById('splitBillPartsJson').value = JSON.stringify(partsData);
            document.getElementById('splitBillNotes').value = notes || '';

            // Set processing state for result modal
            sessionStorage.setItem('splitBillProcessing', 'true');
            sessionStorage.setItem('splitBillOrderId', currentOrderId);

            // Show loading modal
            showSplitBillLoadingModal();

            // Submit form
            document.getElementById('splitBillForm').submit();
        } catch (error) {
            console.error('Error processing split bill:', error);
            // Show system error in modal footer (not field-specific)
            const errorMessage = error.message || 'Lỗi khi chia hóa đơn. Vui lòng thử lại.';
            showToast(errorMessage, 'error');
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = '<i class="bi bi-check-circle me-1"></i>Xác nhận chia hóa đơn';
        }
    };

    function showSplitBillLoadingModal() {
        const modal = new bootstrap.Modal(document.getElementById('splitBillLoadingModal'), {
            backdrop: 'static',
            keyboard: false
        });
        modal.show();
    }

    function hideSplitBillLoadingModal() {
        const modal = bootstrap.Modal.getInstance(document.getElementById('splitBillLoadingModal'));
        if (modal) {
            modal.hide();
        }
    }

    function showSplitBillResultModal(success, message, redirectUrl = null) {
        const resultModal = document.getElementById('splitBillResultModal');
        const resultIcon = document.getElementById('splitBillResultIcon');
        const resultTitle = document.getElementById('splitBillResultTitle');
        const resultMessage = document.getElementById('splitBillResultMessage');
        const resultBtn = document.getElementById('splitBillResultBtn');

        // Set result content
        if (success) {
            resultIcon.innerHTML = '<i class="bi bi-check-circle-fill text-success" style="font-size: 3rem;"></i>';
            resultTitle.textContent = 'Thành công';
            resultBtn.textContent = 'Xem hóa đơn';
            resultBtn.className = 'btn btn-success';
            resultBtn.onclick = function() {
                if (redirectUrl) {
                    window.location.href = redirectUrl;
                } else {
                    window.location.href = `/cashier-flow/receipt/${currentOrderId}`;
                }
            };
        } else {
            resultIcon.innerHTML = '<i class="bi bi-x-circle-fill text-danger" style="font-size: 3rem;"></i>';
            resultTitle.textContent = 'Thất bại';
            resultBtn.textContent = 'Đóng';
            resultBtn.className = 'btn btn-secondary';
            resultBtn.onclick = function() {
                closeSplitBillResultModal();
            };
        }

        resultMessage.textContent = message || '';

        const modal = new bootstrap.Modal(resultModal);
        modal.show();
    }

    window.closeSplitBillResultModal = function() {
        const modal = bootstrap.Modal.getInstance(document.getElementById('splitBillResultModal'));
        if (modal) {
            modal.hide();
        }
        // Clear processing state
        sessionStorage.removeItem('splitBillProcessing');
        sessionStorage.removeItem('splitBillOrderId');

        // Re-enable confirm button if split bill modal is still open
        const splitBillModal = bootstrap.Modal.getInstance(document.getElementById('splitBillModal'));
        if (splitBillModal) {
            const confirmBtn = document.getElementById('confirmSplitBillBtn');
            if (confirmBtn) {
                confirmBtn.disabled = false;
                confirmBtn.innerHTML = '<i class="bi bi-check-circle me-1"></i>Xác nhận chia hóa đơn';
            }
        }
    };

    // Initialize error message close functionality
    document.addEventListener('DOMContentLoaded', function() {
        const errorContainer = document.getElementById('modal-error-message');
        if (errorContainer) {
            const closeBtn = errorContainer.querySelector('.btn-close');
            if (closeBtn) {
                closeBtn.addEventListener('click', hideErrorMessage);
            }

            // Also handle Bootstrap alert dismiss
            const alert = errorContainer.querySelector('.alert');
            if (alert && typeof bootstrap !== 'undefined') {
                alert.addEventListener('closed.bs.alert', function() {
                    errorContainer.classList.add('d-none');
                });
            }
        }

        // Check for split bill processing result
        checkSplitBillResult();
    });

    function checkSplitBillResult() {
        // Check if we just processed a split bill
        const wasProcessing = sessionStorage.getItem('splitBillProcessing');
        if (wasProcessing === 'true') {
            // Clear processing state
            sessionStorage.removeItem('splitBillProcessing');

            // Check for success/error messages from TempData
            const successMessage = getTempDataMessage('SuccessMessage');
            const errorMessage = getTempDataMessage('ErrorMessage');

            if (successMessage) {
                // Find redirect URL from success message or construct it
                const orderId = sessionStorage.getItem('splitBillOrderId') || currentOrderId;
                const redirectUrl = orderId ? `/cashier-flow/receipt/${orderId}` : null;
                showSplitBillResultModal(true, successMessage, redirectUrl);
            } else if (errorMessage) {
                showSplitBillResultModal(false, errorMessage);
            }

            // Clear stored orderId
            sessionStorage.removeItem('splitBillOrderId');
        }
    }

    function getTempDataMessage(key) {
        // Try to get TempData messages from various sources
        // This is a simplified approach - in reality, TempData is server-side only
        // We'll check for success/error indicators in the page
        const alerts = document.querySelectorAll('.alert');
        for (let alert of alerts) {
            if (key === 'SuccessMessage' && alert.classList.contains('alert-success')) {
                return alert.textContent.trim();
            }
            if (key === 'ErrorMessage' && alert.classList.contains('alert-danger')) {
                return alert.textContent.trim();
            }
        }
        return null;
    }

    // Export
    window.SplitBill = {
        openSplitBill,
        changeSplitType,
        calculateEqualSplit,
        renderCustomSplit,
        updateSplitPart,
        updateAmountReceivedInput,
        updateAmountReceivedChange,
        addSplitPart,
        increaseParts,
        decreaseParts,
        confirmSplitBill,
        showSplitPartQR,
        hideSplitPartQR,
        generateSplitPartQR
    };
})();


