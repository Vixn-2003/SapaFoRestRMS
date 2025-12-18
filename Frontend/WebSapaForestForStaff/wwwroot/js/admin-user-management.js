/**
 * Admin User Management JavaScript
 * Handles user management operations including modals, AJAX calls to controller
 * All API calls go through AdminUserManagementController (which uses ApiService)
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
 * Open deactivate modal
 */
function openDeactivateModal(userId, userName) {
    // Escape HTML to prevent XSS
    const safeUserName = $('<div>').text(userName).html();
    
    document.getElementById('deactivateUserId').value = userId;
    document.getElementById('deactivateUserName').textContent = userName;
    document.getElementById('deactivateReason').value = '';
    $('#deactivateModal').modal('show');
}

/**
 * Submit deactivate via AJAX to controller
 */
function submitDeactivate() {
    const userId = parseInt(document.getElementById('deactivateUserId').value);
    const reason = document.getElementById('deactivateReason').value;

    if (!userId) {
        toastr.error('Invalid user ID');
        return;
    }

    // Show loading
    const submitBtn = $('#deactivateModal').find('button[onclick="submitDeactivate()"]');
    const originalText = submitBtn.html();
    submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Processing...');

    // Call controller endpoint via AJAX (controller uses ApiService)
    $.ajax({
        url: '/AdminUserManagement/Deactivate',
        type: 'POST',
        contentType: 'application/json',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        data: JSON.stringify({
            userId: userId,
            reason: reason
        }),
        success: function (response) {
            if (response.success) {
                toastr.success(response.message || 'User deactivated successfully');
                $('#deactivateModal').modal('hide');
                // Reload page after short delay
                setTimeout(function() {
                    window.location.reload();
                }, 1000);
            } else {
                toastr.error(response.message || 'Failed to deactivate user');
                submitBtn.prop('disabled', false).html(originalText);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error deactivating user:', error);
            let errorMessage = 'An error occurred while deactivating user';
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMessage = xhr.responseJSON.message;
            }
            toastr.error(errorMessage);
            submitBtn.prop('disabled', false).html(originalText);
        }
    });
}

/**
 * Change user status (activate/deactivate)
 */
function changeStatus(userId, status) {
    const statusText = status === 0 ? 'kích hoạt' : 'ngừng hoạt động';

    showConfirmModal({
        title: 'Xác nhận',
        message: `Bạn có chắc muốn ${statusText} người dùng này?`,
        confirmText: 'Xác nhận',
        onConfirm: function () {
            toastr.info('Đang xử lý...');
            $.ajax({
                url: '/AdminUserManagement/Deactivate',
                type: 'POST',
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                data: JSON.stringify({
                    userId: userId,
                    reason: status === 0 ? 'Activated by admin' : 'Deactivated by admin'
                }),
                success: function (response) {
                    if (response.success) {
                        toastr.success(`Đã ${statusText} người dùng`);
                        setTimeout(function() {
                            window.location.reload();
                        }, 800);
                    } else {
                        toastr.error(response.message || `Không thể ${statusText} người dùng`);
                    }
                },
                error: function (xhr, status, error) {
                    console.error(`Error ${statusText}:`, error);
                    toastr.error(`Lỗi khi ${statusText} người dùng`);
                }
            });
        }
    });
}

/**
 * Open delete modal
 */
function openDeleteModal(userId, userName) {
    // Escape HTML to prevent XSS
    const safeUserName = $('<div>').text(userName).html();
    
    document.getElementById('deleteUserId').value = userId;
    document.getElementById('deleteUserName').textContent = userName;
    document.getElementById('deleteReason').value = '';
    $('#deleteModal').modal('show');
}

/**
 * Submit delete via AJAX to controller
 */
