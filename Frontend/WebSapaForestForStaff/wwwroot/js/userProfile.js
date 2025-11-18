/**
 * User Profile Management JavaScript
 * Handles loading and updating user profile via AJAX
 */

(function () {
    'use strict';

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        initializeProfile();
        setupEditModal();
        setupFormValidation();
        setupChangePasswordFlow();
    });

    /**
     * Initialize profile page - load profile data
     */
    function initializeProfile() {
        // Profile data is already loaded from server in the view
        // But we can refresh it if needed
        console.log('User Profile page initialized');
    }

    /**
     * Setup edit profile modal
     */
    function setupEditModal() {
        const editModal = document.getElementById('editProfileModal');
        if (!editModal) return;

        // When modal is shown, populate form with current data
        editModal.addEventListener('show.bs.modal', function () {
            populateEditForm();
        });

        // Clear form when modal is hidden
        editModal.addEventListener('hidden.bs.modal', function () {
            clearFormValidation();
        });
    }

    /**
     * Populate edit form with current user data
     */
    function populateEditForm() {
        const fullName = document.getElementById('profileFullName')?.textContent?.trim() || '';
        const phone = document.getElementById('profilePhone')?.textContent?.trim() || '';
        const currentAvatar = document.getElementById('currentAvatarUrl')?.value || '';

        document.getElementById('editFullName').value = fullName;
        document.getElementById('editPhone').value = phone === 'Chưa cập nhật' ? '' : phone;
        document.getElementById('editAvatarUrl').value = currentAvatar;
    }

    /**
     * Setup form validation and submission
     */
    function setupFormValidation() {
        const form = document.getElementById('profileForm');
        if (!form) return;

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            if (validateForm()) {
                updateProfile();
            }
        });

        // Real-time validation
        const fullNameInput = document.getElementById('editFullName');
        if (fullNameInput) {
            fullNameInput.addEventListener('blur', function () {
                validateField(this, function (value) {
                    return value.trim().length >= 2 && value.trim().length <= 100;
                }, 'Họ tên phải có từ 2 đến 100 ký tự');
            });
        }
    }

    /**
     * Validate the entire form
     */
    function validateForm() {
        let isValid = true;
        const fullNameInput = document.getElementById('editFullName');
        const phoneInput = document.getElementById('editPhone');

        // Validate Full Name
        if (!fullNameInput || !fullNameInput.value.trim()) {
            showFieldError(fullNameInput, 'Họ tên là bắt buộc');
            isValid = false;
        } else if (fullNameInput.value.trim().length < 2 || fullNameInput.value.trim().length > 100) {
            showFieldError(fullNameInput, 'Họ tên phải có từ 2 đến 100 ký tự');
            isValid = false;
        } else {
            clearFieldError(fullNameInput);
        }

        // Validate Phone (optional, but if provided, should be valid)
        if (phoneInput && phoneInput.value.trim()) {
            const phoneRegex = /^[0-9]{10,11}$/;
            if (!phoneRegex.test(phoneInput.value.trim())) {
                showFieldError(phoneInput, 'Số điện thoại không hợp lệ (10-11 chữ số)');
                isValid = false;
            } else {
                clearFieldError(phoneInput);
            }
        }

        return isValid;
    }

    /**
     * Validate individual field
     */
    function validateField(field, validator, errorMessage) {
        if (field.value.trim() && !validator(field.value)) {
            showFieldError(field, errorMessage);
            return false;
        } else {
            clearFieldError(field);
            return true;
        }
    }

    /**
     * Show field error
     */
    function showFieldError(field, message) {
        if (!field) return;
        field.classList.add('is-invalid');
        togglePasswordInputError(field, true);
        const feedback = findFeedbackElement(field);
        if (feedback) {
            feedback.textContent = message;
        }
    }

    /**
     * Clear field error
     */
    function clearFieldError(field) {
        if (!field) return;
        field.classList.remove('is-invalid');
        togglePasswordInputError(field, false);
        const feedback = findFeedbackElement(field);
        if (feedback) {
            feedback.textContent = '';
        }
    }

    function togglePasswordInputError(field, hasError) {
        const wrapper = field.closest('.password-input');
        if (!wrapper) return;
        if (hasError) {
            wrapper.classList.add('has-error');
        } else {
            wrapper.classList.remove('has-error');
        }
    }

    function findFeedbackElement(field) {
        if (!field) return null;
        let feedback = field.parentElement?.querySelector('.invalid-feedback');
        if (!feedback) {
            const group = field.closest('.mb-3, .form-group');
            feedback = group ? group.querySelector('.invalid-feedback') : null;
        }
        return feedback;
    }

    /**
     * Clear all form validation errors
     */
    function clearFormValidation() {
        const form = document.getElementById('profileForm');
        if (!form) return;
        const invalidFields = form.querySelectorAll('.is-invalid');
        invalidFields.forEach(field => {
            field.classList.remove('is-invalid');
            const feedback = field.parentElement.querySelector('.invalid-feedback');
            if (feedback) {
                feedback.textContent = '';
            }
        });
    }

    /**
     * Setup password change modal behaviour
     */
    function setupChangePasswordFlow() {
        const modalElement = document.getElementById('changePasswordModal');
        if (!modalElement) return;

        const requestForm = document.getElementById('requestPasswordChangeForm');
        const confirmForm = document.getElementById('confirmPasswordChangeForm');
        const requestStep = document.getElementById('passwordStepRequest');
        const confirmStep = document.getElementById('passwordStepConfirm');
        const backButton = document.getElementById('backToRequestStepBtn');

        const toggleSteps = (showConfirm) => {
            if (showConfirm) {
                requestStep.classList.add('d-none');
                confirmStep.classList.remove('d-none');
            } else {
                requestStep.classList.remove('d-none');
                confirmStep.classList.add('d-none');
            }
        };

        const resetForms = () => {
            if (requestForm) {
                requestForm.reset();
                const currentPasswordInput = requestForm.querySelector('#currentPassword');
                clearFieldError(currentPasswordInput);
            }
            if (confirmForm) {
                confirmForm.reset();
                ['verificationCode', 'newPassword', 'confirmNewPassword'].forEach(id => {
                    const input = document.getElementById(id);
                    clearFieldError(input);
                });
            }
            toggleSteps(false);
        };

        modalElement.addEventListener('hidden.bs.modal', () => {
            resetForms();
        });

        if (backButton) {
            backButton.addEventListener('click', function (e) {
                e.preventDefault();
                toggleSteps(false);
            });
        }

        if (requestForm) {
            requestForm.addEventListener('submit', async function (e) {
                e.preventDefault();
                const currentPasswordInput = document.getElementById('currentPassword');
                const currentPassword = currentPasswordInput?.value?.trim();

                if (!currentPassword) {
                    showFieldError(currentPasswordInput, 'Vui lòng nhập mật khẩu hiện tại');
                    return;
                }

                clearFieldError(currentPasswordInput);

                const submitBtn = document.getElementById('requestChangePasswordBtn');
                const originalText = submitBtn?.innerHTML;
                if (submitBtn) {
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang gửi...';
                }

                try {
                    const response = await fetch('/UserProfile/RequestPasswordChange', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ CurrentPassword: currentPassword })
                    });

                    const result = await response.json();
                    if (result.success) {
                        if (typeof showSuccessToast === 'function') {
                            showSuccessToast(result.message || 'Mã xác nhận đã được gửi. Vui lòng kiểm tra email.');
                        }
                        toggleSteps(true);
                        document.getElementById('verificationCode')?.focus();
                    } else {
                        showFieldError(currentPasswordInput, result.message || 'Không thể gửi mã xác nhận.');
                        if (typeof showErrorToast === 'function') {
                            showErrorToast(result.message || 'Không thể gửi mã xác nhận.');
                        }
                    }
                } catch (error) {
                    console.error('Request password change failed:', error);
                    if (typeof showErrorToast === 'function') {
                        showErrorToast('Không thể gửi mã xác nhận. Vui lòng thử lại sau.');
                    }
                } finally {
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = originalText;
                    }
                }
            });
        }

        if (confirmForm) {
            confirmForm.addEventListener('submit', async function (e) {
                e.preventDefault();

                const codeInput = document.getElementById('verificationCode');
                const newPasswordInput = document.getElementById('newPassword');
                const confirmPasswordInput = document.getElementById('confirmNewPassword');

                const code = codeInput?.value?.trim();
                const newPassword = newPasswordInput?.value?.trim();
                const confirmPassword = confirmPasswordInput?.value?.trim();

                let isValid = true;

                if (!code) {
                    showFieldError(codeInput, 'Vui lòng nhập mã xác nhận.');
                    isValid = false;
                } else {
                    clearFieldError(codeInput);
                }

                if (!newPassword || newPassword.length < 8) {
                    showFieldError(newPasswordInput, 'Mật khẩu mới phải có ít nhất 8 ký tự.');
                    isValid = false;
                } else {
                    clearFieldError(newPasswordInput);
                }

                if (newPassword !== confirmPassword) {
                    showFieldError(confirmPasswordInput, 'Mật khẩu xác nhận không khớp.');
                    isValid = false;
                } else {
                    clearFieldError(confirmPasswordInput);
                }

                if (!isValid) return;

                const submitBtn = document.getElementById('confirmChangePasswordBtn');
                const originalText = submitBtn?.innerHTML;
                if (submitBtn) {
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang xử lý...';
                }

                try {
                    const response = await fetch('/UserProfile/ConfirmPasswordChange', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            Code: code,
                            NewPassword: newPassword,
                            ConfirmPassword: confirmPassword
                        })
                    });

                    const result = await response.json();
                    if (result.success) {
                        if (typeof showSuccessToast === 'function') {
                            showSuccessToast(result.message || 'Đổi mật khẩu thành công.');
                        }

                        const modalInstance = bootstrap.Modal.getInstance(modalElement);
                        if (modalInstance) {
                            modalInstance.hide();
                        }
                    } else {
                        if (typeof showErrorToast === 'function') {
                            showErrorToast(result.message || 'Không thể đổi mật khẩu. Vui lòng thử lại.');
                        }
                    }
                } catch (error) {
                    console.error('Confirm password change failed:', error);
                    if (typeof showErrorToast === 'function') {
                        showErrorToast('Không thể đổi mật khẩu. Vui lòng thử lại sau.');
                    }
                } finally {
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = originalText;
                    }
                }
            });
        }
    }


    /**
     * Update user profile via AJAX
     */
    async function updateProfile() {
        const saveBtn = document.getElementById('saveProfileBtn');
        const originalBtnText = saveBtn?.innerHTML || 'Lưu thay đổi';
        
        // Disable button and show loading state
        if (saveBtn) {
            saveBtn.disabled = true;
            saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...';
        }

        try {
            // Get form data
            const formData = {
                FullName: document.getElementById('editFullName').value.trim(),
                Phone: document.getElementById('editPhone').value.trim() || null,
                AvatarUrl: document.getElementById('editAvatarUrl').value.trim() || null
            };

            // Send POST request
            const response = await fetch('/UserProfile/UpdateProfile', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                // Update UI with new data
                updateProfileDisplay(result.data);
                
                // Show success message
                if (typeof showSuccessToast === 'function') {
                    showSuccessToast(result.message || 'Cập nhật thông tin thành công!');
                }

                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('editProfileModal'));
                if (modal) {
                    modal.hide();
                }

                // Optionally reload page to ensure consistency
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                // Show error message
                if (typeof showErrorToast === 'function') {
                    showErrorToast(result.message || 'Cập nhật thất bại. Vui lòng thử lại.');
                }
                
                // Show validation errors if any
                if (result.errors && Array.isArray(result.errors)) {
                    result.errors.forEach((error, index) => {
                        setTimeout(() => {
                            if (typeof showWarningToast === 'function') {
                                showWarningToast(error);
                            }
                        }, index * 500);
                    });
                }
            }
        } catch (error) {
            console.error('Error updating profile:', error);
            if (typeof showErrorToast === 'function') {
                showErrorToast('Đã xảy ra lỗi khi cập nhật. Vui lòng thử lại sau.');
            }
        } finally {
            // Re-enable button
            if (saveBtn) {
                saveBtn.disabled = false;
                saveBtn.innerHTML = originalBtnText;
            }
        }
    }

    /**
     * Update profile display with new data
     */
    function updateProfileDisplay(data) {
        if (data.FullName) {
            const elements = document.querySelectorAll('#displayFullName, #profileFullName');
            elements.forEach(el => {
                if (el) el.textContent = data.FullName;
            });
        }

        if (data.Email) {
            const elements = document.querySelectorAll('#displayEmail, #profileEmail');
            elements.forEach(el => {
                if (el) el.textContent = data.Email;
            });
        }

        if (data.Phone !== undefined) {
            const phoneValue = data.Phone || 'Chưa cập nhật';
            const phoneElements = document.querySelectorAll('#profilePhone');
            phoneElements.forEach(el => {
                if (el) el.textContent = phoneValue;
            });
        }

        if (data.RoleName) {
            const elements = document.querySelectorAll('#displayRole, #profileRole');
            elements.forEach(el => {
                if (el) el.textContent = data.RoleName;
            });
        }

        const avatarImg = document.getElementById('userAvatar');
        const defaultAvatar = avatarImg?.dataset?.defaultAvatar;
        const avatarHiddenInput = document.getElementById('currentAvatarUrl');
        if (avatarImg) {
            if (data.AvatarUrl) {
                avatarImg.src = data.AvatarUrl;
                if (avatarHiddenInput) {
                    avatarHiddenInput.value = data.AvatarUrl;
                }
            } else if (defaultAvatar) {
                avatarImg.src = defaultAvatar;
                if (avatarHiddenInput) {
                    avatarHiddenInput.value = '';
                }
            }
        }
    }

    /**
     * Show alert message - Now uses Toast notification
     * Kept for backward compatibility
     */
    function showAlert(type, message) {
        // Map to Toast notification types
        if (typeof showToast === 'function') {
            const toastType = type === 'success' ? 'success' : 
                            type === 'danger' ? 'error' : 
                            type === 'warning' ? 'warning' : 'info';
            showToast(message, toastType);
        }
    }

    /**
     * Escape HTML to prevent XSS
     */
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }
})();

