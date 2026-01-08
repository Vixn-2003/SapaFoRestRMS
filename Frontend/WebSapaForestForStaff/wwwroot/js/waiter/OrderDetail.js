// ================================
// orderDetail.js
// ================================

// ================================
// Global variables
// ================================
var cartItems = [];
var tableId = window.__TABLE_ID__ || 0;
var orderStatus = window.__ORDER_STATUS__ || '';
var tempItemToAdd = null;
var pendingModification = null;

var modalConfirm = null; // Modal thêm món
var modalDelete = null;  // Modal xóa món
var modalWarning = null; // Modal cảnh báo

// ================================
// Initialize cartItems from server
// ================================
try {
    if (window.__INITIAL_ORDER_DATA__ && window.__INITIAL_ORDER_DATA__ !== 'null') {
        cartItems = window.__INITIAL_ORDER_DATA__.map(function (item) {
            return {
                ...item,
                isNew: false,
                isDirty: false,
                isDeleted: false
            };
        });
    }
} catch (e) {
    console.error("Error parsing initial data:", e);
    cartItems = [];
}

// ================================
// Render Cart
// ================================
// ================================
// Render Cart (ĐÃ SỬA LỖI)
// ================================
function renderCart() {
    var $container = $('#cartContainer');
    $container.empty();

    var grandTotal = 0;
    var totalQty = 0;

    var visibleItems = cartItems.filter(x => !x.isDeleted);

    if (visibleItems.length === 0) {
        $container.html('<div class="text-center text-muted mt-5"><i class="fa-solid fa-utensils fs-1 mb-3 opacity-25"></i><p style="font-size: 12px;">Chưa có món nào.</p></div>');
    } else {
        visibleItems.forEach(function (item) {
            var realIndex = cartItems.indexOf(item);

            // --- 1. CHUẨN HÓA STATUS ---
            var s = (item.status || "").toString().trim().toLowerCase();
            var isCancelled = (s === "cancelled" || s === "cancel" || s === "đã hủy");

            // --- SỬA LẠI KHÚC NÀY: BỎ 'PENDING' RA KHỎI DANH SÁCH KHÓA ---
            // Chỉ khóa khi Bếp đang làm (Cooking) hoặc Đã xong (Done)
            // Pending (Đã gửi) vẫn được coi là "Mở" để sửa
            var lockedList = ["cooking", "done", "served", "ready", "processing", "đang chế biến", "đã xong"];

            // Biến kiểm tra khóa
            var isLocked = lockedList.includes(s) || isCancelled;

            // --- 2. TÍNH TIỀN ---
            var lineTotal = item.price * item.quantity;
            if (!isCancelled) {
                grandTotal += lineTotal;
                totalQty += item.quantity;
            }

            // --- 3. HIỂN THỊ BADGE (GIỮ NGUYÊN) ---
            var statusHtml = '';
            var itemClass = '';

            if (s === "pending" || s === "đã gửi")
                statusHtml = '<span class="badge bg-primary" style="font-size:10px;">ĐÃ GỬI</span>';
            else if (['cooking', 'processing', 'ready', 'đang chế biến'].includes(s))
                statusHtml = '<span class="badge-processing">CHẾ BIẾN</span>';
            else if (['done', 'served', 'đã xong'].includes(s))
                statusHtml = '<span class="badge-finished">ĐÃ XONG</span>';
            else if (isCancelled) {
                statusHtml = '<span class="badge-cancelled">ĐÃ HỦY</span>';
                itemClass = 'item-cancelled';
            }
            else {
                statusHtml = '<span class="badge bg-success" style="font-size:10px;">Mới</span>';
            }

            var priceStr = lineTotal.toLocaleString('vi-VN');
            var unitPriceStr = item.price.toLocaleString('vi-VN');

            // --- 4. ACTION BUTTONS ---
            // Pending vẫn hiện nút xóa/note vì !isLocked = true
            var actionBtns = '';
            if (!isLocked) {
                var hasNote = item.note && item.note.trim().length > 0;
                var iconNoteClass = hasNote ? "text-warning" : "text-secondary";

                actionBtns += `
                    <button class="btn-note btn btn-link ${iconNoteClass} p-0 me-3" title="Ghi chú">
                        <i class="fa-solid fa-pen-to-square"></i>
                    </button>
                    <button class="btn-delete btn btn-link text-danger p-0" title="Xóa">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                `;
            }

            // --- 5. NOTE AREA ---
            var noteHtml = '';
            if (!isCancelled) {
                var noteContent = item.note || '';
                if (isLocked) {
                    if (noteContent.trim() !== '') {
                        noteHtml = `<div class="text-muted fst-italic small mt-1" style="font-size:11px;"><i class="fa-solid fa-note-sticky me-1"></i>${noteContent}</div>`;
                    }
                } else {
                    var noteDisplay = noteContent.trim().length > 0 ? 'block' : 'none';
                    noteHtml = `
                        <div class="note-box" data-index="${realIndex}" style="display:${noteDisplay}; margin-top:5px;">
                            <textarea class="note-input form-control" rows="1" placeholder="Ghi chú...">${noteContent}</textarea>
                        </div>`;
                }
            }

            // --- 6. CONTROLS HTML (Nút cộng trừ) ---
            // --- 6. CONTROLS HTML (Nút cộng trừ + Input) ---
            var controlsHtml = '';

            // NẾU KHÔNG KHÓA (Tức là Mới hoặc Pending) -> HIỆN NÚT VÀ INPUT
            if (!isLocked) {
                controlsHtml = `
                    <div class="d-flex justify-content-between align-items-center mt-2">
                        <div class="qty-control d-flex align-items-center" data-index="${realIndex}">
                            <button class="btn-minus btn btn-sm btn-outline-secondary"><i class="fa-solid fa-minus"></i></button>
                            
                            <input type="number" class="qty-input form-control form-control-sm mx-2 text-center" 
                                   value="${item.quantity}" 
                                   min="1" 
                                   style="width: 60px; font-weight: bold; padding: 2px;">
                                   
                            <button class="btn-plus btn btn-sm btn-outline-secondary"><i class="fa-solid fa-plus"></i></button>
                        </div>
                        <div class="text-muted small" style="font-size:10px;">
                            ${unitPriceStr} x ${item.quantity} = <strong>${priceStr}</strong>
                        </div>
                    </div>`;
            } else {
                // ĐÃ KHÓA (Cooking/Done) -> CHỈ HIỆN TEXT (Giữ nguyên như cũ)
                controlsHtml = `
                    <div class="d-flex justify-content-between align-items-center mt-2">
                        <div class="fw-bold text-success" style="font-size:13px;">SL: ${item.quantity}</div>
                        <div class="text-muted small" style="font-size:10px;">
                            ${unitPriceStr} x ${item.quantity} = <strong>${priceStr}</strong>
                        </div>
                    </div>`;
            }

            // --- 7. RENDER ---
            var html = `
                <div class="cart-item ${itemClass} border-bottom pb-2 mb-2" data-index="${realIndex}">
                    <div class="d-flex justify-content-between align-items-start">
                        <div class="item-name fw-bold" style="flex:1; font-size:14px;">${item.name}</div>
                        <div>${statusHtml}</div>
                    </div>
                    <div class="d-flex justify-content-end mt-1 mb-1">${actionBtns}</div>
                    ${noteHtml}
                    ${controlsHtml}
                </div>
            `;
            $container.append(html);
        });
    }

    // Update Footer
    $('#lblTotalQty').text(totalQty);
    $('#lblSubTotal').text(grandTotal.toLocaleString('vi-VN') + ' đ');
    $('#lblGrandTotal').text(grandTotal.toLocaleString('vi-VN') + ' đ');

    // Button Save
    var hasChanges = cartItems.some(x => x.isNew || x.isDirty || x.isDeleted);
    var isOrderConfirmed = (typeof orderStatus !== 'undefined' && orderStatus) ? orderStatus.toLowerCase() === 'confirmed' : false;

    if (hasChanges && !isOrderConfirmed) $('.btn-save-order').show();
    else $('.btn-save-order').hide();
}

