/**
 * Combined Payment Module
 * Handles combined cash + QR payment functionality
 */

(function() {
    'use strict';

    // Combined Payment Module
    window.CombinedPayment = {
        // Initialize combined payment functionality
        init: function() {
            // ✅ FIX: Đảm bảo validation được ẩn khi trang load
            const validation = document.getElementById('combinedTotalValidation');
            if (validation) {
                validation.classList.add('d-none');
            }
            
            this.bindEvents();
            this.checkPaymentResult();
        },

        // Bind combined payment events
        bindEvents: function() {
            // Bind combined payment modal open
            const combinedPaymentBtn = document.querySelector('[onclick="openCombinedPaymentModal()"]');
            if (combinedPaymentBtn) {
                combinedPaymentBtn.onclick = () => this.openCombinedPaymentModal();
            }

            // ✅ FIX: Không bind input events ở đây để tránh validation chạy khi trang load
            // Input events sẽ được bind trong openCombinedPaymentModal() khi modal mở

            // Bind generate QR button (có thể bind sớm vì không trigger validation)
            const generateQrBtn = document.getElementById('generateQrBtn');
            if (generateQrBtn) {
                generateQrBtn.onclick = () => this.generateCombinedQR();
            }

            // Bind confirm combined payment (có thể bind sớm vì không trigger validation)
            const confirmBtn = document.getElementById('confirmCombinedPaymentBtn');
            if (confirmBtn) {
                confirmBtn.onclick = () => this.confirmCombinedPayment();
            }
        },

        // Open combined payment modal
        openCombinedPaymentModal: function() {
            if (!window.PaymentCore || !window.PaymentCore.orderContext) {
                console.error('PaymentCore not initialized');
                return;
            }

            window.PaymentCore.populateOrderSummary('combined');
            const total = parseFloat(window.PaymentCore.orderContext.Total || 0);
            const halfAmount = Math.round(total / 2);

            // Reset values
            document.getElementById('cashAmount').value = halfAmount;
            document.getElementById('cashReceived').value = '';
            document.getElementById('cashChangeDisplay').classList.add('d-none');
            document.getElementById('combinedTotalValidation').classList.add('d-none'); // ✅ Đảm bảo ẩn validation
            document.getElementById('combinedErrorMessages').innerHTML = '';

            // Reset QR display
            document.getElementById('combinedQrLoading').classList.add('d-none');
            document.getElementById('combinedQrContent').classList.add('d-none');
            document.getElementById('generateQrBtn').disabled = false;

            // ✅ FIX: Bind input events ở đây khi modal mở (không bind khi trang load)
            // Sử dụng one-time event hoặc kiểm tra xem đã bind chưa
            const cashAmountInput = document.getElementById('cashAmount');
            const cashReceivedInput = document.getElementById('cashReceived');
            
            if (cashAmountInput && !cashAmountInput.hasAttribute('data-bound')) {
                cashAmountInput.setAttribute('data-bound', 'true');
                cashAmountInput.addEventListener('input', () => this.updateCombinedPayment());
            }
            
            if (cashReceivedInput && !cashReceivedInput.hasAttribute('data-bound')) {
                cashReceivedInput.setAttribute('data-bound', 'true');
                cashReceivedInput.addEventListener('input', () => this.updateCombinedPayment());
            }

            // Calculate QR amount automatically
            this.updateCombinedPayment();

            if (window.PaymentCore.modals.combinedModal) {
                window.PaymentCore.modals.combinedModal.show();
            }
        },

        // Update combined payment calculations
        updateCombinedPayment: function() {
            const cashAmountInput = document.getElementById('cashAmount');
            const cashReceivedInput = document.getElementById('cashReceived');

            // Get values and ensure they are valid numbers
            let cashAmount = parseFloat(cashAmountInput.value || 0);
            const cashReceived = parseFloat(cashReceivedInput.value || 0);
            const total = parseFloat(window.PaymentCore.orderContext.Total || 0);

            // Ensure cashAmount doesn't exceed total
            if (cashAmount > total) {
                cashAmount = total;
                cashAmountInput.value = total.toFixed(0);
            }

            // Ensure cashAmount is not negative
            if (cashAmount < 0) {
                cashAmount = 0;
                cashAmountInput.value = '0';
            }

            // Calculate QR amount automatically (total - cashAmount)
            const remaining = total - cashAmount;
            const qrAmount = remaining > 0 ? remaining : 0;

            // Update change display
            const changeDisplay = document.getElementById('cashChangeDisplay');
            const changeAmount = document.getElementById('cashChangeAmount');

            if (cashReceived > cashAmount && cashAmount > 0) {
                changeDisplay.classList.remove('d-none');
                changeAmount.textContent = window.PaymentCore.formatCurrency(cashReceived - cashAmount);
            } else {
                changeDisplay.classList.add('d-none');
            }

            // Update QR amount if QR is already displayed
            const qrContent = document.getElementById('combinedQrContent');
            if (qrContent && !qrContent.classList.contains('d-none')) {
                document.getElementById('combinedQrAmount').textContent = window.PaymentCore.formatCurrency(qrAmount);
            }

            this.validateCombinedPayment();
        },

        // Validate combined payment
        validateCombinedPayment: function() {
            // ✅ FIX: Chỉ validate khi modal đang hiển thị
            const modal = document.getElementById('combinedPaymentModal');
            if (!modal) {
                return; // Không validate nếu không tìm thấy modal
            }
            
            // Kiểm tra xem modal có đang được hiển thị không (Bootstrap modal)
            // Bootstrap 5 sử dụng _element và _isShown, Bootstrap 4 có thể khác
            const modalInstance = bootstrap.Modal.getInstance(modal);
            const isModalShown = modalInstance && (
                modalInstance._isShown || 
                modal.classList.contains('show') || 
                !modal.classList.contains('d-none')
            );
            
            if (!isModalShown) {
                // Đảm bảo validation được ẩn nếu modal chưa mở
                const validation = document.getElementById('combinedTotalValidation');
                if (validation) {
                    validation.classList.add('d-none');
                }
                return; // Không validate nếu modal chưa được show
            }

            const cashAmount = parseFloat(document.getElementById('cashAmount').value || 0);
            const cashReceived = parseFloat(document.getElementById('cashReceived').value || 0);
            const total = parseFloat(window.PaymentCore?.orderContext?.Total || 0);

            // ✅ FIX: Nếu total = 0 hoặc PaymentCore chưa init, không validate
            if (!window.PaymentCore || !window.PaymentCore.orderContext || !total || total <= 0) {
                const validation = document.getElementById('combinedTotalValidation');
                if (validation) {
                    validation.classList.add('d-none');
                }
                return;
            }

            // Calculate qrAmount from total - cashAmount (not from input)
            const qrAmount = total - cashAmount;

            const validation = document.getElementById('combinedTotalValidation');
            const partsTotal = document.getElementById('combinedPartsTotal');
            const billTotal = document.getElementById('combinedBillTotal');
            const errorContainer = document.getElementById('combinedErrorMessages');

            const sum = cashAmount + qrAmount;
            partsTotal.textContent = window.PaymentCore.formatCurrency(sum);
            billTotal.textContent = window.PaymentCore.formatCurrency(total);

            const errors = [];

            if (cashAmount < 0 || qrAmount < 0) {
                errors.push('Giá trị không được âm.');
            }

            if (cashAmount > 0 && cashReceived > 0 && cashReceived < cashAmount) {
                errors.push('Số tiền khách đưa nhỏ hơn phần thanh toán tiền mặt.');
            }

            validation.classList.toggle('d-none', sum === total);
            errorContainer.innerHTML = errors.map(err => `<div class="alert alert-warning mb-2">${err}</div>`).join('');

            document.getElementById('confirmCombinedPaymentBtn').disabled = (sum !== total) || errors.length > 0;
        },

        // Generate QR for combined payment
        generateCombinedQR: function() {
            document.getElementById('combinedQrLoading').classList.remove('d-none');
            document.getElementById('combinedQrContent').classList.add('d-none');
            document.getElementById('generateQrBtn').disabled = true;

            setTimeout(() => {
                document.getElementById('combinedQrLoading').classList.add('d-none');
                document.getElementById('combinedQrContent').classList.remove('d-none');

                const cashAmount = parseFloat(document.getElementById('cashAmount').value || 0);
                const total = parseFloat(window.PaymentCore.orderContext.Total || 0);
                const qrAmount = total - cashAmount;

                const orderCode = window.PaymentCore.orderContext.OrderCode;
                const transactionCode = `TXN-${Date.now()}`;
                const bank = "MB";
                const account = "0397604824";
                const addInfo = `RMS#${orderCode}`;

                const qrUrl = window.PaymentCore.generateVietQrUrl(bank, account, qrAmount, addInfo);

                document.getElementById('combinedQrAmount').textContent = window.PaymentCore.formatCurrency(qrAmount);
                document.getElementById('combinedQrDescription').textContent = addInfo;
                document.getElementById('combinedQrTransactionCode').textContent = transactionCode;
                document.getElementById('combinedQrImage').src = qrUrl;

                document.getElementById('generateQrBtn').disabled = false;
            }, 600);
        },

        // Confirm combined payment
        confirmCombinedPayment: function() {
            if (document.getElementById('confirmCombinedPaymentBtn').disabled) {
                window.PaymentCore.showToast('Vui lòng đảm bảo tổng hai phần khớp với tổng hóa đơn.', 'warning');
                return;
            }

            const cashAmount = parseFloat(document.getElementById('cashAmount').value || 0);
            const cashReceived = parseFloat(document.getElementById('cashReceived').value || 0) || null;
            const total = parseFloat(window.PaymentCore.orderContext.Total || 0);

            // Calculate qrAmount from total - cashAmount
            const qrAmount = total - cashAmount;
            const notes = document.getElementById('combinedNotes')?.value || '';

            if (cashAmount <= 0 || qrAmount <= 0) {
                window.PaymentCore.showToast('Vui lòng nhập số tiền hợp lệ cho cả hai phần.', 'warning');
                return;
            }

            const btn = document.getElementById('confirmCombinedPaymentBtn');
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...';

            try {
                // ✅ Kiểm tra xem có ReservationId không (Reservation-centric payment)
                const isReservationPayment = window.PaymentCore.orderContext?.IsReservationPayment === true;
                const reservationId = window.PaymentCore.orderContext?.ReservationId;
                
                if (isReservationPayment && reservationId) {
                    // Reservation payment: set ReservationId
                    const reservationIdInput = document.querySelector('#combinedPaymentForm input[name="ReservationId"]');
                    if (reservationIdInput) {
                        reservationIdInput.value = reservationId;
                    }
                } else {
                    // Order payment: set OrderId (backward compatible)
                    const orderIdInput = document.getElementById('combinedPaymentOrderId');
                    if (orderIdInput) {
                        orderIdInput.value = window.PaymentCore.orderContext.OrderId;
                    }
                }
                
                document.getElementById('combinedPaymentCashAmount').value = cashAmount;
                document.getElementById('combinedPaymentCashReceived').value = cashReceived ?? '';
                document.getElementById('combinedPaymentQrAmount').value = qrAmount;
                document.getElementById('combinedPaymentNotes').value = notes;

                // Set processing state for result modal
                sessionStorage.setItem('combinedPaymentProcessing', 'true');
                if (isReservationPayment && reservationId) {
                    sessionStorage.setItem('combinedPaymentReservationId', reservationId);
                } else {
                    sessionStorage.setItem('combinedPaymentOrderId', window.PaymentCore.orderContext.OrderId);
                }

                // Show loading modal
                this.showCombinedPaymentLoadingModal();

                document.getElementById('combinedPaymentForm').submit();
            } catch (error) {
                console.error('Error:', error);
                window.PaymentCore.showToast('Lỗi: ' + (error.message || 'Không thể xử lý thanh toán. Vui lòng thử lại.'), 'error');
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-check-circle me-1"></i>Xác nhận thanh toán';
            }
        },

        // Show combined payment loading modal
        showCombinedPaymentLoadingModal: function() {
            const modal = new bootstrap.Modal(document.getElementById('combinedPaymentLoadingModal'), {
                backdrop: 'static',
                keyboard: false
            });
            modal.show();
        },

        // Hide combined payment loading modal
        hideCombinedPaymentLoadingModal: function() {
            const modal = bootstrap.Modal.getInstance(document.getElementById('combinedPaymentLoadingModal'));
            if (modal) {
                modal.hide();
            }
        },

        // Show combined payment result modal
        showCombinedPaymentResultModal: function(success, message, redirectUrl = null) {
            const resultModal = document.getElementById('combinedPaymentResultModal');
            const resultIcon = document.getElementById('combinedPaymentResultIcon');
            const resultTitle = document.getElementById('combinedPaymentResultTitle');
            const resultMessage = document.getElementById('combinedPaymentResultMessage');
            const resultBtn = document.getElementById('combinedPaymentResultBtn');

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
                        const orderId = sessionStorage.getItem('combinedPaymentOrderId');
                        window.location.href = orderId ? `/cashier-flow/receipt/${orderId}` : '/cashier-flow/orders';
                    }
                };
            } else {
                resultIcon.innerHTML = '<i class="bi bi-x-circle-fill text-danger" style="font-size: 3rem;"></i>';
                resultTitle.textContent = 'Thất bại';
                resultBtn.textContent = 'Đóng';
                resultBtn.className = 'btn btn-secondary';
                resultBtn.onclick = function() {
                    closeCombinedPaymentResultModal();
                };
            }

            resultMessage.textContent = message || '';

            const modal = new bootstrap.Modal(resultModal);
            modal.show();
        },

        // Check for combined payment result on page load
        checkPaymentResult: function() {
            // Check if we just processed a combined payment
            const wasProcessing = sessionStorage.getItem('combinedPaymentProcessing');
            if (wasProcessing === 'true') {
                // Clear processing state
                sessionStorage.removeItem('combinedPaymentProcessing');

                // Check for success/error messages from TempData
                const successMessage = window.PaymentCore.getTempDataMessage('SuccessMessage');
                const errorMessage = window.PaymentCore.getTempDataMessage('ErrorMessage');

                if (successMessage) {
                    // Find redirect URL from success message or construct it
                    const orderId = sessionStorage.getItem('combinedPaymentOrderId');
                    const redirectUrl = orderId ? `/cashier-flow/receipt/${orderId}` : null;
                    this.showCombinedPaymentResultModal(true, successMessage, redirectUrl);
                } else if (errorMessage) {
                    this.showCombinedPaymentResultModal(false, errorMessage);
                }

                // Clear stored orderId
                sessionStorage.removeItem('combinedPaymentOrderId');
            }
        }
    };

    // Global function for closing combined payment result modal
    window.closeCombinedPaymentResultModal = function() {
        const modal = bootstrap.Modal.getInstance(document.getElementById('combinedPaymentResultModal'));
        if (modal) {
            modal.hide();
        }
        // Clear processing state
        sessionStorage.removeItem('combinedPaymentProcessing');
        sessionStorage.removeItem('combinedPaymentOrderId');
    };

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        window.CombinedPayment.init();
    });

})();
