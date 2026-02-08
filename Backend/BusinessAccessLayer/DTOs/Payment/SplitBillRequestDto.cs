using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho split bill
/// </summary>
public class SplitBillRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Danh sách các phần thanh toán là bắt buộc")]
    [MinLength(2, ErrorMessage = "Phải chia thành ít nhất 2 phần")]
    public List<SplitBillPartDto> Parts { get; set; } = new List<SplitBillPartDto>();

    public string? Notes { get; set; }
}

/// <summary>
/// DTO cho một phần của split bill
/// </summary>
public class SplitBillPartDto
{
    [Required(ErrorMessage = "Phương thức thanh toán là bắt buộc")]
    public string PaymentMethod { get; set; } = null!;

    [Required(ErrorMessage = "Số tiền là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal Amount { get; set; }

    public decimal? AmountReceived { get; set; } // Cho Cash payment
    public string? Notes { get; set; }
}

