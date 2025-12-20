/**
 * Staff Management Module
 * Clean architecture: State management, API calls, UI rendering separated
 */

// ============================================================================
// STATE MANAGEMENT
// ============================================================================
const StaffManagement = {
    state: {
        currentPage: 1,
        pageSize: 20,
        totalPages: 0,
        totalCount: 0,
        filters: {
            searchKeyword: '',
            position: '',
            status: null,
            sortBy: 'HireDate',
            sortDirection: 'desc'
        },
        isLoading: false
    },

    /**
     * Initialize module
     */
    init() {
        this.bindEvents();
        this.loadStaffList();
    },

    /**
     * Bind all event listeners
     */
    bindEvents() {
        // Filter button
        $('#filterBtn').on('click', () => this.handleFilter());

        // Search on Enter key
        $('#searchKeyword').on('keypress', (e) => {
            if (e.which === 13) {
                this.handleFilter();
            }
        });

        // Sort/Status dropdowns change
        $('#sortBy, #sortDirection, #statusFilter').on('change', () => {
            this.handleFilter();
        });
    },

    /**
     * Handle filter button click
     */
    handleFilter() {
        // Read current filter values from DOM
        this.state.filters = {
            searchKeyword: $('#searchKeyword').val().trim(),
            position: ($('#positionFilter').val() || '').toString().trim(),
            status: $('#statusFilter').val() ? parseInt($('#statusFilter').val()) : null,
            sortBy: $('#sortBy').val() || 'HireDate',
            sortDirection: $('#sortDirection').val() || 'desc'
        };

        // Reset to page 1 when filtering
        this.state.currentPage = 1;

        this.loadStaffList();
    },

    /**
     * Load staff list from API
     */
    async loadStaffList(page = null) {
        if (page !== null) {
            this.state.currentPage = page;
        }

        // Prevent multiple concurrent requests
        if (this.state.isLoading) return;

        this.state.isLoading = true;
        this.showLoading();

        const requestData = {
            searchKeyword: this.state.filters.searchKeyword,
            position: this.state.filters.position,
            status: this.state.filters.status,
            departmentId: null,
            sortBy: this.state.filters.sortBy,
            sortDirection: this.state.filters.sortDirection,
            page: this.state.currentPage,
            pageSize: this.state.pageSize
        };

        try {
            console.log('📤 Sending request to get staff list...', requestData);
            
            const response = await $.ajax({
                url: '/StaffManagement/GetStaffList',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(requestData)
            });

            console.log('📥 Received response:', response);
            this.state.isLoading = false;

            // Normalize response
            const normalizedData = this.normalizeApiResponse(response);
            console.log('✅ Normalized data:', normalizedData);

            if (normalizedData.success) {
                this.state.totalCount = normalizedData.totalCount;
                this.state.totalPages = normalizedData.totalPages;
                this.renderStaffTable(normalizedData.data);
                this.renderPagination();
                console.log('✨ Rendered table successfully!');
            } else {
                console.error('❌ Response indicates failure:', normalizedData.message);
                this.showError(normalizedData.message || 'Không thể tải danh sách nhân viên');
            }
        } catch (error) {
            this.state.isLoading = false;
            console.error('❌ Error loading staff list:', error);
            console.error('Error details:', error.responseText || error.message);
            this.showError('Đã xảy ra lỗi khi tải danh sách nhân viên. Vui lòng thử lại.');
        }
    },

    /**
     * Normalize API response to consistent format
     */
    normalizeApiResponse(response) {
        return {
            success: response?.success ?? false,
            data: response?.data ?? [],
            page: response?.page ?? 1,
            pageSize: response?.pageSize ?? 20,
            totalCount: response?.totalCount ?? 0,
            totalPages: response?.totalPages ?? 0,
            message: response?.message
        };
    },

    /**
     * Show loading state
     */
    showLoading() {
        $('#filterBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Đang tải...');
        $('#staffTableBody').html(`
            <tr>
                <td colspan="9" class="text-center py-5">
                    <div class="spinner-border text-primary" role="status">
                        <span class="sr-only">Đang tải...</span>
                    </div>
                    <p class="mt-2 text-muted">Đang tải danh sách nhân viên...</p>
                </td>
            </tr>
        `);
    },

    /**
     * Show error state with retry option
     */
    showError(message) {
        toastr.error(message);
        $('#filterBtn').prop('disabled', false).html('<i class="fas fa-search"></i>');
        $('#staffTableBody').html(`
            <tr>
                <td colspan="9" class="text-center py-5">
                    <i class="fas fa-exclamation-triangle fa-3x text-danger mb-3"></i>
                    <h5 class="text-danger">${this.escapeHtml(message)}</h5>
                    <button class="btn btn-primary mt-3" onclick="StaffManagement.loadStaffList()">
                        <i class="fas fa-redo"></i> Thử lại
                    </button>
                </td>
            </tr>
        `);
    },

    /**
     * Render staff table with data
     */
    renderStaffTable(staffList) {
        $('#filterBtn').prop('disabled', false).html('<i class="fas fa-search"></i>');

        if (!staffList || staffList.length === 0) {
            $('#staffTableBody').html(`
                <tr>
                    <td colspan="9" class="text-center py-5">
                        <i class="fas fa-users fa-3x text-muted mb-3"></i>
                        <h5 class="text-muted">Không tìm thấy nhân viên nào</h5>
                        <p class="text-muted">Thử thay đổi bộ lọc hoặc từ khóa tìm kiếm</p>
                    </td>
                </tr>
            `);
            this.updateTableInfo(0, 0, 0);
            $('#staffPagination').html('');
            return;
        }

        const html = staffList.map(staff => this.renderStaffRow(staff)).join('');
        $('#staffTableBody').html(html);

        // Bind click handlers for detail buttons - use both jQuery and native JS for maximum compatibility
        // Use setTimeout to ensure DOM is ready and other scripts have loaded
        setTimeout(() => {
            const tableBody = document.getElementById('staffTableBody');
            if (!tableBody) return;

            // Remove any existing handlers first (jQuery)
            $('#staffTableBody').off('click', '.view-detail-btn');
            
            // Also remove native event listeners if any
            const existingButtons = tableBody.querySelectorAll('.view-detail-btn');
            existingButtons.forEach(btn => {
                const newBtn = btn.cloneNode(true);
                btn.parentNode.replaceChild(newBtn, btn);
            });

            // Add jQuery handler (for compatibility)
            $('#staffTableBody').on('click', '.view-detail-btn', function(e) {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();
                
                const staffId = $(this).data('staff-id') || $(this).attr('data-staff-id') || $(this).attr('href')?.match(/\/(\d+)$/)?.[1];
                
                if (staffId) {
                    console.log('Navigating to staff detail:', staffId);
                    window.location.href = `/StaffManagement/Details/${staffId}`;
                } else {
                    console.error('Staff ID not found');
                }
                return false;
            });

            // Also add native JS handler as backup (runs first)
            tableBody.addEventListener('click', function(e) {
                const btn = e.target.closest('.view-detail-btn');
                if (btn) {
                    e.preventDefault();
                    e.stopPropagation();
                    e.stopImmediatePropagation();
                    
                    const staffId = btn.getAttribute('data-staff-id') || btn.getAttribute('href')?.match(/\/(\d+)$/)?.[1];
                    
                    if (staffId) {
                        console.log('Navigating to staff detail (native):', staffId);
                        window.location.href = `/StaffManagement/Details/${staffId}`;
                    }
                    return false;
                }
            }, true); // Use capture phase to run before other handlers
        }, 200);

        // Update info
        const start = (this.state.currentPage - 1) * this.state.pageSize + 1;
        const end = Math.min(this.state.currentPage * this.state.pageSize, this.state.totalCount);
        this.updateTableInfo(start, end, this.state.totalCount);
    },

    /**
     * Render single staff row (safe from XSS)
     */
    renderStaffRow(staff) {
        const avatar = this.escapeHtml(staff.avatarUrl || '/assets/images/faces/face8.jpg');
        const fullName = this.escapeHtml(staff.fullName);
        const phone = this.escapeHtml(staff.phone || 'N/A');
        const email = this.escapeHtml(staff.email);
        const positions = this.escapeHtml(staff.positions || 'N/A');
        const baseSalary = this.formatCurrency(staff.baseSalary);
        const statusBadge = staff.status === 0 // 0 = Active, 1 = Inactive
            ? '<span class="badge badge-success">Đang hoạt động</span>'
            : '<span class="badge badge-danger">Ngừng hoạt động</span>';
        const hireDate = this.formatDate(staff.hireDate);
        
        // Ensure staffId is a number (not escaped, safe for URL)
        const staffId = parseInt(staff.staffId) || 0;

        return `
            <tr data-staff-id="${staffId}">
                <td>
                    <img src="${avatar}" alt="${fullName}" class="avatar" 
                         style="width: 40px; height: 40px; border-radius: 50%; object-fit: cover;">
                </td>
                <td>${fullName}</td>
                <td>${phone}</td>
                <td>${email}</td>
                <td>${positions}</td>
                <td>${baseSalary}</td>
                <td>${statusBadge}</td>
                <td>${hireDate}</td>
                <td class="text-right">
                    <a href="/StaffManagement/Details/${staffId}" 
                       class="btn btn-sm btn-info view-detail-btn" 
                       data-staff-id="${staffId}">
                        <i class="far fa-eye"></i> Xem chi tiết
                    </a>
            </tr>
        `;
    },

    /**
     * Render pagination controls
     */
    renderPagination() {
        if (this.state.totalPages <= 1) {
            $('#staffPagination').html('');
            return;
        }

        const maxPagesToShow = 5;
        let startPage = Math.max(1, this.state.currentPage - Math.floor(maxPagesToShow / 2));
        let endPage = Math.min(this.state.totalPages, startPage + maxPagesToShow - 1);

        if (endPage - startPage < maxPagesToShow - 1) {
            startPage = Math.max(1, endPage - maxPagesToShow + 1);
        }

        let html = '';

        // Previous button
        if (this.state.currentPage > 1) {
            html += `<li class="paginate_button page-item previous">
                <a href="#" class="page-link" data-page="${this.state.currentPage - 1}">Trước</a>
            </li>`;
        } else {
            html += `<li class="paginate_button page-item previous disabled">
                <a href="#" class="page-link">Trước</a>
            </li>`;
        }

        // Page numbers
        for (let i = startPage; i <= endPage; i++) {
            const activeClass = i === this.state.currentPage ? 'active' : '';
            html += `<li class="paginate_button page-item ${activeClass}">
                <a href="#" class="page-link" data-page="${i}">${i}</a>
            </li>`;
        }

        // Next button
        if (this.state.currentPage < this.state.totalPages) {
            html += `<li class="paginate_button page-item next">
                <a href="#" class="page-link" data-page="${this.state.currentPage + 1}">Sau</a>
            </li>`;
        } else {
            html += `<li class="paginate_button page-item next disabled">
                <a href="#" class="page-link">Sau</a>
            </li>`;
        }

        $('#staffPagination').html(html);

        // Bind pagination clicks (event delegation)
        $('#staffPagination').off('click').on('click', 'a[data-page]', (e) => {
            e.preventDefault();
            const page = parseInt($(e.currentTarget).data('page'));
            if (page && page !== this.state.currentPage) {
                this.loadStaffList(page);
            }
        });
    },

    /**
     * Update table info text
     */
    updateTableInfo(start, end, total) {
        $('#staffTableInfo').text(`Hiển thị ${start} đến ${end} trong tổng số ${total} bản ghi`);
    },

    /**
     * Escape HTML to prevent XSS
     */
    escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    /**
     * Format currency (VND)
     */
    formatCurrency(amount) {
        if (!amount) return '0 ₫';
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    },

    /**
     * Format date (DateOnly from API)
     */
    formatDate(dateString) {
        if (!dateString) return 'N/A';

        // Handle DateOnly format from .NET (YYYY-MM-DD)
        const parts = dateString.split('-');
        if (parts.length === 3) {
            return `${parts[2]}/${parts[1]}/${parts[0]}`;
        }

        // Fallback
        const date = new Date(dateString);
        if (isNaN(date.getTime())) return 'N/A';

        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        return `${day}/${month}/${year}`;
    },

};

