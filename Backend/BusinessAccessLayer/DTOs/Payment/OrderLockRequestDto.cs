using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Payment;

/// <summary>
/// DTO cho lock order
/// </summary>
public class OrderLockRequestDto
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    public string? SessionId { get; set; }
    public string? Reason { get; set; }
}

