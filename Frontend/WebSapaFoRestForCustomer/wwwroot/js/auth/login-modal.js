document.addEventListener('DOMContentLoaded', function () {
    const requestForm = document.getElementById('loginRequestOtpForm');
    const verifyForm = document.getElementById('loginVerifyOtpForm');
    const phoneInput = document.getElementById('loginPhone');
    const verifyPhoneHidden = document.getElementById('verifyPhoneHidden');
    const backToPhoneBtn = document.getElementById('loginBackToPhoneBtn');

    function getAntiForgeryToken(form) {
        const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    if (requestForm) {
        requestForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(requestForm);
            const token = getAntiForgeryToken(requestForm);

            fetch(requestForm.action, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token },
                body: formData
            })
                .then(res => {
                    if (res.redirected) {
                        // Server may render VerifyOtp view; we keep flow in modal instead
                        return { ok: true };
                    }
                    return res.text();
                })
                .then(() => {
                    if (!phoneInput.value) {
                        showErrorToast('Vui lòng nhập số điện thoại');
                        return;
                    }
                    verifyPhoneHidden.value = phoneInput.value.trim();
                    requestForm.style.display = 'none';
                    verifyForm.style.display = '';
                    document.getElementById('loginOtpCode').focus();
                    showSuccessToast('Mã OTP đã được gửi tới số điện thoại của bạn.');
                })
                .catch(() => showErrorToast('Không thể gửi mã OTP. Vui lòng thử lại.'));
        });
    }

    if (verifyForm) {
        verifyForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(verifyForm);
            const token = getAntiForgeryToken(verifyForm);

            fetch(verifyForm.action, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token },
                body: formData
            })
                .then(res => {
                    if (res.redirected) {
                        // Successful login returns redirect to Home
                        window.location.href = res.url;
                        return null;
                    }
                    return res.text();
                })
                .then(html => {
                    if (html === null) return;
                    // If not redirected, assume validation error
                    showErrorToast('Mã OTP không đúng hoặc đã hết hạn. Vui lòng thử lại.');
                })
                .catch(() => showErrorToast('Có lỗi xảy ra. Vui lòng thử lại.'));
        });
    }

    if (backToPhoneBtn) {
        backToPhoneBtn.addEventListener('click', function () {
            verifyForm.style.display = 'none';
            requestForm.style.display = '';
        });
    }
});


