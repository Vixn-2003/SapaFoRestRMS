/**
 * Users Management JavaScript
 * Handles user management operations including modals, bulk actions, and UI interactions
 */

// Global variables
let selectedUsers = [];

/**
 * Initialize user management functionality
 */
function initializeUserManagement() {
    // Initialize select all functionality
    const selectAllCheckbox = document.getElementById('selectAll');
    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', toggleSelectAll);
    }

    // Initialize individual checkboxes
    document.querySelectorAll('.user-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', updateSelectAllState);
    });

    // Update bulk action button state
    updateBulkActionButton();
}

/**
 * Toggle select all checkboxes
 */
function toggleSelectAll() {
    const selectAllCheckbox = document.getElementById('selectAll');
    const checkboxes = document.querySelectorAll('.user-checkbox');

    checkboxes.forEach(checkbox => {
        checkbox.checked = selectAllCheckbox.checked;
    });

    updateBulkActionButton();
}

/**
 * Update select all checkbox state based on individual checkboxes
 */
function updateSelectAllState() {
    const selectAllCheckbox = document.getElementById('selectAll');
    const checkboxes = document.querySelectorAll('.user-checkbox');
    const checkedBoxes = document.querySelectorAll('.user-checkbox:checked');

    if (selectAllCheckbox && checkboxes.length > 0) {
        selectAllCheckbox.checked = checkedBoxes.length === checkboxes.length;
        selectAllCheckbox.indeterminate = checkedBoxes.length > 0 && checkedBoxes.length < checkboxes.length;
    }

    updateBulkActionButton();
}

/**
 * Update bulk action button enabled/disabled state
 */
function updateBulkActionButton() {
    const checkedBoxes = document.querySelectorAll('.user-checkbox:checked');
    const bulkActionBtn = document.getElementById('bulkActionBtn');

    if (bulkActionBtn) {
        bulkActionBtn.disabled = checkedBoxes.length === 0;
    }
}

/**
 * Execute bulk action
 */
function executeBulkAction() {
    const bulkAction = document.getElementById('bulkAction');
    const checkedBoxes = document.querySelectorAll('.user-checkbox:checked');

    if (!bulkAction || checkedBoxes.length === 0) {
        toastr.warning('Please select users and choose an action');
        return;
    }

    const action = bulkAction.value;
    const userIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

    if (!action) {
        toastr.warning('Please select an action');
        return;
    }

    switch (action) {
        case 'activate':
            bulkChangeStatus(userIds, 1);
            break;
        case 'deactivate':
            bulkChangeStatus(userIds, 0);
            break;
        case 'delete':
            bulkDelete(userIds);
            break;
        default:
            toastr.error('Unknown action');
    }
}

/**
 * Bulk change status
 */
function bulkChangeStatus(userIds, status) {
    const statusText = status === 1 ? 'activate' : 'deactivate';

    if (confirm(`Are you sure you want to ${statusText} ${userIds.length} selected users?`)) {
        // Create form and submit
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Users/BulkChangeStatus';

        // Add user IDs
        userIds.forEach(id => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'userIds';
            input.value = id;
            form.appendChild(input);
        });

        // Add status
        const statusInput = document.createElement('input');
        statusInput.type = 'hidden';
        statusInput.name = 'status';
        statusInput.value = status;
        form.appendChild(statusInput);

        document.body.appendChild(form);
        form.submit();
    }
}

/**
 * Bulk delete users
 */
function bulkDelete(userIds) {
    if (confirm(`Are you sure you want to delete ${userIds.length} selected users? This action cannot be undone!`)) {
        // Create form and submit
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Users/BulkDelete';

        // Add user IDs
        userIds.forEach(id => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'userIds';
            input.value = id;
            form.appendChild(input);
        });

        document.body.appendChild(form);
        form.submit();
    }
}

/**
 * Open deactivate modal
 */
function openDeactivateModal(userId, userName) {
    document.getElementById('deactivateUserId').value = userId;
    document.getElementById('deactivateUserName').textContent = userName;
    document.getElementById('deactivateReason').value = '';
    $('#deactivateModal').modal('show');
}

/**
 * Submit deactivate
 */
function submitDeactivate() {
    const userId = document.getElementById('deactivateUserId').value;
    const reason = document.getElementById('deactivateReason').value;

    // Create form and submit
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Users/ChangeStatus';

    const idInput = document.createElement('input');
    idInput.type = 'hidden';
    idInput.name = 'id';
    idInput.value = userId;
    form.appendChild(idInput);

    const statusInput = document.createElement('input');
    statusInput.type = 'hidden';
    statusInput.name = 'status';
    statusInput.value = '0';
    form.appendChild(statusInput);

    document.body.appendChild(form);
    form.submit();
}

/**
 * Change user status
 */
function changeStatus(userId, status) {
    const statusText = status === 1 ? 'activate' : 'deactivate';

    if (confirm(`Are you sure you want to ${statusText} this user?`)) {
        // Create form and submit
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Users/ChangeStatus';

        const idInput = document.createElement('input');
        idInput.type = 'hidden';
        idInput.name = 'id';
        idInput.value = userId;
        form.appendChild(idInput);

        const statusInput = document.createElement('input');
        statusInput.type = 'hidden';
        statusInput.name = 'status';
        statusInput.value = status;
        form.appendChild(statusInput);

        document.body.appendChild(form);
        form.submit();
    }
}

/**
 * Open delete modal
 */
function openDeleteModal(userId, userName) {
    document.getElementById('deleteUserId').value = userId;
    document.getElementById('deleteUserName').textContent = userName;
    document.getElementById('deleteReason').value = '';
    $('#deleteModal').modal('show');
}

/**
 * Submit delete
 */
function submitDelete() {
    const userId = document.getElementById('deleteUserId').value;
    const reason = document.getElementById('deleteReason').value;

    // Create form and submit
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Users/Delete';

    const idInput = document.createElement('input');
    idInput.type = 'hidden';
    idInput.name = 'id';
    idInput.value = userId;
    form.appendChild(idInput);

    document.body.appendChild(form);
    form.submit();
}
