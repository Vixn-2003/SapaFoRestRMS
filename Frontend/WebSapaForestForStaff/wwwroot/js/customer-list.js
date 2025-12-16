/**
 * Customer Management Module - JavaScript
 * Handles form submission, sorting, and pagination UI interactions
 * All data loading is done server-side via form submission
 */

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    initializeCustomerList();
    
    // Initialize VIP modal if present
    if (document.getElementById('vipUpdateModal')) {
        initializeVipModal();
    }
});

/**
 * Initialize customer list page
 */
function initializeCustomerList() {
    // Reset button click
    const btnReset = document.getElementById('btnReset');
    if (btnReset) {
        btnReset.addEventListener('click', function () {
            resetFilters();
        });
    }

    // Sort link clicks
    const sortLinks = document.querySelectorAll('.sort-link');
    sortLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const sortValue = this.getAttribute('data-sort');
            handleSort(sortValue);
        });
    });

    // Search on Enter key in search box
    const keywordInput = document.getElementById('keyword');
    if (keywordInput) {
        keywordInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                submitFilterForm(1); // Reset to page 1
            }
        });
    }
}

/**
 * Handle sort column click
 */
function handleSort(sortValue) {
    const sortBySelect = document.getElementById('sortBy');
    const currentSort = sortBySelect.value;
    
    // Determine new sort value based on current sort
    let newSortValue = sortValue;
    
    if (sortValue === 'FullName') {
        if (currentSort === 'FullName') {
            newSortValue = 'FullNameDesc';
        } else {
            newSortValue = 'FullName';
        }
    } else if (sortValue === 'TotalSpending') {
        if (currentSort === 'TotalSpending') {
            newSortValue = 'TotalSpendingAsc';
        } else {
            newSortValue = 'TotalSpending';
        }
    } else if (sortValue === 'TotalVisits') {
        if (currentSort === 'TotalVisits') {
            newSortValue = 'TotalVisitsAsc';
        } else {
            newSortValue = 'TotalVisits';
        }
    } else if (sortValue === 'LastVisit') {
        newSortValue = 'LastVisit';
    }
    
    // Update select value
    sortBySelect.value = newSortValue;
    
    // Submit form with page reset to 1
    submitFilterForm(1);
}

/**
 * Go to specific page
 */
function goToPage(pageNumber) {
    submitFilterForm(pageNumber);
}

/**
 * Submit filter form
 */
function submitFilterForm(pageNumber) {
    const form = document.getElementById('filterForm');
    const pageNumberInput = document.getElementById('pageNumber');
    
    if (pageNumberInput && pageNumber) {
        pageNumberInput.value = pageNumber;
    }
    
    if (form) {
        form.submit();
    }
}

/**
 * Reset filters
 */
function resetFilters() {
    // Clear all filter inputs
    document.getElementById('keyword').value = '';
    document.getElementById('isVip').value = '';
    document.getElementById('minSpending').value = '';
    document.getElementById('maxSpending').value = '';
    document.getElementById('minVisits').value = '';
    document.getElementById('maxVisits').value = '';
    document.getElementById('sortBy').value = 'TotalSpending';
    document.getElementById('pageNumber').value = 1;
    
    // Submit form
    submitFilterForm(1);
}

/**
 * Initialize VIP Modal
 */
function initializeVipModal() {
    // Check VIP Criteria button
    const btnCheckCriteria = document.getElementById('btnCheckCriteria');
    if (btnCheckCriteria) {
        btnCheckCriteria.addEventListener('click', checkVipCriteria);
    }

    // Confirm Update button
    const btnConfirmUpdate = document.getElementById('btnConfirmUpdate');
    if (btnConfirmUpdate) {
        btnConfirmUpdate.addEventListener('click', confirmVipUpdate);
    }
}

// Global variables for VIP modal
let currentCustomerId = null;
let currentCustomerName = '';
let currentVipStatus = false;

/**
 * Open VIP Update Modal
 */
function openVipUpdateModal(customerId, isVip, customerName) {
    currentCustomerId = customerId;
    currentCustomerName = customerName;
    currentVipStatus = isVip;

    // Set modal content
    const modalCustomerId = document.getElementById('modalCustomerId');
    const modalCustomerName = document.getElementById('modalCustomerName');
    
    if (modalCustomerId) modalCustomerId.textContent = customerId;
    if (modalCustomerName) modalCustomerName.textContent = customerName;
    
    const statusBadge = document.getElementById('modalCurrentVipStatus');
    if (statusBadge) {
        if (isVip) {
            statusBadge.className = 'badge bg-warning text-dark';
            statusBadge.innerHTML = '<i class="fas fa-crown"></i> VIP Customer';
        } else {
            statusBadge.className = 'badge bg-secondary';
            statusBadge.textContent = 'Regular Customer';
        }
    }

    // Set new status to opposite of current
    const newVipStatus = document.getElementById('newVipStatus');
    if (newVipStatus) {
        newVipStatus.value = (!isVip).toString();
    }

    // Reset form
    const manualOverride = document.getElementById('manualOverride');
    const changeReason = document.getElementById('changeReason');
    const vipCriteriaResult = document.getElementById('vipCriteriaResult');
    
    if (manualOverride) manualOverride.checked = false;
    if (changeReason) changeReason.value = '';
    if (vipCriteriaResult) vipCriteriaResult.style.display = 'none';

    // Show modal
    const modalElement = document.getElementById('vipUpdateModal');
    if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
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

    const newVipStatusEl = document.getElementById('newVipStatus');
    const manualOverrideEl = document.getElementById('manualOverride');
    const reasonEl = document.getElementById('changeReason');

    const newVipStatus = newVipStatusEl?.value === 'true';
    const manualOverride = manualOverrideEl?.checked || false;
    const reason = reasonEl?.value.trim() || '';

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
            const modalElement = document.getElementById('vipUpdateModal');
            if (modalElement) {
                const modal = bootstrap.Modal.getInstance(modalElement);
                if (modal) modal.hide();
            }

            // Reload page to refresh data
            setTimeout(() => location.reload(), 1500);
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

