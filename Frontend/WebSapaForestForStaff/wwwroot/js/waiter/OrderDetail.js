// ================================
// orderDetail.js
// ================================

// ================================
// Global variables
// ================================
var cartItems = [];
var tableId = window.__TABLE_ID__ || 0;
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
function renderCart() {
    var $container = $('#cartContainer');
    $container.empty();

    var grandTotal = 0;
    var totalQty = 0;

    var visibleItems = cartItems.filter(x => !x.isDeleted);

    if (visibleItems.length === 0) {
        $container.html('<div class="text-center text-muted mt-5"><i class="fa-solid fa-utensils fs-1 mb-3 opacity-25"></i><p style="font-size: 12px;">Chưa có món nào.</p></div>');
    } else {
        visibleItems.forEach(function (item, idx) {
            var realIndex = cartItems.indexOf(item);

            var isCancelled = item.status === "Cancelled" || item.status === "Đã hủy";
            var isProcessing = item.status === "Processing" || item.status === "Đang chế biến";
            var isFinished = item.status === "Finished" || item.status === "Served" || item.status === "Đã xong";

            var lineTotal = item.price * item.quantity;
            if (!isCancelled) {
                grandTotal += lineTotal;
                totalQty += item.quantity;
            }

            var statusHtml = '';
            var itemClass = '';
            if (item.isNew) statusHtml = '<span class="badge bg-success" style="font-size:10px;">Mới</span>';
            else if (item.isDirty) statusHtml = '<span class="badge bg-warning text-dark" style="font-size:10px;">Chưa lưu</span>';
            else if (isCancelled) {
                statusHtml = '<span class="badge-cancelled">ĐÃ HỦY</span>';
                itemClass = 'item-cancelled';
            }
            else if (isProcessing) statusHtml = '<span class="badge-processing">CHẾ BIẾN</span>';
            else if (isFinished) statusHtml = '<span class="badge-finished">ĐÃ XONG</span>';
            else statusHtml = '<span class="text-muted bg-light border px-2 rounded" style="font-size:10px;">' + item.status + '</span>';

            var priceStr = lineTotal.toLocaleString('vi-VN');
            var unitPriceStr = item.price.toLocaleString('vi-VN');

            // Buttons
            var actionBtns = '';
            if (!isCancelled && !isFinished) {
                var hasNote = item.note && item.note.trim().length > 0;
                var iconNoteClass = hasNote ? "text-warning" : "text-secondary";

                actionBtns += '<button class="btn-note btn btn-link ' + iconNoteClass + ' p-0 me-3" title="Ghi chú"><i class="fa-solid fa-pen-to-square"></i></button>';
                actionBtns += '<button class="btn-delete btn btn-link text-danger p-0" title="Xóa"><i class="fa-solid fa-trash"></i></button>';
            }

            // Note
            var noteHtml = '';
            if (!isCancelled) {
                var noteContent = item.note || '';
                if (isFinished) {
                    if (noteContent) {
                        noteHtml = '<div class="text-muted fst-italic small mt-1" style="font-size:11px;"><i class="fa-solid fa-note-sticky me-1"></i>' + noteContent + '</div>';
                    }
                } else {
                    var noteDisplay = noteContent.trim().length > 0 ? 'block' : 'none';
                    noteHtml = '<div class="note-box" data-index="' + realIndex + '" style="display:' + noteDisplay + ';margin-top:5px;">' +
                        '<textarea class="note-input form-control" rows="1" placeholder="Ghi chú cho bếp...">' + noteContent + '</textarea>' +
                        '</div>';
                }
            }

            // Controls
            var controlsHtml = '';
            if (!isCancelled) {
                if (isFinished) {
                    controlsHtml = '<div class="d-flex justify-content-between align-items-center mt-2">' +
                        '<div class="fw-bold text-success" style="font-size:13px;">SL: ' + item.quantity + '</div>' +
                        '<div class="text-muted small" style="font-size:10px;">' + unitPriceStr + ' x ' + item.quantity + ' = <strong>' + priceStr + '</strong></div>' +
                        '</div>';
                } else {
                    controlsHtml = '<div class="d-flex justify-content-between align-items-center mt-2">' +
                        '<div class="qty-control" data-index="' + realIndex + '">' +
                        '<button class="btn-minus btn btn-sm btn-outline-secondary"><i class="fa-solid fa-minus"></i></button>' +
                        '<span class="qty-val">' + item.quantity + '</span>' +
                        '<button class="btn-plus btn btn-sm btn-outline-secondary"><i class="fa-solid fa-plus"></i></button>' +
                        '</div>' +
                        '<div class="text-muted small" style="font-size:10px;">' + unitPriceStr + ' x ' + item.quantity + ' = <strong>' + priceStr + '</strong></div>' +
                        '</div>';
                }
            }

            var html = '<div class="cart-item ' + itemClass + '" data-index="' + realIndex + '">' +
                '<div class="d-flex justify-content-between align-items-start">' +
                '<div class="item-name" style="flex:1">' + item.name + '</div>' +
                '<div>' + statusHtml + '</div>' +
                '</div>' +
                '<div class="d-flex justify-content-end mt-1 mb-1">' + actionBtns + '</div>' +
                noteHtml +
                controlsHtml +
                '</div>';

            $container.append(html);
        });
    }

    // Footer update
    $('#lblTotalQty').text(totalQty);
    $('#lblSubTotal').text(grandTotal.toLocaleString('vi-VN') + ' đ');
    $('#lblGrandTotal').text(grandTotal.toLocaleString('vi-VN') + ' đ');

    var hasChanges = cartItems.some(x => x.isNew || x.isDirty || x.isDeleted);
    if (hasChanges) $('.btn-save-order').show();
    else $('.btn-save-order').hide();
}

