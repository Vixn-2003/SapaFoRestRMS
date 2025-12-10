$(document).on('click', '.btn-print-receipt', function () {
    const orderCode = $(this).data('order-code');
    // ✅ REMOVED: tables variable - không sử dụng trong receipt print
    const customerName = $(this).data('customer-name');
    const customerPhone = $(this).data('customer-phone');
    const createdAt = $(this).data('created-at');
    const paidAt = $(this).data('paid-at');
    const staffName = $(this).data('staff-name');
    const paymentMethod = $(this).data('payment-method');

    const subtotal = parseFloat($(this).data('subtotal') || 0);
    const vat = parseFloat($(this).data('vat') || 0);
    const serviceFee = parseFloat($(this).data('service-fee') || 0);
    const discount = parseFloat($(this).data('discount') || 0);
    const total = parseFloat($(this).data('total') || 0);

    // NEW: validate dữ liệu
    if (!orderCode || total <= 0) {
        alert("❗ Không thể in hóa đơn vì dữ liệu không hợp lệ!");
        return;
    }

    // NEW: nhận breakdown từ receiptData.Transactions (nếu có)
    const txs = (window.receiptData && Array.isArray(window.receiptData.Transactions))
        ? window.receiptData.Transactions
        : [];

    // Nếu không có transactions, fallback dùng PaymentMethod + tổng
    const paymentBreakdown = txs.length
        ? txs.map(t => ({
            method: t.PaymentMethod || 'Unknown',
            amount: parseFloat(t.Amount || 0),
            amountReceived: parseFloat(t.AmountReceived || 0),
            refundAmount: parseFloat(t.RefundAmount || 0)
        }))
        : [{
            method: paymentMethod || 'Unknown',
            amount: total,
            amountReceived: parseFloat($(this).data('customer-paid') || 0),
            refundAmount: parseFloat($(this).data('change-amount') || 0)
        }];

    // Lấy dữ liệu items từ biến global (được định nghĩa trong Receipt.cshtml)
    const items = (window.receiptData && window.receiptData.Items) ? window.receiptData.Items : [];

    function formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN').format(amount) + ' ₫';
    }

    function renderMethod(method) {
        const m = (method || '').toLowerCase();
        if (m === 'cash') return 'Tiền mặt';
        if (m === 'qrbanktransfer' || m === 'qr' || m === 'vietqr') return 'QR';
        if (m === 'card') return 'Thẻ';
        if (m === 'ewallet') return 'Ví điện tử';
        if (m === 'split') return 'Chia hóa đơn';
        return method || 'Khác';
    }

    let itemsHtml = "";
    items.forEach((item, index) => {
        itemsHtml += `
            <tr>
                <td style="text-align:center;">${index + 1}</td>
                <td>${item.Name}${item.IsCombo ? "<br><small style='color:#666;'>Combo</small>" : ""}</td>
                <td style="text-align:center;">${item.QuantityUsed}</td>
                <td style="text-align:right;">${formatCurrency(item.UnitPrice)}</td>
                <td style="text-align:right; font-weight:bold;">${formatCurrency(item.TotalPrice)}</td>
            </tr>
        `;
    });

    const html = `
        <html>
        <head>
            <meta charset="utf-8" />
            <title>Hóa đơn ${orderCode}</title>
        </head>
        <body style="font-family: Arial; padding:20px;">
            <div style="text-align:center; margin-bottom:20px;">
                <h2 style="margin:0; color:#16a34a;">NHÀ HÀNG SAPA FOREST</h2>
                <div style="font-size:14px;">Địa chỉ: 123 Đường ABC, Sa Pa</div>
                <div style="font-size:14px;">Hotline: 0123 456 789</div>
                <hr style="margin-top:15px;">
                <h3>HÓA ĐƠN THANH TOÁN</h3>
                <div>Mã đơn: <b>${orderCode}</b></div>
            </div>

            <div>
                <div><b>Khách hàng:</b> ${customerName}</div>
                ${customerPhone !== "—" ? `<div><b>Điện thoại:</b> ${customerPhone}</div>` : ""}
                <div><b>Thời gian tạo:</b> ${createdAt}</div>
                <div><b>Thanh toán lúc:</b> ${paidAt}</div>
                <div><b>Thu ngân:</b> ${staffName}</div>
                <div><b>Phương thức:</b> ${paymentMethod}</div>
            </div>

            <table style="width:100%; margin-top:20px; border-collapse:collapse;">
                <thead>
                    <tr style="background:#16a34a; color:#fff;">
                        <th>STT</th>
                        <th>Tên món</th>
                        <th>SL</th>
                        <th style="text-align:right;">Đơn giá</th>
                        <th style="text-align:right;">Thành tiền</th>
                    </tr>
                </thead>
                <tbody>${itemsHtml}</tbody>
            </table>

            <hr>

            <table style="width:100%; margin-top:10px; font-size:16px;">
                <tr><td>Tạm tính</td><td style="text-align:right;">${formatCurrency(subtotal)}</td></tr>
                <tr><td>VAT 10%</td><td style="text-align:right;">${formatCurrency(vat)}</td></tr>
                <tr><td>Phí dịch vụ 5%</td><td style="text-align:right;">${formatCurrency(serviceFee)}</td></tr>
                ${discount > 0 ? `<tr><td style="color:red;">Giảm giá</td><td style="text-align:right; color:red;">-${formatCurrency(discount)}</td></tr>` : ""}
                <tr style="font-size:20px; font-weight:bold; color:#16a34a;">
                    <td>TỔNG THANH TOÁN</td>
                    <td style="text-align:right;">${formatCurrency(total)}</td>
                </tr>

                <!-- NEW: breakdown theo phương thức + tiền thối nếu có -->
                ${paymentBreakdown.map(p => `
                    <tr>
                        <td>${renderMethod(p.method)}</td>
                        <td style="text-align:right;">${formatCurrency(p.amount)}</td>
                    </tr>
                    ${p.method && p.method.toLowerCase() === 'cash' && p.refundAmount > 0
                        ? `<tr><td>Tiền thối lại</td><td style="text-align:right;">${formatCurrency(p.refundAmount)}</td></tr>`
                        : ''
                    }
                `).join('')}
            </table>

            <div style="text-align:center; margin-top:30px; color:#16a34a;">
                <b>Cảm ơn quý khách! Hẹn gặp lại 💚</b>
            </div>

            <script>
                window.onload = () => {
                    window.print();
                    setTimeout(() => window.close(), 500);
                };
            </scr` + `ipt>
        </body>
        </html>
    `;

    const printWindow = window.open("", "_blank");
    printWindow.document.open();
    printWindow.document.write(html);
    printWindow.document.close();
});