// ================================
// Change Quantity
// ================================
function changeQty(index, change) {
    var item = cartItems[index];

    // Kiểm tra order đã Confirmed chưa
    if (orderStatus && orderStatus.toLowerCase() === 'confirmed') {
        showWarningModal({
            type: 'orderConfirmed',
            message: 'Đơn hàng đã được xác nhận, không thể chỉnh sửa món.'
        });
        return;
    }

    // Kiểm tra món đã Finished chưa
    var isFinished = item.status === "Done" || item.status === "Served" || item.status === "Đã xong";
    if (isFinished) {
        showWarningModal({
            type: 'invalidStatus',
            message: 'Món đã hoàn thành, không thể chỉnh số lượng.'
        });
        return;
    }

    if (!item.isNew) {
        showWarningModal({ type: 'qty', index: index, change: change });
        return;
    }

    executeChangeQty(index, change);
}

function executeChangeQty(index, change) {
    var item = cartItems[index];
    var newQty = item.quantity + change;

    if (newQty <= 0) executeRemoveItem(index);
    else {
        item.quantity = newQty;
        if (!item.isNew) item.isDirty = true;
        renderCart();
    }
}

// ================================
// Remove Item
// ================================
function removeItem(index) {
    var item = cartItems[index];

    // Kiểm tra order đã Confirmed chưa
    if (orderStatus && orderStatus.toLowerCase() === 'confirmed') {
        showWarningModal({
            type: 'orderConfirmed',
            message: 'Đơn hàng đã được xác nhận, không thể xóa món.'
        });
        return;
    }

    // Kiểm tra món đã Finished chưa
    var isFinished = item.status === "Done" || item.status === "Served" || item.status === "Đã xong";
    if (isFinished) {
        showWarningModal({
            type: 'invalidStatus',
            message: 'Món đã hoàn thành, không thể xóa.'
        });
        return;
    }

    if (!item.isNew) {
        showWarningModal({ type: 'delete', index: index });
        return;
    }

    if (confirm("Xóa món " + item.name + " vừa thêm?")) {
        executeRemoveItem(index);
    }
}

