/**
 * Users Management - Index Page
 * Handles user management operations with Toast notifications, loading states, and improved UX
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initializePage();
        setupBulkActions();
        setupIndividualActions();
        setupSortableColumns();
        setupLoadingStates();
    });

    /**
     * Initialize page
     */
    function initializePage() {
        // Update select all checkbox state
        updateSelectAllState();
        
        // Listen for checkbox changes
        document.querySelectorAll('.user-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', updateSelectAllState);
        });
    }

    /**
     * Update select all checkbox state
     */
    function updateSelectAllState() {
        const selectAll = document.getElementById('selectAll');
        const checkboxes = document.querySelectorAll('.user-checkbox');
        const checkedCount = document.querySelectorAll('.user-checkbox:checked').length;
        
        if (selectAll && checkboxes.length > 0) {
            selectAll.checked = checkedCount === checkboxes.length;
            selectAll.indeterminate = checkedCount > 0 && checkedCount < checkboxes.length;
        }
    }

    /**
     * Setup bulk action buttons
     */
    function setupBulkActions() {
        // Update bulk actions button states based on selection
        const checkboxes = document.querySelectorAll('.user-checkbox');
        checkboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function() {
                const selectedCount = document.querySelectorAll('.user-checkbox:checked').length;
                updateBulkActionButtons(selectedCount > 0);
            });
        });
    }

    /**
     * Update bulk action buttons enabled/disabled state
     */
    function updateBulkActionButtons(enabled) {
        const bulkButtons = document.querySelectorAll('.bulk-actions button');
        bulkButtons.forEach(btn => {
            if (enabled) {
                btn.classList.remove('disabled');
                btn.disabled = false;
            } else {
                btn.classList.add('disabled');
                btn.disabled = true;
            }
        });
    }

    /**
     * Setup individual action buttons (delete, change status)
     */
    function setupIndividualActions() {
        // Handle delete confirmation
        document.querySelectorAll('.delete-user-form').forEach(form => {
            form.addEventListener('submit', function(e) {
                e.preventDefault();
                const formData = new FormData(form);
                const userId = formData.get('id');
                const userName = form.querySelector('.delete-user-btn').getAttribute('data-user-name');
                
                showConfirmModal(
                    'Xóa người dùng',
                    `Bạn có chắc muốn xóa người dùng "${userName}"? Hành động này không thể hoàn tác.`,
                    'error',
                    () => {
                        showLoadingState(form);
                        form.submit();
                    }
                );
            });
        });

        // Handle status change
        document.querySelectorAll('.change-status-form').forEach(form => {
            form.addEventListener('submit', function(e) {
                e.preventDefault();
                const formData = new FormData(form);
                const userId = formData.get('id');
                const status = parseInt(formData.get('status'));
                const userName = form.querySelector('.change-status-btn').getAttribute('data-user-name');
                const statusText = status === 0 ? 'kích hoạt' : 'vô hiệu hóa';
                
                showConfirmModal(
                    status === 0 ? 'Kích hoạt người dùng' : 'Vô hiệu hóa người dùng',
                    `Bạn có chắc muốn ${statusText} người dùng "${userName}"?`,
                    status === 0 ? 'success' : 'warning',
                    () => {
                        showLoadingState(form);
                        form.submit();
                    }
                );
            });
        });
    }

    /**
     * Setup sortable columns
     */
    function setupSortableColumns() {
        const sortableHeaders = document.querySelectorAll('.table-custom th[data-sort]');
        sortableHeaders.forEach(header => {
            header.style.cursor = 'pointer';
            header.classList.add('sortable-header');
            
            header.addEventListener('click', function() {
                const sortBy = this.getAttribute('data-sort');
                const currentSortOrder = getUrlParameter('sortOrder') || 'asc';
                const newSortOrder = currentSortOrder === 'asc' ? 'desc' : 'asc';
                
                // Build new URL with sort parameters
                const url = buildUrlWithParams({
                    sortBy: sortBy,
                    sortOrder: newSortOrder
                });
                
                window.location.href = url;
            });
        });
    }

    /**
     * Setup loading states for async operations
     */
    function setupLoadingStates() {
        // Ensure overlay is hidden on load
        hidePageLoading();

        // Add loading overlay for page transitions
        document.querySelectorAll('a[href], form[method="get"]').forEach(element => {
            element.addEventListener('click', function(e) {
                // Ignore UI triggers (modals, dropdowns, collapse) and non-navigation links
                const isAnchor = this.tagName === 'A';
                const isGetForm = this.tagName === 'FORM' && this.method === 'get';
                const hasToggle = this.getAttribute('data-bs-toggle');
                const href = isAnchor ? (this.getAttribute('href') || '') : '';
                const isHashLink = isAnchor && (href.startsWith('#') || href.toLowerCase().startsWith('javascript:'));

                if ((isAnchor || isGetForm) && !hasToggle && !isHashLink) {
                    showPageLoading();

                    // Safety: auto-hide if navigation doesn't happen within 3s
                    setTimeout(() => hidePageLoading(), 3000);
                }
            });
        });

        // Hide overlay when new page is shown (bfcache or fast loads)
        window.addEventListener('pageshow', hidePageLoading);
    }

    /**
     * Toggle select all checkboxes
     */
    window.toggleSelectAll = function() {
        const selectAll = document.getElementById('selectAll');
        const checkboxes = document.querySelectorAll('.user-checkbox');
        
        if (selectAll) {
            checkboxes.forEach(checkbox => {
                checkbox.checked = selectAll.checked;
            });
            updateBulkActionButtons(selectAll.checked);
        }
    };

    /**
     * Bulk delete users
     */
    window.bulkDelete = function() {
        const selectedIds = getSelectedUserIds();
        if (selectedIds.length === 0) {
            showWarningToast('Vui lòng chọn ít nhất một người dùng để xóa');
            return;
        }

        showConfirmModal(
            'Xóa hàng loạt',
            `Bạn có chắc muốn xóa ${selectedIds.length} người dùng đã chọn? Hành động này không thể hoàn tác.`,
            'error',
            () => {
                document.getElementById('bulkUserIds').value = selectedIds.join(',');
                showLoadingState(document.getElementById('bulkForm'));
                document.getElementById('bulkForm').submit();
            }
        );
    };

    /**
     * Bulk change status
     */
    window.bulkChangeStatus = function(status) {
        const selectedIds = getSelectedUserIds();
        if (selectedIds.length === 0) {
            showWarningToast('Vui lòng chọn ít nhất một người dùng để thay đổi trạng thái');
            return;
        }

        const statusText = status === 0 ? 'kích hoạt' : 'vô hiệu hóa';
        
        showConfirmModal(
            status === 0 ? 'Kích hoạt hàng loạt' : 'Vô hiệu hóa hàng loạt',
            `Bạn có chắc muốn ${statusText} ${selectedIds.length} người dùng đã chọn?`,
            status === 0 ? 'success' : 'warning',
            () => {
                document.getElementById('bulkStatusUserIds').value = selectedIds.join(',');
                document.getElementById('bulkStatus').value = status;
                showLoadingState(document.getElementById('bulkStatusForm'));
                document.getElementById('bulkStatusForm').submit();
            }
        );
    };

    /**
     * Get selected user IDs
     */
    window.getSelectedUserIds = function() {
        const checkboxes = document.querySelectorAll('.user-checkbox:checked');
        return Array.from(checkboxes).map(cb => cb.value);
    };

    /**
     * Show confirmation modal using Bootstrap modal or fallback to confirm
     */
    function showConfirmModal(title, message, type, onConfirm) {
        // Try to use Bootstrap modal if available
        if (typeof bootstrap !== 'undefined' && document.getElementById('confirmModal')) {
            // Use custom modal (would need to be added to HTML)
            const modalElement = document.getElementById('confirmModal');
            const modal = new bootstrap.Modal(modalElement);
            
            document.getElementById('confirmModalTitle').textContent = title;
            document.getElementById('confirmModalBody').textContent = message;
            document.getElementById('confirmModalButton').className = `btn-${type}-custom btn-medium`;
            document.getElementById('confirmModalButton').onclick = function() {
                modal.hide();
                onConfirm();
            };
            
            modal.show();
        } else {
            // Fallback to native confirm
            if (confirm(`${title}\n\n${message}`)) {
                onConfirm();
            }
        }
    }

    /**
     * Show loading state for form/button
     */
    function showLoadingState(element) {
        const buttons = element.querySelectorAll('button[type="submit"], button');
        buttons.forEach(btn => {
            const originalText = btn.innerHTML;
            btn.disabled = true;
            btn.setAttribute('data-original-text', originalText);
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...';
        });
    }

    /**
     * Show page loading overlay
     */
    function showPageLoading() {
        let overlay = document.getElementById('pageLoadingOverlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'pageLoadingOverlay';
            overlay.className = 'page-loading-overlay';
            overlay.innerHTML = `
                <div class="loading-spinner">
                    <div class="spinner-border text-primary-custom" role="status">
                        <span class="visually-hidden">Đang tải...</span>
                    </div>
                    <p class="mt-3 body-base">Đang tải dữ liệu...</p>
                </div>
            `;
            document.body.appendChild(overlay);
        }
        overlay.style.display = 'flex';
    }

    function hidePageLoading() {
        const overlay = document.getElementById('pageLoadingOverlay');
        if (overlay) overlay.style.display = 'none';
    }

    /**
     * Get URL parameter
     */
    function getUrlParameter(name) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(name);
    }

    /**
     * Build URL with parameters
     */
    function buildUrlWithParams(params) {
        const url = new URL(window.location.href);
        Object.keys(params).forEach(key => {
            if (params[key]) {
                url.searchParams.set(key, params[key]);
            } else {
                url.searchParams.delete(key);
            }
        });
        return url.toString();
    }
})();