function submitDelete() {
    const userId = parseInt(document.getElementById('deleteUserId').value);
    const reason = document.getElementById('deleteReason').value;

    if (!userId) {
        toastr.error('Invalid user ID');
        return;
    }

    // Show loading
    const submitBtn = $('#deleteModal').find('button[onclick="submitDelete()"]');
    const originalText = submitBtn.html();
    submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Processing...');

    // Call controller endpoint via AJAX (controller uses ApiService)
    $.ajax({
        url: `/AdminUserManagement/Delete/${userId}`,
        type: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                toastr.success(response.message || 'User deleted successfully');
                $('#deleteModal').modal('hide');
                // Reload page after short delay
                setTimeout(function() {
                    window.location.reload();
                }, 1000);
            } else {
                toastr.error(response.message || 'Failed to delete user');
                submitBtn.prop('disabled', false).html(originalText);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error deleting user:', error);
            let errorMessage = 'An error occurred while deleting user';
            if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMessage = xhr.responseJSON.message;
            }
            toastr.error(errorMessage);
            submitBtn.prop('disabled', false).html(originalText);
        }
    });
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
            bulkChangeStatus(userIds, 0); // 0 = Active
            break;
        case 'deactivate':
            bulkChangeStatus(userIds, 1); // 1 = Inactive
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
    const statusText = status === 0 ? 'kích hoạt' : 'ngừng hoạt động';

    showConfirmModal({
        title: 'Xác nhận',
        message: `Bạn có chắc muốn ${statusText} ${userIds.length} người dùng đã chọn?`,
        confirmText: 'Xác nhận',
        onConfirm: function () {
            toastr.info(`Đang xử lý ${userIds.length} người dùng...`);

            let completed = 0;
            let failed = 0;

            userIds.forEach((userId, index) => {
                setTimeout(() => {
                    $.ajax({
                        url: '/AdminUserManagement/Deactivate',
                        type: 'POST',
                        contentType: 'application/json',
                        headers: {
                            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                        },
                        data: JSON.stringify({
                            userId: userId,
                            reason: `Bulk ${statusText} by admin`
                        }),
                        success: function () {
                            completed++;
                            if (completed + failed === userIds.length) {
                                if (failed === 0) {
                                    toastr.success(`Đã ${statusText} ${completed} người dùng`);
                                } else {
                                    toastr.warning(`Đã ${statusText} ${completed} người dùng, ${failed} thất bại`);
                                }
                                setTimeout(() => window.location.reload(), 800);
                            }
                        },
                        error: function () {
                            failed++;
                            if (completed + failed === userIds.length) {
                                if (failed === userIds.length) {
                                    toastr.error(`Không thể ${statusText} người dùng`);
                                } else {
                                    toastr.warning(`Đã ${statusText} ${completed} người dùng, ${failed} thất bại`);
                                }
                                setTimeout(() => window.location.reload(), 800);
                            }
                        }
                    });
                }, index * 100);
            });
        }
    });
}

/**
 * Bulk delete users
 */
function bulkDelete(userIds) {
    showConfirmModal({
        title: 'Xác nhận xóa',
        message: `Bạn có chắc muốn xóa ${userIds.length} người dùng đã chọn? Thao tác này không thể hoàn tác!`,
        confirmText: 'Xóa',
        onConfirm: function () {
            toastr.info(`Đang xử lý ${userIds.length} người dùng...`);

            let completed = 0;
            let failed = 0;

            userIds.forEach((userId, index) => {
                setTimeout(() => {
                    $.ajax({
                        url: `/AdminUserManagement/Delete/${userId}`,
                        type: 'POST',
                        headers: {
                            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                        },
                        success: function () {
                            completed++;
                            if (completed + failed === userIds.length) {
                                if (failed === 0) {
                                    toastr.success(`Đã xóa ${completed} người dùng`);
                                } else {
                                    toastr.warning(`Đã xóa ${completed} người dùng, ${failed} thất bại`);
                                }
                                setTimeout(() => window.location.reload(), 800);
                            }
                        },
                        error: function () {
                            failed++;
                            if (completed + failed === userIds.length) {
                                if (failed === userIds.length) {
                                    toastr.error('Không thể xóa người dùng');
                                } else {
                                    toastr.warning(`Đã xóa ${completed} người dùng, ${failed} thất bại`);
                                }
                                setTimeout(() => window.location.reload(), 800);
                            }
                        }
                    });
                }, index * 100);
            });
        }
    });
}

/**
 * Generic confirmation modal (replaces alert/confirm)
 */
function showConfirmModal(options) {
    const opts = Object.assign({
        title: 'Xác nhận',
        message: 'Bạn chắc chắn muốn thực hiện hành động này?',
        confirmText: 'Xác nhận',
        cancelText: 'Hủy',
        onConfirm: null
    }, options || {});

    const modal = $('#confirmModal');
    if (!modal || modal.length === 0) {
        console.error('Confirm modal not found on page');
        return;
    }

    modal.find('.modal-title').text(opts.title);
    modal.find('.confirm-message').text(opts.message);
    modal.find('.btn-confirm').text(opts.confirmText);
    modal.find('.btn-cancel').text(opts.cancelText);

    modal.find('.btn-confirm').off('click').on('click', function () {
        modal.modal('hide');
        if (typeof opts.onConfirm === 'function') {
            opts.onConfirm();
        }
    });

    modal.modal('show');
}

