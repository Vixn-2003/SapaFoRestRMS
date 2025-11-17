/**
 * Create Staff Page - JavaScript
 * Handles email verification flow and minimal validation
 */

document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('createStaffForm');
    if (!form) return;

    const sendBtn = document.getElementById('sendVerificationBtn');
    const fullNameInput = document.getElementById('FullName');
    const emailInput = document.getElementById('Email');
    const codeInput = document.getElementById('VerificationCode');
    const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
    const sendCodeUrl = form.dataset.sendCodeUrl;

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    let countdownInterval = null;
    const cooldownSeconds = 60;

    function setButtonState(disabled, label) {
        if (!sendBtn) return;
        sendBtn.disabled = disabled;
        if (label) {
            sendBtn.innerHTML = label;
        }
    }

    function startCooldown() {
        let remaining = cooldownSeconds;
        setButtonState(true, `Gửi lại (${remaining}s)`);
        countdownInterval = setInterval(() => {
            remaining -= 1;
            if (remaining <= 0) {
                clearInterval(countdownInterval);
                setButtonState(false, '<i class="mdi mdi-email-send-outline"></i> Gửi mã xác minh');
                return;
            }
            setButtonState(true, `Gửi lại (${remaining}s)`);
        }, 1000);
    }

    async function sendVerificationCode() {
        if (!sendCodeUrl || !sendBtn) return;

        const fullName = fullNameInput?.value.trim();
        const email = emailInput?.value.trim();

        if (!fullName) {
            showErrorToast('Vui lòng nhập họ và tên trước khi gửi mã.');
            fullNameInput?.focus();
            return;
        }

        if (!email || !emailRegex.test(email)) {
            showErrorToast('Vui lòng nhập email hợp lệ trước khi gửi mã.');
            emailInput?.focus();
            return;
        }

        const token = tokenInput?.value;
        if (!token) {
            showErrorToast('Thiếu mã xác thực. Vui lòng tải lại trang.');
            return;
        }

        setButtonState(true, '<span class="spinner-border spinner-border-sm me-1"></span>Đang gửi...');

        try {
            const response = await fetch(sendCodeUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ fullName, email })
            });

            const data = await response.json();
            if (response.ok) {
                showSuccessToast(data.message || 'Đã gửi mã xác minh.');
                if (countdownInterval) {
                    clearInterval(countdownInterval);
                }
                startCooldown();
            } else {
                setButtonState(false, '<i class="mdi mdi-email-send-outline"></i> Gửi mã xác minh');
                showErrorToast(data.message || 'Không thể gửi mã xác minh. Vui lòng thử lại.');
            }
        } catch (error) {
            setButtonState(false, '<i class="mdi mdi-email-send-outline"></i> Gửi mã xác minh');
            showErrorToast('Không thể kết nối tới máy chủ. Vui lòng thử lại sau.');
        }
    }

    sendBtn?.addEventListener('click', sendVerificationCode);

    form.addEventListener('submit', function (e) {
        const fullName = fullNameInput?.value.trim();
        const email = emailInput?.value.trim();
        const code = codeInput?.value.trim();

        if (!fullName) {
            e.preventDefault();
            showErrorToast('Vui lòng nhập họ và tên');
            fullNameInput?.focus();
            return;
        }

        if (!email || !emailRegex.test(email)) {
            e.preventDefault();
            showErrorToast('Vui lòng nhập email hợp lệ');
            emailInput?.focus();
            return;
        }

        if (!code || code.length !== 6) {
            e.preventDefault();
            showErrorToast('Vui lòng nhập mã xác minh gồm 6 ký tự');
            codeInput?.focus();
        }
    });
});

