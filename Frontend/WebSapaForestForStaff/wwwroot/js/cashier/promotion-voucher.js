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
                    <div class="voucher-item" data-voucher-code="${code}" style="cursor: pointer;">
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
            
            // ✅ Bind click event listeners sau khi render HTML (chỉ bind 1 lần)
            // Sử dụng event delegation để tránh xung đột với onclick inline
            // Remove old listener nếu có để tránh bind nhiều lần
            const oldHandler = voucherListContainer._voucherClickHandler;
            if (oldHandler) {
                voucherListContainer.removeEventListener('click', oldHandler);
            }
            
            // Tạo handler mới
            const clickHandler = function(e) {
                const voucherItem = e.target.closest('.voucher-item');
                if (voucherItem) {
                    e.preventDefault();
                    e.stopPropagation();
                    
                    const voucherCode = voucherItem.getAttribute('data-voucher-code');
                    if (voucherCode) {
                        // ✅ Chỉ gọi selectVoucher - KHÔNG có AJAX call
                        window.selectVoucher(voucherCode, e);
                    }
                }
            };
            
            // Lưu reference để có thể remove sau
            voucherListContainer._voucherClickHandler = clickHandler;
            voucherListContainer.addEventListener('click', clickHandler);
        },

        // Update voucher count badge
        updateVoucherCount: function(count) {
            const badge = document.getElementById('voucherCountBadge');
            if (badge) {
                badge.textContent = count;
            }
        },

        // Select voucher function (called from onclick)
        // ✅ CHỈ fill input và highlight - KHÔNG có AJAX call
        // ✅ KHÔNG đóng modal - User phải click "Áp dụng" để apply
        selectVoucher: function(voucherCode) {
            // ✅ Fill voucher code vào input field
            const voucherInput = document.getElementById('VoucherCode');
            if (voucherInput) {
                voucherInput.value = voucherCode;
            }

            // ✅ Highlight selected voucher (border xanh)
            document.querySelectorAll('.voucher-item').forEach(item => {
                item.classList.remove('selected');
            });
            
            // Find and highlight the clicked voucher item
            const voucherItems = document.querySelectorAll('.voucher-item');
            voucherItems.forEach(item => {
                const codeElement = item.querySelector('.voucher-code');
                if (codeElement && codeElement.textContent.trim() === voucherCode) {
                    item.classList.add('selected');
                }
            });

            // ✅ Clear error message
            const errorDiv = document.getElementById('voucherErrorMessage');
            if (errorDiv) {
                errorDiv.classList.add('d-none');
            }

            // ✅ KHÔNG show toast message khi chọn voucher
            // ✅ KHÔNG gọi AJAX khi chọn voucher
            // ✅ KHÔNG đóng modal - để user có thể click nút "Áp dụng"
            // Modal sẽ chỉ đóng sau khi apply thành công (trong applyVoucherConfirmed)
        }
    };

    // Global function for selecting voucher (used in onclick handlers)
    // ✅ CHỈ fill input và highlight - KHÔNG có AJAX call
    // ✅ KHÔNG đóng modal - User phải click "Áp dụng" để apply
    window.selectVoucher = function(voucherCode, event) {
        // ✅ Prevent event bubbling và default behavior
        if (event) {
            event.stopPropagation();
            event.preventDefault();
        }
        
        // ✅ Gọi function selectVoucher từ PromotionVoucher module
        // Function này CHỈ fill input và highlight - KHÔNG có AJAX
        window.PromotionVoucher.selectVoucher(voucherCode);
        
        // ✅ Return false để đảm bảo không trigger bất kỳ event nào khác
        // Điều này ngăn form submit hoặc modal close
        return false;
    };

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        window.PromotionVoucher.init();
    });

})();
