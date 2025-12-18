// ===================================
// USER PROFILE PAGE JAVASCRIPT
// Handle all profile management functionality
// ===================================

document.addEventListener('DOMContentLoaded', function () {
    // ===================================
    // INITIALIZATION
    // ===================================

    // Get modal elements
    const editProfileModal = new bootstrap.Modal(document.getElementById('editProfileModal'));
    const changePasswordModal = new bootstrap.Modal(document.getElementById('changePasswordModal'));

    // Get form elements
    const profileForm = document.getElementById('profileForm');
    const requestPasswordChangeForm = document.getElementById('requestPasswordChangeForm');
    const confirmPasswordChangeForm = document.getElementById('confirmPasswordChangeForm');

    // Get step elements for password change
    const passwordStepRequest = document.getElementById('passwordStepRequest');
    const passwordStepConfirm = document.getElementById('passwordStepConfirm');
    const backToRequestStepBtn = document.getElementById('backToRequestStepBtn');

    // ===================================
    // EDIT PROFILE MODAL
    // ===================================

    // When edit profile modal is opened, populate form with current data
    document.getElementById('editProfileModal').addEventListener('show.bs.modal', function () {
        const fullName = document.getElementById('displayFullName').textContent;
        const phone = document.getElementById('profilePhone').textContent;

        document.getElementById('editFullName').value = fullName;
        document.getElementById('editPhone').value = phone !== 'Chưa cập nhật' ? phone : '';

        // Clear file input
        document.getElementById('editAvatarFile').value = '';

        // Clear validation states
        clearValidation(profileForm);
    });

    // Handle profile form submission
    profileForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const saveBtn = document.getElementById('saveProfileBtn');
        const originalBtnText = saveBtn.innerHTML;

        try {
            // Disable button and show loading state
            saveBtn.disabled = true;
            saveBtn.innerHTML = '<i class="mdi mdi-loading mdi-spin me-1"></i> Đang lưu...';

            // Create FormData
            const formData = new FormData(profileForm);

            // Get anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            // Send request
            const response = await fetch('/UserProfile/UpdateProfile', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                // Update UI with new data
                updateProfileUI(result.data);

                // Close modal
                editProfileModal.hide();

                // Show success message
                showToast('success', 'Thành công!', 'Cập nhật hồ sơ thành công');
            } else {
                // Show validation errors
                if (result.errors) {
                    showValidationErrors(profileForm, result.errors);
                }
                showToast('error', 'Lỗi!', result.message || 'Có lỗi xảy ra khi cập nhật hồ sơ');
            }
        } catch (error) {
            console.error('Error updating profile:', error);
            showToast('error', 'Lỗi!', 'Không thể kết nối đến máy chủ');
        } finally {
            // Restore button
            saveBtn.disabled = false;
            saveBtn.innerHTML = originalBtnText;
        }
    });

    // ===================================
    // CHANGE PASSWORD MODAL
    // ===================================

    // When change password modal is opened, reset to step 1
    document.getElementById('changePasswordModal').addEventListener('show.bs.modal', function () {
        resetPasswordModal();
    });

    // Handle request password change (Step 1)
    requestPasswordChangeForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const requestBtn = document.getElementById('requestChangePasswordBtn');
        const originalBtnText = requestBtn.innerHTML;

        try {
            // Disable button and show loading state
            requestBtn.disabled = true;
            requestBtn.innerHTML = '<i class="mdi mdi-loading mdi-spin me-1"></i> Đang gửi...';

            // Get form data
            const formData = new FormData(requestPasswordChangeForm);
            const token = document.querySelector('#requestPasswordChangeForm input[name="__RequestVerificationToken"]').value;

            // Send request
            const response = await fetch('/UserProfile/RequestPasswordChange', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                // Move to step 2
                passwordStepRequest.classList.add('d-none');
                passwordStepConfirm.classList.remove('d-none');

                // Clear step 1 form
                requestPasswordChangeForm.reset();
                clearValidation(requestPasswordChangeForm);

                // Show success message
                showToast('success', 'Thành công!', 'Mã xác nhận đã được gửi đến email của bạn');
            } else {
                // Show validation errors
                if (result.errors) {
                    showValidationErrors(requestPasswordChangeForm, result.errors);
                }
                showToast('error', 'Lỗi!', result.message || 'Mật khẩu hiện tại không đúng');
            }
        } catch (error) {
            console.error('Error requesting password change:', error);
            showToast('error', 'Lỗi!', 'Không thể kết nối đến máy chủ');
        } finally {
            // Restore button
            requestBtn.disabled = false;
            requestBtn.innerHTML = originalBtnText;
        }
    });

    // Handle confirm password change (Step 2)
    confirmPasswordChangeForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        // Validate password match
        const newPassword = document.getElementById('newPassword').value;
        const confirmPassword = document.getElementById('confirmNewPassword').value;

        if (newPassword !== confirmPassword) {
            showFieldError('confirmNewPassword', 'Mật khẩu xác nhận không khớp');
            showToast('error', 'Lỗi!', 'Mật khẩu xác nhận không khớp');
            return;
        }

        const confirmBtn = document.getElementById('confirmChangePasswordBtn');
        const originalBtnText = confirmBtn.innerHTML;

        try {
            // Disable button and show loading state
            confirmBtn.disabled = true;
            confirmBtn.innerHTML = '<i class="mdi mdi-loading mdi-spin me-1"></i> Đang xác nhận...';

            // Get form data
            const formData = new FormData(confirmPasswordChangeForm);
            const token = document.querySelector('#confirmPasswordChangeForm input[name="__RequestVerificationToken"]').value;

            // Send request
            const response = await fetch('/UserProfile/ConfirmPasswordChange', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                // Close modal
                changePasswordModal.hide();

                // Reset modal
                resetPasswordModal();

                // Show success message
                showToast('success', 'Thành công!', 'Đổi mật khẩu thành công. Vui lòng đăng nhập lại.');

                // Redirect to login after 2 seconds
                setTimeout(() => {
                    window.location.href = '/Auth/Login';
                }, 2000);
            } else {
                // Show validation errors
                if (result.errors) {
                    showValidationErrors(confirmPasswordChangeForm, result.errors);
                }
                showToast('error', 'Lỗi!', result.message || 'Mã xác nhận không đúng hoặc đã hết hạn');
            }
        } catch (error) {
            console.error('Error confirming password change:', error);
            showToast('error', 'Lỗi!', 'Không thể kết nối đến máy chủ');
        } finally {
            // Restore button
            confirmBtn.disabled = false;
            confirmBtn.innerHTML = originalBtnText;
        }
    });

    // Back to step 1 button
    backToRequestStepBtn.addEventListener('click', function () {
        passwordStepConfirm.classList.add('d-none');
        passwordStepRequest.classList.remove('d-none');

        // Clear step 2 form
        confirmPasswordChangeForm.reset();
        clearValidation(confirmPasswordChangeForm);
    });

    // ===================================
    // HELPER FUNCTIONS
    // ===================================

    // Update profile UI after successful update
    function updateProfileUI(data) {
        // Update avatar if changed
        if (data.avatarUrl) {
            const avatarImg = document.getElementById('userAvatar');
            avatarImg.src = data.avatarUrl;
        }

        // Update display name
        if (data.fullName) {
            document.getElementById('displayFullName').textContent = data.fullName;
            document.getElementById('profileFullName').textContent = data.fullName;
        }

        // Update phone
        if (data.phone) {
            document.getElementById('profilePhone').textContent = data.phone;
        } else {
            document.getElementById('profilePhone').textContent = 'Chưa cập nhật';
        }

        // Update email if changed
        if (data.email) {
            document.getElementById('displayEmail').textContent = data.email;
            document.getElementById('profileEmail').textContent = data.email;
        }
    }

    // Reset password change modal to step 1
    function resetPasswordModal() {
        passwordStepRequest.classList.remove('d-none');
        passwordStepConfirm.classList.add('d-none');

        requestPasswordChangeForm.reset();
        confirmPasswordChangeForm.reset();

        clearValidation(requestPasswordChangeForm);
        clearValidation(confirmPasswordChangeForm);
    }

    // Clear all validation states in a form
    function clearValidation(form) {
        const inputs = form.querySelectorAll('.form-control');
        inputs.forEach(input => {
            input.classList.remove('is-invalid');
            const feedback = input.parentElement.querySelector('.invalid-feedback');
            if (feedback) {
                feedback.textContent = '';
            }
        });
    }

    // Show validation errors for form fields
    function showValidationErrors(form, errors) {
        // Clear previous errors
        clearValidation(form);

        // Show new errors
        for (const [field, messages] of Object.entries(errors)) {
            const input = form.querySelector(`[name="${field}"]`);
            if (input) {
                input.classList.add('is-invalid');
                const feedback = input.parentElement.querySelector('.invalid-feedback');
                if (feedback) {
                    feedback.textContent = Array.isArray(messages) ? messages[0] : messages;
                }
            }
        }
    }

    // Show error for a specific field
    function showFieldError(fieldId, message) {
        const input = document.getElementById(fieldId);
        if (input) {
            input.classList.add('is-invalid');
            const feedback = input.parentElement.querySelector('.invalid-feedback');
            if (feedback) {
                feedback.textContent = message;
            }
        }
    }

    // ===================================
    // REAL-TIME VALIDATION
    // ===================================

    // Remove validation error on input
    document.querySelectorAll('.form-control').forEach(input => {
        input.addEventListener('input', function () {
            if (this.classList.contains('is-invalid')) {
                this.classList.remove('is-invalid');
                const feedback = this.parentElement.querySelector('.invalid-feedback');
                if (feedback) {
                    feedback.textContent = '';
                }
            }
        });
    });

    // Password match validation
    const confirmPasswordInput = document.getElementById('confirmNewPassword');
    if (confirmPasswordInput) {
        confirmPasswordInput.addEventListener('input', function () {
            const newPassword = document.getElementById('newPassword').value;
            const confirmPassword = this.value;

            if (confirmPassword && newPassword !== confirmPassword) {
                this.classList.add('is-invalid');
                const feedback = this.parentElement.querySelector('.invalid-feedback');
                if (feedback) {
                    feedback.textContent = 'Mật khẩu xác nhận không khớp';
                }
            } else {
                this.classList.remove('is-invalid');
                const feedback = this.parentElement.querySelector('.invalid-feedback');
                if (feedback) {
                    feedback.textContent = '';
                }
            }
        });
    }

    // ===================================
    // FILE UPLOAD PREVIEW
    // ===================================

    const avatarFileInput = document.getElementById('editAvatarFile');
    if (avatarFileInput) {
        avatarFileInput.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                // Validate file type
                if (!file.type.startsWith('image/')) {
                    showToast('error', 'Lỗi!', 'Vui lòng chọn file ảnh');
                    this.value = '';
                    return;
                }

                // Validate file size (max 5MB)
                if (file.size > 5 * 1024 * 1024) {
                    showToast('error', 'Lỗi!', 'Kích thước ảnh không được vượt quá 5MB');
                    this.value = '';
                    return;
                }

                // Preview image
                const reader = new FileReader();
                reader.onload = function (e) {
                    document.getElementById('userAvatar').src = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        });
    }

    // ===================================
    // MODAL CLEANUP
    // ===================================

    // Reset forms when modals are hidden
    document.getElementById('editProfileModal').addEventListener('hidden.bs.modal', function () {
        profileForm.reset();
        clearValidation(profileForm);

        // Restore original avatar if upload was cancelled
        const currentAvatarUrl = document.getElementById('currentAvatarUrl').value;
        const defaultAvatar = document.getElementById('userAvatar').getAttribute('data-default-avatar');
        const avatarImg = document.getElementById('userAvatar');

        if (currentAvatarUrl) {
            avatarImg.src = currentAvatarUrl;
        } else {
            avatarImg.src = defaultAvatar;
        }
    });

    document.getElementById('changePasswordModal').addEventListener('hidden.bs.modal', function () {
        resetPasswordModal();
    });
});

// ===================================
// TOAST NOTIFICATION FUNCTION
// ===================================

function showToast(type, title, message) {
    // Check if toast-notification.js is loaded
    if (typeof window.showNotification === 'function') {
        window.showNotification(type, title, message);
    } else {
        // Fallback to simple alert container
        const alertContainer = document.getElementById('alertContainer');
        if (!alertContainer) return;

        const alertTypes = {
            'success': 'alert-success',
            'error': 'alert-danger',
            'warning': 'alert-warning',
            'info': 'alert-info'
        };

        const alertClass = alertTypes[type] || 'alert-info';

        const alertElement = document.createElement('div');
        alertElement.className = `alert ${alertClass} alert-dismissible fade show`;
        alertElement.setAttribute('role', 'alert');
        alertElement.innerHTML = `
            <strong>${title}</strong> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;

        alertContainer.appendChild(alertElement);

        // Auto remove after 5 seconds
        setTimeout(() => {
            alertElement.classList.remove('show');
            setTimeout(() => alertElement.remove(), 150);
        }, 5000);
    }


}