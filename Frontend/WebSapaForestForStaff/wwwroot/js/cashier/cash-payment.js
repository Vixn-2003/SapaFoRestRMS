/**
 * Cash Payment Module
 * Handles all cash payment related functionality
 */

(function() {
    'use strict';

    // Cash Payment Module
    window.CashPayment = {
        // Initialize cash payment functionality
        init: function() {
            this.bindEvents();
            this.checkPaymentResult();
        },

        // Bind cash payment events
        bindEvents: function() {
            // Bind cash payment modal open
            const cashPaymentBtn = document.querySelector('[onclick="openCashPaymentModal()"]');
            if (cashPaymentBtn) {
                cashPaymentBtn.onclick = () => this.openCashPaymentModal();
            }

            // Bind amount received input change
            const amountReceivedInput = document.getElementById('amountReceived');
            if (amountReceivedInput) {
                amountReceivedInput.addEventListener('input', () => this.calculateCashChange());
            }

            // Bind confirm cash payment
            const confirmBtn = document.getElementById('confirmCashPaymentBtn');
            if (confirmBtn) {
                confirmBtn.onclick = () => this.confirmCashPayment();
            }

            // Bind refund confirmation checkbox
            const refundConfirmed = document.getElementById('refundConfirmed');
            if (refundConfirmed) {
                refundConfirmed.addEventListener('change', () => this.toggleConfirmButton());
            }
        },

        // Open cash payment modal
        openCashPaymentModal: function() {
            if (!window.PaymentCore || !window.PaymentCore.orderContext) {
                console.error('PaymentCore not initialized');
                return;
            }

            window.PaymentCore.populateOrderSummary('cash');
            const total = parseFloat(window.PaymentCore.orderContext.Total ?? 0);
            const depositRefund = parseFloat(window.PaymentCore.orderContext.DepositRefundAmount ?? 0);

            // Handle deposit refund case
            if (total === 0 && depositRefund > 0) {
                // Already paid by deposit, just need to refund excess
                document.getElementById('amountReceived').value = '0';
                document.getElementById('amountReceived').disabled = true;
                document.getElementById('changeDisplay').classList.remove('d-none');
                document.getElementById('refundAmount').textContent = window.PaymentCore.formatCurrency(depositRefund);
                document.getElementById('refundAmount').parentElement.innerHTML = `
                    <i class="bi bi-arrow-left-right me-2 fs-4 text-warning"></i>
                    <div>
                        <strong class="text-warning">Tiền cần trả lại cho khách (từ tiền cọc):</strong>
                        <span class="fs-4 ms-2 text-warning fw-bold" id="refundAmount">${window.PaymentCore.formatCurrency(depositRefund)}</span>
                    </div>
                `;
                document.getElementById('underpaidWarning').classList.add('d-none');
                document.getElementById('refundConfirmation').classList.remove('d-none');
                document.getElementById('refundConfirmed').checked = false;
                document.getElementById('confirmCashPaymentBtn').disabled = true;
            } else {
                // Normal payment case
                document.getElementById('amountReceived').value = '';
                document.getElementById('amountReceived').disabled = false;
                document.getElementById('changeDisplay').classList.add('d-none');
                document.getElementById('underpaidWarning').classList.add('d-none');
                document.getElementById('refundConfirmation').classList.add('d-none');
                document.getElementById('refundConfirmed').checked = false;
                document.getElementById('confirmCashPaymentBtn').disabled = true;
            }

            if (window.PaymentCore.modals.cashModal) {
                window.PaymentCore.modals.cashModal.show();
            }
        },

        // Calculate cash change
        calculateCashChange: function() {
            const receivedInput = document.getElementById('amountReceived');
            const received = parseFloat(receivedInput.value ?? 0);
            const total = parseFloat(window.PaymentCore.orderContext.Total ?? 0);

            document.getElementById('receivedAmount').textContent = window.PaymentCore.formatCurrency(received);
            document.getElementById('requiredAmount').textContent = window.PaymentCore.formatCurrency(total);

            const changeWrapper = document.getElementById('changeDisplay');
            const refundAmount = document.getElementById('refundAmount');
            const warning = document.getElementById('underpaidWarning');
            const missingAmount = document.getElementById('missingAmount');
            const refundConfirmGroup = document.getElementById('refundConfirmation');

            if (!receivedInput.value) {
                changeWrapper.classList.add('d-none');
                warning.classList.add('d-none');
                document.getElementById('confirmCashPaymentBtn').disabled = true;
                return;
            }

            if (received < total) {
                warning.classList.remove('d-none');
                changeWrapper.classList.add('d-none');
                missingAmount.textContent = window.PaymentCore.formatCurrency(total - received);
                document.getElementById('confirmCashPaymentBtn').disabled = true;
                return;
            }

            warning.classList.add('d-none');
            const change = received - total;
            refundAmount.textContent = window.PaymentCore.formatCurrency(change);
            changeWrapper.classList.toggle('d-none', change === 0);
            refundConfirmGroup.classList.toggle('d-none', change === 0);

            document.getElementById('confirmCashPaymentBtn').disabled = change > 0 && !document.getElementById('refundConfirmed').checked;
            if (change === 0) {
                document.getElementById('confirmCashPaymentBtn').disabled = false;
            }
        },

        // Toggle confirm button based on refund confirmation
        toggleConfirmButton: function() {
            const changeWrapper = document.getElementById('changeDisplay');
            if (changeWrapper.classList.contains('d-none')) {
                document.getElementById('confirmCashPaymentBtn').disabled = false;
            } else {
                document.getElementById('confirmCashPaymentBtn').disabled = !document.getElementById('refundConfirmed').checked;
            }
        },

        // Confirm cash payment
        confirmCashPayment: function() {
            console.log('[DEBUG] confirmCashPayment() called');

            try {
                const total = parseFloat(window.PaymentCore.orderContext.Total ?? 0);
                const depositRefund = parseFloat(window.PaymentCore.orderContext.DepositRefundAmount ?? 0);
                const orderId = window.PaymentCore.orderContext.OrderId;

                console.log('[DEBUG] OrderId:', orderId, 'Total:', total, 'DepositRefund:', depositRefund);

                // Handle deposit refund case
                if (total === 0 && depositRefund > 0) {
                    if (!document.getElementById('refundConfirmed').checked) {
                        console.log('[DEBUG] Refund not confirmed');
                        window.PaymentCore.showToast('Vui lòng xác nhận đã trả lại tiền cho khách hàng.', 'warning');
                        return;
                    }

                    // Submit form with AmountReceived = 0
                    console.log('[DEBUG] Submitting form with AmountReceived = 0');
                    
                    // ✅ Kiểm tra xem có ReservationId không (Reservation-centric payment)
                    const isReservationPayment = window.PaymentCore.orderContext?.IsReservationPayment === true;
                    const reservationId = window.PaymentCore.orderContext?.ReservationId;
                    
                    if (isReservationPayment && reservationId) {
                        // Reservation payment: set ReservationId
                        const reservationIdInput = document.querySelector('#cashPaymentForm input[name="ReservationId"]');
                        if (reservationIdInput) {
                            reservationIdInput.value = reservationId;
                        }
                    } else {
                        // Order payment: set OrderId (backward compatible)
                        const orderIdInput = document.getElementById('cashPaymentOrderId');
                        if (orderIdInput) {
                            orderIdInput.value = orderId;
                        }
                    }
                    
                    document.getElementById('cashPaymentAmountReceived').value = '0';
                    document.getElementById('cashPaymentNotes').value = `Đã thanh toán đủ bằng tiền cọc. Trả lại tiền thừa: ${depositRefund.toLocaleString('vi-VN')} ₫`;

                    const form = document.getElementById('cashPaymentForm');
                    console.log('[DEBUG] Form element:', form);
                    console.log('[DEBUG] Form action:', form?.action);
                    console.log('[DEBUG] Form method:', form?.method);
                    console.log('[DEBUG] Form values:', {
                        OrderId: document.getElementById('cashPaymentOrderId')?.value,
                        ReservationId: document.querySelector('#cashPaymentForm input[name="ReservationId"]')?.value,
                        AmountReceived: document.getElementById('cashPaymentAmountReceived').value,
                        Notes: document.getElementById('cashPaymentNotes').value
                    });

                    // Set processing state for result modal
                    sessionStorage.setItem('cashPaymentProcessing', 'true');
                    if (isReservationPayment && reservationId) {
                        sessionStorage.setItem('cashPaymentReservationId', reservationId);
                    } else {
                        sessionStorage.setItem('cashPaymentOrderId', orderId);
                    }

                    // Show loading modal
                    this.showCashPaymentLoadingModal();

                    form.submit();
                    console.log('[DEBUG] Form submitted');
                } else {
                    // Normal payment case
                    const received = parseFloat(document.getElementById('amountReceived').value ?? 0);
                    console.log('[DEBUG] Amount received:', received);

                    if (!received || received < total) {
                        console.log('[DEBUG] Invalid amount received');
                        window.PaymentCore.showToast('Vui lòng nhập số tiền hợp lệ.', 'warning');
                        return;
                    }

                    if (received > total && !document.getElementById('refundConfirmed').checked) {
                        console.log('[DEBUG] Refund required but not confirmed');
                        window.PaymentCore.showToast('Vui lòng xác nhận đã trả lại tiền thối cho khách hàng.', 'warning');
                        return;
                    }

                    // Submit form with AmountReceived
                    console.log('[DEBUG] Submitting form with AmountReceived =', received);
                    
                    // ✅ Kiểm tra xem có ReservationId không (Reservation-centric payment)
                    const isReservationPayment = window.PaymentCore.orderContext?.IsReservationPayment === true;
                    const reservationId = window.PaymentCore.orderContext?.ReservationId;
                    
                    if (isReservationPayment && reservationId) {
                        // Reservation payment: set ReservationId
                        const reservationIdInput = document.querySelector('#cashPaymentForm input[name="ReservationId"]');
                        if (reservationIdInput) {
                            reservationIdInput.value = reservationId;
                        }
                    } else {
                        // Order payment: set OrderId (backward compatible)
                        const orderIdInput = document.getElementById('cashPaymentOrderId');
                        if (orderIdInput) {
                            orderIdInput.value = orderId;
                        }
                    }
                    
                    document.getElementById('cashPaymentAmountReceived').value = received;
                    document.getElementById('cashPaymentNotes').value = document.getElementById('paymentNotes')?.value || '';

                    const form = document.getElementById('cashPaymentForm');
                    console.log('[DEBUG] Form element:', form);
                    console.log('[DEBUG] Form action:', form?.action);
                    console.log('[DEBUG] Form method:', form?.method);
                    console.log('[DEBUG] Form values:', {
                        OrderId: document.getElementById('cashPaymentOrderId')?.value,
                        ReservationId: document.querySelector('#cashPaymentForm input[name="ReservationId"]')?.value,
                        AmountReceived: document.getElementById('cashPaymentAmountReceived').value,
                        Notes: document.getElementById('cashPaymentNotes').value
                    });

                    // Set processing state for result modal
                    sessionStorage.setItem('cashPaymentProcessing', 'true');
                    if (isReservationPayment && reservationId) {
                        sessionStorage.setItem('cashPaymentReservationId', reservationId);
                    } else {
                        sessionStorage.setItem('cashPaymentOrderId', orderId);
                    }

                    // Show loading modal
                    this.showCashPaymentLoadingModal();

                    form.submit();
                    console.log('[DEBUG] Form submitted');
                }
            } catch (error) {
                console.error('[ERROR] Error in confirmCashPayment:', error);
                window.PaymentCore.showToast('Có lỗi xảy ra khi xử lý thanh toán. Vui lòng thử lại.', 'error');
            }
        },

        // Show cash payment loading modal
        showCashPaymentLoadingModal: function() {
            const modal = new bootstrap.Modal(document.getElementById('cashPaymentLoadingModal'), {
                backdrop: 'static',
                keyboard: false
            });
            modal.show();
        },

        // Hide cash payment loading modal
        hideCashPaymentLoadingModal: function() {
            const modal = bootstrap.Modal.getInstance(document.getElementById('cashPaymentLoadingModal'));
            if (modal) {
                modal.hide();
            }
        },

        // Show cash payment result modal
        showCashPaymentResultModal: function(success, message, redirectUrl = null) {
            const resultModal = document.getElementById('cashPaymentResultModal');
            const resultIcon = document.getElementById('cashPaymentResultIcon');
            const resultTitle = document.getElementById('cashPaymentResultTitle');
            const resultMessage = document.getElementById('cashPaymentResultMessage');
            const resultBtn = document.getElementById('cashPaymentResultBtn');

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
                        const orderId = sessionStorage.getItem('cashPaymentOrderId');
                        window.location.href = orderId ? `/cashier-flow/receipt/${orderId}` : '/cashier-flow/orders';
                    }
                };
            } else {
                resultIcon.innerHTML = '<i class="bi bi-x-circle-fill text-danger" style="font-size: 3rem;"></i>';
                resultTitle.textContent = 'Thất bại';
                resultBtn.textContent = 'Đóng';
                resultBtn.className = 'btn btn-secondary';
                resultBtn.onclick = function() {
                    closeCashPaymentResultModal();
                };
            }

            resultMessage.textContent = message || '';

            const modal = new bootstrap.Modal(resultModal);
            modal.show();
        },

        // Check for cash payment result on page load
        checkPaymentResult: function() {
            // Check if we just processed a cash payment
            const wasProcessing = sessionStorage.getItem('cashPaymentProcessing');
            if (wasProcessing === 'true') {
                // Clear processing state
                sessionStorage.removeItem('cashPaymentProcessing');

                // Check for success/error messages from TempData
                const successMessage = window.PaymentCore.getTempDataMessage('SuccessMessage');
                const errorMessage = window.PaymentCore.getTempDataMessage('ErrorMessage');

                if (successMessage) {
                    // ✅ Kiểm tra xem có ReservationId không (Reservation-centric payment)
                    const reservationId = sessionStorage.getItem('cashPaymentReservationId');
                    const orderId = sessionStorage.getItem('cashPaymentOrderId');
                    
                    let redirectUrl = null;
                    if (reservationId) {
                        // Reservation payment: redirect đến receipt của reservation
                        redirectUrl = `/cashier-flow/receipt/reservation/${reservationId}`;
                    } else if (orderId) {
                        // Order payment: redirect đến receipt của order
                        redirectUrl = `/cashier-flow/receipt/${orderId}`;
                    }
                    
                    this.showCashPaymentResultModal(true, successMessage, redirectUrl);
                } else if (errorMessage) {
                    this.showCashPaymentResultModal(false, errorMessage);
                }

                // Clear stored IDs
                sessionStorage.removeItem('cashPaymentOrderId');
                sessionStorage.removeItem('cashPaymentReservationId');
            }
        }
    };

    // Global function for closing cash payment result modal
    window.closeCashPaymentResultModal = function() {
        const modal = bootstrap.Modal.getInstance(document.getElementById('cashPaymentResultModal'));
        if (modal) {
            modal.hide();
        }
        // Clear processing state
        sessionStorage.removeItem('cashPaymentProcessing');
        sessionStorage.removeItem('cashPaymentOrderId');
    };

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        window.CashPayment.init();
    });

})();
