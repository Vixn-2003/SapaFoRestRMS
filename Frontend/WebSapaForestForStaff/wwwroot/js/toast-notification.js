/**
 * Toast Notification System
 * Replaces alert() with Bootstrap 5 Toasts
 * Design System compliant
 */

(function () {
    'use strict';

    // Ensure toast container exists
    function ensureToastContainer() {
        let container = document.getElementById('toastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container-custom';
            document.body.appendChild(container);
        }
        return container;
    }

    /**
     * Show Toast Notification
     * @param {string} message - Message to display
     * @param {string} type - Type: 'success', 'error', 'warning', 'info'
     * @param {number} duration - Duration in milliseconds (default: 3000)
     */
    window.showToast = function (message, type = 'info', duration = 3000) {
        const container = ensureToastContainer();

        // Map type to Bootstrap classes and icons
        const toastConfig = {
            success: {
                bgClass: 'bg-success text-white',
                icon: '<i class="mdi mdi-check-circle toast-icon"></i>',
                title: 'Thành công'
            },
            error: {
                bgClass: 'bg-danger text-white',
                icon: '<i class="mdi mdi-alert-circle toast-icon"></i>',
                title: 'Lỗi'
            },
            warning: {
                bgClass: 'bg-warning text-dark',
                icon: '<i class="mdi mdi-alert toast-icon"></i>',
                title: 'Cảnh báo'
            },
            info: {
                bgClass: 'bg-primary text-white',
                icon: '<i class="mdi mdi-information-outline toast-icon"></i>',
                title: 'Thông tin'
            }
        };

        const config = toastConfig[type] || toastConfig.info;

        // Create toast element
        const toastId = 'toast_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        const toast = document.createElement('div');
        toast.id = toastId;
        toast.className = `toast align-items-center border-0 ${config.bgClass}`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');
        toast.style.minWidth = '320px';

        // Toast content
        toast.innerHTML = `
            <div class="toast-body-custom">
                <div class="d-flex align-items-center w-100">
                    ${config.icon}
                    <div class="flex-grow-1">
                        <strong>${config.title}</strong>
                        <div class="mt-1">${escapeHtml(message)}</div>
                    </div>
                    <button type="button" class="btn-close btn-close-white ms-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        // Append to container
        container.appendChild(toast);

        // Initialize and show Bootstrap Toast
        const toastBootstrap = new bootstrap.Toast(toast, {
            delay: duration,
            autohide: duration > 0
        });

        toastBootstrap.show();

        // Remove from DOM after hiding
        toast.addEventListener('hidden.bs.toast', function () {
            toast.remove();
        });

        return toastBootstrap;
    };

    /**
     * Show Success Toast
     */
    window.showSuccessToast = function (message, duration = 3000) {
        return showToast(message, 'success', duration);
    };

    /**
     * Show Error Toast
     */
    window.showErrorToast = function (message, duration = 5000) {
        return showToast(message, 'error', duration);
    };

    /**
     * Show Warning Toast
     */
    window.showWarningToast = function (message, duration = 4000) {
        return showToast(message, 'warning', duration);
    };

    /**
     * Show Info Toast
     */
    window.showInfoToast = function (message, duration = 3000) {
        return showToast(message, 'info', duration);
    };

    /**
     * Escape HTML to prevent XSS
     */
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text || '').replace(/[&<>"']/g, m => map[m]);
    }

    // Expose escapeHtml for use in other scripts
    window.escapeHtml = escapeHtml;

})();

