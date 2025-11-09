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
        const avatarUrl = document.getElementById('userAvatar')?.src || '';

        document.getElementById('editFullName').value = fullName;
        document.getElementById('editPhone').value = phone === 'Chưa cập nhật' ? '' : phone;
        document.getElementById('editAvatarUrl').value = '';
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
        const feedback = field.parentElement.querySelector('.invalid-feedback');
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
        const feedback = field.parentElement.querySelector('.invalid-feedback');
        if (feedback) {
            feedback.textContent = '';
        }
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

        if (data.AvatarUrl) {
            const avatarImg = document.getElementById('userAvatar');
            if (avatarImg) {
                avatarImg.src = data.AvatarUrl;
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

