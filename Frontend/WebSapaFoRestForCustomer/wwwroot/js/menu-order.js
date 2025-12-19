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
    const hubBaseUrl = apiBaseUrl.replace(/\/api$/, "") + "/tableHub";

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

    

    //connection.on("ReceiveOrderStatusUpdate", (orderDetailId, newStatus) => {
    //    const statusElement = document.getElementById(`status-text-${orderDetailId}`);
    //    if (statusElement) {
    //        statusElement.innerText = newStatus;
    //        statusElement.className = 'badge'; // Reset class

    //        if (newStatus === 'Cooking' || newStatus === 'Đang nấu') {
    //            statusElement.classList.add('bg-warning');
    //        } else if (newStatus === 'Ready' || newStatus === 'Đã xong') {
    //            statusElement.classList.add('bg-primary');
    //        } else if (newStatus === 'Served' || newStatus === 'Đã phục vụ') {
    //            statusElement.classList.add('bg-success');
    //        } else if (newStatus === 'Cancelled' || newStatus === 'Đã hủy') {
    //            statusElement.classList.add('bg-danger');
    //        }
    //    }
    //});

    //// Bắt đầu kết nối
    //connection.start()
    //    .then(() => console.log("SignalR connected"))
    //    .catch(err => console.error("SignalR không kết nối được:", err));









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
    // (ĐÃ SỬA HOÀN CHỈNH) Hàm vẽ lại giỏ hàng
    function updateCartUI() {
        if (!$cartPage || $cartPage.hasClass('page-hidden') || !Array.isArray(cart)) return;

        $cartPage.empty(); // Xóa toàn bộ nội dung

        let totalPrice = 0;
        let cartHtml = '<h3 class="category-title" style="margin-top:0;margin-bottom:20px;">Giỏ hàng của bạn</h3>';

        if (cart.length === 0) {
            cartHtml += '<p class="text-center text-muted mt-4">Giỏ hàng của bạn đang trống.</p>';
            // Footer khi giỏ hàng rỗng (Code của bạn đã đúng)
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

        // === PHẦN SỬA LỖI NẰM Ở ĐÂY ===
        // Vẽ các món
        for (const item of cart) {
            if (!item || typeof item.id === 'undefined' || typeof item.price !== 'number' || typeof item.quantity !== 'number' || item.quantity <= 0) continue;

            totalPrice += item.price * item.quantity;
            const notes = item.notes || '';
            const imageUrl = item.imageUrl || 'https://via.placeholder.com/100';
            const itemName = item.name || 'Chưa có tên';
            const itemPriceDisplay = item.price.toLocaleString('vi-VN');
            const itemType = item.type || 'item'; // Lấy type (item hoặc combo)

            // Đây là code HTML đầy đủ cho một món hàng
            cartHtml += `
<div class="cart-item mb-3 p-3" 
     data-item-id="${item.id}" 
     data-item-type="${itemType}"
     style="background:#fff;border-radius:10px;box-shadow:0 2px 6px rgba(0,0,0,0.08);">

    <div class="row g-3">
        
        <!-- Ảnh món -->
        <div class="col-auto">
            <img src="${imageUrl}" alt="${itemName}"
                 style="width:65px;height:65px;border-radius:8px;object-fit:cover;">
        </div>

        <!-- Tên + số lượng -->
        <div class="col">
            <h6 class="fw-bold mb-1" style="font-size:1rem;">${itemName}</h6>

            <!-- KHỐI SỐ LƯỢNG -->
            <div class="d-flex align-items-center mb-2" style="gap:6px;">
                <button class="btn-cart-qty-minus"
                    data-item-id="${item.id}"
                    data-item-type="${itemType}"
                    style="width:32px;height:32px;border:1px solid #ccc;border-radius:6px;background:#f2f2f2;font-size:18px;font-weight:bold;">
                    −
                </button>
<input type="number"
       class="qty-input"
       data-item-id="${item.id}"
       data-item-type="${itemType}"
       value="${item.quantity}"
       min="1"
       style="width:50px;text-align:center;padding:4px;border:1px solid #ccc;border-radius:6px;font-weight:bold;"
       onfocus="this.select()">

                <button class="btn-cart-qty-plus"
                    data-item-id="${item.id}"
                    data-item-type="${itemType}"
                    style="width:32px;height:32px;border:1px solid #ccc;border-radius:6px;background:#f2f2f2;font-size:18px;font-weight:bold;">
                    +
                </button>
            </div>
        </div>

        <!-- Giá + Xóa -->
       <div class="col-auto text-end d-flex flex-column align-items-end" style="position:relative; gap:6px;">

    <!-- Giá -->
    <p class="fw-bold mb-0"
       style="font-size:1rem;color:#28a745;">
       ${itemPriceDisplay}đ
    </p>

    <!-- Nút Xóa -->
    <a href="#" 
       class="btn-cart-remove small"
       data-item-id="${item.id}"
       data-item-type="${itemType}"
       style="color:#dc3545;font-weight:600;text-decoration:none;">
       Xóa
    </a>

    <!-- Icon mở ghi chú -->
    <i class="fas fa-pen cart-item-notes-icon"
       data-item-id="${item.id}" 
       style="cursor:pointer;color:#6c757d;font-size:16px;"></i>

</div>

    </div>

    <!-- GHI CHÚ: nằm riêng phía dưới -->
    <div style="margin-top:10px;">
        <div class="position-relative">
            <!-- icon -->
          
            <!-- input -->
            <input type="text"
                class="form-control form-control-sm cart-item-notes-input d-none"
                data-item-id="${item.id}"
                value="${notes}"
                placeholder="Ghi chú..."
                style="padding-left:32px;border-radius:6px;">
        </div>
    </div>

</div>`;

        }
        // === HẾT PHẦN SỬA LỖI ===

        // Footer khi có món (Code của bạn đã đúng)
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

    // Khi click vào icon bút
    $(document).on('click', '.cart-item-notes-icon', function () {
        const itemId = $(this).data('item-id');
        const $input = $(`.cart-item-notes-input[data-item-id='${itemId}']`);

        // Hiển thị input và focus
        $input.toggleClass('d-none').focus();

        // Ẩn icon khi input đang hiển thị
        $(this).toggleClass('d-none');
    });

    // Khi blur khỏi input ghi chú, lưu dữ liệu
    $(document).on('blur', '.cart-item-notes-input', function () {
        const itemId = $(this).data('item-id');
        const newNotes = $(this).val(); 
        const $icon = $(`.cart-item-notes-icon[data-item-id='${itemId}']`);

        // 1. Cập nhật biến cart
        const itemToUpdate = cart.find(i => i.id === itemId);
        if (itemToUpdate) {
            itemToUpdate.notes = newNotes; // Gán ghi chú mới
        } else {
            console.error("Không tìm thấy món để cập nhật ghi chú.");
        }

        // 2. Lưu giỏ hàng vào localStorage
        saveCart();

        $(this).addClass('d-none'); // Ẩn input
        $icon.removeClass('d-none'); // Hiện icon
    });

    // === 4. SỰ KIỆN "GỌI MÓN" ===
    $(document).on('click', '.btn-add-to-cart', function () {
        const button = $(this);
        const itemId = button.data('item-id');
        const itemName = button.data('item-name');
        const itemPrice = parseFloat(button.data('item-price'));
        const imageUrl = button.data('item-image');

        if (typeof itemId === 'undefined' || !itemName || isNaN(itemPrice)) {
            console.error("Dữ liệu item không hợp lệ:", button.data());
            showMobileToast("Lỗi: Không thể thêm món này.", "error");
            return;
        }

        // Xác nhận trước khi thêm món
        showMobileConfirm(`Bạn có chắc chắn muốn gọi món: "${itemName}" không?`, function () {
            const existingItem = cart.find(i => i.id === itemId && i.type === 'item');
            if (existingItem) {
                existingItem.quantity++;
            } else {
                cart.push({
                    id: itemId,
                    name: itemName,
                    price: itemPrice,
                    quantity: 1,
                    notes: "",
                    imageUrl: imageUrl,
                    type: 'item'
                });
            }

            saveCart();

            button.prop('disabled', true).text('Đã thêm');
            setTimeout(() => {
                button.prop('disabled', false).text('Gọi món');
            }, 1000);
        });
    });

    // === 5. SỰ KIỆN "GỌI COMBO" (MỚI) ===
    // === 5. SỰ KIỆN "GỌI COMBO" ===
    $(document).on('click', '.btn-add-combo-to-cart', function () {
        const button = $(this);
        const comboId = button.data('combo-id');
        const comboName = button.data('combo-name');
        const comboPrice = parseFloat(button.data('combo-price'));
        const imageUrl = button.data('combo-image');

        if (typeof comboId === 'undefined' || !comboName || isNaN(comboPrice)) {
            console.error("Dữ liệu combo không hợp lệ:", button.data());
            showMobileToast("Lỗi: Không thể thêm combo này.", "error");
            return;
        }

        // Xác nhận trước khi thêm combo
        showMobileConfirm(`Bạn có chắc chắn muốn gọi combo: "${comboName}" không?`, function () {
            const existingCombo = cart.find(i => i.id === comboId && i.type === 'combo');
            if (existingCombo) {
                existingCombo.quantity++;
            } else {
                cart.push({
                    id: comboId,
                    name: comboName,
                    price: comboPrice,
                    quantity: 1,
                    notes: "",
                    imageUrl: imageUrl,
                    type: 'combo'
                });
            }

            saveCart();

            button.prop('disabled', true).text('Đã thêm');
            setTimeout(() => {
                button.prop('disabled', false).text('Gọi combo');
            }, 1000);
        });
    });


    // === 5. LOGIC LỌC/SEARCH AJAX ===

    // === THAY THẾ TOÀN BỘ HÀM NÀY ===
//    function renderMenu(menuItems) {


//        const categories = {};
//        menuItems.forEach(item => {
//            if (!item || typeof item.menuItemId === 'undefined') return;
//            const categoryName = item.categoryName || "Khác";
//            if (!categories[categoryName]) { categories[categoryName] = []; }
//            categories[categoryName].push(item);
//        });

//        const sortedCategoryNames = Object.keys(categories).sort();

//        // (MỚI) Đặt giới hạn hiển thị ra 5 món
//        const initialShowCount = 5;

//        sortedCategoryNames.forEach(categoryName => {
//            let categoryTitleHtml = `<h3 class="category-title">${categoryName}</h3>`;
//            // (MỚI) Thêm data-show-count
//            let categoryListHtml = `<div class="menu-item-list mt-3" data-show-count="${initialShowCount}">`;

//            const itemsInCategory = categories[categoryName];

//            itemsInCategory.forEach((item, index) => { // (MỚI) Lấy index
//                const menuItemId = item.menuItemId;
//                const itemName = item.name || 'Chưa có tên';
//                const itemPrice = typeof item.price === 'number' ? item.price : 0;
//                const imageUrl = item.imageUrl || 'https://via.placeholder.com/100';

//                // Lấy lại thông tin "Đã gọi" (như cũ)
//                let orderedQty = 0, processingQty = 0;
//                if (Array.isArray(initialOrderedItems)) {
//                    initialOrderedItems.forEach(orderedItem => {
//                        if (orderedItem && orderedItem.menuItemId === menuItemId) {
//                            const qty = typeof orderedItem.quantity === 'number' ? orderedItem.quantity : (typeof orderedItem.Quantity === 'number' ? orderedItem.Quantity : 0);
//                            orderedQty += qty;
//                            if (orderedItem.status === "Đang chế biến") { processingQty += qty; }
//                        }
//                    });
//                }
//                let detailsHtml = '';
//                if (orderedQty > 0) { detailsHtml += `<span class="item-ordered text-success">Đã gọi: ${orderedQty}</span>`; }
//                //if (processingQty > 0) { detailsHtml += `<span class="status-processing-text">Đang chế biến: ${processingQty}</span>`; }

//                // (MỚI) Thêm class và style nếu item vượt quá giới hạn
//                const isHiddenClass = (index >= initialShowCount) ? "menu-item-hidden" : "";
//                const style = (index >= initialShowCount) ? "display: none;" : "";
//                // Nếu đã gọi >=1 → bôi viền xanh
//                const borderStyle = (orderedQty > 0)
//                    ? "border: 1px solid #28a745;"
//                    : "border: 1px solid #e0e0e0;";

//                // Dòng trạng thái "Đã gọi"
//                const orderedLabel = (orderedQty > 0)
//                    ? `<span class="badge text-white" style="background:#28a745; font-size:10px; margin-left:6px;">Đã gọi: ${orderedQty}</span>`
//                    : "";

//                categoryListHtml += `
//<div class="menu-item-card ${isHiddenClass}" 
//     style="${style} ${borderStyle}; border-radius:10px; padding:10px;">

//    <!-- Ảnh click được -->
//    <img src="${imageUrl}" class="btn-details" alt="${itemName}" 
//         data-item-id="${menuItemId}" style="cursor:pointer;" />

//    <div class="details">
//        <!-- Tên món click được -->
//        <div style="display:flex; justify-content:space-between; align-items:center; width:100%;">
//            <h5 class="btn-details" style="margin:0; font-size:1rem;" 
//                data-item-id="${menuItemId}">
//                ${itemName}
//            </h5>

//            ${orderedQty > 0
//                        ? `<span class="badge text-white" 
//                     style="background:#28a745; font-size:11px; padding:4px 8px;font-weight:normal;">
//                        Đã gọi: ${orderedQty}
//                   </span>`
//                        : ""
//                    }
//        </div>

//        <p style="margin:4px 0;">${itemPrice.toLocaleString('vi-VN')} VNĐ</p>
//    </div>

//    <div class="actions">
//        <button class="btn-order btn-add-to-cart "
//                data-item-id="${menuItemId}"
//                data-item-name="${itemName}"
//                data-item-price="${itemPrice}"
//                data-item-image="${imageUrl}">
//        <i class="fas fa-shopping-cart" style="margin-right:5px;"></i> Gọi món
//        </button>
//    </div>
//</div>`;
//;


//            });

//            // (MỚI) Thêm nút "Hiển thị thêm" nếu cần
//            if (itemsInCategory.length > initialShowCount) {
//                categoryListHtml += '<a href="#" class="btn-show-more text-center d-block mt-2 text-decoration-none fw-bold" style="color: var(--brand-gold);">Hiển thị thêm...</a>';
//            }

//            categoryListHtml += '</div>'; // Đóng .menu-item-list

//            // Thêm cả tiêu đề và danh sách vào container
//            $menuListContainer.append(categoryTitleHtml + categoryListHtml);
//        });
//    }
    function renderMenu(menuItems) {
        const categories = {};
        menuItems.forEach(item => {
            if (!item || typeof item.menuItemId === 'undefined') return;
            const categoryName = item.categoryName || "Khác";
            if (!categories[categoryName]) { categories[categoryName] = []; }
            categories[categoryName].push(item);
        });

        const sortedCategoryNames = Object.keys(categories).sort();

        // Giới hạn hiển thị ban đầu
        const initialShowCount = 5;

        sortedCategoryNames.forEach(categoryName => {
            let categoryTitleHtml = `<h3 class="category-title">${categoryName}</h3>`;
            let categoryListHtml = `<div class="menu-item-list mt-3" data-show-count="${initialShowCount}">`;

            const itemsInCategory = categories[categoryName];

            itemsInCategory.forEach((item, index) => {
                const menuItemId = item.menuItemId;
                const itemName = item.name || 'Chưa có tên';
                const itemPrice = typeof item.price === 'number' ? item.price : 0;
                const imageUrl = item.imageUrl || 'https://via.placeholder.com/100';

                // === (MỚI) KIỂM TRA IS ADS ===
                // Lưu ý: Kiểm tra cả viết hoa/thường tùy vào cách API trả về (thường là camelCase 'isAds')
                const isAds = item.isAds === true || item.IsAds === true;

                // === (MỚI) TẠO HTML CHO NHÃN BÁN CHẠY ===
                const adsBadgeHtml = isAds
                    ? `<div style="position: absolute; top: 0; left: 0; background: linear-gradient(90deg, #ff4b2b, #ff416c); color: white; padding: 2px 8px; font-size: 11px; font-weight: bold; border-top-left-radius: 10px; border-bottom-right-radius: 10px; z-index: 10; box-shadow: 2px 2px 5px rgba(0,0,0,0.2);">
                    🔥 Món bán chạy
                   </div>`
                    : '';
                // ===============================

                // Xử lý số lượng đã gọi (Code cũ)
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

                const isHiddenClass = (index >= initialShowCount) ? "menu-item-hidden" : "";
                const style = (index >= initialShowCount) ? "display: none;" : "";

                // Border style (Code cũ)
                const borderStyle = (orderedQty > 0)
                    ? "border: 1px solid #28a745;"
                    : "border: 1px solid #e0e0e0;";

                categoryListHtml += `
            <div class="menu-item-card ${isHiddenClass}" 
                 style="${style} ${borderStyle}; border-radius:10px; padding:10px; position: relative; overflow: hidden;"> 
                 ${adsBadgeHtml} <img src="${imageUrl}" class="btn-details" alt="${itemName}" 
                     data-item-id="${menuItemId}" style="cursor:pointer;" />

                <div class="details">
                    <div style="display:flex; justify-content:space-between; align-items:center; width:100%;">
                        <h5 class="btn-details" style="margin:0; font-size:1rem;" 
                            data-item-id="${menuItemId}">
                            ${itemName}
                        </h5>

                        ${orderedQty > 0
                        ? `<span class="badge text-white" 
                                 style="background:#28a745; font-size:11px; padding:4px 8px;font-weight:normal;">
                                    Đã gọi: ${orderedQty}
                               </span>`
                        : ""
                    }
                    </div>

                    <p style="margin:4px 0;">${itemPrice.toLocaleString('vi-VN')} VNĐ</p>
                </div>

                <div class="actions">
                    <button class="btn-order btn-add-to-cart "
                            data-item-id="${menuItemId}"
                            data-item-name="${itemName}"
                            data-item-price="${itemPrice}"
                            data-item-image="${imageUrl}">
                    <i class="fas fa-shopping-cart" style="margin-right:5px;"></i> Gọi món
                    </button>
                </div>
            </div>`;
            });

            if (itemsInCategory.length > initialShowCount) {
                categoryListHtml += '<a href="#" class="btn-show-more text-center d-block mt-2 text-decoration-none fw-bold" style="color: var(--brand-gold);">Hiển thị thêm...</a>';
            }

            categoryListHtml += '</div>';
            $menuListContainer.append(categoryTitleHtml + categoryListHtml);
        });
    }

    function renderCombos(combos) {
        let categoryTitleHtml = '<h3 class="category-title">Combos & Ưu Đãi</h3>';

        const initialShowCount = 5;
        let categoryListHtml = `<div class="menu-item-list mt-3" data-show-count="${initialShowCount}">`;

        combos.forEach((combo, index) => {
            const comboId = combo.comboId;
            const comboName = combo.name || 'Combo';
            const comboPrice = typeof combo.price === 'number' ? combo.price : 0;
            const imageUrl = combo.imageUrl || 'https://via.placeholder.com/100';
            const originalPrice = typeof combo.originalPrice === 'number' ? combo.originalPrice : 0;

            // --- Tính đã gọi ---
            let orderedQty = 0;
            if (Array.isArray(initialOrderedItems)) {
                initialOrderedItems.forEach(orderedItem => {
                    if (orderedItem && orderedItem.comboId === comboId) {
                        orderedQty += Number(orderedItem.quantity || orderedItem.Quantity || 0);
                    }
                });
            }

            // Border xanh nếu đã gọi
            const borderStyle = orderedQty > 0
                ? "border:1px solid #28a745;"
                : "border:1px solid #ddd;";

            // Ẩn các combo sau 5 item
            const hiddenClass = index >= initialShowCount ? "combo-hidden" : "";
            const hiddenStyle = index >= initialShowCount ? "display:none;" : "";

            // Giá
            let priceHtml = `
            <p style="color:green;margin:2px 0;">
                Giá ưu đãi: ${comboPrice.toLocaleString('vi-VN')} VNĐ
            </p>`;

            if (originalPrice > comboPrice) {
                priceHtml += `
            <p style="color:red;margin:0;">
                Giá cũ: <s>${originalPrice.toLocaleString('vi-VN')} VNĐ</s>
            </p>`;
            }

            // Badge đã gọi
            const orderedHtml = orderedQty > 0
                ? `<span style="background:#28a745;color:white;padding:4px 6px;border-radius:6px;font-size:10px;">Đã gọi: ${orderedQty}</span>`
                : "";

            // Build combo card
            categoryListHtml += `
        <div class="menu-item-card ${hiddenClass}" 
             style="padding:10px;border-radius:10px;${borderStyle};${hiddenStyle}">
            
            <img src="${imageUrl}" 
                 alt="${comboName}" 
                 class="btn-combo-details"
                 data-combo-id="${comboId}"
                 style="cursor:pointer;">

            <div class="details" style="width:100%;">

                <!-- Tên + badge đã gọi -->
                <div style="display:flex;justify-content:space-between;align-items:center;">
                    <h5 class="btn-combo-details" 
                        data-combo-id="${comboId}"
                        style="margin:0;cursor:pointer;">
                        ${comboName}
                    </h5>
                    ${orderedHtml}
                </div>

                ${priceHtml}
            </div>

            <div class="actions">
                <button class="btn-order btn-add-combo-to-cart"
                        data-combo-id="${comboId}"
                        data-combo-name="${comboName}"
                        data-combo-price="${comboPrice}"
                        data-combo-image="${imageUrl}">
        <i class="fas fa-shopping-cart" style="margin-right:5px;"></i> Gọi Combo
                </button>
            </div>
        </div>`;
        });

        // Nếu > 5 combo → thêm nút "Hiển thị thêm"
        if (combos.length > initialShowCount) {
            categoryListHtml += `
            <a href="#" class="btn-show-more-combos d-block text-center mt-2 fw-bold"
               style="color:var(--brand-gold); text-decoration:none;">
                Hiển thị thêm...
            </a>
        `;
        }

        categoryListHtml += `</div>`;

        $menuListContainer.append(categoryTitleHtml + categoryListHtml);
    }


    $(document).on("click", ".btn-show-more-combos", function (e) {
        e.preventDefault();
        const $button = $(this);
        const $list = $button.closest(".menu-item-list");
        const $hiddenItems = $list.find(".combo-hidden");

        if ($button.hasClass("expanded")) {
            // Đang ở trạng thái hiển thị → ẩn bớt
            $hiddenItems.slideUp(150);
            $button.text("Hiển thị thêm...").removeClass("expanded");
        } else {
            // Đang ở trạng thái ẩn → hiện ra
            $hiddenItems.slideDown(150);
            $button.text("Ẩn bớt").addClass("expanded");
        }
    });


    //function performFilter() {
    //    const searchString = $searchInput.val();
    //    const categoryId = $categoryTabs.filter('.active').data('id');

    //    let url = `${apiBaseUrl}/api/OrderTable/MenuOrder/${tableId}?`;

    //    // === ĐÃ SỬA LỖI LOGIC (if categoryId): Chấp nhận số 0 ===
    //    if (categoryId !== undefined && categoryId !== null && categoryId !== "") {
    //        url += `categoryId=${categoryId}&`;
    //    }

    //    if (searchString) {
    //        url += `searchString=${encodeURIComponent(searchString)}`;
    //    }

    //    // Hiển thị "Đang tải..."
    //    $menuListContainer.html('<p class="text-center text-muted mt-4">Đang tải...</p>');

    //    // Gọi AJAX
    //    $.ajax({
    //        url: url,
    //        type: 'GET',
    //        dataType: 'json',
    //        success: function (response) {

    //            // 1. Xóa "Đang tải..."
    //            $menuListContainer.empty();

    //            if (response) {
    //                let hasContent = false;

    //                // 2. Render Combos (nếu có)
    //                if (response.combos && Array.isArray(response.combos) && response.combos.length > 0) {
    //                    renderCombos(response.combos);
    //                    hasContent = true;
    //                }

    //                // 3. Render MenuItems (nếu có)
    //                if (response.menuItems && Array.isArray(response.menuItems) && response.menuItems.length > 0) {
    //                    renderMenu(response.menuItems);
    //                    hasContent = true;
    //                }

    //                // 4. Xử lý khi không có gì
    //                if (!hasContent) {
    //                    const currentCatId = $categoryTabs.filter('.active').data('id');
    //                    if (currentCatId == -1) { // Tab Combo
    //                        $menuListContainer.html('<p class="text-center text-muted mt-4">Không có combo nào để hiển thị.</p>');
    //                    } else { // Các tab khác
    //                        $menuListContainer.html('<p class="text-center text-muted mt-4">Không tìm thấy món ăn nào.</p>');
    //                    }
    //                }
    //            }
    //            else {
    //                // Xử lý API trả về rỗng
    //                $menuListContainer.empty();
    //                console.error("API response không hợp lệ:", response);
    //                $menuListContainer.html('<p class="text-center text-danger mt-4">Lỗi: Dữ liệu menu không đúng.</p>');
    //            }
    //        },
    //        error: function (xhr, status, error) {
    //            // Xử lý lỗi (Giữ nguyên)
    //            $menuListContainer.empty();
    //            console.error("Lỗi AJAX:", status, error, xhr.responseText);
    //            let errorMsg = "Lỗi khi tải menu.";
    //            if (xhr.responseJSON && xhr.responseJSON.message) {
    //                errorMsg = xhr.responseJSON.message;
    //            } else if (xhr.responseText) {
    //                try { const err = JSON.parse(xhr.responseText); if (err.message) errorMsg = err.message; } catch (e) { }
    //            }
    //            $menuListContainer.html(`<p class="text-center text-danger mt-4">${errorMsg}</p>`);
    //        }
    //    });
    //}





    // Sự kiện Lọc/Search


    // === 2. Hàm Filter Menu ===
    function performFilter() {
        const searchString = $searchInput.val();
        const categoryId = $categoryTabs.filter('.active').data('id');

        let url = `${apiBaseUrl}/OrderTable/MenuOrder/${tableId}?`;

        if (categoryId !== undefined && categoryId !== null && categoryId !== "") {
            url += `categoryId=${categoryId}&`;
        }

        if (searchString) {
            url += `searchString=${encodeURIComponent(searchString)}`;
        }

        $menuListContainer.html('<p class="text-center text-muted mt-4">Đang tải...</p>');

        $.ajax({
            url: url,
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                $menuListContainer.empty();

                if (response) {
                    let hasContent = false;

                    if (response.combos && Array.isArray(response.combos) && response.combos.length > 0) {
                        renderCombos(response.combos);
                        hasContent = true;
                    }

                    if (response.menuItems && Array.isArray(response.menuItems) && response.menuItems.length > 0) {
                        renderMenu(response.menuItems);
                        hasContent = true;
                    }

                    if (!hasContent) {
                        const currentCatId = $categoryTabs.filter('.active').data('id');
                        if (currentCatId == -1) {
                            $menuListContainer.html('<p class="text-center text-muted mt-4">Không có combo nào để hiển thị.</p>');
                        } else {
                            $menuListContainer.html('<p class="text-center text-muted mt-4">Không tìm thấy món ăn nào.</p>');
                        }
                    }
                } else {
                    $menuListContainer.html('<p class="text-center text-danger mt-4">Lỗi: Dữ liệu menu không đúng.</p>');
                    console.error("API response không hợp lệ:", response);
                }
            },
            error: function (xhr, status, error) {
                $menuListContainer.empty();
                let errorMsg = "Lỗi khi tải menu.";
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMsg = xhr.responseJSON.message;
                } else if (xhr.responseText) {
                    try { const err = JSON.parse(xhr.responseText); if (err.message) errorMsg = err.message; } catch (e) { }
                }
                $menuListContainer.html(`<p class="text-center text-danger mt-4">${errorMsg}</p>`);
                console.error("Lỗi AJAX:", status, error, xhr.responseText);
            }
        });
    }

    // === 3. Lấy reservationId đang active ===
    async function fetchActiveReservation() {
        try {
            const response = await $.getJSON(`${apiBaseUrl}/Reservation/active/${tableId}`);
            if (response && response.reservationId) {
                return response.reservationId;
            }
        } catch (err) {
            console.error("Không lấy được reservationId:", err.statusText || err);
            return null;
        }
    }

    // === 4. Khởi tạo SignalR ===
    async function initSignalR() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubBaseUrl)
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveItemStatusUpdate", (orderDetailId, orderComboItemId, newStatus) => {
            const statusElement = document.getElementById(`status-text-${orderDetailId}`);
            if (statusElement) {
                statusElement.innerText = newStatus;
                statusElement.className = 'badge';

                if (newStatus === 'Cooking' || newStatus === 'Đang nấu') {
                    statusElement.classList.add('bg-warning');
                } else if (newStatus === 'Ready' || newStatus === 'Đã xong') {
                    statusElement.classList.add('bg-primary');
                } else if (newStatus === 'Served' || newStatus === 'Đã phục vụ') {
                    statusElement.classList.add('bg-success');
                } else if (newStatus === 'Cancelled' || newStatus === 'Đã hủy') {
                    statusElement.classList.add('bg-danger');
                }
            }
        });

        try {
            await connection.start();
            console.log("SignalR connected");

            const reservationId = await fetchActiveReservation();
            if (reservationId) {
                await connection.invoke("JoinGroup", `Reservation_${reservationId}`);
                console.log("Joined group Reservation_" + reservationId);
            }
        } catch (err) {
            console.error("SignalR connection failed:", err);
        }
    }

    // === 5. Khởi chạy khi document ready ===
    $(document).ready(function () {
        performFilter(); // load menu
        initSignalR();   // kết nối SignalR
    });



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


    // Trừ
    $(document).on('click', '.btn-cart-qty-minus', function () {
        const itemId = $(this).data('item-id');
        const itemType = $(this).data('item-type');
        const $input = $(`.qty-input[data-item-id='${itemId}'][data-item-type='${itemType}']`);
        let qty = parseInt($input.val()) || 1;

        if (qty > 1) {
            qty--;
            $input.val(qty);
            updateCart(itemId, itemType, qty);
        } else {
            showMobileConfirm('Xóa món này khỏi giỏ hàng?', function () {
                removeCartItem(itemId, itemType);
            });
        }
    });

    // Cộng
    $(document).on('click', '.btn-cart-qty-plus', function () {
        const itemId = $(this).data('item-id');
        const itemType = $(this).data('item-type');
        const $input = $(`.qty-input[data-item-id='${itemId}'][data-item-type='${itemType}']`);
        let qty = parseInt($input.val()) || 1;
        qty++;
        $input.val(qty);
        updateCart(itemId, itemType, qty);
    });

    // Nhập trực tiếp / thay đổi số lượng
    // Khi thay đổi giá trị (rời ô hoặc Enter)
    $(document).on('change', '.qty-input', function () {
        let val = parseInt($(this).val());
        if (isNaN(val) || val < 1) val = 1;
        $(this).val(val);

        const itemId = $(this).data('item-id');
        const itemType = $(this).data('item-type');
        updateCart(itemId, itemType, val);
    });

    // Ngăn Enter submit form, vẫn update khi nhấn Enter
    $(document).on('keydown', '.qty-input', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();  // Không submit form
            $(this).trigger('change'); // Trigger change để update giá
        }
    });




    // Hàm cập nhật giỏ hàng
    function updateCart(itemId, itemType, qty) {
        // Cập nhật trong cart array
        const itemIndex = cart.findIndex(i => i.id === itemId && i.type === itemType);
        if (itemIndex === -1) return;

        cart[itemIndex].quantity = qty;

        // Cập nhật hiển thị giá tiền
        const itemPrice = cart[itemIndex].price || 0;
        const totalPrice = itemPrice * qty;
        $(`.cart-item-price[data-item-id='${itemId}'][data-item-type='${itemType}']`).text(totalPrice.toLocaleString('vi-VN') + 'đ');

        saveCart(); // nếu bạn vẫn muốn lưu vào localStorage
    }


    // Hàm xóa món
    function removeCartItem(itemId, itemType) {
        const index = cart.findIndex(i => i.id === itemId && i.type === itemType);
        if (index > -1) {
            cart.splice(index, 1);
            saveCart();
        }
    }
    $(document).on('click', '.btn-cart-remove', function (e) {
        e.preventDefault();

        const itemId = $(this).data('item-id');
        const itemType = $(this).data('item-type');

        showMobileConfirm('Xóa món này khỏi giỏ hàng?', () => {

            const index = cart.findIndex(i => i.id === itemId && i.type === itemType);

            if (index > -1) {
                cart.splice(index, 1);
                saveCart();
            }

            // XÓA LUÔN KHỎI HTML
            $(`.cart-item[data-item-id='${itemId}'][data-item-type='${itemType}']`).remove();

            renderCart(); // nếu bạn dùng render lại toàn bộ
        });
    });


    // === HÀM SUBMIT ORDER xác nhận gọi món ===
    $(document).on('click', '#btn-submit-order', function () {
        if (!Array.isArray(cart) || cart.length === 0) {
            showMobileToast("Giỏ hàng của bạn đang trống!", "error");
            return;
        }

        // Hiển thị xác nhận
        showMobileConfirm("Bạn có chắc chắn muốn gọi món này không?", function () {
            const btn = $('#btn-submit-order');
            btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang gửi...');

            // 1. Lọc ra 2 danh sách (Giữ nguyên logic của bạn)
            const orderItems = cart
                .filter(item => item && item.type === 'item' && item.quantity > 0)
                .map(item => ({
                    menuItemId: item.id,
                    quantity: item.quantity,
                    notes: item.notes || ""
                }));

            const orderCombos = cart
                .filter(item => item && item.type === 'combo' && item.quantity > 0)
                .map(item => ({
                    comboId: item.id,
                    quantity: item.quantity,
                    notes: item.notes || ""
                }));

            // === BƯỚC 2: SỬA LỖI TẠI ĐÂY ===

            // Kiểm tra giỏ hàng rỗng (Sử dụng 2 mảng gốc)
            if (orderItems.length === 0 && orderCombos.length === 0) {
                showMobileToast("Giỏ hàng không có món hợp lệ để gửi.", "error");
                btn.prop('disabled', false).html('Xác nhận gọi món');
                return;
            }

            // Tạo đối tượng data mới (ĐÃ SỬA)
            const orderData = {
                tableId: parseInt(tableId),

                // Hack: Nếu mảng 'items' rỗng, hãy gửi một món "ảo"
                // (Đúng như cách cURL của bạn đã test thành công)
                items: (orderItems.length > 0)
                    ? orderItems
                    : [{ menuItemId: 0, quantity: 0, notes: "string" }],

                combos: orderCombos
            };

            // === HẾT PHẦN SỬA ===

            // (Thêm dòng này để bạn tự kiểm tra)
            console.log("=== DỮ LIỆU SẮP GỬI (ĐÃ SỬA) ===");
            console.log(JSON.stringify(orderData, null, 2));


            // 3. Gửi AJAX (Giữ nguyên code của bạn)
            $.ajax({
                url: apiBaseUrl + '/OrderTable/SubmitOrder',
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
                    btn.prop('disabled', false).html('Xác nhận gọi món');
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
                url: apiBaseUrl + '/OrderTable/CancelItem/' + orderDetailId,
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
                url: apiBaseUrl + '/OrderTable/RequestAssistance',
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
    document.getElementById("search-icon-btn").addEventListener("click", function () {
        const bar = document.getElementById("search-bar");
        if (bar.style.display === "block") {
            bar.style.display = "none";
        } else {
            bar.style.display = "block";
        }
    });

    // === DÁN HÀM NÀY VÀO menu-order.js ===

    function showComboDetailsModal(comboId) {
        const modal = new bootstrap.Modal(document.getElementById('comboDetailModal'));
        const $modal = $('#comboDetailModal');

        // 1️⃣ Reset modal
        $modal.find('.modal-title').text('Đang tải chi tiết combo...');
        $modal.find('#combo-modal-image-container').html('');
        $modal.find('#combo-modal-items').html('<p class="text-center text-muted py-3">Đang tải...</p>');
        $modal.find('#combo-modal-pricing').html('');
        $modal.find('#combo-modal-footer').html('');

        modal.show();

        // 2️⃣ Gọi API
        $.ajax({
            url: `${apiBaseUrl}/OrderTable/ComboDetails/${comboId}`,
            type: 'GET',
            success: function (combo) {
                // --- Tiêu đề ---
                $modal.find('.modal-title').text(combo.name || 'Chi tiết Combo');

                // --- Ảnh combo ---
                if (combo.imageUrl) {
                    $modal.find('#combo-modal-image-container').html(`
                    <div class="text-center mb-3">
                        <img src="${combo.imageUrl}" 
                             alt="${combo.name}" 
                             class="img-fluid rounded shadow-sm" 
                             style="max-height: 160px; width: 90%; object-fit: cover;">
                    </div>
                `);
                }
                let itemsHtml = '<h6><i class="fas fa-clipboard-list me-2"></i>Bao gồm các món:</h6>';
                itemsHtml += '<ul class="list-group list-group-flush" style="margin-top: 10px;">';
                // --- Danh sách món ăn trong combo ---

                combo.items.forEach(item => {
                    const itemImage = item.imageUrl || 'https://via.placeholder.com/80?text=No+Image';
                    const totalPrice = (item.unitPrice * item.quantity).toLocaleString('vi-VN');

                    itemsHtml += `
                    <div class="d-flex align-items-center border-bottom py-2">
                        <img src="${itemImage}" 
                             alt="${item.itemName}" 
                             class="rounded me-3" 
                             style="width: 60px; height: 60px; object-fit: cover;">
                        <div class="flex-grow-1">
                            <div class="fw-semibold">${item.itemName}</div>
                            <div class="text-muted small">Số lượng: ${item.quantity}</div>
                        </div>
                        <div class="text-end text-muted small">${totalPrice}đ</div>
                    </div>
                `;
                });
                itemsHtml += `</div>`;
                $modal.find('#combo-modal-items').html(itemsHtml);

                // --- Chi tiết giá ---
                let pricingHtml = `
                <div class="combo-pricing mt-3">
                    <div class="d-flex justify-content-between border-bottom py-1">
                        <span>Tổng giá gốc:</span>
                        <span class="text-muted"><s>${combo.originalPrice?.toLocaleString('vi-VN') || 0}đ</s></span>
                    </div>
                    <div class="d-flex justify-content-between border-bottom py-1">
                        <span class="text-success fw-bold">Tiết kiệm:</span>
                        <span class="text-success fw-bold">-${combo.savedAmount?.toLocaleString('vi-VN') || 0}đ</span>
                    </div>
                    <div class="d-flex justify-content-between align-items-center py-2">
                        <span class="fw-bold fs-5">Giá Combo:</span>
                        <span class="fw-bold fs-5 text-danger">${combo.comboPrice.toLocaleString('vi-VN')}đ</span>
                    </div>
                </div>
            `;
                $modal.find('#combo-modal-pricing').html(pricingHtml);

                // --- Nút footer ---
                let footerHtml = `
    <div class="d-flex justify-content-center gap-2 mt-2">
        <button class="btn btn-light flex-fill" 
                style="max-width: 130px; font-size: 0.9rem; padding: 6px 12px;"
                data-bs-dismiss="modal">
            <i class="fas fa-times me-1"></i> Đóng
        </button>
        <button class="btn text-white btn-add-combo-to-cart flex-fill"
                data-combo-id="${combo.comboId}"
                data-combo-name="${combo.name}"
                data-combo-price="${combo.comboPrice}"
                data-combo-image="${combo.imageUrl}"
                style="background-color: var(--brand-gold); font-weight: 600; max-width: 150px; font-size: 0.9rem; padding: 6px 12px;">
            <i class="fas fa-shopping-cart me-1"></i> Gọi combo
        </button>
    </div>
`;

                $modal.find('#combo-modal-footer').html(`
                <div class="d-flex gap-2 mt-2">${footerHtml}</div>
            `);
            },
            error: function (xhr) {
                modal.hide();
                const errorMsg = xhr.responseJSON?.message || "Không thể tải chi tiết combo.";
                showMobileToast(errorMsg, "error");
            }
        });
    }

    // === DÁN SỰ KIỆN NÀY VÀO menu-order.js ===

    $(document).on('click', '.btn-combo-details', function (e) {
        e.preventDefault(); // Ngăn trình duyệt nhảy trang

        const comboId = $(this).data('combo-id');
        if (comboId) {
            showComboDetailsModal(comboId);
        }
    });


    // === DÁN HÀM NÀY VÀO menu-order.js ===

    function showMenuItemDetailsModal(menuItemId) {
        const modal = new bootstrap.Modal(document.getElementById('comboDetailModal'));

        // 1. Reset Modal về trạng thái "Đang tải"
        const $modal = $('#comboDetailModal');
        $modal.find('.modal-title').text('Đang tải chi tiết món...');
        $modal.find('#combo-modal-image-container').html('');
        $modal.find('#combo-modal-items').html('<p class="text-center text-muted">Đang tải...</p>');
        $modal.find('#combo-modal-pricing').html('');
        $modal.find('#combo-modal-footer').html('');

        modal.show(); // Hiển thị modal

        // 2. Gọi API mới
        $.ajax({
            url: `${apiBaseUrl}/OrderTable/MenuItemDetails/${menuItemId}`,
            type: 'GET',
            success: function (item) {

                // 3. Đổ dữ liệu MÓN ĂN vào Modal
                $modal.find('.modal-title').text('Tên món: ' + item.name);

                // Thêm ảnh (nếu có)
                if (item.imageUrl) {
                    $modal.find('#combo-modal-image-container').html(
                        `<img src="${item.imageUrl}" alt="${item.name}" class="img-fluid rounded" style="max-height: 200px;">`
                    );
                }

                // Thêm mô tả (thay vì danh sách món)
                let descriptionHtml = `
                <p><strong>Danh mục:</strong> ${item.categoryName}</p>
                <p><strong>Mô tả: </strong> ${item.description || 'Món ăn này chưa có mô tả.'}</p>
            `;
                $modal.find('#combo-modal-items').html(descriptionHtml);

                // Thêm giá (chỉ 1 dòng)
                let pricingHtml = `
                <ul class="list-group list-group-flush">
                    <li class="list-group-item d-flex justify-content-between">
                        <span class="fw-bold fs-5">Giá:</span>
                        <span class="fw-bold fs-5 text-danger">${item.price.toLocaleString('vi-VN')}đ</span>
                    </li>
                </ul>`;
                $modal.find('#combo-modal-pricing').html(pricingHtml);

                // Thêm nút "Gọi Món" vào footer
                // (Nút này y hệt nút .btn-add-to-cart)
                let footerHtml = `
<div class="d-flex justify-content-center gap-2 mt-2">
    <button class="btn btn-secondary flex-fill" 
            style="max-width: 140px; font-size: 0.9rem; padding: 6px 12px;"
            data-bs-dismiss="modal">
        Đóng
    </button>
    <button class="btn-order btn-add-to-cart flex-fill"
            data-item-id="${item.menuItemId}"
            data-item-name="${item.name}"
            data-item-price="${item.price}"
            data-item-image="${item.imageUrl}"
            style="background-color: var(--brand-gold); color: white; max-width: 140px; font-size: 0.9rem; padding: 6px 12px;">
        <i class="fas fa-shopping-cart me-1"></i> Gọi Món này
    </button>
</div>
`;



                $modal.find('#combo-modal-footer').html(footerHtml);
            },
            error: function (xhr) {
                modal.hide(); // Ẩn modal nếu lỗi
                const errorMsg = xhr.responseJSON?.message || "Không thể tải chi tiết món ăn.";
                showMobileToast(errorMsg, "error");
            }
        });
    }

    // === DÁN SỰ KIỆN NÀY VÀO menu-order.js ===

    // Đây là nút "Chi tiết" của MÓN ĂN LẺ
    $(document).on('click', '.btn-details', function (e) {
        e.preventDefault();

        // (Chúng ta đã sửa 'renderMenu' để thêm data-item-id vào)
        const menuItemId = $(this).data('item-id');

        if (menuItemId) {
            showMenuItemDetailsModal(menuItemId);
        }
    });



    // === 7. CHẠY LẦN ĐẦU ===
    loadCart(); // Tải giỏ hàng từ localStorage
    performFilter();
}); // End of $(document).ready