// ============================================================================
// DEACTIVATE STAFF MODULE
// ============================================================================
const StaffDeactivate = {
    currentStaffId: null,
    currentStaffName: '',

    /**
     * Open deactivate modal
     */
    open(staffId, staffName) {
        this.currentStaffId = staffId;
        this.currentStaffName = staffName;
        $('#deactivateStaffName').text(staffName);
        $('#deactivateReason').val('');
        $('#deactivateModal').modal('show');
    },

    /**
     * Submit deactivate request
     */
    async submit() {
        if (!this.currentStaffId) {
            toastr.error('ID nhân viên không hợp lệ');
            return;
        }

        const reason = $('#deactivateReason').val().trim();
        const $btn = $('#deactivateModal .btn-danger');
        const originalText = $btn.html();

        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Đang xử lý...');

        try {
            const response = await $.ajax({
                url: '/StaffManagement/Deactivate',
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                data: JSON.stringify({
                    staffId: this.currentStaffId,
                    reason: reason
                })
            });

            if (response.success) {
                toastr.success(response.message || 'Ngừng hoạt động nhân viên thành công');
                $('#deactivateModal').modal('hide');
                StaffManagement.loadStaffList(); // Reload current page
            } else {
                toastr.error(response.message || 'Không thể ngừng hoạt động nhân viên');
            }
        } catch (error) {
            console.error('Error deactivating staff:', error);
            toastr.error('Đã xảy ra lỗi khi ngừng hoạt động nhân viên');
        } finally {
            $btn.prop('disabled', false).html(originalText);
        }
    }
};

