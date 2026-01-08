namespace BusinessAccessLayer.Services.Interfaces;

/// <summary>
/// Interface for Receipt Service - PDF receipt generation
/// </summary>
public interface IReceiptService
{
    /// <summary>
    /// Generate PDF receipt for a paid order
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <returns>Relative URL path to the generated PDF (e.g., "/receipts/RMS000123.pdf")</returns>
    Task<string> GenerateReceiptPdfAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// Generate PDF receipt for a paid reservation (tổng hợp tất cả Orders)
    /// </summary>
    /// <param name="reservationId">Reservation ID</param>
    /// <returns>Relative URL path to the generated PDF (e.g., "/receipts/RES000123.pdf")</returns>
    Task<string> GenerateReceiptPdfByReservationAsync(int reservationId, CancellationToken ct = default);
}

