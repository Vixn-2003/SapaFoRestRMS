/**
 * Edit User Page - JavaScript
 * Handles form validation for editing users
 */

document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('editUserForm');
    if (form) {
        form.addEventListener('submit', function(e) {
            const roleId = document.getElementById('RoleId').value;
            const fullName = document.getElementById('FullName').value.trim();
            const email = document.getElementById('Email').value.trim();

            if (!fullName) {
                e.preventDefault();
                showErrorToast('Vui lòng nhập họ và tên');
                document.getElementById('FullName').focus();
                return;
            }

            if (!email) {
                e.preventDefault();
                showErrorToast('Vui lòng nhập email');
                document.getElementById('Email').focus();
                return;
            }

            // Validate email format
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(email)) {
                e.preventDefault();
                showErrorToast('Email không hợp lệ');
                document.getElementById('Email').focus();
                return;
            }

            if (!roleId) {
                e.preventDefault();
                showErrorToast('Vui lòng chọn vai trò cho người dùng');
                document.getElementById('RoleId').focus();
                return;
            }
        });
    }
});

