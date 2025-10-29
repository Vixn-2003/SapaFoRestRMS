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
    function saveCart() {
        try {
            localStorage.setItem('cart_' + tableId, JSON.stringify(cart));
            // Chỉ gọi updateCartUI nếu trang giỏ hàng đang hiển thị
            if (!$cartPage.hasClass('page-hidden')) {
                updateCartUI();
            }
            updateCartBadge(); // Luôn cập nhật badge
        } catch (e) {
            console.error("Lỗi khi lưu giỏ hàng:", e);
            alert("Đã xảy ra lỗi khi lưu giỏ hàng.");
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
        if ($cartPage.hasClass('page-hidden') || !Array.isArray(cart)) return;

        $cartPage.empty(); // Xóa sạch trang giỏ hàng

        let totalPrice = 0;
        let cartHtml = '<h3 class="category-title" style="margin-top: 0; margin-bottom: 20px;">Giỏ hàng của bạn</h3>';

        if (cart.length === 0) {
            cartHtml += '<p class="text-center text-muted mt-4">Giỏ hàng của bạn đang trống.</p>';
            // Footer khi giỏ hàng rỗng
            cartHtml += `
                <div class="cart-footer mt-4" style="background: #fff; padding: 15px; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
                    <div class="d-flex justify-content-between mb-3 align-items-center">
                        <h6 class="mb-0 text-muted">Tổng tiền:</h6>
                        <h6 class="mb-0 text-danger fw-bold">0đ</h6>
                    </div>
                    <div class="row g-2">
                         <div class="col">
                            <button id="btn-back-to-menu" class="btn btn-secondary w-100 fw-bold">Tiếp tục chọn món</button>
                         </div>
                         <div class="col">
                            <button id="btn-submit-order-disabled" class="btn w-100 fw-bold disabled" disabled style="background-color: var(--brand-gold); color: white;">Xác nhận gọi món</button>
                         </div>
                    </div>
                </div>`;
            $cartPage.html(cartHtml);
            return;
        }

        // Vẽ các món trong giỏ
        for (const item of cart) {
            if (!item || typeof item.id === 'undefined' || typeof item.price !== 'number' || typeof item.quantity !== 'number' || item.quantity <= 0) {
                console.warn("Bỏ qua item giỏ hàng không hợp lệ:", item);
                continue;
            }

            totalPrice += (item.price * item.quantity);
            const notes = item.notes || '';
            const imageUrl = item.imageUrl || 'https://via.placeholder.com/100';
            const itemName = item.name || 'Chưa có tên';
            const itemPriceDisplay = item.price.toLocaleString('vi-VN');

            // HTML cho từng món
            cartHtml += `
                <div class="cart-item mb-3 p-3" data-item-id="${item.id}" style="background: #fff; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
                    <div class="row g-3 align-items-start">
                        <div class="col-auto">
                            <img src="${imageUrl}" alt="${itemName}" style="width: 60px; height: 60px; border-radius: 6px; object-fit: cover;">
                        </div>
                        <div class="col">
                            <h6 class="fw-bold mb-1" style="font-size: 0.95rem;">${itemName}</h6>
                            <div class="d-flex align-items-center mb-2">
                                <div class="qty-selector">
                                    <button class="btn btn-outline-secondary btn-sm btn-cart-qty-minus py-0 px-2" data-item-id="${item.id}">-</button>
                                    <span class="mx-2 fw-bold" style="min-width: 20px; text-align: center;">${item.quantity}</span>
                                    <button class="btn btn-outline-secondary btn-sm btn-cart-qty-plus py-0 px-2" data-item-id="${item.id}">+</button>
                                </div>
                            </div>
                            <div class="cart-item-notes">
                                <input type="text" class="form-control form-control-sm cart-item-notes-input" data-item-id="${item.id}" value="${notes}" placeholder="Ghi chú...">
                            </div>
                        </div>
                        <div class="col-auto text-end d-flex flex-column align-items-end">
                            <p class="fw-bold mb-1" style="color: var(--brand-green); font-size: 0.9rem;">${itemPriceDisplay}đ</p>
                            <a href="#" class="btn-cart-remove small mt-auto" data-item-id="${item.id}" style="color: #6c757d; text-decoration: none;">Xóa</a>
                        </div>
                    </div>
                </div>`;
        }

        // Footer khi có món
        cartHtml += `
            <div class="cart-footer mt-4" style="background: #fff; padding: 15px; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
                <div class="d-flex justify-content-between mb-3 align-items-center">
                    <h6 class="mb-0 text-muted">Tổng tiền:</h6>
                    <h6 class="mb-0 text-danger fw-bold">${totalPrice.toLocaleString('vi-VN')}đ</h6>
                </div>
                <div class="row g-2">
                     <div class="col">
                        <button id="btn-back-to-menu" class="btn btn-secondary w-100 fw-bold">Tiếp tục gọi món</button>
                     </div>
                     <div class="col">
                        <button id="btn-submit-order" class="btn w-100 fw-bold" style="background-color: var(--brand-gold); color: white;">Xác nhận gọi món</button>
                     </div>
                </div>
            </div>`;

        $cartPage.html(cartHtml); // Cập nhật lại toàn bộ trang
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
            alert("Lỗi: Không thể thêm món này.");
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

        sortedCategoryNames.forEach(categoryName => {
            let categoryHtml = `<h3 class="category-title">${categoryName}</h3>`;
            categoryHtml += '<div class="menu-item-list mt-3">';

            categories[categoryName].forEach(item => {
                const menuItemId = item.menuItemId;
                const itemName = item.name || 'Chưa có tên';
                const itemPrice = typeof item.price === 'number' ? item.price : 0;
                const imageUrl = item.imageUrl || 'https://via.placeholder.com/100';

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
                if (processingQty > 0) { detailsHtml += `<span class="status-processing-text">Đang chế biến: ${processingQty}</span>`; }

                categoryHtml += `
                    <div class="menu-item-card">
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
            categoryHtml += '</div>';
            $menuListContainer.append(categoryHtml);
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
                if (confirm('Xóa món này khỏi giỏ hàng?')) {
                    cart.splice(itemIndex, 1);
                    saveCart(); // Gọi saveCart
                }
            }
        }
    });
    $(document).on('click', '.btn-cart-remove', function (e) {
        e.preventDefault();
        const itemId = $(this).data('item-id');
        if (typeof itemId === 'undefined') return;
        if (confirm('Xóa món này khỏi giỏ hàng?')) {
            const itemIndex = cart.findIndex(i => i.id === itemId);
            if (itemIndex > -1) {
                cart.splice(itemIndex, 1);
                saveCart(); // Gọi saveCart
            }
        }
    });
    $(document).on('change', '.cart-item-notes-input', function () {
        const itemId = $(this).data('item-id');
        const notes = $(this).val();
        if (typeof itemId === 'undefined') return;
        const item = cart.find(i => i.id === itemId);
        if (item) {
            item.notes = notes;
            // Chỉ lưu, không cần vẽ lại UI ngay
            try { localStorage.setItem('cart_' + tableId, JSON.stringify(cart)); }
            catch (e) { console.error("Lỗi khi lưu ghi chú:", e); }
        }
    });
    $(document).on('click', '#btn-submit-order', function () { // Bắt cả nút submit khi giỏ hàng có đồ
        if (!Array.isArray(cart) || cart.length === 0) {
            alert("Giỏ hàng của bạn đang trống!");
            return;
        }
        const btn = $(this);
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
            alert("Giỏ hàng không có món hợp lệ để gửi.");
            btn.prop('disabled', false).html('<i class="fas fa-check me-2"></i> Xác nhận gọi món');
            return;
        }

        $.ajax({
            url: apiBaseUrl + '/api/OrderTable/SubmitOrder',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(orderData),
            success: function (response) {
                alert('Đã gửi order vào bếp thành công!');
                cart = []; // Xóa cart trong JS
                localStorage.removeItem('cart_' + tableId); // Xóa cart trong localStorage
                location.reload(); // Tải lại trang để cập nhật "Món đã gọi"
            },
            error: function (xhr, status, error) {
                console.error("Lỗi gửi Order:", status, error, xhr.responseText);
                let errorMsg = 'Không thể gửi order.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMsg = xhr.responseJSON.message;
                } else if (xhr.responseText) {
                    try { const err = JSON.parse(xhr.responseText); if (err.message) errorMsg = err.message; } catch (e) { }
                }
                alert('Lỗi! ' + errorMsg);
                // Reset lại nút Submit
                btn.prop('disabled', false);
                // Cần xác định lại text ban đầu của nút submit nếu muốn reset text
                $('#btn-submit-order').html('<i class="fas fa-check me-2"></i> Xác nhận gọi món');
                $('#btn-submit-order-disabled').addClass('disabled').prop('disabled', true).html('Xác nhận gọi món'); // Reset cả nút disabled nếu có

            }
        });
    });
    $(document).on('click', '.btn-cancel-item', function () {
        // Hiển thị hộp thoại xác nhận
        if (!confirm('Bạn có chắc muốn hủy món này không?')) {
            return; // Nếu người dùng chọn "Cancel", dừng lại
        }

        const button = $(this);
        // Lấy OrderDetailId từ data attribute của nút
        const orderDetailId = button.data('item-id');

        // Kiểm tra xem có lấy được ID không
        if (typeof orderDetailId === 'undefined' || orderDetailId === null) {
            console.error("Không tìm thấy data-item-id trên nút Hủy.");
            alert("Lỗi: Không thể xác định món cần hủy.");
            return;
        }

        // Vô hiệu hóa nút và hiển thị loading
        button.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>'); // Loading indicator

        // Gọi API để hủy
        $.ajax({
            url: apiBaseUrl + '/api/OrderTable/CancelItem/' + orderDetailId,
            type: 'POST', // Hoặc DELETE nếu API của bạn dùng DELETE
            success: function (response) {
                alert('Đã hủy món thành công!');
                // Xóa dòng món ăn khỏi giao diện
                button.closest('.order-status-item').fadeOut(500, function () {
                    $(this).remove(); // Xóa element khỏi DOM

                    // Cập nhật lại biến global chứa danh sách món đã gọi
                    const detailId = parseInt($(this).data('orderdetailid')); // Lấy ID trước khi xóa hẳn
                    initialOrderedItems = initialOrderedItems.filter(i => i && i.orderDetailId !== detailId);
                    const newCount = initialOrderedItems.length; // Đếm lại số lượng

                    // Cập nhật badge "Món đã gọi"
                    const $badge = $("#status-count-badge");
                    if (newCount > 0) {
                        $badge.text(newCount);
                    } else {
                        $badge.addClass('page-hidden').text('0');
                        // Nếu hết món, hiển thị thông báo
                        if ($("#status-page .order-status-item").length === 0) {
                            // Tìm container đúng để chèn thông báo
                            $("#status-page .order-status-container").html('<p class="text-center text-muted mt-4">Bạn chưa gọi món nào.</p>');
                        }
                    }
                });
            },
            error: function (xhr, status, error) {
                console.error("Lỗi hủy món:", status, error, xhr.responseText);
                let errorMsg = 'Không thể hủy món.';
                // Cố gắng đọc lỗi chi tiết từ API
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMsg = xhr.responseJSON.message;
                } else if (xhr.responseText) {
                    try { const err = JSON.parse(xhr.responseText); if (err.message) errorMsg = err.message; } catch (e) { }
                }
                alert('Lỗi! ' + errorMsg);
                // Kích hoạt lại nút Hủy nếu có lỗi
                button.prop('disabled', false).text('Hủy');
            }
        });
    });

    // === 7. CHẠY LẦN ĐẦU ===
    loadCart(); // Tải giỏ hàng từ localStorage

}); // End of $(document).ready