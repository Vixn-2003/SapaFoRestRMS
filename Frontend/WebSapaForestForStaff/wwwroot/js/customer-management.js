/**
 * Customer Management Module - JavaScript
 * UC145 - View List Customer
 * UC146 - View Customer Detail
 * UC147 - Update VIP Status
 */

// Global state
let currentPage = 1;
let currentPageSize = 20;
let currentFilters = {};
let currentCustomerId = null;
let currentCustomerName = '';
let currentVipStatus = false;

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    // Only initialize if on customer management page
    if (document.getElementById('customerTableBody')) {
        initializeCustomerList();
    }

    // Initialize VIP modal if present
    if (document.getElementById('vipUpdateModal')) {
        initializeVipModal();
    }
});

/**
 * Initialize customer list page
 */
function initializeCustomerList() {
    // Load initial customer data
    loadCustomers();

    // Search button click
    document.getElementById('btnSearch')?.addEventListener('click', function () {
        currentPage = 1; // Reset to first page
        loadCustomers();
    });

    // Reset button click
    document.getElementById('btnReset')?.addEventListener('click', function () {
        resetFilters();
    });

    // Search on Enter key
    document.getElementById('searchKeyword')?.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            currentPage = 1;
            loadCustomers();
        }
    });
}

/**
 * Load customers with current filters
 */
async function loadCustomers() {
    showLoading(true);

    // Collect filter values
    const filters = {
        page: currentPage,
        pageSize: currentPageSize,
        searchKeyword: document.getElementById('searchKeyword')?.value || '',
        isVipOnly: document.getElementById('vipFilter')?.value === '' ? null : document.getElementById('vipFilter')?.value === 'true',
        minSpending: parseFloat(document.getElementById('minSpending')?.value) || null,
        minVisits: parseInt(document.getElementById('minVisits')?.value) || null,
        sortBy: document.getElementById('sortBy')?.value || 'TotalSpending',
        sortDirection: 'desc'
    };

    currentFilters = filters;

    try {
        const response = await fetch('/CustomerManagement/LoadCustomers', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(filters)
        });

        const result = await response.json();

        if (result.success) {
            renderCustomerTable(result.data);
            renderPagination(result.page, result.pageSize, result.totalCount, result.totalPages);
        } else {
            showToast('Error loading customers: ' + result.message, 'error');
            document.getElementById('noResultsMessage').style.display = 'block';
        }
    } catch (error) {
        console.error('Error loading customers:', error);
        showToast('An error occurred while loading customers', 'error');
    } finally {
        showLoading(false);
    }
}

/**
 * Render customer table
 */
function renderCustomerTable(customers) {
    const tbody = document.getElementById('customerTableBody');
    
    if (!customers || customers.length === 0) {
        tbody.innerHTML = '';
        document.getElementById('noResultsMessage').style.display = 'block';
        document.getElementById('customerTableContainer').querySelector('table').style.display = 'none';
        return;
    }

    document.getElementById('noResultsMessage').style.display = 'none';
    document.getElementById('customerTableContainer').querySelector('table').style.display = 'table';

    tbody.innerHTML = customers.map(customer => `
        <tr>
            <td>
                <strong>${escapeHtml(customer.fullName)}</strong>
                ${customer.isVip ? '<span class="badge bg-warning text-dark ms-2"><i class="fas fa-crown"></i> VIP</span>' : ''}
            </td>
            <td>${customer.phone || 'N/A'}</td>
            <td>${customer.email || 'N/A'}</td>
            <td class="text-end">${formatCurrency(customer.totalSpending)}</td>
            <td class="text-center">
                <span class="badge bg-primary">${customer.totalVisits}</span>
            </td>
            <td class="text-center">
                ${customer.isVip 
                    ? '<span class="badge bg-warning text-dark">VIP</span>' 
                    : '<span class="badge bg-secondary">Regular</span>'}
            </td>
            <td class="text-center">${customer.lastVisit ? formatDate(customer.lastVisit) : 'N/A'}</td>
            <td class="text-center">
                <div class="btn-group btn-group-sm" role="group">
                    <a href="/CustomerManagement/Detail/${customer.customerId}" 
                       class="btn btn-info" 
                       title="View Details">
                        <i class="fas fa-eye"></i>
                    </a>
                    <button type="button" 
                            class="btn btn-warning" 
                            onclick="openVipUpdateModal(${customer.customerId}, ${customer.isVip}, '${escapeHtml(customer.fullName)}')"
                            title="Update VIP Status">
                        <i class="fas fa-crown"></i>
                    </button>
                </div>
            </td>
        </tr>
    `).join('');
}

