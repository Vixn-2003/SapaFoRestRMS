/**
 * Manager Customer Management JavaScript
 * UC145 - View List Customer
 * UC146 - View Customer Detail
 * UC147 - Update VIP Status
 */

// Global variables for pagination
let currentPage = 1;
let pageSize = 20;

/**
 * Load customer list with filters
 */
function loadCustomerList(page = 1) {
    currentPage = page;

    // Build filter data from form
    const filterData = {
        keyword: $('#searchKeyword').val(),
        isVip: $('#isVip').val() ? ($('#isVip').val() === 'true') : null,
        minSpending: $('#minSpending').val() ? parseFloat($('#minSpending').val()) : null,
        maxSpending: $('#maxSpending').val() ? parseFloat($('#maxSpending').val()) : null,
        minVisits: $('#minVisits').val() ? parseInt($('#minVisits').val()) : null,
        maxVisits: $('#maxVisits').val() ? parseInt($('#maxVisits').val()) : null,
        sortBy: $('#sortBy').val() || 'TotalSpending',
        sortDirection: $('#sortDirection').val() || 'desc',
        pageNumber: currentPage,
        pageSize: pageSize
    };

    // Show loading state
    const tableBody = $('#customerTableBody');
    tableBody.html(`
        <tr>
            <td colspan="6" class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="sr-only">Đang tải...</span>
                </div>
                <div class="mt-2">Đang tải danh sách khách hàng...</div>
            </td>
        </tr>
    `);

    // Update URL with filter parameters
    const urlParams = new URLSearchParams();
    Object.keys(filterData).forEach(key => {
        if (filterData[key] !== null && filterData[key] !== undefined && filterData[key] !== '') {
            urlParams.set(key, filterData[key]);
        }
    });

    // Navigate to update URL and reload page
    const newUrl = window.location.pathname + (urlParams.toString() ? '?' + urlParams.toString() : '');
    window.location.href = newUrl;
}

/**
 * Sort by column
 */
function sortBy(sortField) {
    const currentSortBy = '@Model.Filters.SortBy';
    const currentSortDirection = '@Model.Filters.SortDirection';

    let newSortDirection = 'asc';
    if (sortField === currentSortBy && currentSortDirection === 'asc') {
        newSortDirection = 'desc';
    }

    document.getElementById('sortBy').value = sortField;
    document.getElementById('sortDirection').value = newSortDirection;
    loadCustomerList(1);
}

/**
 * Go to specific page
 */
function goToPage(page) {
    loadCustomerList(page);
}

/**
 * Reset all filters
 */
function resetFilters() {
    $('#searchKeyword').val('');
    $('#isVip').val('');
    $('#minSpending').val('');
    $('#maxSpending').val('');
    $('#minVisits').val('');
    $('#maxVisits').val('');
    $('#sortBy').val('TotalSpending');
    $('#sortDirection').val('desc');
    loadCustomerList(1);
}

/**
 * Refresh data
 */
function refreshData() {
    // Show loading state
    const refreshBtn = document.querySelector('button[onclick="refreshData()"]');
    const originalText = refreshBtn.innerHTML;
    refreshBtn.innerHTML = '<i class="mdi mdi-loading mdi-spin me-1"></i>Đang tải...';
    refreshBtn.disabled = true;

    // Reload the page
    setTimeout(() => {
        window.location.reload();
    }, 500);
}

/**
 * Open VIP update modal
 */
function openVipUpdateModal(customerId, isVip) {
    currentCustomerId = customerId;
    currentVipStatus = isVip === 'true';

    const modalText = currentVipStatus
        ? 'Bạn có chắc muốn hủy trạng thái VIP của khách hàng này?'
        : 'Bạn có chắc muốn cấp trạng thái VIP cho khách hàng này?';

    document.getElementById('vipModalText').textContent = modalText;

    const confirmBtn = document.getElementById('confirmVipUpdate');
    confirmBtn.className = currentVipStatus ? 'btn btn-danger' : 'btn btn-warning';
    confirmBtn.textContent = currentVipStatus ? 'Hủy VIP' : 'Cấp VIP';

    const modal = new bootstrap.Modal(document.getElementById('vipUpdateModal'));
    modal.show();
}

/**
 * Confirm VIP update
 */
function confirmVipUpdate() {
    if (!currentCustomerId) return;

    const formData = new FormData();
    formData.append('__RequestVerificationToken', '@antiToken');
    formData.append('customerId', currentCustomerId);
    formData.append('isVip', (!currentVipStatus).toString());

    // Close modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('vipUpdateModal'));
    modal.hide();

    // Show loading
    const confirmBtn = document.getElementById('confirmVipUpdate');
    const originalText = confirmBtn.textContent;
    confirmBtn.innerHTML = '<i class="mdi mdi-loading mdi-spin me-1"></i>Đang xử lý...';
    confirmBtn.disabled = true;

        fetch('/ManagerCustomer/UpdateCustomerVip', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (response.ok) {
            // Reload page to show updated data
            window.location.reload();
        } else {
            return response.text().then(text => {
                throw new Error(text || 'Không thể cập nhật trạng thái VIP');
            });
        }
    })
    .catch(error => {
        console.error('Error:', error);
        alert('Đã xảy ra lỗi khi cập nhật trạng thái VIP: ' + error.message);
        confirmBtn.innerHTML = originalText;
        confirmBtn.disabled = false;
    });
}

/**
 * Format currency
 */
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN').format(amount) + ' đ';
}

/**
 * Format date
 */
function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN');
}

// Global variables for VIP modal
let currentCustomerId = null;
let currentVipStatus = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Auto-hide success/error messages after 5 seconds
    setTimeout(function() {
        const alerts = document.querySelectorAll('.alert');
        alerts.forEach(alert => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);

    // Handle Enter key in filter inputs
    document.querySelectorAll('#filterForm input').forEach(input => {
        input.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                loadCustomerList(1);
            }
        });
    });

    // Handle filter form submission
    document.getElementById('filterForm').addEventListener('submit', function(e) {
        e.preventDefault();
        loadCustomerList(1);
    });

    // Smooth scroll to messages if they exist
    const alerts = document.querySelectorAll('.alert');
    if (alerts.length > 0) {
        alerts[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
});
