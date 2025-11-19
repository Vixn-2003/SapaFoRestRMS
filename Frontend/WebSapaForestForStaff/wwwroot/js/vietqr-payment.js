/**
 * VietQR Payment Handler
 * Xử lý thanh toán qua VietQR
 */

(function () {
    'use strict';

    // API Base URL - có thể lấy từ config hoặc global variable
    function getApiBaseUrl() {
        // Thử lấy từ global variable
        if (window.API_BASE_URL) return window.API_BASE_URL;
        if (typeof apiBaseUrl !== 'undefined') return apiBaseUrl;
        
        // Default
        return 'https://localhost:7000/api';
    }
    
    // Lưu trữ thông tin order hiện tại
    let currentOrderId = null;

    /**
     * Mở modal thanh toán VietQR
     * @param {number} orderId - ID của đơn hàng
     */
    window.openQrPayment = async function (orderId) {
        if (!orderId) {
            showError('Vui lòng chọn đơn hàng');
            return;
        }

        currentOrderId = orderId;

        try {
            // Hiển thị loading
            const modal = new bootstrap.Modal(document.getElementById('qrPaymentModal'));
            document.getElementById('qrImage').src = '';
            document.getElementById('qrAmount').textContent = '0';
            document.getElementById('qrOrderCode').textContent = '-';
            document.getElementById('qrDescription').textContent = '-';
            document.getElementById('confirmPaymentBtn').disabled = true;
            modal.show();

            // Gọi API để lấy VietQR
            const response = await fetch(`${getApiBaseUrl()}/payment/vietqr/${orderId}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${getToken()}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Không thể tạo mã QR');
            }

            const data = await response.json();

            // Cập nhật UI
            document.getElementById('qrImage').src = data.qrUrl;
            document.getElementById('qrAmount').textContent = formatCurrency(data.total);
            document.getElementById('qrOrderCode').textContent = data.orderCode || `#${orderId}`;
            document.getElementById('qrDescription').textContent = data.description || '';
            document.getElementById('confirmPaymentBtn').disabled = false;

            // Lưu orderId vào data attribute
            document.getElementById('qrImage').dataset.orderId = orderId;

        } catch (error) {
            console.error('Error generating VietQR:', error);
            showError(error.message || 'Không thể tạo mã QR. Vui lòng thử lại.');
            
            // Đóng modal nếu có lỗi
            const modalInstance = bootstrap.Modal.getInstance(document.getElementById('qrPaymentModal'));
            if (modalInstance) {
                modalInstance.hide();
            }
        }
    };

    /**
     * Xác nhận thanh toán VietQR
     */
    window.confirmVietQRPayment = async function () {
        if (!currentOrderId) {
            showError('Không tìm thấy thông tin đơn hàng');
            return;
        }

        const confirmBtn = document.getElementById('confirmPaymentBtn');
        const originalText = confirmBtn.innerHTML;
        
        try {
            // Disable button và hiển thị loading
            confirmBtn.disabled = true;
            confirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang xử lý...';

            // Gọi API xác nhận thanh toán
            const response = await fetch(`${getApiBaseUrl()}/payment/vietqr/${currentOrderId}/confirm`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${getToken()}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Không thể xác nhận thanh toán');
            }

            const result = await response.json();

            // Hiển thị thông báo thành công
            showSuccess(result.message || 'Xác nhận thanh toán thành công!');

            // Đóng modal
            const modalInstance = bootstrap.Modal.getInstance(document.getElementById('qrPaymentModal'));
            if (modalInstance) {
                modalInstance.hide();
            }

            // Reload trang sau 1 giây
            setTimeout(() => {
                window.location.reload();
            }, 1000);

        } catch (error) {
            console.error('Error confirming payment:', error);
            showError(error.message || 'Không thể xác nhận thanh toán. Vui lòng thử lại.');
            
            // Re-enable button
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = originalText;
        }
    };

    /**
     * Lấy token từ session hoặc cookie
     */
    function getToken() {
        // Thử lấy từ sessionStorage
        let token = sessionStorage.getItem('Token');
        if (token) return token;

        // Thử lấy từ localStorage
        token = localStorage.getItem('Token');
        if (token) return token;

        // Thử lấy từ cookie (nếu có)
        const cookies = document.cookie.split(';');
        for (let cookie of cookies) {
            const [name, value] = cookie.trim().split('=');
            if (name === 'Token') return value;
        }

        return '';
    }

    /**
     * Format currency VND
     */
    function formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    }

    /**
     * Hiển thị thông báo lỗi
     */
    function showError(message) {
        if (typeof toastr !== 'undefined') {
            toastr.error(message, 'Lỗi', { timeOut: 3000 });
        } else if (typeof showErrorToast === 'function') {
            showErrorToast(message);
        } else {
            alert('Lỗi: ' + message);
        }
    }

    /**
     * Hiển thị thông báo thành công
     */
    function showSuccess(message) {
        if (typeof toastr !== 'undefined') {
            toastr.success(message, 'Thành công', { timeOut: 3000 });
        } else if (typeof showSuccessToast === 'function') {
            showSuccessToast(message);
        } else {
            alert('Thành công: ' + message);
        }
    }

    // Reset khi modal đóng
    document.addEventListener('DOMContentLoaded', function () {
        const modal = document.getElementById('qrPaymentModal');
        if (modal) {
            modal.addEventListener('hidden.bs.modal', function () {
                currentOrderId = null;
                document.getElementById('qrImage').src = '';
                document.getElementById('qrAmount').textContent = '0';
                document.getElementById('qrOrderCode').textContent = '-';
                document.getElementById('qrDescription').textContent = '-';
            });
        }
    });

})();

