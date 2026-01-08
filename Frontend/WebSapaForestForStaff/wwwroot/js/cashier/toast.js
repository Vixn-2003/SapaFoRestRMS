// ✅ Toast notification function
function showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = 'position: fixed; top: 80px; right: 20px; z-index: 9999;';
        document.body.appendChild(container);
    }

    const bgClass = type === 'success' ? 'bg-success' : (type === 'error' || type === 'danger' ? 'bg-danger' : type === 'warning' ? 'bg-warning' : 'bg-info');
    const icon = type === 'success' ? 'fa-circle-check' : (type === 'error' || type === 'danger' ? 'fa-circle-exclamation' : type === 'warning' ? 'fa-triangle-exclamation' : 'fa-circle-info');

    const toastHtml = `
                <div class="toast align-items-center text-white ${bgClass} border-0 mb-2 show" role="alert" aria-live="assertive" aria-atomic="true">
                    <div class="d-flex">
                        <div class="toast-body"><i class="fa-solid ${icon} me-2"></i> ${message}</div>
                        <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                    </div>
                </div>`;
    container.insertAdjacentHTML('beforeend', toastHtml);

    // Auto hide sau 3 giây
    setTimeout(() => {
        const toastElement = container.lastElementChild;
        if (toastElement) {
            toastElement.classList.remove('show');
            setTimeout(() => toastElement.remove(), 300);
        }
    }, 3000);
}