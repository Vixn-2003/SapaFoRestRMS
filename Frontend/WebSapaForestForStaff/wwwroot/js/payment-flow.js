/**
 * Payment Flow JavaScript
 * Handles: Cash Payment, Split Bill, Status Polling, Error Handling, Retry, Offline Caching
 */

(function () {
    'use strict';

    // Configuration
    const CONFIG = {
        POLL_INTERVAL: 5000, // 5 seconds
        POLL_TIMEOUT: 60000, // 60 seconds
        OFFLINE_CACHE_KEY: 'payment_offline_cache',
        RETRY_DELAYS: [1000, 2000, 5000] // Exponential backoff
    };

    // State
    let currentOrderId = null;
    let currentOrderData = null;
    let paymentPollInterval = null;
    let paymentPollStartTime = null;
    let retryCount = 0;

    // ========== UTILITY FUNCTIONS ==========

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

    /**
     * Step 8: Play success sound when payment is confirmed
     * Plays audio feedback for successful payment
     */
    function playSuccessSound() {
        try {
            // Create audio context for beep sound
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);

            // Configure beep sound (success tone)
            oscillator.frequency.value = 800; // Higher pitch for success
            oscillator.type = 'sine';
            gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.3);

            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.3);

            // Alternative: Use HTML5 Audio if file exists
            // const audio = new Audio('/sounds/payment-success.mp3');
            // audio.play().catch(e => console.log('Audio play failed:', e));
        } catch (error) {
            console.log('Sound playback not available:', error);
            // Silently fail - sound is optional
        }
    }

    // ========== OFFLINE CACHING ==========

    function saveToOfflineCache(transaction) {
        try {
            const cache = JSON.parse(localStorage.getItem(CONFIG.OFFLINE_CACHE_KEY) || '[]');
            cache.push({
                ...transaction,
                cachedAt: new Date().toISOString(),
                synced: false
            });
            localStorage.setItem(CONFIG.OFFLINE_CACHE_KEY, JSON.stringify(cache));
            console.log('Saved to offline cache:', transaction);
        } catch (error) {
            console.error('Error saving to offline cache:', error);
        }
    }

    function getOfflineCache() {
        try {
            return JSON.parse(localStorage.getItem(CONFIG.OFFLINE_CACHE_KEY) || '[]');
        } catch (error) {
            console.error('Error reading offline cache:', error);
            return [];
        }
    }

    function removeFromOfflineCache(transactionId) {
        try {
            const cache = getOfflineCache();
            const filtered = cache.filter(t => t.transactionId !== transactionId);
            localStorage.setItem(CONFIG.OFFLINE_CACHE_KEY, JSON.stringify(filtered));
        } catch (error) {
            console.error('Error removing from offline cache:', error);
        }
    }

    async function syncOfflinePayments() {
        const cache = getOfflineCache();
        const unsynced = cache.filter(t => !t.synced);

        if (unsynced.length === 0) return;

        console.log(`Syncing ${unsynced.length} offline payments...`);

        try {
            const response = await fetch(`${getApiBaseUrl()}/payment/sync`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${getToken()}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    transactionIds: unsynced.map(t => t.transactionId)
                })
            });

            if (response.ok) {
                const syncedIds = await response.json();
                syncedIds.forEach(id => {
                    const transaction = cache.find(t => t.transactionId === id);
                    if (transaction) {
                        transaction.synced = true;
                    }
                });
                localStorage.setItem(CONFIG.OFFLINE_CACHE_KEY, JSON.stringify(cache));
                showToast(`Đã đồng bộ ${syncedIds.length} giao dịch từ offline cache`, 'success');
            }
        } catch (error) {
            console.error('Error syncing offline payments:', error);
        }
    }

    // Auto sync on page load
    if (typeof window !== 'undefined' && window.addEventListener) {
        window.addEventListener('online', syncOfflinePayments);
        window.addEventListener('load', syncOfflinePayments);
    }

    // ========== CASH PAYMENT ==========

    window.openCashPayment = function (orderId, orderData) {
        currentOrderId = orderId;
        currentOrderData = orderData;

        // Populate modal
        document.getElementById('cashOrderCode').textContent = orderData.orderCode || `ORD-${orderId}`;
        document.getElementById('cashTableNumber').textContent = orderData.tableNumber || '-';
        document.getElementById('cashSubtotal').textContent = formatCurrency(orderData.subtotal || 0);
        document.getElementById('cashVat').textContent = formatCurrency(orderData.vat || 0);
        document.getElementById('cashServiceFee').textContent = formatCurrency(orderData.serviceFee || 0);
        document.getElementById('cashDiscount').textContent = `-${formatCurrency(orderData.discount || 0)}`;
        document.getElementById('cashTotalAmount').textContent = formatCurrency(orderData.total || 0);

        // Reset form
        document.getElementById('amountReceived').value = '';
        document.getElementById('paymentNotes').value = '';
        document.getElementById('refundConfirmed').checked = false;
        document.getElementById('changeDisplay').classList.add('d-none');
        document.getElementById('underpaidWarning').classList.add('d-none');
        document.getElementById('refundConfirmation').classList.add('d-none');
        document.getElementById('confirmCashPaymentBtn').disabled = true;

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('cashPaymentModal'));
        modal.show();
    };

    window.calculateCashChange = function () {
        const amountReceived = parseFloat(document.getElementById('amountReceived').value) || 0;
        const totalAmount = currentOrderData?.total || 0;

        const changeDisplay = document.getElementById('changeDisplay');
        const underpaidWarning = document.getElementById('underpaidWarning');
        const refundConfirmation = document.getElementById('refundConfirmation');
        const confirmBtn = document.getElementById('confirmCashPaymentBtn');

        // Reset
        changeDisplay.classList.add('d-none');
        underpaidWarning.classList.add('d-none');
        refundConfirmation.classList.add('d-none');
        confirmBtn.disabled = true;

        if (amountReceived === 0) return;

        if (amountReceived < totalAmount) {
            // Underpaid
            const missing = totalAmount - amountReceived;
            document.getElementById('requiredAmount').textContent = formatCurrency(totalAmount);
            document.getElementById('receivedAmount').textContent = formatCurrency(amountReceived);
            document.getElementById('missingAmount').textContent = formatCurrency(missing);
            underpaidWarning.classList.remove('d-none');
            confirmBtn.disabled = true;
        } else if (amountReceived > totalAmount) {
            // Overpaid
            const refund = amountReceived - totalAmount;
            document.getElementById('refundAmount').textContent = formatCurrency(refund);
            changeDisplay.classList.remove('d-none');
            refundConfirmation.classList.remove('d-none');
            confirmBtn.disabled = !document.getElementById('refundConfirmed').checked;
        } else {
            // Exact amount
            confirmBtn.disabled = false;
        }
    };

    window.toggleConfirmButton = function () {
        const confirmed = document.getElementById('refundConfirmed').checked;
        document.getElementById('confirmCashPaymentBtn').disabled = !confirmed;
    };

    window.confirmCashPayment = async function () {
        const amountReceived = parseFloat(document.getElementById('amountReceived').value) || 0;
        const totalAmount = currentOrderData?.total || 0;
        const notes = document.getElementById('paymentNotes').value;

        if (amountReceived < totalAmount) {
            showToast('⚠️ Số tiền chưa đủ. Vui lòng kiểm tra lại.', 'error');
            return;
        }

        const confirmBtn = document.getElementById('confirmCashPaymentBtn');
        const originalText = confirmBtn.innerHTML;
        confirmBtn.disabled = true;
        confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang xử lý...';

        try {
            const response = await fetch(`${getApiBaseUrl()}/payment/cash`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${getToken()}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    orderId: currentOrderId,
                    totalAmount: totalAmount,
                    amountReceived: amountReceived,
                    notes: notes
                })
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Thanh toán thất bại');
            }

            const transaction = await response.json();

            // Save to offline cache if needed
            if (!navigator.onLine) {
                saveToOfflineCache(transaction);
            }

            // Step 8: Play success sound
            playSuccessSound();

            showToast('✅ Thanh toán thành công! Đang tạo hóa đơn...', 'success');

            // Generate and open receipt PDF
            setTimeout(() => {
                const receiptUrl = `${getApiBaseUrl()}/payment/receipt/${currentOrderId}`;
                window.open(receiptUrl, '_blank');
            }, 500);

            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('cashPaymentModal'));
            modal.hide();

            // Reload page or redirect
            setTimeout(() => {
                window.location.reload();
            }, 2000);
        } catch (error) {
            console.error('Error processing cash payment:', error);
            showToast(error.message || 'Lỗi khi xử lý thanh toán. Vui lòng thử lại.', 'error');
            
            // Save to offline cache if offline
            if (!navigator.onLine) {
                saveToOfflineCache({
                    orderId: currentOrderId,
                    amount: totalAmount,
                    amountReceived: amountReceived,
                    paymentMethod: 'Cash',
                    status: 'Pending',
                    notes: notes
                });
                showToast('💾 Đã lưu tạm giao dịch cục bộ. Sẽ tự động đồng bộ khi có kết nối.', 'info');
            }
        } finally {
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = originalText;
        }
    };

    // ========== PAYMENT STATUS POLLING ==========

    window.startPaymentPolling = function (orderId) {
        if (paymentPollInterval) {
            clearInterval(paymentPollInterval);
        }

        paymentPollStartTime = Date.now();
        paymentPollInterval = setInterval(async () => {
            const elapsed = Date.now() - paymentPollStartTime;
            
            if (elapsed > CONFIG.POLL_TIMEOUT) {
                stopPaymentPolling();
                showManualConfirmationOption(orderId);
                return;
            }

            try {
                const response = await fetch(`${getApiBaseUrl()}/payment/status/${orderId}`, {
                    headers: {
                        'Authorization': `Bearer ${getToken()}`
                    }
                });

                if (response.ok) {
                    const status = await response.json();
                    
                    if (status.status === 'Paid' || status.status === 'Success') {
                        stopPaymentPolling();
                        showToast('✅ Thanh toán thành công!', 'success');
                        setTimeout(() => window.location.reload(), 1500);
                    } else if (status.status === 'Failed') {
                        stopPaymentPolling();
                        showRetryOption(orderId, status);
                    }
                }
            } catch (error) {
                console.error('Error polling payment status:', error);
            }
        }, CONFIG.POLL_INTERVAL);
    };

    window.stopPaymentPolling = function () {
        if (paymentPollInterval) {
            clearInterval(paymentPollInterval);
            paymentPollInterval = null;
        }
    };

    function showManualConfirmationOption(orderId) {
        if (confirm('⏳ Thanh toán đang chờ xử lý. Bạn có muốn xác nhận thủ công không?')) {
            // Call manual confirmation API
            confirmManualPayment(orderId);
        }
    }

    function showRetryOption(orderId, status) {
        const message = `Thanh toán thất bại: ${status.gatewayErrorMessage || 'Lỗi không xác định'}\n\nBạn có muốn thử lại không?`;
        if (confirm(message)) {
            retryPayment(orderId);
        }
    }

    // ========== RETRY LOGIC ==========

    window.retryPayment = async function (orderId, transactionId = null) {
        if (retryCount >= CONFIG.RETRY_DELAYS.length) {
            showToast('Đã thử lại quá nhiều lần. Vui lòng liên hệ hỗ trợ.', 'error');
            return;
        }

        const delay = CONFIG.RETRY_DELAYS[retryCount];
        showToast(`Đang thử lại sau ${delay / 1000} giây...`, 'info');

        setTimeout(async () => {
            try {
                const response = await fetch(`${getApiBaseUrl()}/payment/retry`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${getToken()}`,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        transactionId: transactionId,
                        notes: `Retry attempt ${retryCount + 1}`
                    })
                });

                if (response.ok) {
                    retryCount = 0;
                    showToast('Đang xử lý lại thanh toán...', 'info');
                    startPaymentPolling(orderId);
                } else {
                    retryCount++;
                    const errorData = await response.json();
                    throw new Error(errorData.message || 'Retry thất bại');
                }
            } catch (error) {
                console.error('Error retrying payment:', error);
                showToast(error.message || 'Lỗi khi thử lại. Vui lòng thử lại sau.', 'error');
            }
        }, delay);
    };

    async function confirmManualPayment(orderId) {
        try {
            const response = await fetch(`${getApiBaseUrl()}/payment/manual-confirm`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${getToken()}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ orderId })
            });

            if (response.ok) {
                showToast('✅ Đã xác nhận thanh toán thủ công', 'success');
                setTimeout(() => window.location.reload(), 1500);
            }
        } catch (error) {
            console.error('Error confirming manual payment:', error);
            showToast('Lỗi khi xác nhận thanh toán', 'error');
        }
    }

    // ========== ERROR HANDLING ==========

    window.handlePaymentError = function (error, orderId) {
        console.error('Payment error:', error);

        let errorMessage = 'Lỗi không xác định';
        let showRetry = false;
        let showSwitchMethod = false;

        if (error.message) {
            errorMessage = error.message;
        }

        if (error.code === 'NETWORK_ERROR' || !navigator.onLine) {
            errorMessage = 'Mất kết nối mạng. Đã lưu tạm giao dịch cục bộ.';
            showRetry = true;
        } else if (error.code === 'GATEWAY_ERROR') {
            errorMessage = `Lỗi từ cổng thanh toán: ${error.message}`;
            showRetry = true;
            showSwitchMethod = true;
        } else if (error.code === 'UNDERPAID') {
            errorMessage = '⚠️ Số tiền chưa đủ. Vui lòng kiểm tra lại.';
        }

        // Show error UI
        showErrorModal(errorMessage, orderId, showRetry, showSwitchMethod);
    };

    function showErrorModal(message, orderId, showRetry, showSwitchMethod) {
        // Create or update error modal
        let errorModal = document.getElementById('paymentErrorModal');
        if (!errorModal) {
            errorModal = document.createElement('div');
            errorModal.id = 'paymentErrorModal';
            errorModal.className = 'modal fade';
            errorModal.innerHTML = `
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header bg-danger text-white">
                            <h5 class="modal-title">
                                <i class="bi bi-exclamation-triangle-fill me-2"></i>Lỗi thanh toán
                            </h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p id="errorMessage">${message}</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Đóng</button>
                            <button type="button" class="btn btn-warning d-none" id="retryErrorBtn" onclick="retryPayment(${orderId})">
                                <i class="bi bi-arrow-clockwise me-1"></i>Thử lại
                            </button>
                            <button type="button" class="btn btn-primary d-none" id="switchMethodBtn" onclick="switchPaymentMethod(${orderId})">
                                <i class="bi bi-arrow-left-right me-1"></i>Đổi phương thức
                            </button>
                        </div>
                    </div>
                </div>
            `;
            document.body.appendChild(errorModal);
        }

        document.getElementById('errorMessage').textContent = message;
        document.getElementById('retryErrorBtn').classList.toggle('d-none', !showRetry);
        document.getElementById('switchMethodBtn').classList.toggle('d-none', !showSwitchMethod);

        const modal = new bootstrap.Modal(errorModal);
        modal.show();
    }

    window.switchPaymentMethod = function (orderId) {
        // Close error modal
        const errorModal = bootstrap.Modal.getInstance(document.getElementById('paymentErrorModal'));
        if (errorModal) errorModal.hide();

        // Show payment method selection
        window.location.href = `/Payment/PaymentMethod?orderId=${orderId}`;
    };

    // Export functions
    window.PaymentFlow = {
        openCashPayment,
        calculateCashChange,
        toggleConfirmButton,
        confirmCashPayment,
        startPaymentPolling,
        stopPaymentPolling,
        retryPayment,
        handlePaymentError,
        switchPaymentMethod,
        syncOfflinePayments,
        getOfflineCache
    };
})();

