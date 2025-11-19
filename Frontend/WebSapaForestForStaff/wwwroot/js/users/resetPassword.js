/**
 * Reset Password Page - JavaScript
 * Handles password toggle, generation, and form submission
 */

document.addEventListener('DOMContentLoaded', function() {
    // Toggle password visibility
    const togglePasswordBtn = document.getElementById('togglePassword');
    if (togglePasswordBtn) {
        togglePasswordBtn.addEventListener('click', function() {
            const passwordInput = document.getElementById('NewPassword');
            const toggleIcon = this.querySelector('i');
            
            if (passwordInput && toggleIcon) {
                if (passwordInput.type === 'password') {
                    passwordInput.type = 'text';
                    toggleIcon.classList.remove('bi-eye');
                    toggleIcon.classList.add('bi-eye-slash');
                } else {
                    passwordInput.type = 'password';
                    toggleIcon.classList.remove('bi-eye-slash');
                    toggleIcon.classList.add('bi-eye');
                }
            }
        });
    }

    // Auto-generate password if field is empty
    const resetPasswordForm = document.getElementById('resetPasswordForm');
    if (resetPasswordForm) {
        resetPasswordForm.addEventListener('submit', function(e) {
            const passwordInput = document.getElementById('NewPassword');
            if (passwordInput && !passwordInput.value.trim()) {
                const generatedPassword = generatePassword();
                passwordInput.value = generatedPassword;
                const generatedPasswordDisplay = document.getElementById('generatedPassword');
                const passwordPreview = document.getElementById('passwordPreview');
                
                if (generatedPasswordDisplay) {
                    generatedPasswordDisplay.textContent = generatedPassword;
                }
                if (passwordPreview) {
                    passwordPreview.style.display = 'block';
                }
            }
        });
    }
});

/**
 * Generate a random password
 */
function generatePassword() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%^&*';
    let password = '';
    for (let i = 0; i < 10; i++) {
        password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return password;
}

/**
 * Copy password to clipboard
 */
function copyPassword() {
    const passwordText = document.getElementById('generatedPassword')?.textContent;
    if (!passwordText) {
        if (typeof showErrorToast === 'function') {
            showErrorToast('Không tìm thấy mật khẩu để sao chép');
        }
        return;
    }

    navigator.clipboard.writeText(passwordText).then(function() {
        if (typeof showSuccessToast === 'function') {
            showSuccessToast('Mật khẩu đã được sao chép vào clipboard!');
        }
    }).catch(function() {
        if (typeof showErrorToast === 'function') {
            showErrorToast('Không thể sao chép mật khẩu');
        }
    });
}

