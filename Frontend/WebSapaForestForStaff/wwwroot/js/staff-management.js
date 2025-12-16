/**
 * Staff Management JavaScript
 * UC55 - View List Staff
 * UC56 - Update Staff
 * UC57 - Deactivate / Delete Staff
 */

// Global variables
let currentPage = 1;
let pageSize = 20;
let totalPages = 1;

/**
 * Load staff list with filters
 */
function loadStaffList(page = 1) {
    currentPage = page;

    const filter = {
        searchKeyword: $('#searchKeyword').val(),
        position: $('#positionFilter').val(),
        status: $('#statusFilter').val() ? parseInt($('#statusFilter').val()) : null,
        departmentId: null,
        sortBy: $('#sortBy').val() || 'HireDate',
        sortDirection: $('#sortDirection').val() || 'desc',
        page: currentPage,
        pageSize: pageSize
    };

    // Show loading
    $('#staffTableBody').html(`
        <tr>
            <td colspan="9" class="text-center">
                <div class="spinner-border text-primary" role="status">
                    <span class="sr-only">Loading...</span>
                </div>
            </td>
        </tr>
    `);

    // Call API via AJAX
    $.ajax({
        url: '/StaffManagement/LoadStaffList',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(filter),
        success: function (response) {
            if (response.success) {
                renderStaffTable(response.data);
                updatePagination(response.page, response.pageSize, response.totalCount, response.totalPages);
            } else {
                toastr.error(response.message || 'Không thể tải danh sách nhân viên');
                $('#staffTableBody').html('<tr><td colspan="9" class="text-center">Không thể tải dữ liệu</td></tr>');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error loading staff list:', error);
            toastr.error('Đã xảy ra lỗi khi tải danh sách nhân viên');
            $('#staffTableBody').html('<tr><td colspan="9" class="text-center">Lỗi khi tải dữ liệu</td></tr>');
        }
    });
}

/**
 * Render staff table rows
 */
function renderStaffTable(staffList) {
    if (!staffList || staffList.length === 0) {
        $('#staffTableBody').html('<tr><td colspan="9" class="text-center">Không tìm thấy nhân viên nào</td></tr>');
        return;
    }

    let html = '';
    staffList.forEach(staff => {
        const avatar = staff.avatarUrl || '/images/default-avatar.png';
        const statusBadge = staff.status === 1 
            ? '<span class="badge badge-success">Đang hoạt động</span>' 
            : '<span class="badge badge-danger">Ngừng hoạt động</span>';

        html += `
            <tr>
                <td>
                    <img src="${avatar}" alt="${staff.fullName}" class="avatar" style="width: 40px; height: 40px; border-radius: 50%;">
                </td>
                <td>${staff.fullName}</td>
                <td>${staff.phone || 'N/A'}</td>
                <td>${staff.email}</td>
                <td>${staff.positions || 'N/A'}</td>
                <td>${formatCurrency(staff.baseSalary)}</td>
                <td>${statusBadge}</td>
                <td>${formatDate(staff.hireDate)}</td>
                <td class="text-right">
                    <div class="dropdown dropdown-action">
                        <a href="#" class="action-icon dropdown-toggle" data-toggle="dropdown" aria-expanded="false">
                            <i class="fas fa-ellipsis-v"></i>
                        </a>
                        <div class="dropdown-menu dropdown-menu-right">
                            <a class="dropdown-item" href="/StaffManagement/Edit/${staff.staffId}">
                                <i class="fas fa-edit m-r-5"></i> Chỉnh sửa
                            </a>
                            <a class="dropdown-item" href="#" onclick="openDeactivateModal(${staff.staffId}, '${staff.fullName}')">
                                <i class="fas fa-ban m-r-5"></i> Ngừng hoạt động
                            </a>
                        </div>
                    </div>
                </td>
            </tr>
        `;
    });

    $('#staffTableBody').html(html);
}

/**
 * Update pagination UI
 */
function updatePagination(page, size, totalCount, totalPagesCount) {
    currentPage = page;
    pageSize = size;
    totalPages = totalPagesCount;

    // Update info text
    const start = (page - 1) * size + 1;
    const end = Math.min(page * size, totalCount);
    $('#staffTableInfo').text(`Hiển thị ${start} đến ${end} trong tổng số ${totalCount} bản ghi`);

    // Generate pagination buttons
    let paginationHtml = '';

    // Previous button
    if (page > 1) {
        paginationHtml += `<li class="paginate_button page-item previous">
            <a href="#" class="page-link" onclick="loadStaffList(${page - 1}); return false;">Trước</a>
        </li>`;
    } else {
        paginationHtml += `<li class="paginate_button page-item previous disabled">
            <a href="#" class="page-link">Trước</a>
        </li>`;
    }

    // Page numbers
    const maxPagesToShow = 5;
    let startPage = Math.max(1, page - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(totalPages, startPage + maxPagesToShow - 1);

    if (endPage - startPage < maxPagesToShow - 1) {
        startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
        const activeClass = i === page ? 'active' : '';
        paginationHtml += `<li class="paginate_button page-item ${activeClass}">
            <a href="#" class="page-link" onclick="loadStaffList(${i}); return false;">${i}</a>
        </li>`;
    }

    // Next button
    if (page < totalPages) {
        paginationHtml += `<li class="paginate_button page-item next">
            <a href="#" class="page-link" onclick="loadStaffList(${page + 1}); return false;">Sau</a>
        </li>`;
    } else {
        paginationHtml += `<li class="paginate_button page-item next disabled">
            <a href="#" class="page-link">Sau</a>
        </li>`;
    }

    $('#staffPagination').html(paginationHtml);
}

/**
 * Filter staff list
 */
function filterStaff() {
    loadStaffList(1); // Reset to page 1 when filtering
}

/**
 * Search staff (triggered on Enter key)
 */
$('#searchKeyword').on('keypress', function (e) {
    if (e.which === 13) { // Enter key
        filterStaff();
    }
});

/**
 * Open deactivate modal
 */
function openDeactivateModal(staffId, staffName) {
    $('#deactivateStaffId').val(staffId);
    $('#deactivateStaffName').text(staffName);
    $('#deactivateReason').val('');
    $('#deactivateModal').modal('show');
}

/**
 * Submit deactivate request
 */
function submitDeactivate() {
    const staffId = parseInt($('#deactivateStaffId').val());
    const reason = $('#deactivateReason').val();

    const dto = {
        staffId: staffId,
        reason: reason
    };

    // Disable button to prevent double-click
    const $btn = $('#deactivateModal .btn-danger');
    $btn.prop('disabled', true).text('Đang xử lý...');

    $.ajax({
        url: '/StaffManagement/Deactivate',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(dto),
        success: function (response) {
            if (response.success) {
                toastr.success(response.message || 'Ngừng hoạt động nhân viên thành công');
                $('#deactivateModal').modal('hide');
                loadStaffList(currentPage); // Reload current page
            } else {
                toastr.error(response.message || 'Không thể ngừng hoạt động nhân viên');
            }
        },
        error: function (xhr, status, error) {
            console.error('Error deactivating staff:', error);
            toastr.error('Đã xảy ra lỗi khi ngừng hoạt động nhân viên');
        },
        complete: function () {
            $btn.prop('disabled', false).text('Ngừng hoạt động');
        }
    });
}

/**
 * Sort staff by salary
 */
function sortBySalary(direction) {
    $('#sortBy').val('BaseSalary');
    $('#sortDirection').val(direction);
    filterStaff();
}

/**
 * Sort staff by position
 */
function sortByPosition(direction) {
    $('#sortBy').val('Position');
    $('#sortDirection').val(direction);
    filterStaff();
}

/**
 * Format currency (VND)
 */
function formatCurrency(amount) {
    if (!amount) return '0 VND';
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

/**
 * Format date (DateOnly from API)
 */
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    
    // Handle DateOnly format from .NET (YYYY-MM-DD)
    const parts = dateString.split('-');
    if (parts.length === 3) {
        const year = parts[0];
        const month = parts[1];
        const day = parts[2];
        return `${day}/${month}/${year}`;
    }
    
    // Fallback to Date parsing
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return 'N/A';
    
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

