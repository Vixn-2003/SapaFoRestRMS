// vouchers.js

function initializeVouchersPage(successMessage, errorMessage) {
    $(function () {
        console.log("jQuery đã sẵn sàng");

        if (successMessage) {
            toastr.success(successMessage, '', {
                positionClass: 'toast-top-center',
                timeOut: 3000,
                closeButton: true,
                progressBar: true
            });
        }

        if (errorMessage) {
            toastr.error(errorMessage, '', {
                positionClass: 'toast-top-center',
                timeOut: 3000,
                closeButton: true,
                progressBar: true
            });
        }
    });

    document.addEventListener("DOMContentLoaded", function () {
        const form = document.getElementById("filterForm");

        form.addEventListener("submit", function (e) {
            const startDate = document.getElementById("startDate").value;
            const endDate = document.getElementById("endDate").value;
            const discountValue = parseFloat(document.getElementById("discountValue").value);
            const minOrderValue = parseFloat(document.getElementById("minOrderValue").value);
            const maxDiscount = parseFloat(document.getElementById("maxDiscount").value);
            const keywordInput = document.getElementById("keyword");

            let errorMessages = [];

            if (startDate && endDate && new Date(startDate) > new Date(endDate)) {
                errorMessages.push("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");
            }

            if (!isNaN(discountValue) && discountValue < 0)
                errorMessages.push("Discount Value không được âm.");

            if (!isNaN(minOrderValue) && minOrderValue < 0)
                errorMessages.push("Min Order Value không được âm.");

            if (!isNaN(maxDiscount) && maxDiscount < 0)
                errorMessages.push("Max Discount không được âm.");

            if (keywordInput.value && keywordInput.value.trim() === "") {
                errorMessages.push("Keyword không được chỉ có khoảng trắng.");
            } else {
                keywordInput.value = keywordInput.value.trim();
            }

            if (errorMessages.length > 0) {
                e.preventDefault();
                alert(errorMessages.join("\n"));
            }
        });
    });
}