/**
 * Render pagination
 */
function renderPagination(page, pageSize, totalCount, totalPages) {
    const paginationInfo = document.getElementById('paginationInfo');
    const paginationList = document.getElementById('paginationList');

    if (totalCount === 0) {
        paginationInfo.textContent = '';
        paginationList.innerHTML = '';
        return;
    }

    const startItem = (page - 1) * pageSize + 1;
    const endItem = Math.min(page * pageSize, totalCount);
    paginationInfo.textContent = `Showing ${startItem} to ${endItem} of ${totalCount} customers`;

    // Generate pagination buttons
    let paginationHTML = '';

    // Previous button
    paginationHTML += `
        <li class="page-item ${page === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="changePage(${page - 1}); return false;">
                <i class="fas fa-chevron-left"></i>
            </a>
        </li>
    `;

    // Page numbers
    const maxVisiblePages = 5;
    let startPage = Math.max(1, page - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);

    if (endPage - startPage < maxVisiblePages - 1) {
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    if (startPage > 1) {
        paginationHTML += `<li class="page-item"><a class="page-link" href="#" onclick="changePage(1); return false;">1</a></li>`;
        if (startPage > 2) {
            paginationHTML += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
    }

    for (let i = startPage; i <= endPage; i++) {
        paginationHTML += `
            <li class="page-item ${i === page ? 'active' : ''}">
                <a class="page-link" href="#" onclick="changePage(${i}); return false;">${i}</a>
            </li>
        `;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            paginationHTML += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
        }
        paginationHTML += `<li class="page-item"><a class="page-link" href="#" onclick="changePage(${totalPages}); return false;">${totalPages}</a></li>`;
    }

    // Next button
    paginationHTML += `
        <li class="page-item ${page === totalPages ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="changePage(${page + 1}); return false;">
                <i class="fas fa-chevron-right"></i>
            </a>
        </li>
    `;

    paginationList.innerHTML = paginationHTML;
}

/**
 * Change page
 */
function changePage(page) {
    if (page < 1) return;
    currentPage = page;
    loadCustomers();
}

/**
 * Reset filters
 */
function resetFilters() {
    document.getElementById('searchKeyword').value = '';
    document.getElementById('vipFilter').value = '';
    document.getElementById('minSpending').value = '';
    document.getElementById('minVisits').value = '';
    document.getElementById('sortBy').value = 'TotalSpending';
    currentPage = 1;
    loadCustomers();
}

/**
 * Show/hide loading spinner
 */
function showLoading(show) {
    const spinner = document.getElementById('loadingSpinner');
    const tableContainer = document.getElementById('customerTableContainer');
    
    if (spinner) {
        spinner.style.display = show ? 'block' : 'none';
    }
    if (tableContainer) {
        tableContainer.style.display = show ? 'none' : 'block';
    }
}

/**
 * Initialize VIP Modal
 */
function initializeVipModal() {
    // Check VIP Criteria button
    document.getElementById('btnCheckCriteria')?.addEventListener('click', checkVipCriteria);

    // Confirm Update button
    document.getElementById('btnConfirmUpdate')?.addEventListener('click', confirmVipUpdate);
}

/**
 * Open VIP Update Modal
 */
function openVipUpdateModal(customerId, isVip, customerName) {
    currentCustomerId = customerId;
    currentCustomerName = customerName;
    currentVipStatus = isVip;

    // Set modal content
    document.getElementById('modalCustomerId').textContent = customerId;
    document.getElementById('modalCustomerName').textContent = customerName;
    
    const statusBadge = document.getElementById('modalCurrentVipStatus');
    if (isVip) {
        statusBadge.className = 'badge bg-warning text-dark';
        statusBadge.innerHTML = '<i class="fas fa-crown"></i> VIP Customer';
    } else {
        statusBadge.className = 'badge bg-secondary';
        statusBadge.textContent = 'Regular Customer';
    }

    // Set new status to opposite of current
    document.getElementById('newVipStatus').value = (!isVip).toString();

    // Reset form
    document.getElementById('manualOverride').checked = false;
    document.getElementById('changeReason').value = '';
    document.getElementById('vipCriteriaResult').style.display = 'none';

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('vipUpdateModal'));
    modal.show();
}

