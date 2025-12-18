/**
 * QR Payment Module
 * Handles all QR/e-wallet payment related functionality
 */

(function() {
    'use strict';

    // QR Payment Module
    window.QrPayment = {
        // Initialize QR payment functionality
        init: function() {
            this.bindEvents();
            this.checkPaymentResult();
        },

        // Bind QR payment events
        bindEvents: function() {
            // Bind QR payment modal open
            const qrPaymentBtn = document.querySelector('[onclick="openQrPaymentModal()"]');
            if (qrPaymentBtn) {
                qrPaymentBtn.onclick = () => this.openQrPaymentModal();
            }

            // Bind confirm QR payment
            const confirmBtn = document.getElementById('confirmQRPaymentBtn');
            if (confirmBtn) {
                confirmBtn.onclick = () => this.confirmQRPayment();
            }

            // Bind retry QR load
            const retryBtn = document.querySelector('[onclick="retryLoadQR()"]');
            if (retryBtn) {
                retryBtn.onclick = () => this.retryLoadQR();
            }
        },

        // Open QR payment modal
        openQrPaymentModal: function() {
            if (!window.PaymentCore || !window.PaymentCore.orderContext) {
                console.error('PaymentCore not initialized');
                return;
            }

            // Reset modal state
            document.getElementById('qrLoading').classList.remove('d-none');
            document.getElementById('qrContent').classList.add('d-none');
            document.getElementById('qrError').classList.add('d-none');
            document.getElementById('confirmQRPaymentBtn').classList.add('d-none');

            if (window.PaymentCore.modals.qrModal) {
                window.PaymentCore.modals.qrModal.show();
            }

            setTimeout(() => this.loadQrPreview(), 600);
        },

        // Load QR preview
        loadQrPreview: function() {
            const amount = window.PaymentCore.orderContext.Total;
            const orderCode = window.PaymentCore.orderContext.OrderCode;
            const transactionCode = `TXN-${Date.now()}`;

            const bank = "MB"; // MBBank
            const account = "0397604824";
            const addInfo = `RMS#${orderCode}`;

            // Generate QR URL
            const qrUrl = window.PaymentCore.generateVietQrUrl(bank, account, amount, addInfo);

            // Update UI
            document.getElementById('qrLoading').classList.add('d-none');
            document.getElementById('qrContent').classList.remove('d-none');
            document.getElementById('qrAmount').textContent = window.PaymentCore.formatCurrency(amount);
            document.getElementById('qrDescription').textContent = addInfo;
            document.getElementById('qrTransactionCode').textContent = transactionCode;
            document.getElementById('qrImage').src = qrUrl;

            document.getElementById('confirmQRPaymentBtn').classList.remove('d-none');
        },

        // Retry loading QR
        retryLoadQR: function() {
            this.openQrPaymentModal();
        },

        // Confirm QR payment
        confirmQRPayment: function() {
            const btn = document.getElementById('confirmQRPaymentBtn');
            const original = btn ? btn.innerHTML : '';
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang xác nhận...';
            }

            try {
                // Submit form to controller (not using fetch)
                document.getElementById('qrConfirmOrderId').value = window.PaymentCore.orderContext.OrderId;
                document.getElementById('qrConfirmNotes').value = 'Thu ngân xác nhận đã nhận tiền qua QR';

                // Set processing state for result modal
                sessionStorage.setItem('qrPaymentProcessing', 'true');
                sessionStorage.setItem('qrPaymentOrderId', window.PaymentCore.orderContext.OrderId);

                // Show loading modal
                this.showQrPaymentLoadingModal();

                document.getElementById('qrConfirmForm').submit();
            } catch (error) {
                console.error('[ERROR] confirmQRPayment:', error);
                window.PaymentCore.showToast(error.message || 'Đã xảy ra lỗi khi xác nhận thanh toán QR.', 'error');
                if (btn) {
                    btn.disabled = false;
                    btn.innerHTML = original;
                }
            }
        },

        // Show QR payment loading modal
        showQrPaymentLoadingModal: function() {
            const modal = new bootstrap.Modal(document.getElementById('qrPaymentLoadingModal'), {
                backdrop: 'static',
                keyboard: false
            });
            modal.show();
        },

        // Hide QR payment loading modal
        hideQrPaymentLoadingModal: function() {
            const modal = bootstrap.Modal.getInstance(document.getElementById('qrPaymentLoadingModal'));
            if (modal) {
                modal.hide();
            }
        },

        // Show QR payment result modal
        showQrPaymentResultModal: function(success, message, redirectUrl = null) {
            const resultModal = document.getElementById('qrPaymentResultModal');
            const resultIcon = document.getElementById('qrPaymentResultIcon');
            const resultTitle = document.getElementById('qrPaymentResultTitle');
            const resultMessage = document.getElementById('qrPaymentResultMessage');
            const resultBtn = document.getElementById('qrPaymentResultBtn');

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
                        const orderId = sessionStorage.getItem('qrPaymentOrderId');
                        window.location.href = orderId ? `/cashier-flow/receipt/${orderId}` : '/cashier-flow/orders';
                    }
                };
            } else {
                resultIcon.innerHTML = '<i class="bi bi-x-circle-fill text-danger" style="font-size: 3rem;"></i>';
                resultTitle.textContent = 'Thất bại';
                resultBtn.textContent = 'Đóng';
                resultBtn.className = 'btn btn-secondary';
                resultBtn.onclick = function() {
                    closeQrPaymentResultModal();
                };
            }

            resultMessage.textContent = message || '';

            const modal = new bootstrap.Modal(resultModal);
            modal.show();
        },

        // Check for QR payment result on page load
        checkPaymentResult: function() {
            // Check if we just processed a QR payment
            const wasProcessing = sessionStorage.getItem('qrPaymentProcessing');
            if (wasProcessing === 'true') {
                // Clear processing state
                sessionStorage.removeItem('qrPaymentProcessing');

                // Check for success/error messages from TempData
                const successMessage = window.PaymentCore.getTempDataMessage('SuccessMessage');
                const errorMessage = window.PaymentCore.getTempDataMessage('ErrorMessage');

                if (successMessage) {
                    // Find redirect URL from success message or construct it
                    const orderId = sessionStorage.getItem('qrPaymentOrderId');
                    const redirectUrl = orderId ? `/cashier-flow/receipt/${orderId}` : null;
                    this.showQrPaymentResultModal(true, successMessage, redirectUrl);
                } else if (errorMessage) {
                    this.showQrPaymentResultModal(false, errorMessage);
                }

                // Clear stored orderId
                sessionStorage.removeItem('qrPaymentOrderId');
            }
        }
    };

    // Global function for closing QR payment result modal
    window.closeQrPaymentResultModal = function() {
        const modal = bootstrap.Modal.getInstance(document.getElementById('qrPaymentResultModal'));
        if (modal) {
            modal.hide();
        }
        // Clear processing state
        sessionStorage.removeItem('qrPaymentProcessing');
        sessionStorage.removeItem('qrPaymentOrderId');
    };

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        window.QrPayment.init();
    });

})();
