/**
 * Promotion Voucher Module
 * Handles voucher and promotion related functionality
 */

(function() {
    'use strict';

    // Promotion Voucher Module
    window.PromotionVoucher = {
        // Initialize promotion voucher functionality
        init: function() {
            this.bindEvents();
        },

        // Bind promotion voucher events
        bindEvents: function() {
            // Bind promotion modal open
            const promoBtn = document.querySelector('[onclick="openPromotionModal()"]');
            if (promoBtn) {
                promoBtn.onclick = () => this.openPromotionModal();
            }
        },

        // Open promotion modal and load vouchers
        openPromotionModal: async function() {
            // Show loading state
            const voucherListContainer = document.getElementById('voucherListContainer');
            if (voucherListContainer) {
                voucherListContainer.innerHTML = '<div class="text-center py-4"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Đang tải...</span></div><p class="mt-2 text-muted">Đang tải danh sách voucher...</p></div>';
            }

            if (window.PaymentCore.modals.promoModal) {
                window.PaymentCore.modals.promoModal.show();
            }

            // Load voucher list via AJAX
            try {
                const subtotal = parseFloat(window.PaymentCore.orderContext.Subtotal || 0);
                const response = await fetch(`/cashier-flow/vouchers/available?subtotal=${subtotal}`);

                if (!response.ok) {
                    throw new Error('Không thể tải danh sách voucher');
                }

                const vouchers = await response.json();
                this.renderVoucherList(vouchers);
            } catch (error) {
                console.error('Error loading vouchers:', error);
                const voucherListContainer = document.getElementById('voucherListContainer');
                if (voucherListContainer) {
                    voucherListContainer.innerHTML = `
                        <div class="text-center py-4 text-muted">
                            <i class="bi bi-exclamation-triangle fs-1 d-block mb-2"></i>
                            <p class="mb-0">Không thể tải danh sách voucher.</p>
                            <small>Vui lòng nhập mã voucher thủ công ở cột bên trái.</small>
                        </div>
                    `;
                }
            }
        },

        // Render voucher list into modal
        renderVoucherList: function(vouchers) {
            const voucherListContainer = document.getElementById('voucherListContainer');
            if (!voucherListContainer) return;

            // Update count badge
            this.updateVoucherCount(vouchers ? vouchers.length : 0);

            if (!vouchers || vouchers.length === 0) {
                voucherListContainer.innerHTML = `
                    <div class="text-center py-4 text-muted">
                        <i class="bi bi-inbox fs-1 d-block mb-2"></i>
                        <p class="mb-0">Không có voucher nào khả dụng cho đơn hàng này.</p>
                        <small>Vui lòng nhập mã voucher thủ công ở cột bên trái.</small>
                    </div>
                `;
                return;
            }

            const voucherHtml = vouchers.map(voucher => {
                const discountText = voucher.discountType === "Phần trăm"
                    ? `${voucher.discountValue}%`
                    : `${parseFloat(voucher.discountValue).toLocaleString('vi-VN')} ₫`;
                const conditionText = voucher.minOrderValue
                    ? `Đơn tối thiểu ${parseFloat(voucher.minOrderValue).toLocaleString('vi-VN')} ₫`
                    : "Không giới hạn";

                // Escape HTML to prevent XSS
                const code = (voucher.code || '').replace(/['"]/g, '');
                const description = (voucher.description || '').replace(/['"]/g, '');

                return `
                    <div class="voucher-item" onclick="selectVoucher('${code}')">
                        <div class="d-flex justify-content-between align-items-start">
                            <div class="flex-grow-1">
                                <div class="voucher-code">${code}</div>
                                ${description ? `<div class="voucher-desc">${description}</div>` : ''}
                                <div class="voucher-condition">
                                    <i class="bi bi-info-circle me-1"></i>${conditionText}
                                    ${voucher.maxDiscount && voucher.discountType === "Phần trăm"
                                        ? ` • Tối đa ${parseFloat(voucher.maxDiscount).toLocaleString('vi-VN')} ₫`
                                        : ''}
                                </div>
                            </div>
                            <div class="text-end">
                                <div class="voucher-discount">-${discountText}</div>
                                <small class="text-muted">Giảm giá</small>
                            </div>
                        </div>
                    </div>
                `;
            }).join('');

            voucherListContainer.innerHTML = voucherHtml;
        },

        // Update voucher count badge
        updateVoucherCount: function(count) {
            const badge = document.getElementById('voucherCountBadge');
            if (badge) {
                badge.textContent = count;
            }
        },

        // Select voucher function (called from onclick)
        selectVoucher: function(voucherCode) {
            // This function should be implemented to handle voucher selection
            // For now, just close the modal and show a message
            console.log('Selected voucher:', voucherCode);
            window.PaymentCore.showToast(`Đã chọn voucher: ${voucherCode}`, 'success');

            if (window.PaymentCore.modals.promoModal) {
                window.PaymentCore.modals.promoModal.hide();
            }
        }
    };

    // Global function for selecting voucher (used in onclick handlers)
    window.selectVoucher = function(voucherCode) {
        window.PromotionVoucher.selectVoucher(voucherCode);
    };

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        window.PromotionVoucher.init();
    });

})();