/**
 * Check VIP Criteria
 */
async function checkVipCriteria() {
    if (!currentCustomerId) return;

    try {
        const response = await fetch(`/CustomerManagement/CheckVipCriteria/${currentCustomerId}`);
        const result = await response.json();

        const criteriaDiv = document.getElementById('vipCriteriaResult');
        const contentDiv = document.getElementById('vipCriteriaContent');

        if (result.success && result.data) {
            const data = result.data;
            
            if (data.meetsCriteria) {
                criteriaDiv.className = 'alert alert-success';
                contentDiv.innerHTML = `
                    <i class="fas fa-check-circle me-2"></i>
                    <strong>Customer meets VIP criteria!</strong><br>
                    Average amount per person: <strong>${formatCurrency(data.averageAmountPerPerson)}</strong><br>
                    <small>${escapeHtml(data.reason)}</small>
                `;
            } else {
                criteriaDiv.className = 'alert alert-warning';
                contentDiv.innerHTML = `
                    <i class="fas fa-exclamation-triangle me-2"></i>
                    <strong>Customer does not meet VIP criteria</strong><br>
                    Average amount per person: <strong>${formatCurrency(data.averageAmountPerPerson)}</strong><br>
                    <small>${escapeHtml(data.reason)}</small><br>
                    <small class="text-muted">You can still update VIP status by checking "Manual Override"</small>
                `;
            }
            
            criteriaDiv.style.display = 'block';
        } else {
            showToast('Error checking VIP criteria: ' + result.message, 'error');
        }
    } catch (error) {
        console.error('Error checking VIP criteria:', error);
        showToast('An error occurred while checking VIP criteria', 'error');
    }
}

/**
 * Confirm VIP Update
 */
async function confirmVipUpdate() {
    if (!currentCustomerId) return;

    const newVipStatus = document.getElementById('newVipStatus').value === 'true';
    const manualOverride = document.getElementById('manualOverride').checked;
    const reason = document.getElementById('changeReason').value.trim();

    // Confirmation
    const action = newVipStatus ? 'upgrade to VIP' : 'downgrade from VIP';
    if (!confirm(`Are you sure you want to ${action} customer "${currentCustomerName}"?`)) {
        return;
    }

    const data = {
        customerId: currentCustomerId,
        isVip: newVipStatus,
        reason: reason || null,
        isManualOverride: manualOverride
    };

    try {
        const response = await fetch('/CustomerManagement/UpdateVipStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.success) {
            showToast(result.message || 'VIP status updated successfully!', 'success');
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('vipUpdateModal'));
            modal.hide();

            // Reload customer list if on index page
            if (document.getElementById('customerTableBody')) {
                loadCustomers();
            } else {
                // Reload page if on detail page
                setTimeout(() => location.reload(), 1500);
            }
        } else {
            showToast('Error: ' + result.message, 'error');
        }
    } catch (error) {
        console.error('Error updating VIP status:', error);
        showToast('An error occurred while updating VIP status', 'error');
    }
}

/**
 * Show toast notification
 */
function showToast(message, type = 'info') {
    // Check if toast notification system exists
    if (typeof window.showToast === 'function') {
        window.showToast(message, type);
        return;
    }

    // Fallback to alert
    alert(message);
}

/**
 * Format currency
 */
function formatCurrency(amount) {
    if (amount === null || amount === undefined) return '0';
    return new Intl.NumberFormat('vi-VN', {
        style: 'decimal',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount) + ' VND';
}

/**
 * Format date
 */
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
    });
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

