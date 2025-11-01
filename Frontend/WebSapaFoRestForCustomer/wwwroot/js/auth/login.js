document.addEventListener("DOMContentLoaded", function () {
    // Phone number formatting
    const phoneInput = document.querySelector('input[type="tel"]');
    if (phoneInput) {
        phoneInput.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, ''); // Remove non-digits
            if (value.length > 0) {
                if (value.startsWith('0')) {
                    // Vietnamese phone number format: 0xxxxxxxxx
                    if (value.length > 10) {
                        value = value.substring(0, 10);
                    }
                } else {
                    // If doesn't start with 0, add it
                    if (value.length <= 9) {
                        value = '0' + value;
                    } else {
                        value = value.substring(0, 10);
                    }
                }
            }
            e.target.value = value;
        });

        // Auto-format on paste
        phoneInput.addEventListener('paste', function (e) {
            setTimeout(() => {
                let value = e.target.value.replace(/\D/g, '');
                if (value.length > 0 && !value.startsWith('0')) {
                    e.target.value = '0' + value;
                }
            }, 0);
        });
    }

    // Form validation
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', function (e) {
            const phone = phoneInput?.value;
            
            if (!phone || phone.length < 10) {
                e.preventDefault();
                showError('Vui lòng nhập số điện thoại hợp lệ');
                return;
            }

            // Show loading state
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="bi bi-hourglass-split"></i> Đang gửi...';
            }
        });
    }

    // Auto-hide alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        }, 5000);
    });
});

function showError(message) {
    // Remove existing error messages
    const existingErrors = document.querySelectorAll('.error-message');
    existingErrors.forEach(error => error.remove());

    // Create new error message
    const errorDiv = document.createElement('div');
    errorDiv.className = 'alert alert-danger error-message';
    errorDiv.textContent = message;

    // Insert before form
    const form = document.querySelector('form');
    if (form) {
        form.parentNode.insertBefore(errorDiv, form);
    }

    // Auto-hide after 5 seconds
    setTimeout(() => {
        errorDiv.style.opacity = '0';
        setTimeout(() => errorDiv.remove(), 300);
    }, 5000);
}
