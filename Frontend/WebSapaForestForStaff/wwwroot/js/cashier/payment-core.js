/**
 * Payment Core Module - Shared utilities and common functions
 * Contains basic payment utilities, modal management, and helper functions
 */

(function() {
    'use strict';

    // Global payment context
    window.PaymentCore = {
        // Order context data
        orderContext: null,

        // Modal instances
        modals: {},

        // Initialize core functionality
        init: function(orderData) {
            this.orderContext = orderData;
            this.initializeModals();
            this.bindCommonEvents();
        },

        // Initialize modal instances
        initializeModals: function() {
            this.modals.cashModal = new bootstrap.Modal('#cashPaymentModal');
            this.modals.qrModal = new bootstrap.Modal('#qrPaymentModal');
            this.modals.combinedModal = new bootstrap.Modal('#combinedPaymentModal');
            this.modals.splitModal = new bootstrap.Modal('#splitBillModal');
            this.modals.promoModal = new bootstrap.Modal('#promotionModal');
        },

        // Bind common events
        bindCommonEvents: function() {
            // Add event listeners for form debugging if needed
            const cashPaymentForm = document.getElementById('cashPaymentForm');
            if (cashPaymentForm) {
                cashPaymentForm.addEventListener('submit', function(e) {
                    console.log('[DEBUG] Form submit event triggered');
                    console.log('[DEBUG] Form action:', this.action);
                    console.log('[DEBUG] Form method:', this.method);
                    console.log('[DEBUG] Form data:', {
                        OrderId: document.getElementById('cashPaymentOrderId').value,
                        AmountReceived: document.getElementById('cashPaymentAmountReceived').value,
                        Notes: document.getElementById('cashPaymentNotes').value
                    });

                    if (e.defaultPrevented) {
                        console.warn('[WARNING] Form submit was prevented by another handler!');
                    }
                });
            }
        },

        // Format currency utility
        formatCurrency: function(value) {
            return (value || 0).toLocaleString('vi-VN') + ' ₫';
        },

        // Submit payment function
        submitPayment: function(method, amountOverride) {
            const paymentForm = document.getElementById('paymentActionForm');
            const methodInput = document.getElementById('paymentMethodInput');
            const amountInput = document.getElementById('paymentAmountInput');

            methodInput.value = method;
            amountInput.value = amountOverride ?? this.orderContext.Total;
            paymentForm.submit();
        },

        // Populate order summary in modals
        populateOrderSummary: function(prefix) {
            const orderCode = document.getElementById(`${prefix}OrderCode`);
            if (orderCode) {
                // ✅ Nếu là Reservation payment, hiển thị tất cả order codes
                if (this.orderContext.IsReservationPayment && this.orderContext.ReservationId) {
                    const orderCount = this.orderContext.OrderCount || 1;
                    const orderCodes = this.orderContext.OrderCodes || [];
                    
                    if (orderCodes.length > 0) {
                        // Hiển thị tất cả order codes
                        orderCode.textContent = orderCodes.join(', ');
                    } else if (orderCount > 1) {
                        // Fallback: hiển thị order code đầu tiên với số lượng orders
                        orderCode.textContent = `${this.orderContext.OrderCode || '-'} (+${orderCount - 1} đơn khác)`;
                    } else {
                        orderCode.textContent = this.orderContext.OrderCode || '-';
                    }
                    
                    // Hiển thị số lượng orders
                    const orderCountEl = document.getElementById(`${prefix}OrderCount`);
                    if (orderCountEl) {
                        orderCountEl.textContent = `(${orderCount} đơn)`;
                    }
                } else {
                    orderCode.textContent = this.orderContext.OrderCode || '-';
                }
            }

            const tableNumber = document.getElementById(`${prefix}TableNumber`);
            if (tableNumber) tableNumber.textContent = this.orderContext.Tables || '-';

            const totalAmount = document.getElementById(`${prefix}TotalAmount`);
            if (totalAmount) totalAmount.textContent = this.formatCurrency(this.orderContext.Total);

            const subtotalEl = document.getElementById(`${prefix}Subtotal`);
            if (subtotalEl) subtotalEl.textContent = this.formatCurrency(this.orderContext.Subtotal);

            const vatEl = document.getElementById(`${prefix}Vat`);
            if (vatEl) vatEl.textContent = this.formatCurrency(this.orderContext.Vat);

            const serviceEl = document.getElementById(`${prefix}ServiceFee`);
            if (serviceEl) serviceEl.textContent = this.formatCurrency(this.orderContext.ServiceFee);

            const discountEl = document.getElementById(`${prefix}Discount`);
            if (discountEl) discountEl.textContent = '-' + this.formatCurrency(this.orderContext.Discount || 0);
        },

        // Generate VietQR URL
        generateVietQrUrl: function(bank, account, amount, addInfo) {
            return `https://img.vietqr.io/image/${bank}-${account}-compact.png`
                + `?amount=${amount}`
                + `&addInfo=${encodeURIComponent(addInfo)}`;
        },

        // Show toast message
        showToast: function(message, type = 'success') {
            if (typeof showToast === 'function') {
                showToast(message, type);
            } else {
                console.log(`[${type.toUpperCase()}] ${message}`);
            }
        },

        // Get temp data message from page
        getTempDataMessage: function(key) {
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
    };

    // Make it globally available
    window.PaymentCore = window.PaymentCore || PaymentCore;

})();
