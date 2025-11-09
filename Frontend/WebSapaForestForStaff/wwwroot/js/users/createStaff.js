/**
 * Create Staff Page - JavaScript
 * Handles form initialization, salary formatting, and validation
 */

document.addEventListener('DOMContentLoaded', function() {
    // Set default hire date to today if not set
    const hireDateInput = document.getElementById('HireDate');
    if (hireDateInput && !hireDateInput.value) {
        const today = new Date().toISOString().split('T')[0];
        hireDateInput.value = today;
    }

    // Set default RoleId to Staff (4) if not set
    const roleIdSelect = document.getElementById('RoleId');
    if (roleIdSelect && !roleIdSelect.value) {
        roleIdSelect.value = '4'; // Staff role
    }

    // Format salary display in VND
    const salaryInput = document.getElementById('salaryBaseInput');
    const salaryDisplay = document.getElementById('salaryDisplay');
    
    /**
     * Format number to VND currency
     */
    function formatVND(value) {
        if (!value || value === '0' || value === '') {
            return '';
        }
        // Convert to number and format with thousand separators
        const numValue = parseFloat(value);
        if (isNaN(numValue) || numValue <= 0) {
            return '';
        }
        return numValue.toLocaleString('vi-VN') + ' VNĐ';
    }

    if (salaryInput && salaryDisplay) {
        // Format on input
        salaryInput.addEventListener('input', function(e) {
            const value = e.target.value;
            const formatted = formatVND(value);
            salaryDisplay.textContent = formatted;
        });

        // Format on load if value exists
        if (salaryInput.value) {
            salaryDisplay.textContent = formatVND(salaryInput.value);
        }
    }

    // Form validation
    const form = document.getElementById('createStaffForm');
    if (form) {
        form.addEventListener('submit', function(e) {
            const fullName = document.getElementById('FullName').value.trim();
            const email = document.getElementById('Email').value.trim();
            const hireDate = document.getElementById('HireDate').value;
            const salaryBase = document.getElementById('salaryBaseInput').value;

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

            if (!hireDate) {
                e.preventDefault();
                showErrorToast('Vui lòng chọn ngày tuyển dụng');
                document.getElementById('HireDate').focus();
                return;
            }

            if (!salaryBase || parseFloat(salaryBase) <= 0) {
                e.preventDefault();
                showErrorToast('Vui lòng nhập lương cơ bản hợp lệ (lớn hơn 0)');
                document.getElementById('salaryBaseInput').focus();
                return;
            }

            // Ensure minimum salary (e.g., 1,000,000 VND)
            const salaryValue = parseFloat(salaryBase);
            if (salaryValue < 1000000) {
                e.preventDefault();
                showWarningToast('Lương cơ bản thấp hơn 1.000.000 VNĐ. Vui lòng kiểm tra lại.', 4000);
                document.getElementById('salaryBaseInput').focus();
                return;
            }
        });
    }
});

