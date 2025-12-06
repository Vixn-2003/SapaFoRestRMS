// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/**
 * ========================================
 * CASHIER CONFIRMATION POPUP SYSTEM
 * ========================================
 * Reusable confirmation dialog for critical cashier operations
 * Follows design from _PaymentConfirmationModal.cshtml
 */

/**
 * Shows a styled confirmation popup with consistent UI
 * @param {string} title - Modal title with emoji (e.g., "🧾 Xác nhận tạo đơn thanh toán")
 * @param {string} message - Confirmation message (supports HTML)
 * @param {string} confirmText - Text for confirm button (default: "Xác nhận")
 * @param {string} cancelText - Text for cancel button (default: "Hủy")
 * @returns {Promise<boolean>} - Resolves to true if confirmed, false if cancelled
 */
async function showConfirmPopup(title, message, confirmText = "Xác nhận", cancelText = "Hủy") {
    return new Promise(resolve => {
        // Remove any existing confirmation modal
        const existingModal = document.getElementById('cashierConfirmModal');
        if (existingModal) {
            existingModal.remove();
        }

        // Create modal HTML with consistent design
        const modalHTML = `
            <div class="modal fade show" id="cashierConfirmModal" tabindex="-1" 
                 style="display: block; background: rgba(0,0,0,0.5);" 
                 aria-labelledby="cashierConfirmModalLabel" aria-modal="true" role="dialog">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content border-0 shadow-lg" style="border-radius: 1rem; overflow: hidden;">
                        <div class="modal-header text-white border-0" 
                             style="background: linear-gradient(135deg, #0d6efd 0%, #0a58ca 100%); padding: 1.25rem 1.5rem;">
                            <h5 class="modal-title fw-bold d-flex align-items-center" id="cashierConfirmModalLabel">
                                ${title}
                            </h5>
                            <button type="button" class="btn-close btn-close-white" 
                                    onclick="dismissCashierConfirmModal(false)" 
                                    aria-label="Close"></button>
                        </div>
                        <div class="modal-body" style="padding: 1.5rem;">
                            <div class="confirmation-message">
                                ${message}
                            </div>
                        </div>
                        <div class="modal-footer border-0 pt-0" style="padding: 0 1.5rem 1.5rem;">
                            <button type="button" class="btn btn-outline-secondary px-4" 
                                    onclick="dismissCashierConfirmModal(false)">
                                <i class="fa-solid fa-times me-1"></i>
                                ${cancelText}
                            </button>
                            <button type="button" class="btn btn-success px-4" 
                                    onclick="dismissCashierConfirmModal(true)"
                                    style="background: linear-gradient(135deg, #198754 0%, #146c43 100%); border: none;">
                                <i class="fa-solid fa-check me-1"></i>
                                ${confirmText}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Insert modal into DOM
        document.body.insertAdjacentHTML('beforeend', modalHTML);

        // Store resolve function globally for button handlers
        window._cashierConfirmResolve = resolve;

        // Add escape key handler
        const escapeHandler = (e) => {
            if (e.key === 'Escape') {
                dismissCashierConfirmModal(false);
                document.removeEventListener('keydown', escapeHandler);
            }
        };
        document.addEventListener('keydown', escapeHandler);

        // Focus confirm button for better UX
        setTimeout(() => {
            const confirmBtn = document.querySelector('#cashierConfirmModal .btn-success');
            if (confirmBtn) confirmBtn.focus();
        }, 100);
    });
}

/**
 * Dismisses the cashier confirmation modal
 * @param {boolean} result - true if confirmed, false if cancelled
 */
function dismissCashierConfirmModal(result) {
    const modal = document.getElementById('cashierConfirmModal');
    if (modal) {
        // Fade out animation
        modal.classList.remove('show');
        modal.style.opacity = '0';
        setTimeout(() => {
            modal.remove();
        }, 150);
    }
    
    // Resolve the promise
    if (window._cashierConfirmResolve) {
        window._cashierConfirmResolve(result);
        delete window._cashierConfirmResolve;
    }
}

// Export to global scope
window.showConfirmPopup = showConfirmPopup;
window.dismissCashierConfirmModal = dismissCashierConfirmModal;