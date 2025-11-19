// print-qr.js (ĐỒNG BỘ WIDTH + FIX CACHE)

// Link logo
const logoUrl = "https://bizweb.dktcdn.net/100/461/735/themes/872641/assets/logo.png?1697183839847";

// ==================== IN QR TRỰC TIẾP (THU HẸP LẠI) ====================
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
                body { 
                    font-family: Arial, sans-serif; 
                    text-align: center; 
                    background: #fefefe;
                    margin: 0; padding: 0;
                }
                .container {
                    width: 100%;
                    /* === THAY ĐỔI: Thu hẹp từ 400px xuống 375px (bằng 280pt) === */
                    max-width: 375px; 
                    margin: 30px auto;
                    border: 2px solid #1e88e5;
                    border-radius: 15px;
                    padding: 20px;
                    background: #ffffff;
                    box-shadow: 0 4px 10px rgba(0,0,0,0.1);
                }
                .logo {
                    width: 120px;
                    margin-bottom: 10px;
                }
                h2 {
                    color: #1e88e5; 
                    margin: 5px 0 15px;
                }
                p { 
                    font-size: 16px; 
                    color: #555; 
                    margin: 5px 0; 
                }
                .highlight { color: #e53935; font-weight: bold; }
                .qr-box {
                    display: inline-block;
                    padding: 0; 
                    /* Viền xanh đã bị xóa theo yêu cầu trước */
                    margin: 25px 0; 
                    line-height: 0; 
                }
                .qr-img {
                    width: 180px;
                    height: 180px;
                    display: block; 
                    margin: 0;
                    padding: 0;
                }
                .instruction { 
                    font-size: 16px; 
                    color: #1e88e5; 
                    margin-top: 15px; 
                    display: flex; 
                    justify-content: center; 
                    align-items: center; 
                    gap: 6px;
                    font-weight: bold;
                }
            </style>
        </head>
        <body>
            <div class="container">
                <img src="${logoUrl}" alt="Logo" class="logo" />
                <h2>🏨 Nhà hàng Sapa</h2>
                <p><span class="highlight">Bàn:</span> ${tableNumber}</p>
                <p><span class="highlight">Khu vực:</span> ${areaName}</p>
                <p><span class="highlight">Tầng:</span> ${floor}</p>
                <div class="qr-box">
                    <img src="${qrUrl}" alt="QR Code" class="qr-img"/>
                </div>
                <div class="instruction">
                    📲 Hãy quét Mã QR để gọi món tại đây
                </div>
            </div>
            <script>
                window.onload = function() { 
                    window.print(); 
                    window.close(); 
                }
            </script>
        </body>
        </html>
    `;

    const printWindow = window.open('', '_blank');
    printWindow.document.open();
    printWindow.document.write(html);
    printWindow.document.close();
});

// ==================== XUẤT WORD (ĐÃ THU HẸP) ====================
$(document).on('click', '.btn-export-qr', async function () {
    const tableNumber = $(this).data('table-number');
    const areaName = $(this).data('area-name');
    const floor = $(this).data('floor');
    const qrUrl = $(this).data('qr-url');

    const logoUrl = "https://bizweb.dktcdn.net/100/461/735/themes/872641/assets/logo.png?1697183839847";

    async function getBase64(url) {
        const res = await fetch(url, { cache: 'no-cache' });
        const blob = await res.blob();
        return new Promise(resolve => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result);
            reader.readAsDataURL(blob);
        });
    }

    const qrBase64 = await getBase64(qrUrl);
    const logoBase64 = await getBase64(logoUrl);

    const content = `
    <html xmlns:o='urn:schemas-microsoft-com:office:office'
          xmlns:w='urn:schemas-microsoft-com:office:word'
          xmlns='http://www.w3.org/TR/REC-html40'>
    <head>
        <meta charset="utf-8">
        <title>QR Bàn ${tableNumber}</title>
    </head>
    <body style="margin:0; padding:0; background:#fefefe; font-family: Arial, sans-serif; text-align:center;">
                <div style="
            width: 280pt; 
            margin: 0 auto; 
            border: 2px solid #1e88e5; 
            border-radius: 15px;
            padding: 20pt; 
            background: #ffffff;
            text-align: center;
            ">
            
            <img src="${logoBase64}" alt="Logo" width="90" style="width:90pt; height:auto; margin-bottom: 10pt;" />
            
            <h2 style="color:#1e88e5; margin: 5pt 0 15pt; font-weight: bold; font-size: 16pt;">🏨 Nhà hàng Sapa</h2>
            
            <p style="font-size:12pt; color:#555; margin:5pt 0;">
                <span style="color:#e53935; font-weight:bold;">Bàn:</span> 
                <span>${tableNumber}</span>
            </p>
            <p style="font-size:12pt; color:#555; margin:5pt 0;">
                <span style="color:#e53935; font-weight:bold;">Khu vực:</span> 
                <span>${areaName}</span>
            </p>
            <p style="font-size:12pt; color:#555; margin:5pt 0;">
                <span style="color:#e53935; font-weight:bold;">Tầng:</span> 
                <span>${floor}</span>
            </p>
            
            <div align="center" style="margin: 25pt 0;">
                <!-- Viền xanh đã bị xóa theo yêu cầu -->
                <div style="
                    width: 142pt; 
                    padding: 0;
                    line-height: 0;
                ">
                    <img src="${qrBase64}" alt="QR Code" width="142" height="142"
                    style="width:142pt; height:142pt; display: block;" />
                </div>
            </div>
            
            <div style="
                font-size:12pt; 
                color: #1e88e5;
                margin-top:15pt; 
                font-weight:bold;
                text-align: center;
                ">
                📲 Hãy quét Mã QR để gọi món tại đây
            </div>
        </div>
    </body>
    </html>
    `;

    const blob = new Blob(['\ufeff', content], { type: 'application/msword' });
    saveAs(blob, `QR_Ban_${tableNumber}.doc`);
});