
// IN QR TRỰC TIẾP
$(document).on('click', '.btn-print-qr', function () {
    const tableNumber = $(this).data('table-number');
    const areaName = $(this).data('area-name');
    const floor = $(this).data('floor');
    const qrUrl = $(this).data('qr-url');

    const html = `
        <html>
        <head>
            <meta charset="utf-8">
            <title>In QR Bàn ${tableNumber}</title>
            <style>
                body { font-family: Arial; text-align: center; margin-top: 40px; }
                img { width: 200px; height: 200px; }
            </style>
        </head>
        <body>
            <h2>Nhà hàng Sapa</h2>
            <p><strong>Bàn:</strong> ${tableNumber}</p>
            <p><strong>Khu vực:</strong> ${areaName}</p>
            <p><strong>Tầng:</strong> ${floor}</p>
            <img src="${qrUrl}" alt="QR Code" />
            <script>window.onload = function() { window.print(); window.close(); }</script>
        </body>
        </html>
    `;

    const printWindow = window.open('', '_blank');
    printWindow.document.open();
    printWindow.document.write(html);
    printWindow.document.close();
});

// XUẤT PDF QR - canh giữa, bố cục đẹp
$(document).on('click', '.btn-export-qr', async function () {
    const { jsPDF } = window.jspdf;
    const tableNumber = $(this).data('table-number');
    const areaName = $(this).data('area-name');
    const floor = $(this).data('floor');
    const qrUrl = $(this).data('qr-url');

    // Tải ảnh QR về dạng Base64
    const imgData = await fetch(qrUrl).then(r => r.blob()).then(blob => {
        return new Promise(resolve => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result);
            reader.readAsDataURL(blob);
        });
    });

    const pdf = new jsPDF({
        orientation: 'portrait',
        unit: 'mm',
        format: 'a4'
    });

    const pageWidth = pdf.internal.pageSize.getWidth();
    let y = 40;

    // Tiêu đề
    pdf.setFont('helvetica', 'bold');
    pdf.setFontSize(22);
    const title = "QR CODE - SapaForest";
    const titleWidth = pdf.getTextWidth(title);
    pdf.text(title, (pageWidth - titleWidth) / 2, y);
    y += 20;

    // Thông tin bàn
    pdf.setFont('helvetica', 'normal');
    pdf.setFontSize(16);
    const infoLines = [
        `Bàn: ${tableNumber}`,
        `Khu vuc: ${areaName}`,
        `Tang: ${floor}`
    ];

    infoLines.forEach(line => {
        const lineWidth = pdf.getTextWidth(line);
        pdf.text(line, (pageWidth - lineWidth) / 2, y);
        y += 10;
    });

    //  Thêm QR ở giữa
    const imgWidth = 100;
    const imgHeight = 100;
    const imgX = (pageWidth - imgWidth) / 2;
    y += 10;
    pdf.addImage(imgData, 'PNG', imgX, y, imgWidth, imgHeight);

    //  Xuất file
    pdf.save(`QR_Ban_${tableNumber}.pdf`);
});

