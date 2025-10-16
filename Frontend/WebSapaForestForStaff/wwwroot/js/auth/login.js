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
});