function executeRemoveItem(index) {
    var item = cartItems[index];
    if (item.isNew) cartItems.splice(index, 1);
    else item.isDeleted = true;

    renderCart();
}

// ================================
// Note
// ================================
function toggleNote(index) {
    var item = cartItems[index];

    // Kiểm tra order đã Confirmed chưa
    if (orderStatus && orderStatus.toLowerCase() === 'confirmed') {
        showWarningModal({
            type: 'orderConfirmed',
            message: 'Đơn hàng đã được xác nhận, không thể chỉnh sửa ghi chú.'
        });
        return;
    }

    // Kiểm tra món đã Finished chưa
    var isFinished = item.status === "Done" || item.status === "Served" || item.status === "Đã xong";
    if (isFinished) return;

    var $noteBox = $('.note-box[data-index="' + index + '"]');
    $noteBox.toggle();
    if ($noteBox.is(':visible')) {
        $noteBox.find('textarea').focus();
    }
}

function updateNote(index, value) {
    var item = cartItems[index];
    item.note = value;
    if (!item.isNew) item.isDirty = true;
}

// ================================
// Warning Modal
// ================================
function showWarningModal(actionData) {
    pendingModification = actionData;
    if (modalWarning) modalWarning.show();
}

// ================================
// Document Ready
// ================================
$(document).ready(function () {

    modalConfirm = new bootstrap.Modal($('#confirmAddModal'));
    modalDelete = new bootstrap.Modal($('#confirmDeleteModal'));
    modalWarning = new bootstrap.Modal($('#modifyWarningModal'));

    renderCart();

    // ================================
    // Qty Events
    // ================================
    $(document).on('click', '.btn-plus', function () {
        var index = $(this).closest('.cart-item').data('index');
        changeQty(index, 1);
    });

    $(document).on('click', '.btn-minus', function () {
        var index = $(this).closest('.cart-item').data('index');
        changeQty(index, -1);
    });

    // ================================
    // Note
    // ================================
    $(document).on('click', '.btn-note', function () {
        var index = $(this).closest('.cart-item').data('index');
        toggleNote(index);
    });

    $(document).on('input', '.note-input', function () {
        var index = $(this).closest('.note-box').data('index');
        updateNote(index, $(this).val());
    });

    // ================================
    // Delete item
    // ================================
    $(document).on('click', '.btn-delete', function () {
        var index = $(this).closest('.cart-item').data('index');
        removeItem(index);
    });

    // Confirm modify modal
    $('#btnConfirmModify').click(function () {
        if (!pendingModification) return;

        if (pendingModification.type === 'qty')
            executeChangeQty(pendingModification.index, pendingModification.change);
        else if (pendingModification.type === 'delete')
            executeRemoveItem(pendingModification.index);

        pendingModification = null;
        modalWarning.hide();
    });

    // ================================
    // Add new item to cart
    // ================================
    $(document).on('click', '.btn-select-item', function () {
        // Kiểm tra order đã Confirmed chưa
        if (orderStatus && orderStatus.toLowerCase() === 'confirmed') {
            showWarningModal({
                type: 'orderConfirmed',
                message: 'Đơn hàng đã được xác nhận, không thể thêm món mới.'
            });
            return;
        }

        var $btn = $(this);
        var itemId = $btn.data('id');
        var isCombo = $btn.data('iscombo') === true || $btn.data('iscombo') === "True";
        var name = $btn.data('name');
        var price = parseFloat($btn.data('price'));

        tempItemToAdd = {
            id: 0,
            itemId: itemId,
            isCombo: isCombo,
            name: name,
            price: price,
            quantity: 1,
            note: "",
            status: "Pending",
            isNew: true,
            isDirty: false,
            isDeleted: false
        };

        $('#modalItemName').text(name);
        if (modalConfirm) modalConfirm.show();
    });

    $('#btnConfirmAdd').click(function () {
        if (!tempItemToAdd) return;

        var existing = cartItems.find(x =>
            x.itemId === tempItemToAdd.itemId &&
            x.isCombo === tempItemToAdd.isCombo &&
            x.isNew && !x.isDeleted
        );

        if (existing) existing.quantity++;
        else cartItems.push(tempItemToAdd);

        renderCart();
        modalConfirm.hide();
        tempItemToAdd = null;

        showToast("Thêm món thành công!", "success");
    });

    // ================================
    // Save Order
    // ================================
    $('.btn-save-order').click(function () {
        // Kiểm tra order đã Confirmed chưa
        if (orderStatus && orderStatus.toLowerCase() === 'confirmed') {
            alert('⚠️ Đơn hàng đã được xác nhận, không thể lưu thay đổi.');
            return;
        }

        var $btn = $(this);
        $btn.prop('disabled', true).text('Đang lưu...');

        var changedItems = cartItems.filter(x => x.isNew || x.isDirty || x.isDeleted);
        if (changedItems.length === 0) {
            alert("Không có thay đổi.");
            $btn.prop('disabled', false).text('Lưu Order');
            return;
        }

        var payload = {
            tableId: parseInt(tableId),
            items: changedItems.map(x => ({
                orderItemId: x.id,
                menuItemId: x.isCombo ? null : x.itemId,
                comboId: x.isCombo ? x.itemId : null,
                quantity: x.quantity,
                note: x.note,
                action: x.isDeleted ? "Delete" : (x.isNew ? "Add" : "Update")
            }))
        };

        $.ajax({
            url: 'https://localhost:7096/api/DashboardTable/SaveChanges',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: () => {
                showToast2("Lưu Order thành công!", "success");
                setTimeout(() => location.reload(), 1000);
            },
            error: err => {
                const msg =
                    err.responseJSON?.message ||
                    err.responseJSON?.error ||
                    "Lỗi không xác định";

                showToast2(msg, "danger");
                $btn.prop("disabled", false).text("Lưu Order");
            }
        });
    });

    // Toast supporting functions
    function showToast(message, type = 'success') {
        $('#liveToastContainer').remove();
        $("body").append(`
            <div id="liveToastContainer" style="position: fixed; top: 20px; right: 20px; z-index: 1055;">
                <div class="toast bg-${type} text-white show" role="alert">
                    <div class="toast-body">${message}</div>
                </div>
            </div>
        `);

        setTimeout(() => $("#liveToastContainer").remove(), 3000);
    }

    function showToast2(message, type = "success") {
        $("#liveToastContainer").remove();

        $("body").append(`
            <div id="liveToastContainer"
                style="position:fixed; top:20px; right:20px; z-index:2000;">
                <div class="toast align-items-center text-white bg-${type} border-0 show">
                    <div class="d-flex">
                        <div class="toast-body">${message}</div>
                    </div>
                </div>
            </div>
        `);

        setTimeout(() => $("#liveToastContainer").remove(), 3000);
    }
});
// ================================
// Input Quantity Direct Change
// ================================
$(document).on('change', '.qty-input', function () {
    var $input = $(this);
    var index = $input.closest('.cart-item').data('index');
    var newVal = parseInt($input.val());
    var item = cartItems[index];

    // 1. Validate dữ liệu nhập
    if (isNaN(newVal) || newVal < 1) {
        alert("Số lượng phải lớn hơn 0");
        renderCart(); // Reset lại hiển thị số cũ
        return;
    }

    // 2. Kiểm tra các điều kiện chặn (Confirmed/Finished)
    // (Copy logic từ changeQty sang để đảm bảo an toàn)
    if (orderStatus && orderStatus.toLowerCase() === 'confirmed') {
        showWarningModal({
            type: 'orderConfirmed',
            message: 'Đơn hàng đã được xác nhận, không thể chỉnh sửa số lượng.'
        });
        renderCart(); // Reset lại số cũ
        return;
    }

    var isFinished = item.status === "Done" || item.status === "Served" || item.status === "Đã xong";
    if (isFinished) {
        showWarningModal({
            type: 'invalidStatus',
            message: 'Món đã hoàn thành, không thể chỉnh số lượng.'
        });
        renderCart(); // Reset lại số cũ
        return;
    }

    // 3. Nếu là món cũ (đã lưu DB) -> Cảnh báo
    if (!item.isNew) {
        // Tính độ lệch để hiển thị trong modal (Mới - Cũ)
        var diff = newVal - item.quantity;
        if (diff === 0) return; // Không thay đổi gì

        // Hack: dùng lại modal warning nhưng truyền tham số đặc biệt để xử lý set trực tiếp
        // Tuy nhiên để đơn giản, ta gán tạm vào logic existing
        // Vì logic modal hiện tại dùng +/- (change), nên ta cần custom lại một chút
        // Cách đơn giản nhất: Gọi thẳng hàm update nếu user confirm

        // Ở đây tôi sẽ cập nhật trực tiếp biến pendingModification để modal hiểu
        // Nhưng modal hiện tại logic là "change" (+/-). 
        // Để hỗ trợ set trực tiếp, ta tính diff và dùng logic cũ:
        showWarningModal({ type: 'qty', index: index, change: diff });

        // Reset hiển thị về số cũ trước khi user bấm Đồng ý trong modal
        renderCart();
        return;
    }

    // 4. Nếu là món mới -> Cập nhật luôn
    item.quantity = newVal;
    renderCart();
});

// (Tùy chọn) Focus vào input thì bôi đen toàn bộ số để dễ nhập
$(document).on('focus', '.qty-input', function () {
    $(this).select();
});