// ============================================================================
// ACTIVATE STAFF MODULE
// ============================================================================
const StaffActivate = {
    /**
     * Activate staff
     */
    async activate(staffId, staffName) {
        if (!confirm(`Bạn có chắc chắn muốn kích hoạt lại nhân viên "${staffName}"?`)) {
            return;
        }

        try {
            const response = await $.ajax({
                url: '/StaffManagement/Activate',
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                data: JSON.stringify({
                    staffId: staffId
                })
            });

            if (response.success) {
                toastr.success(response.message || 'Kích hoạt nhân viên thành công');
                StaffManagement.loadStaffList(); // Reload current page
            } else {
                toastr.error(response.message || 'Không thể kích hoạt nhân viên');
            }
        } catch (error) {
            console.error('Error activating staff:', error);
            toastr.error('Đã xảy ra lỗi khi kích hoạt nhân viên');
        }
    }
};

// ============================================================================
// RESET PASSWORD MODULE
// ============================================================================
const StaffResetPassword = {
    /**
     * Reset staff password
     */
    async reset(staffId, staffName) {
        if (!confirm(`Bạn có chắc chắn muốn reset mật khẩu cho nhân viên "${staffName}"?\n\nMật khẩu mới sẽ được gửi qua email.`)) {
            return;
        }

        try {
            const response = await $.ajax({
                url: '/StaffManagement/ResetPassword',
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                data: JSON.stringify({
                    staffId: staffId
                })
            });

            if (response.success) {
                toastr.success(response.message || 'Reset mật khẩu thành công. Email đã được gửi.');
            } else {
                toastr.error(response.message || 'Không thể reset mật khẩu');
            }
        } catch (error) {
            console.error('Error resetting password:', error);
            toastr.error('Đã xảy ra lỗi khi reset mật khẩu');
        }
    }
};

// ============================================================================
// INITIALIZATION
// ============================================================================
$(document).ready(function () {
    // Initialize staff management
    StaffManagement.init();
});
