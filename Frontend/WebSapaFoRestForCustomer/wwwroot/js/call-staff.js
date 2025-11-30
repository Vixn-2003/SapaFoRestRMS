// call-staff.js

function showMobileToast(message, type = "success") {
    const toastEl = document.getElementById("mobileToast");
    const toastMessageEl = document.getElementById("mobileToastMessage");

    if (!toastEl || !toastMessageEl) return;

    toastMessageEl.textContent = message;
    toastMessageEl.style.backgroundColor = type === "error" ? "#dc3545" : "#28a745";

    toastEl.style.display = "block";
    toastEl.style.opacity = 0;
    toastEl.style.transition = "opacity 0.3s";

    setTimeout(() => { toastEl.style.opacity = 1; }, 10);
    setTimeout(() => {
        toastEl.style.opacity = 0;
        setTimeout(() => { toastEl.style.display = "none"; }, 300);
    }, 3000);
}

document.addEventListener("DOMContentLoaded", function () {
    const modalEl = document.getElementById('callStaffModal');
    const callBtn = document.getElementById('call-staff-btn');

    if (!modalEl || !callBtn) return;

    const tableIdInput = document.getElementById("tableId");
    const apiBaseUrlInput = document.getElementById("apiBaseUrl");

    if (!tableIdInput || !apiBaseUrlInput) return;

    const tableId = parseInt(tableIdInput.value);
    const apiBaseUrl = apiBaseUrlInput.value;

    const hasCustomer = callBtn.dataset.hasCustomer === "true"; // lấy trạng thái bàn từ data attribute
    const useReservation = hasCustomer ? true : false;

    const modal = new bootstrap.Modal(modalEl);

    callBtn.addEventListener('click', function () {
        modal.show();
        this.classList.add('ringing');
    });

    const sendBtn = document.getElementById('sendStaffRequest');
    if (!sendBtn) return;

    sendBtn.addEventListener('click', async function () {
        const note = document.getElementById('staffNote').value || "";

        const requestData = {
            tableId: tableId,
            note: note,
            useReservation: useReservation
        };

        sendBtn.disabled = true;
        sendBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang gửi...';

        try {
            const res = await fetch(apiBaseUrl + '/api/OrderTable/RequestAssistance', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json-patch+json' },
                body: JSON.stringify(requestData)
            });

            const data = await res.json();

            if (!res.ok) {
                showMobileToast(data.message || "Gửi yêu cầu thất bại.", "error");
            } else {
                showMobileToast(data.message || "Đã gửi yêu cầu thành công!", "success");
                modal.hide();
                callBtn.classList.remove('ringing');
                document.getElementById('staffNote').value = '';
            }
        } catch (e) {
            console.error("Lỗi khi gửi request:", e);
            showMobileToast("Gửi yêu cầu thất bại.", "error");
        } finally {
            sendBtn.disabled = false;
            sendBtn.innerHTML = '<i class="fas fa-paper-plane"></i> Gửi yêu cầu';
        }
    });
});
