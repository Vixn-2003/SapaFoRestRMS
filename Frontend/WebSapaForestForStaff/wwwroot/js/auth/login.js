function togglePassword() {
    const passwordInput = document.getElementById("password");
    const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
    passwordInput.setAttribute("type", type);
}

document.addEventListener("DOMContentLoaded", function () {
    const toggle = document.getElementById("togglePassword");
    if (toggle) {
        toggle.style.cursor = "pointer";
        toggle.addEventListener("click", function () {
            const pwd = document.getElementById("password");
            if (!pwd) return;
            const isPwd = pwd.getAttribute("type") === "password";
            pwd.setAttribute("type", isPwd ? "text" : "password");
            // toggle eye icon class if available
            if (toggle.classList.contains("bi")) {
                toggle.classList.toggle("bi-eye");
                toggle.classList.toggle("bi-eye-slash");
            }
        });
    }

    // Form validation with Toast notifications
    const loginForm = document.querySelector('form[asp-action="Login"]');
    if (loginForm) {
        loginForm.addEventListener('submit', function(e) {
            const email = document.getElementById('Email')?.value.trim();
            const password = document.getElementById('password')?.value.trim();

            if (!email) {
                e.preventDefault();
                showErrorToast('Vui lòng nhập email');
                document.getElementById('Email')?.focus();
                return;
            }

            // Validate email format
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(email)) {
                e.preventDefault();
                showErrorToast('Email không hợp lệ');
                document.getElementById('Email')?.focus();
                return;
            }

            if (!password) {
                e.preventDefault();
                showErrorToast('Vui lòng nhập mật khẩu');
                document.getElementById('password')?.focus();
                return;
            }

            if (password.length < 6) {
                e.preventDefault();
                showErrorToast('Mật khẩu phải có ít nhất 6 ký tự');
                document.getElementById('password')?.focus();
                return;
            }
        });
    }
});