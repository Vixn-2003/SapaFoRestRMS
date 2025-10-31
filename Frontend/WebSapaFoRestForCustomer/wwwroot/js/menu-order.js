// Đảm bảo biến global 'initialOrderedItems' tồn tại trước khi chạy
if (typeof initialOrderedItems === 'undefined') {
    console.error("Lỗi nghiêm trọng: Biến initialOrderedItems chưa được định nghĩa. Hãy đảm bảo nó được khai báo trước script này.");
    initialOrderedItems = []; // Khởi tạo mảng rỗng để tránh lỗi thêm
}

$(document).ready(function () {

    // === 1. KHAI BÁO BIẾN ===
    let cart = [];
    const tableId = $("#tableId").val();
    const apiBaseUrl = $("#apiBaseUrl").val();

    // Kiểm tra các biến quan trọng
    if (!tableId || !apiBaseUrl) {
        console.error("Lỗi nghiêm trọng: Thiếu tableId hoặc apiBaseUrl trong HTML.");
        // Có thể hiển thị thông báo lỗi cho người dùng ở đây
        // return; // Dừng thực thi nếu thiếu
    }

    const $menuPage = $("#menu-page");
    const $cartPage = $("#cart-page");
    const $statusPage = $("#status-page");
    const $navMenu = $("#nav-menu");
    const $navCart = $("#nav-cart");
    const $navStatus = $("#nav-status");
    const $cartCountBadge = $("#cart-count-badge");
    const $statusCountBadge = $("#status-count-badge");
    // $cartItemsContainer và $cartTotal không còn cần thiết vì updateCartUI vẽ lại toàn bộ
    const orderedItemCount = Array.isArray(initialOrderedItems) ? initialOrderedItems.length : 0;

    // Biến Lọc/Search
    const $searchIconBtn = $("#search-icon-btn");
    const $searchBar = $("#search-bar");
    const $searchInput = $("#search-input");
    const $categoryTabs = $(".category-tab");
    const $menuListContainer = $("#menu-list-container");
    const $callStaffBtn = $("#call-staff-btn");

    // === 2. CHUYỂN TRANG ===
    function showMenuPage() {
        $menuPage.removeClass("page-hidden");
        $cartPage.addClass("page-hidden");
        $statusPage.addClass("page-hidden");
        $navMenu.addClass("active");
        $navCart.removeClass("active");
        $navStatus.removeClass("active");
    }
    function showCartPage() {
        $menuPage.addClass("page-hidden");
        $cartPage.removeClass("page-hidden");
        $statusPage.addClass("page-hidden");
        $navMenu.removeClass("active");
        $navCart.addClass("active");
        $navStatus.removeClass("active");
        updateCartUI(); // Vẽ lại giỏ hàng khi mở
    }
    function showStatusPage() {
        $menuPage.addClass("page-hidden");
        $cartPage.addClass("page-hidden");
        $statusPage.removeClass("page-hidden");
        $navMenu.removeClass("active");
        $navCart.removeClass("active");
        $navStatus.addClass("active");
    }

    $navMenu.on('click', function (e) { e.preventDefault(); showMenuPage(); });
    $navCart.on('click', function (e) { e.preventDefault(); showCartPage(); });
    $navStatus.on('click', function (e) { e.preventDefault(); showStatusPage(); });

    // (ĐÃ SỬA) Bắt sự kiện cho nút "Tiếp tục gọi món" (dùng event delegation)
    $(document).on('click', '#btn-back-to-menu', function (e) {
        e.preventDefault();
        showMenuPage();
    });


    // === 3. LOGIC GIỎ HÀNG ===
    function loadCart() {
        const storedCart = localStorage.getItem('cart_' + tableId);
        if (storedCart) {
            try {
                cart = JSON.parse(storedCart);
                if (!Array.isArray(cart)) {
                    console.warn("Dữ liệu giỏ hàng không hợp lệ. Đặt lại giỏ hàng.");
                    cart = [];
                }
            } catch (e) {
                console.error("Lỗi khi đọc giỏ hàng:", e);
                cart = [];
            }
        } else {
            cart = [];
        }
        updateCartBadge();
        updateStatusBadge();
    }
    //function saveCart() {
    //    try {
    //        localStorage.setItem('cart_' + tableId, JSON.stringify(cart));
    //        // Chỉ gọi updateCartUI nếu trang giỏ hàng đang hiển thị
    //        if (!$cartPage.hasClass('page-hidden')) {
    //            updateCartUI();
    //        }
    //        updateCartBadge(); // Luôn cập nhật badge
    //    } catch (e) {
    //        console.error("Lỗi khi lưu giỏ hàng:", e);
    //        alert("Đã xảy ra lỗi khi lưu giỏ hàng.");
    //    }
    //}

    // === Hàm lưu giỏ hàng ===
    function saveCart() {
        try {
            localStorage.setItem('cart_' + tableId, JSON.stringify(cart));
            if (!$cartPage.hasClass('page-hidden')) {
                updateCartUI();
            }
            updateCartBadge();
        } catch (e) {
            console.error("Lỗi khi lưu giỏ hàng:", e);
            showMobileToast("Đã xảy ra lỗi khi lưu giỏ hàng.", "error"); // Thay alert
        }
    }
    function updateCartBadge() {
        let totalCount = 0;
        if (Array.isArray(cart)) {
            for (const item of cart) {
                if (item && typeof item.quantity === 'number' && item.quantity > 0) {
                    totalCount += item.quantity;
                }
            }
        }
        const $badge = $("#cart-count-badge"); // Chọn lại badge trong hàm
        if (totalCount > 0) {
            $badge.text(totalCount).removeClass('page-hidden');
        } else {
            $badge.addClass('page-hidden').text('0'); // Đặt về 0 khi ẩn
        }
    }
    function updateStatusBadge() {
        const count = typeof orderedItemCount === 'number' ? orderedItemCount : 0;
        const $badge = $("#status-count-badge"); // Chọn lại badge
        if (count > 0) {
            $badge.text(count).removeClass('page-hidden');
        } else {
            $badge.addClass('page-hidden').text('0');
        }
    }

    // (ĐÃ SỬA) Hàm vẽ lại giỏ hàng (phiên bản mới nhất theo mockup)
    function updateCartUI() {
        if (!$cartPage || $cartPage.hasClass('page-hidden') || !Array.isArray(cart)) return;

        $cartPage.empty(); // Xóa toàn bộ nội dung

        let totalPrice = 0;
        let cartHtml = '<h3 class="category-title" style="margin-top:0;margin-bottom:20px;">Giỏ hàng của bạn</h3>';

        if (cart.length === 0) {
            cartHtml += '<p class="text-center text-muted mt-4">Giỏ hàng của bạn đang trống.</p>';
            // Footer khi giỏ hàng rỗng
            cartHtml += `
        <div class="cart-footer mt-4" style="background:#fff;padding:15px;border-radius:8px;box-shadow:0 1px 3px rgba(0,0,0,0.1);">
            <div class="d-flex justify-content-between mb-3 align-items-center">
                <h6 class="mb-0 text-muted">Tổng tiền:</h6>
                <h6 class="mb-0 text-danger fw-bold">0đ</h6>
            </div>
            <div class="d-flex gap-2">
                <button id="btn-back-to-menu" class="btn btn-secondary flex-fill fw-bold">Tiếp tục chọn món</button>
                <button id="btn-submit-order" class="btn flex-fill fw-bold disabled" disabled style="background-color: var(--brand-gold); color:white;">Xác nhận gọi món</button>
            </div>
        </div>`;
            $cartPage.html(cartHtml);
            return;
        }

        // Vẽ các món
        for (const item of cart) {
            if (!item || typeof item.id === 'undefined' || typeof item.price !== 'number' || typeof item.quantity !== 'number' || item.quantity <= 0) continue;

            totalPrice += item.price * item.quantity;
            const notes = item.notes || '';
            const imageUrl = item.imageUrl || 'https://via.placeholder.com/100';
            const itemName = item.name || 'Chưa có tên';
            const itemPriceDisplay = item.price.toLocaleString('vi-VN');

            cartHtml += `
        <div class="cart-item mb-3 p-3" data-item-id="${item.id}" style="background:#fff;border-radius:8px;box-shadow:0 1px 3px rgba(0,0,0,0.1);">
            <div class="row g-3 align-items-start">
                <div class="col-auto">
                    <img src="${imageUrl}" alt="${itemName}" style="width:60px;height:60px;border-radius:6px;object-fit:cover;">
                </div>
                <div class="col">
                    <h6 class="fw-bold mb-1" style="font-size:0.95rem;">${itemName}</h6>
                    <div class="d-flex align-items-center mb-2">
                        <div class="qty-selector">
                            <button class="btn btn-outline-secondary btn-sm btn-cart-qty-minus py-0 px-2" data-item-id="${item.id}">-</button>
                            <span class="mx-2 fw-bold" style="min-width:20px;text-align:center;">${item.quantity}</span>
                            <button class="btn btn-outline-secondary btn-sm btn-cart-qty-plus py-0 px-2" data-item-id="${item.id}">+</button>
                        </div>
                    </div>
                    <div class="cart-item-notes">
                        <input type="text" class="form-control form-control-sm cart-item-notes-input" data-item-id="${item.id}" value="${notes}" placeholder="Ghi chú...">
                    </div>
                </div>
                <div class="col-auto text-end d-flex flex-column align-items-end">
                    <p class="fw-bold mb-1" style="color:var(--brand-green);font-size:0.9rem;">${itemPriceDisplay}đ</p>
                    <a href="#" class="btn-cart-remove small mt-auto" data-item-id="${item.id}" style="color:#6c757d;text-decoration:none;">Xóa</a>
                </div>
            </div>
        </div>`;
        }

        // Footer khi có món
        cartHtml += `
    <div class="cart-footer mt-4" style="background:#fff;padding:15px;border-radius:8px;box-shadow:0 1px 3px rgba(0,0,0,0.1);">
        <div class="d-flex justify-content-between mb-3 align-items-center">
            <h6 class="mb-0 text-muted">Tổng tiền:</h6>
            <h6 class="mb-0 text-danger fw-bold">${totalPrice.toLocaleString('vi-VN')}đ</h6>
        </div>
        <div class="d-flex gap-2">
            <button id="btn-back-to-menu" style="font-size: 0.7rem" class="btn btn-secondary flex-fill fw-bold">Tiếp tục gọi món</button>
            <button id="btn-submit-order" style="font-size: 0.7rem" class="btn flex-fill fw-bold" style="background-color: var(--brand-gold); color:white;">Xác nhận gọi món</button>
        </div>
    </div>`;

        $cartPage.html(cartHtml);
    }


    // === 4. SỰ KIỆN "GỌI MÓN" ===
    $(document).on('click', '.btn-add-to-cart', function () {
        const button = $(this);
        const itemId = button.data('item-id');
        const itemName = button.data('item-name');
        const itemPrice = parseFloat(button.data('item-price'));
        const imageUrl = button.data('item-image');

        if (typeof itemId === 'undefined' || !itemName || isNaN(itemPrice)) {
            console.error("Dữ liệu item không hợp lệ:", button.data());
            showMobileToast("Lỗi: Không thể thêm món này.", "error"); // Thay alert
            return;
        }

        const existingItem = cart.find(i => i.id === itemId);
        if (existingItem) {
            existingItem.quantity++;
        } else {
            cart.push({ id: itemId, name: itemName, price: itemPrice, quantity: 1, notes: "", imageUrl: imageUrl });
        }
        saveCart(); // Gọi saveCart để cập nhật

        button.prop('disabled', true).text('Đã thêm');
        setTimeout(function () {
            button.prop('disabled', false).text('Gọi món');
        }, 1000);
    });


    // === 5. LOGIC LỌC/SEARCH AJAX ===

    // === THAY THẾ TOÀN BỘ HÀM NÀY ===
    function renderMenu(menuItems) {
        $menuListContainer.empty();
        if (!Array.isArray(menuItems) || menuItems.length === 0) {
            $menuListContainer.html('<p class="text-center text-muted mt-4">Không tìm thấy món ăn nào.</p>');
            return;
        }

        const categories = {};
        menuItems.forEach(item => {
            if (!item || typeof item.menuItemId === 'undefined') return;
            const categoryName = item.categoryName || "Khác";
            if (!categories[categoryName]) { categories[categoryName] = []; }
            categories[categoryName].push(item);
        });

        const sortedCategoryNames = Object.keys(categories).sort();

        // (MỚI) Đặt giới hạn hiển thị ra 5 món
        const initialShowCount = 5;

        sortedCategoryNames.forEach(categoryName => {
            let categoryTitleHtml = `<h3 class="category-title">${categoryName}</h3>`;
            // (MỚI) Thêm data-show-count
            let categoryListHtml = `<div class="menu-item-list mt-3" data-show-count="${initialShowCount}">`;

            const itemsInCategory = categories[categoryName];

            itemsInCategory.forEach((item, index) => { // (MỚI) Lấy index
                const menuItemId = item.menuItemId;
                const itemName = item.name || 'Chưa có tên';
                const itemPrice = typeof item.price === 'number' ? item.price : 0;
                const imageUrl = item.imageUrl || 'https://via.placeholder.com/100';

                // Lấy lại thông tin "Đã gọi" (như cũ)
                let orderedQty = 0, processingQty = 0;
                if (Array.isArray(initialOrderedItems)) {
                    initialOrderedItems.forEach(orderedItem => {
                        if (orderedItem && orderedItem.menuItemId === menuItemId) {
                            const qty = typeof orderedItem.quantity === 'number' ? orderedItem.quantity : (typeof orderedItem.Quantity === 'number' ? orderedItem.Quantity : 0);
                            orderedQty += qty;
                            if (orderedItem.status === "Đang chế biến") { processingQty += qty; }
                        }
                    });
                }
                let detailsHtml = '';
                if (orderedQty > 0) { detailsHtml += `<span class="item-ordered text-success fw-bold">Đã gọi: ${orderedQty}</span>`; }
                //if (processingQty > 0) { detailsHtml += `<span class="status-processing-text">Đang chế biến: ${processingQty}</span>`; }

                // (MỚI) Thêm class và style nếu item vượt quá giới hạn
                const isHiddenClass = (index >= initialShowCount) ? "menu-item-hidden" : "";
                const style = (index >= initialShowCount) ? "display: none;" : "";

                categoryListHtml += `
                            <div class="menu-item-card ${isHiddenClass}" style="${style}">
                                <img src="${imageUrl}" alt="${itemName}" />
                                <div class="details">
                                    <h5>${itemName}</h5>
                                    <p>${itemPrice.toLocaleString('vi-VN')} VNĐ</p>
                                    ${detailsHtml}
                                </div>
                                <div class="actions">
                                    <a href="#" class="btn-details">Chi tiết</a>
                                    <button class="btn-order btn-add-to-cart"
                                            data-item-id="${menuItemId}"
                                            data-item-name="${itemName}"
                                            data-item-price="${itemPrice}"
                                            data-item-image="${imageUrl}">
                                        Gọi món
                                    </button>
                                </div>
                            </div>`;
            });

            // (MỚI) Thêm nút "Hiển thị thêm" nếu cần
            if (itemsInCategory.length > initialShowCount) {
                categoryListHtml += '<a href="#" class="btn-show-more text-center d-block mt-2 text-decoration-none fw-bold" style="color: var(--brand-gold);">Hiển thị thêm...</a>';
            }

            categoryListHtml += '</div>'; // Đóng .menu-item-list

            // Thêm cả tiêu đề và danh sách vào container
            $menuListContainer.append(categoryTitleHtml + categoryListHtml);
        });
    }

    function performFilter() {
        const searchString = $searchInput.val();
        const categoryId = $categoryTabs.filter('.active').data('id');

        let url = `${apiBaseUrl}/api/OrderTable/MenuOrder/${tableId}?`;
        if (categoryId) { url += `categoryId=${categoryId}&`; }
        if (searchString) { url += `searchString=${encodeURIComponent(searchString)}`; }

        $menuListContainer.html('<p class="text-center text-muted mt-4">Đang tải...</p>');
        $.ajax({
            url: url,
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response && Array.isArray(response.menuItems)) {
                    renderMenu(response.menuItems);
                } else {
                    console.error("API response không hợp lệ:", response);
                    $menuListContainer.html('<p class="text-center text-danger mt-4">Lỗi: Dữ liệu menu không đúng.</p>');
                }
            },
            error: function (xhr, status, error) {
                console.error("Lỗi AJAX:", status, error, xhr.responseText);
                let errorMsg = "Lỗi khi tải menu.";
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMsg = xhr.responseJSON.message;
                } else if (xhr.responseText) {
                    try { const err = JSON.parse(xhr.responseText); if (err.message) errorMsg = err.message; } catch (e) { }
                }
                $menuListContainer.html(`<p class="text-center text-danger mt-4">${errorMsg}</p>`);
            }
        });
    }

    // Sự kiện Lọc/Search
    $searchIconBtn.on('click', function () {
        $searchBar.toggleClass('page-hidden');
        if (!$searchBar.hasClass('page-hidden')) {
            $searchInput.focus();
        }
    });
    let searchTimeout;
    $searchInput.on('keyup', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(performFilter, 500);
    });
    $categoryTabs.on('click', function (e) {
        e.preventDefault();
        if ($(this).hasClass('active')) return;
        $categoryTabs.removeClass('active');
        $(this).addClass('active');
        performFilter();
    });


    // === 6. CÁC SỰ KIỆN CŨ (Đã sửa nút Xóa) ===
    $(document).on('click', '.btn-cart-qty-plus', function () {
        const itemId = $(this).data('item-id');
        if (typeof itemId === 'undefined') return;
        const item = cart.find(i => i.id === itemId);
        if (item) {
            item.quantity++;
            saveCart(); // Gọi saveCart
        }
    });
   
    $(document).on('click', '.btn-cart-qty-minus', function () {
        const itemId = $(this).data('item-id');
        if (typeof itemId === 'undefined') return;
        const itemIndex = cart.findIndex(i => i.id === itemId);
        if (itemIndex > -1) {
            if (cart[itemIndex].quantity > 1) {
                cart[itemIndex].quantity--;
                saveCart(); // Gọi saveCart
            } else {
                showMobileConfirm('Xóa món này khỏi giỏ hàng?', function () {
                    cart.splice(itemIndex, 1);
                    saveCart(); // Gọi saveCart
                });
            }
        }
    });
    $(document).on('click', '.btn-cart-remove', function (e) {
        e.preventDefault();
        const itemId = $(this).data('item-id');
        if (typeof itemId === 'undefined') return;
        showMobileConfirm('Xóa món này khỏi giỏ hàng?', function () {
            const itemIndex = cart.findIndex(i => i.id === itemId);
            if (itemIndex > -1) {
                cart.splice(itemIndex, 1);
                saveCart(); // Gọi saveCart
            }
        });
    });

    //Xác nhận gọi mon
    $(document).on('click', '#btn-submit-order', function () {
        if (!Array.isArray(cart) || cart.length === 0) {
            showMobileToast("Giỏ hàng của bạn đang trống!", "error");
            return;
        }

        // Hiển thị xác nhận trước khi submit
        showMobileConfirm("Bạn có chắc chắn muốn gọi món này không?", function () {
            // Callback khi người dùng bấm "Đồng ý"
            const btn = $('#btn-submit-order');
            btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang gửi...');

            const orderData = {
                tableId: parseInt(tableId),
                items: cart.filter(item => item && typeof item.id !== 'undefined' && typeof item.quantity === 'number' && item.quantity > 0)
                    .map(item => ({
                        menuItemId: item.id,
                        quantity: item.quantity,
                        notes: item.notes || ""
                    }))
            };

            if (orderData.items.length === 0) {
                showMobileToast("Giỏ hàng không có món hợp lệ để gửi.", "error");
                btn.prop('disabled', false).html('<i class="fas fa-check me-2"></i> Xác nhận gọi món');
                return;
            }

            $.ajax({
                url: apiBaseUrl + '/api/OrderTable/SubmitOrder',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(orderData),
                success: function (response) {
                    showMobileToast('Đã gửi gọi món thành công!', 'success');
                    cart = [];
                    localStorage.removeItem('cart_' + tableId);
                    setTimeout(() => location.reload(), 1000);
                },
                error: function (xhr, status, error) {
                    console.error("Lỗi gửi Order:", status, error, xhr.responseText);
                    let errorMsg = 'Không thể gửi order.';
                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        errorMsg = xhr.responseJSON.message;
                    } else if (xhr.responseText) {
                        try { const err = JSON.parse(xhr.responseText); if (err.message) errorMsg = err.message; } catch (e) { }
                    }
                    showMobileToast('Lỗi! ' + errorMsg, 'error');
                    btn.prop('disabled', false).html('<i class="fas fa-check me-2"></i> Xác nhận gọi món');
                }
            });
        });
    });

    function showMobileConfirm(message, onConfirm) {
        // Xóa confirm cũ nếu có
        $('.mobile-toast.confirm-toast').remove();

        const toast = $(`
         <div class="mobile-toast confirm-toast" style="
    opacity:0;
    position:fixed;
    top:50%;
    left:50%;
    transform:translate(-50%, -50%);
    z-index:2000;
    background:#444;
    color:#fff;
    padding:15px;
    border-radius:10px;
    text-align:center;
    width:90%;
    max-width:400px;
    box-sizing:border-box;
">
    <p style="margin-bottom:15px;">${message}</p>
    <div style="display:flex; justify-content:center; gap:10px;">
        <button class="btn btn-sm btn-success">Đồng ý</button>
        <button class="btn btn-sm btn-secondary">Hủy</button>
    </div>
</div>
    `);

        $('body').append(toast);
        toast.animate({ opacity: 1 }, 200);

        toast.find('.btn-success').on('click', function () {
            if (typeof onConfirm === 'function') onConfirm();
            toast.animate({ opacity: 0 }, 200, () => toast.remove());
        });

        toast.find('.btn-secondary').on('click', function () {
            toast.animate({ opacity: 0 }, 200, () => toast.remove());
        });
    }



    // Hàm hiển thị toast thông báo
    function showMobileToast(message, type = 'info', duration = 3000) {
        const $toast = $(`
        <div class="mobile-toast toast-${type}">
            ${message}
        </div>
    `).appendTo('body');

        // Thêm animation vào
        $toast.css({ opacity: 0, position: 'fixed', bottom: '80px', left: '50%', transform: 'translateX(-50%)', zIndex: 2000, padding: '10px 20px', borderRadius: '8px', color: '#fff', backgroundColor: type === 'success' ? '#28a745' : type === 'error' ? '#dc3545' : '#333', fontSize: '0.9rem' }).animate({ opacity: 1 }, 300);

        setTimeout(() => {
            $toast.animate({ opacity: 0 }, 300, function () {
                $toast.remove();
            });
        }, duration);
    }

    // Hàm hiển thị toast xác nhận
    function showConfirmToast(message, onConfirm) {
        const $toast = $(`
     <div class="mobile-toast confirm-toast" style="
    opacity:0;
    position:fixed;
    top:50%;
    left:50%;
    transform:translate(-50%, -50%);
    z-index:2000;
    background:#444;
    color:#fff;
    padding:15px;
    border-radius:10px;
    text-align:center;
    width:90%;        
    max-width:400px;  
    box-sizing:border-box; 
">
    <p style="margin-bottom:15px;">${message}</p>
    <div style="display:flex; justify-content:center; gap:10px;">
        <button class="btn btn-sm btn-success">Đồng ý</button>
        <button class="btn btn-sm btn-secondary">Hủy</button>
    </div>
</div>


    `).appendTo('body');

        $toast.animate({ opacity: 1 }, 300);

        $toast.find('.btn-success').on('click', function () {
            onConfirm();
            $toast.remove();
        });

        $toast.find('.btn-secondary').on('click', function () {
            $toast.remove();
        });
    }

    // Xử lý nút Hủy món
    $(document).on('click', '.btn-cancel-item', function () {
        const button = $(this);
        const orderDetailId = button.data('item-id');

        if (typeof orderDetailId === 'undefined' || orderDetailId === null) {
            console.error("Không tìm thấy data-item-id trên nút Hủy.");
            showMobileToast("Lỗi: Không thể xác định món cần hủy.", 'error');
            return;
        }

        // Hiển thị toast xác nhận thay cho confirm
        showConfirmToast('Bạn có chắc muốn hủy món này không?', function () {
            // Vô hiệu hóa nút và hiển thị loading
            button.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');

            // Gọi API hủy món
            $.ajax({
                url: apiBaseUrl + '/api/OrderTable/CancelItem/' + orderDetailId,
                type: 'POST',
                success: function (response) {
                    showMobileToast('Đã hủy món thành công!', 'success');

                    button.closest('.order-status-item').fadeOut(500, function () {
                        const $this = $(this);
                        const detailId = parseInt($this.data('orderdetailid'));
                        $this.remove();

                        initialOrderedItems = initialOrderedItems.filter(i => i && i.orderDetailId !== detailId);
                        const newCount = initialOrderedItems.length;

                        const $badge = $("#status-count-badge");
                        if (newCount > 0) {
                            $badge.text(newCount).removeClass('page-hidden');
                        } else {
                            $badge.addClass('page-hidden').text('0');
                            if ($("#status-page .order-status-item").length === 0) {
                                $("#status-page .order-status-container").html('<p class="text-center text-muted mt-4">Bạn chưa gọi món nào.</p>');
                            }
                        }
                    });
                },
                error: function (xhr, status, error) {
                    console.error("Lỗi hủy món:", status, error, xhr.responseText);

                    let errorMsg = 'Không thể hủy món.';
                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        errorMsg = xhr.responseJSON.message;
                    } else if (xhr.responseText) {
                        try {
                            const err = JSON.parse(xhr.responseText);
                            if (err.message) errorMsg = err.message;
                        } catch (e) { }
                    }

                    showMobileToast(errorMsg, 'error');
                    button.prop('disabled', false).text('Hủy');
                }
            });
        });
    });

    
    // === (MỚI) THÊM SỰ KIỆN CLICK CHO NÚT "HIỂN THỊ THÊM" ===
    $(document).on('click', '.btn-show-more', function (e) {
        e.preventDefault();
        const $button = $(this);
        const $list = $button.closest('.menu-item-list');

        // Tìm tất cả các item đang bị ẩn (class .menu-item-hidden)
        const $hiddenItems = $list.find('.menu-item-hidden');

        if ($button.hasClass('expanded')) {
            // --- ĐANG Ở TRẠNG THÁI "ẨN BỚT" ---
            $hiddenItems.slideUp(); // Ẩn đi
            $button.text('Hiển thị thêm...').removeClass('expanded');
        } else {
            // --- ĐANG Ở TRẠNG THÁI "HIỂN THỊ THÊM" ---
            $hiddenItems.slideDown(); // Hiển thị ra
            $button.text('Ẩn bớt').addClass('expanded');
        }
    });

    // === (MỚI) THAY THẾ SỰ KIỆN GỌI NHÂN VIÊN ===
    function showMobileToast(message, type = 'success') {
        const toastContainer = document.getElementById('mobileToast');
        const toastMessage = document.getElementById('mobileToastMessage');

        // Gán nội dung
        toastMessage.innerText = message;

        // Màu nền theo loại thông báo
        if (type === 'success') {
            toastMessage.style.backgroundColor = '#28a745'; // xanh
        } else if (type === 'error') {
            toastMessage.style.backgroundColor = '#dc3545'; // đỏ
        } else if (type === 'warning') {
            toastMessage.style.backgroundColor = '#ffc107'; // vàng
            toastMessage.style.color = '#333';
        }

        // Hiển thị toast
        toastContainer.style.display = 'block';
        toastContainer.style.opacity = 0;
        let opacity = 0;
        const fadeIn = setInterval(() => {
            if (opacity < 1) {
                opacity += 0.1;
                toastContainer.style.opacity = opacity;
            } else {
                clearInterval(fadeIn);
            }
        }, 20);

        // Tự ẩn sau 3 giây
        setTimeout(() => {
            let fadeOutOpacity = 1;
            const fadeOut = setInterval(() => {
                if (fadeOutOpacity > 0) {
                    fadeOutOpacity -= 0.1;
                    toastContainer.style.opacity = fadeOutOpacity;
                } else {
                    clearInterval(fadeOut);
                    toastContainer.style.display = 'none';
                }
            }, 20);
        }, 3000);
    }

    $(document).ready(function () {
        // Mở modal
        $('#call-staff-btn').on('click', function () {
            var modal = new bootstrap.Modal(document.getElementById('callStaffModal'));
            modal.show();
            $(this).addClass('ringing');
        });

        // Gửi yêu cầu
        $('#sendStaffRequest').on('click', function () {
            const note = $('#staffNote').val();
            const btn = $(this);
            btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm"></span> Đang gửi...');

            const requestData = {
                tableId: parseInt(tableId), // chắc chắn tableId có giá trị
                note: note
            };
           
            $.ajax({
                url: apiBaseUrl + '/api/OrderTable/RequestAssistance',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(requestData),
                success: function (response) {
                    showMobileToast(response.message || 'Đã gửi yêu cầu nhân viên thành công!', 'success');
                    btn.prop('disabled', false).html('<i class="fas fa-paper-plane"></i> Gửi yêu cầu');
                    bootstrap.Modal.getInstance(document.getElementById('callStaffModal')).hide();
                    $('#staffNote').val('');
                    $('#call-staff-btn').removeClass('ringing');
                },
                error: function (xhr) {
                    let errorMsg = xhr.responseJSON ? xhr.responseJSON.message : "Gửi yêu cầu thất bại.";
                    showMobileToast(errorMsg, 'error');
                    btn.prop('disabled', false).html('<i class="fas fa-paper-plane"></i> Gửi yêu cầu');
                    $('#call-staff-btn').removeClass('ringing');
                }
            });

        });
    });



    // === 7. CHẠY LẦN ĐẦU ===
    loadCart(); // Tải giỏ hàng từ localStorage

}); // End of $(document).ready