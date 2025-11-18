document.addEventListener("DOMContentLoaded", function () {
    const otpInput = document.querySelector('input[name="Code"]');
    const form = document.querySelector('form');
    const submitBtn = form?.querySelector('button[type="submit"]');

    // OTP input formatting and validation
    if (otpInput) {
        otpInput.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, ''); // Only digits
            if (value.length > 6) {
                value = value.substring(0, 6);
            }
            e.target.value = value;

            // Auto-submit when 6 digits are entered
            if (value.length === 6) {
                setTimeout(() => {
                    if (form) {
                        form.submit();
                    }
                }, 500);
            }
        });

        // Handle paste
        otpInput.addEventListener('paste', function (e) {
            setTimeout(() => {
                let value = e.target.value.replace(/\D/g, '');
                if (value.length > 6) {
                    value = value.substring(0, 6);
                }
                e.target.value = value;

                if (value.length === 6) {
                    setTimeout(() => {
                        if (form) {
                            form.submit();
                        }
                    }, 500);
                }
            }, 0);
        });

        // Focus on load
        otpInput.focus();
    }

    // Form submission handling
    if (form) {
        form.addEventListener('submit', function (e) {
            const code = otpInput?.value;
            
            if (!code || code.length !== 6) {
                e.preventDefault();
                showError('Vui lòng nhập đầy đủ 6 chữ số OTP');
                otpInput?.focus();
                return;
            }

            // Show loading state
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="bi bi-hourglass-split"></i> Đang xác thực...';
            }
        });
    }

    // Resend OTP countdown
    const resendLink = document.querySelector('.resend-link');
    if (resendLink) {
        let countdown = 60; // 60 seconds countdown
        const originalText = resendLink.textContent;
        
        resendLink.addEventListener('click', function (e) {
            e.preventDefault();
            
            if (resendLink.classList.contains('disabled')) {
                return;
            }

            // Disable link and start countdown
            resendLink.classList.add('disabled');
            resendLink.style.pointerEvents = 'none';
            
            const countdownInterval = setInterval(() => {
                resendLink.textContent = `Gửi lại sau ${countdown}s`;
                countdown--;
                
                if (countdown < 0) {
                    clearInterval(countdownInterval);
                    resendLink.textContent = originalText;
                    resendLink.classList.remove('disabled');
                    resendLink.style.pointerEvents = 'auto';
                    countdown = 60; // Reset for next time
                }
            }, 1000);

            // Actually resend OTP
            fetch(resendLink.href, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
            .then(response => {
                if (response.ok) {
                    showSuccess('Mã OTP mới đã được gửi');
                } else {
                    showError('Không thể gửi lại mã OTP. Vui lòng thử lại.');
                }
            })
            .catch(() => {
                showError('Có lỗi xảy ra. Vui lòng thử lại.');
            });
        });
    }

    // Auto-hide alerts
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        }, 5000);
    });
});

function showError(message) {
    // Remove existing messages
    const existingMessages = document.querySelectorAll('.error-message, .success-message');
    existingMessages.forEach(msg => msg.remove());

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

function showSuccess(message) {
    // Remove existing messages
    const existingMessages = document.querySelectorAll('.error-message, .success-message');
    existingMessages.forEach(msg => msg.remove());

    // Create new success message
    const successDiv = document.createElement('div');
    successDiv.className = 'alert alert-success success-message';
    successDiv.textContent = message;

    // Insert before form
    const form = document.querySelector('form');
    if (form) {
        form.parentNode.insertBefore(successDiv, form);
    }

    // Auto-hide after 3 seconds
    setTimeout(() => {
        successDiv.style.opacity = '0';
        setTimeout(() => successDiv.remove(), 300);
    }, 3000);
}