// ================================
// Change Quantity
// ================================
function changeQty(index, change) {
    var item = cartItems[index];

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
    var $noteBox = $('.note-box[data-index="' + index + '"]');
    $noteBox.toggle();
    if ($noteBox.is(':visible')) $noteBox.find('textarea').focus();
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
    // Modals
    modalConfirm = new bootstrap.Modal($('#confirmAddModal'));
    modalDelete = new bootstrap.Modal($('#confirmDeleteModal'));
    modalWarning = new bootstrap.Modal($('#modifyWarningModal'));

    renderCart();

    // ================================
    // Event delegation for cart items
    // ================================
    $(document).on('click', '.btn-plus', function () {
        var index = $(this).closest('.cart-item').data('index');
        changeQty(index, 1);
    });

    $(document).on('click', '.btn-minus', function () {
        var index = $(this).closest('.cart-item').data('index');
        changeQty(index, -1);
    });

    $(document).on('click', '.btn-note', function () {
        var index = $(this).closest('.cart-item').data('index');
        toggleNote(index);
    });

    $(document).on('input', '.note-input', function () {
        var index = $(this).closest('.note-box').data('index');
        updateNote(index, $(this).val());
    });

    $(document).on('click', '.btn-delete', function () {
        var index = $(this).closest('.cart-item').data('index');
        removeItem(index);
    });

    // Modal confirm modify
    $('#btnConfirmModify').click(function () {
        if (!pendingModification) return;

        if (pendingModification.type === 'qty') {
            executeChangeQty(pendingModification.index, pendingModification.change);
        } else if (pendingModification.type === 'delete') {
            executeRemoveItem(pendingModification.index);
        }

        pendingModification = null;
        modalWarning.hide();
    });

    // Modal confirm delete
    $('#btnConfirmDelete').click(function () {
        if (typeof tempDeleteIndex !== "undefined" && tempDeleteIndex !== null) {
            executeRemoveItem(tempDeleteIndex);
            tempDeleteIndex = null;
            modalDelete.hide();
        }
    });
    // ----------------------------
    // Toast function
    // ----------------------------
    function showToast(message, type = 'success') {
        $('#liveToastContainer').remove();
        $("body").append(`
        <div id="liveToastContainer" style="position: fixed; top: 20px; right: 20px; z-index: 1055;">
            <div class="toast bg-${type} text-white" role="alert">
                <div class="toast-body">${message}</div>
            </div>
        </div>
    `);

        const toastElement = $("#liveToastContainer .toast");
        toastElement.addClass("show");

        setTimeout(() => {
            toastElement.removeClass("show");
        }, 3000);
    }

    // ----------------------------
    // Add new item events
    // ----------------------------
    $(document).on('click', '.btn-select-item', function () {
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

        var existing = cartItems.find(x => x.itemId === tempItemToAdd.itemId && x.isCombo === tempItemToAdd.isCombo && x.isNew && !x.isDeleted);
        if (existing) existing.quantity++;
        else cartItems.push(tempItemToAdd);

        renderCart();
        modalConfirm.hide();
        showToast(`Thêm món ${tempItemToAdd.name} thành công!`, "success");
        tempItemToAdd = null;
    });

    // Save order
    $('.btn-save-order').click(function () {
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
        function showToast2(message, type = "success") {

            $("#liveToastContainer").remove();

            $("body").append(`
        <div id="liveToastContainer"
            style="position:fixed; top:20px; right:20px; z-index:2000;">
            <div class="toast align-items-center text-white bg-${type} border-0 show">
                <div class="d-flex">
                    <div class="toast-body">
                        ${message}
                    </div>
                </div>
            </div>
        </div>
    `);

            setTimeout(() => $("#liveToastContainer").remove(), 3000);
        }

    });
});
