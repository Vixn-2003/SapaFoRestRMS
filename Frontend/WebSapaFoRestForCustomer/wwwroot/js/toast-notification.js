function ensureToastContainer() {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        document.body.appendChild(container);
    }
    return container;
}

function showToast(message, type = 'info') {
    const container = ensureToastContainer();
    const wrapper = document.createElement('div');
    wrapper.className = 'toast align-items-center text-bg-' + mapType(type) + ' border-0';
    wrapper.setAttribute('role', 'alert');
    wrapper.setAttribute('aria-live', 'assertive');
    wrapper.setAttribute('aria-atomic', 'true');

    wrapper.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${escapeHtml(message)}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    container.appendChild(wrapper);
    const toast = new bootstrap.Toast(wrapper, { delay: 3000 });
    toast.show();

    wrapper.addEventListener('hidden.bs.toast', () => {
        wrapper.remove();
    });
}

function showSuccessToast(message) { showToast(message, 'success'); }
function showErrorToast(message) { showToast(message, 'danger'); }
function showWarningToast(message) { showToast(message, 'warning'); }
function showInfoToast(message) { showToast(message, 'info'); }

function mapType(type) {
    switch (type) {
        case 'success':
        case 'danger':
        case 'warning':
        case 'info':
            return type;
        default:
            return 'secondary';
    }
}

function escapeHtml(unsafe) {
    return String(unsafe)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}


