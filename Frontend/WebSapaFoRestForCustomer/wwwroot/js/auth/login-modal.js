document.addEventListener('DOMContentLoaded', function () {
    const requestForm = document.getElementById('loginRequestOtpForm');
    const verifyForm = document.getElementById('loginVerifyOtpForm');
    const phoneInput = document.getElementById('loginPhone');
    const verifyPhoneHidden = document.getElementById('verifyPhoneHidden');
    const backToPhoneBtn = document.getElementById('loginBackToPhoneBtn');
    const messageBox = document.getElementById('loginModalMessage');

    function getAntiForgeryToken(form) {
        const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    function setModalMessage(type, text) {
        if (!messageBox) return;
        messageBox.classList.remove('d-none', 'alert-success', 'alert-danger', 'alert-warning', 'alert-info');
        const cls =
            type === 'success' ? 'alert-success' :
                type === 'warning' ? 'alert-warning' :
                    type === 'info' ? 'alert-info' : 'alert-danger';
        messageBox.classList.add(cls);
        messageBox.textContent = text || '';
    }

    function clearModalMessage() {
        if (!messageBox) return;
        messageBox.classList.add('d-none');
        messageBox.textContent = '';
        messageBox.classList.remove('alert-success', 'alert-danger', 'alert-warning', 'alert-info');
    }

    if (requestForm) {
        requestForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(requestForm);
            const token = getAntiForgeryToken(requestForm);

            clearModalMessage();
            if (!phoneInput.value || !phoneInput.value.trim()) {
                setModalMessage('danger', 'Vui lòng nhập số điện thoại');
                return;
            }


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
                  
                    verifyPhoneHidden.value = phoneInput.value.trim();
                    requestForm.style.display = 'none';
                    verifyForm.style.display = '';
                    document.getElementById('loginOtpCode').focus();
                    setModalMessage('success', 'Mã OTP đã được gửi tới số điện thoại của bạn.');
                })
                .catch(() => setModalMessage('danger', 'Không thể gửi mã OTP. Vui lòng thử lại.'));
        });
    }

    if (verifyForm) {
        verifyForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(verifyForm);
            const token = getAntiForgeryToken(verifyForm);

            clearModalMessage();

            fetch(verifyForm.action, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token },
                body: formData
            })
                .then(res => {
                    if (res.redirected) {
                        // Successful login returns redirect (usually Home or returnUrl)
                        try {
                            const modalEl = document.getElementById('loginModal');
                            const modalInstance = modalEl ? bootstrap.Modal.getInstance(modalEl) : null;
                            if (modalInstance) modalInstance.hide();
                        } catch (e) {
                            // ignore
                        }

                        if (window.toastr && toastr.success) {
                            toastr.success('Đăng nhập thành công');
                        }

                        // Navigate so navbar updates (authenticated state)
                        setTimeout(() => { window.location.href = res.url; }, 600);
                        return null;
                    }
                    return res;
                })
                .then(async (res) => {
                    if (res === null) return;

                    // Prefer JSON { message } (e.g. inactive account), fallback to default text
                    let msg = 'Mã OTP không đúng hoặc đã hết hạn. Vui lòng thử lại.';
                    try {
                        const data = await res.json();
                        if (data && data.message) msg = data.message;
                    } catch { }
                    setModalMessage('danger', msg);
                })
                .catch(() => setModalMessage('danger', 'Có lỗi xảy ra. Vui lòng thử lại.'));
        });
    }


    if (backToPhoneBtn) {
        backToPhoneBtn.addEventListener('click', function () {
            verifyForm.style.display = 'none';
            requestForm.style.display = '';
        });
    }
});


