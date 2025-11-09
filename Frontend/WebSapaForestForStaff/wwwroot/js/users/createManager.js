/**
 * Create Manager Page - JavaScript
 * Handles form initialization and validation
 */

document.addEventListener('DOMContentLoaded', function() {
    // Set default RoleId to Manager (3) if not set
    const roleIdSelect = document.getElementById('RoleId');
    if (roleIdSelect && !roleIdSelect.value) {
        roleIdSelect.value = '3'; // Manager role
    }

    // Form validation
    const form = document.getElementById('createManagerForm');
    if (form) {
        form.addEventListener('submit', function(e) {
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
        });
    }
});

